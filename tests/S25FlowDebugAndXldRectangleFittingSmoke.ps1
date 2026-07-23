param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw "Assertion failed: $Message" } }
function Pump-Ui([int]$Milliseconds = 25) {
    $frame = New-Object Windows.Threading.DispatcherFrame
    $timer = New-Object Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds)
    $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false })
    $timer.Start()
    [Windows.Threading.Dispatcher]::PushFrame($frame)
}
function Wait-Condition([scriptblock]$Condition, [string]$Message, [int]$TimeoutMilliseconds = 5000) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while ($watch.ElapsedMilliseconds -lt $TimeoutMilliseconds) {
        Pump-Ui 25
        if (& $Condition) { return }
    }
    throw "Timeout: $Message"
}
function Write-TestPgm([string]$Path) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n8 8`n255`n")
    $pixels = New-Object byte[] 64
    for ($i = 0; $i -lt $pixels.Length; $i++) { $pixels[$i] = [byte](20 + ($i % 8) * 20) }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}
function New-DenseRectangleXld([double]$Row, [double]$Column, [double]$Phi, [double]$Length1, [double]$Length2) {
    $rows = New-Object 'System.Collections.Generic.List[double]'
    $columns = New-Object 'System.Collections.Generic.List[double]'
    $sin = [Math]::Sin($Phi)
    $cos = [Math]::Cos($Phi)
    foreach ($side in 0..3) {
        foreach ($sample in 0..20) {
            $t = $sample / 20.0
            if ($side -eq 0) { $u = -$Length1 + 2 * $Length1 * $t; $v = -$Length2 }
            elseif ($side -eq 1) { $u = $Length1; $v = -$Length2 + 2 * $Length2 * $t }
            elseif ($side -eq 2) { $u = $Length1 - 2 * $Length1 * $t; $v = $Length2 }
            else { $u = -$Length1; $v = $Length2 - 2 * $Length2 * $t }
            $rows.Add($Row - $u * $sin + $v * $cos)
            $columns.Add($Column + $u * $cos + $v * $sin)
        }
    }
    $result = $null
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$result, [HalconDotNet.HTuple]$rows.ToArray(), [HalconDotNet.HTuple]$columns.ToArray())
    $result
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s25-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'debug-source.pgm'
Write-TestPgm $imagePath

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$runModeType = $assembly.GetType('HalconWinFormsDemo.Models.VmFlowRunRequestMode', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$args = New-Object 'object[]' 1
$args[0] = $tempRoot.ToString()
$service = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($args)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$window = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$catalog = $mainType.GetField('toolCatalog', $private).GetValue($window)
$create = $mainType.GetMethod('CreateFlowTool', $private)
$runRectangle = $mainType.GetMethod('RunXldRectangleFittingTool', $private)
$dispose = $mainType.GetMethod('DisposeFlowTools', $private)
$captureFlow = $mainType.GetMethod('CaptureFlowRecipe', $private)
$applyFlow = $mainType.GetMethod('ApplyFlowFromRecipe', $private)
$runRange = $mainType.GetMethod('RunInspectionRangeAsync', $private)
$toggleBreakpoint = $mainType.GetMethod('ToggleBreakpointButton_Click', $private)
$clearBreakpoints = $mainType.GetMethod('ClearBreakpointsButton_Click', $private)
$debugStep = $mainType.GetMethod('DebugStepButton_Click', $private)
$debugContinue = $mainType.GetMethod('DebugContinueButton_Click', $private)
$undo = $mainType.GetMethod('UndoFlowEditButton_Click', $private)
$redo = $mainType.GetMethod('RedoFlowEditButton_Click', $private)
$list = $mainType.GetField('FlowToolList', $all).GetValue($window)

function New-Tool([string]$Kind, [string]$Name, [bool]$Enabled = $true) {
    $create.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $Enabled, $null))
}
function Get-Out($Tool, [string]$Port) {
    $value = $null
    if (-not $Tool.TryGetOutputValue($Port, [ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }
    $value
}
function Has-Out($Tool, [string]$Port) {
    $value = $null
    $Tool.TryGetOutputValue($Port, [ref]$value)
}
function Invoke-Rectangle($Tool, $Xld) {
    try { $runRectangle.Invoke($window, [object[]]@($Tool, $Xld)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

$dispose.Invoke($window, @()) | Out-Null
$flow.Clear()
$first = New-Tool ImageSource 'Debug Source 1'
$second = New-Tool ImageSource 'Debug Source 2'
$third = New-Tool ImageSource 'Debug Source 3'
$disabled = New-Tool ImageSource 'Disabled Breakpoint' $false
foreach ($tool in @($first, $second, $third, $disabled)) {
    $tool.Parameters.LocalImagePath = $imagePath
    $tool.Parameters.LocalImageSerialNumber = 'S25-DEBUG'
    $flow.Add($tool)
}
$disabled.IsBreakpoint = $true
$list.SelectedItems.Clear()
$list.SelectedItem = $second
$toggleBreakpoint.Invoke($window, [object[]]@($null, $null)) | Out-Null
Assert-True $second.IsBreakpoint 'F9-equivalent breakpoint action did not mark the selected module.'
Assert-True ($mainType.GetField('flowUndoHistory', $private).GetValue($window).Count -gt 0) 'Breakpoint edit was not recorded in undo history.'
$undo.Invoke($window, [object[]]@($null, $null)) | Out-Null
$second = $flow | Where-Object InstanceName -eq 'Debug Source 2' | Select-Object -First 1
Assert-True (-not $second.IsBreakpoint) 'Undo did not remove the breakpoint.'
$redo.Invoke($window, [object[]]@($null, $null)) | Out-Null
$first = $flow | Where-Object InstanceName -eq 'Debug Source 1' | Select-Object -First 1
$second = $flow | Where-Object InstanceName -eq 'Debug Source 2' | Select-Object -First 1
$third = $flow | Where-Object InstanceName -eq 'Debug Source 3' | Select-Object -First 1
$disabled = $flow | Where-Object InstanceName -eq 'Disabled Breakpoint' | Select-Object -First 1
Assert-True $second.IsBreakpoint 'Redo did not restore the breakpoint.'

$visionRecipeType = $assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe', $true)
$recipe = [Activator]::CreateInstance($visionRecipeType)
$recipe.ToolFlow = $captureFlow.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's25-debug-recipe.json'
$service.SaveRecipe($recipePath, $recipe)
$loaded = $service.LoadRecipe($recipePath)
Assert-True (($loaded.ToolFlow | Where-Object InstanceName -eq 'Debug Source 2').IsBreakpoint) 'Breakpoint did not survive recipe save/load.'

$fullMode = [Enum]::Parse($runModeType, 'Full')
$task = $runRange.Invoke($window, [object[]]@($fullMode, 'S25 debug smoke'))
Wait-Condition {
    $mainType.GetField('isPauseRequested', $private).GetValue($window) -and $second.IsExecutionPointer
} 'Flow did not pause before the breakpoint module.'
Assert-True (Has-Out $first 'Image') 'The module before the breakpoint did not execute.'
Assert-True (-not (Has-Out $second 'Image')) 'The breakpoint module executed before the user continued.'
Assert-True ($mainType.GetField('debugPausedToolId', $private).GetValue($window) -eq $second.ToolId) 'Debug pointer did not identify the next module.'

$debugStep.Invoke($window, [object[]]@($null, $null)) | Out-Null
Wait-Condition {
    (Has-Out $second 'Image') -and $mainType.GetField('isPauseRequested', $private).GetValue($window) -and $third.IsExecutionPointer
} 'Single-step did not execute exactly one module and pause before the next.'
Assert-True (-not (Has-Out $third 'Image')) 'Single-step executed more than one module.'

$debugContinue.Invoke($window, [object[]]@($null, $null)) | Out-Null
Wait-Condition { $task.IsCompleted } 'Continue did not complete the deterministic flow.'
if ($task.IsFaulted) { throw $task.Exception.GetBaseException() }
Assert-True (Has-Out $third 'Image') 'Continue did not execute the remaining enabled module.'
Assert-True (-not (Has-Out $disabled 'Image')) 'Disabled breakpoint module was not skipped.'
Assert-True (-not $mainType.GetField('isPauseRequested', $private).GetValue($window)) 'Debug pause state was not cleared after completion.'
Assert-True (($flow | Where-Object IsExecutionPointer).Count -eq 0) 'Execution pointer was not cleared after completion.'

$list.SelectedItems.Clear()
$list.SelectedItem = $second
$clearBreakpoints.Invoke($window, [object[]]@($null, $null)) | Out-Null
Assert-True (($flow | Where-Object IsBreakpoint).Count -eq 0) 'Clear-all did not remove every breakpoint.'

$rectangleTool = New-Tool XldRectangleFitting 'S25 Rectangle Fit'
Assert-True ($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'XldRectangleFitting' } | Select-Object -First 1)) 'Rectangle-fitting tool missing from toolbox.'
foreach ($fieldName in @('DockXldRectangleFittingPanel','DockXldRectangleFitAlgorithmComboBox','DebugStepButton','DebugContinueButton','ToggleBreakpointButton','FlowDebugStateText')) {
    Assert-True ($null -ne $mainType.GetField($fieldName, $all).GetValue($window)) "$fieldName is missing."
}
$metadata = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata', $true)
$getOutputs = $metadata.GetMethod('GetOutputPorts', [Reflection.BindingFlags]'Static,Public')
$rectanglePorts = $getOutputs.Invoke($null, [object[]]@([Enum]::Parse($toolKindType, 'XldRectangleFitting')))
foreach ($port in @('Length1','Length2','Corner1Row','Corner4Column','OrientationState','MaxResidual')) {
    Assert-True (($rectanglePorts | Where-Object PortName -eq $port).Count -eq 1) "Rectangle output $port is missing."
}

[HalconDotNet.HObject]$rotated = $null
[HalconDotNet.HObject]$square = $null
[HalconDotNet.HObject]$empty = $null
[HalconDotNet.HObject]$short = $null
[HalconDotNet.HObject]$line = $null
try {
    $rotated = New-DenseRectangleXld 120 180 0.55 60 25
    foreach ($algorithm in @('Regression','Huber','Tukey')) {
        $rectangleTool.Parameters.XldRectangleFitAlgorithm = $algorithm
        $record = Invoke-Rectangle $rectangleTool $rotated
        Assert-True ($record.ResultCode -eq 'OK') "$algorithm rotated rectangle fitting failed."
        Assert-True ([Math]::Abs([double](Get-Out $rectangleTool CenterRow) - 120) -lt .1 -and [Math]::Abs([double](Get-Out $rectangleTool CenterColumn) - 180) -lt .1) 'Rectangle center is wrong.'
        Assert-True ([Math]::Abs([double](Get-Out $rectangleTool Length1) - 60) -lt .15 -and [Math]::Abs([double](Get-Out $rectangleTool Length2) - 25) -lt .15) 'Rectangle half lengths are wrong.'
        Assert-True ([Math]::Abs([Math]::Sin([double](Get-Out $rectangleTool Phi) - 0.55)) -lt .01) 'Rectangle direction is wrong.'
        Assert-True ((Get-Out $rectangleTool XLD).ObjectCount -eq 1) 'Managed fitted rectangle XLD is missing.'
    }

    $oldSnapshot = Get-Out $rectangleTool XLD
    Invoke-Rectangle $rectangleTool $rotated | Out-Null
    Assert-True $oldSnapshot.IsDisposed 'Rerun did not release the previous fitted rectangle XLD snapshot.'

    $square = New-DenseRectangleXld 70 90 -0.35 30 30
    $squareRecord = Invoke-Rectangle $rectangleTool $square
    $squareOrientation = [string](Get-Out $rectangleTool OrientationState)
    Assert-True ($squareRecord.ResultCode -eq 'OK' -and $squareOrientation.Contains('90')) "Near-square direction equivalence was not reported: $squareOrientation; lengths=$(Get-Out $rectangleTool Length1)/$(Get-Out $rectangleTool Length2)."

    [HalconDotNet.HOperatorSet]::GenEmptyObj([ref]$empty)
    $emptyRecord = Invoke-Rectangle $rectangleTool $empty
    Assert-True ($emptyRecord.ResultCode -eq 'NG' -and [int][double](Get-Out $rectangleTool RectangleCount) -eq 0) 'Empty XLD did not return managed NG.'

    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$short, [HalconDotNet.HTuple]@([double]1,[double]2,[double]3,[double]4,[double]5,[double]6,[double]7), [HalconDotNet.HTuple]@([double]1,[double]2,[double]3,[double]4,[double]5,[double]6,[double]7))
    $rejected = $false
    try { Invoke-Rectangle $rectangleTool $short | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Seven-point XLD was not rejected.'

    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$line, [HalconDotNet.HTuple]@([double]10,[double]10,[double]10,[double]10,[double]10,[double]10,[double]10,[double]10), [HalconDotNet.HTuple]@([double]1,[double]10,[double]20,[double]30,[double]40,[double]50,[double]60,[double]70))
    $rejected = $false
    try { Invoke-Rectangle $rectangleTool $line | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Degenerate line XLD was not rejected.'

    foreach ($case in @(
        @('XldRectangleFitAlgorithm','Bad'),
        @('XldRectangleFitMaxNumPoints',7),
        @('XldRectangleFitMaxClosureDistance',-1),
        @('XldRectangleFitClippingEndPoints',-1),
        @('XldRectangleFitIterations',-1),
        @('XldRectangleFitClippingFactor',0)
    )) {
        $badTool = New-Tool XldRectangleFitting 'S25 Invalid'
        $badTool.Parameters.$($case[0]) = $case[1]
        $bad = $false
        try { Invoke-Rectangle $badTool $rotated | Out-Null } catch { $bad = $true }
        Assert-True $bad "Invalid $($case[0]) was accepted."
        $badTool.Dispose()
    }

    $rectangleTool.Parameters.XldRectangleFitAlgorithm = 'Huber'
    $rectangleTool.Parameters.XldRectangleFitMaxNumPoints = 321
    $rectangleTool.Parameters.XldRectangleFitMaxClosureDistance = 2.75
    $rectangleTool.Parameters.XldRectangleFitClippingEndPoints = 2
    $rectangleTool.Parameters.XldRectangleFitIterations = 7
    $rectangleTool.Parameters.XldRectangleFitClippingFactor = 1.8
    $flow.Add($rectangleTool)
    $recipe2 = [Activator]::CreateInstance($visionRecipeType)
    $recipe2.ToolFlow = $captureFlow.Invoke($window, @())
    $recipePath2 = Join-Path $tempRoot 's25-rectangle-recipe.json'
    $service.SaveRecipe($recipePath2, $recipe2)
    $loaded2 = $service.LoadRecipe($recipePath2)
    $loadedRectangle = $loaded2.ToolFlow | Where-Object ToolType -eq 'XldRectangleFitting' | Select-Object -First 1
    Assert-True ($loadedRectangle.Parameters.XldRectangleFitAlgorithm -eq 'Huber' -and $loadedRectangle.Parameters.XldRectangleFitMaxNumPoints -eq 321 -and [Math]::Abs($loadedRectangle.Parameters.XldRectangleFitMaxClosureDistance - 2.75) -lt .001 -and $loadedRectangle.Parameters.XldRectangleFitClippingEndPoints -eq 2 -and $loadedRectangle.Parameters.XldRectangleFitIterations -eq 7 -and [Math]::Abs($loadedRectangle.Parameters.XldRectangleFitClippingFactor - 1.8) -lt .001) 'Rectangle parameters did not survive recipe save/load.'

    $fitMethods = [HalconDotNet.HOperatorSet].GetMethods() | Where-Object Name -eq 'FitRectangle2ContourXld'
    Assert-True ($fitMethods.Count -eq 1 -and $fitMethods[0].GetParameters().Count -eq 13) 'HALCON 20.11 FitRectangle2ContourXld .NET signature was not verified.'
}
finally {
    foreach ($obj in @($rotated,$square,$empty,$short,$line)) { if ($null -ne $obj) { $obj.Dispose() } }
}

$dispose.Invoke($window, @()) | Out-Null
$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
if (Test-Path $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
Write-Output 'S25 VM flow breakpoint/single-step and HALCON XLD rotated rectangle fitting smoke passed.'
