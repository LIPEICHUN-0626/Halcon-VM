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
function New-LineXld([double]$Row1, [double]$Column1, [double]$Row2, [double]$Column2) {
    $rows = New-Object 'System.Collections.Generic.List[double]'
    $columns = New-Object 'System.Collections.Generic.List[double]'
    foreach ($index in 0..20) {
        $t = $index / 20.0
        $rows.Add($Row1 + ($Row2 - $Row1) * $t)
        $columns.Add($Column1 + ($Column2 - $Column1) * $t)
    }
    [HalconDotNet.HObject]$line = $null
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$line, [HalconDotNet.HTuple]$rows.ToArray(), [HalconDotNet.HTuple]$columns.ToArray())
    $line
}
function New-NoisyLineXld([double]$Row1, [double]$Column1, [double]$Row2, [double]$Column2) {
    $rows = New-Object 'System.Collections.Generic.List[double]'
    $columns = New-Object 'System.Collections.Generic.List[double]'
    foreach ($index in 0..40) {
        $t = $index / 40.0
        $noise = if ($index -eq 20) { 4.0 } elseif ($index % 2 -eq 0) { 0.25 } else { -0.25 }
        $rows.Add($Row1 + ($Row2 - $Row1) * $t)
        $columns.Add($Column1 + ($Column2 - $Column1) * $t + $noise)
    }
    [HalconDotNet.HObject]$line = $null
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$line, [HalconDotNet.HTuple]$rows.ToArray(), [HalconDotNet.HTuple]$columns.ToArray())
    $line
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s26-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'debug-source.pgm'
Write-TestPgm $imagePath

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$runModeType = $assembly.GetType('HalconWinFormsDemo.Models.VmFlowRunRequestMode', $true)
$visionRecipeType = $assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$service = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$window = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$catalog = $mainType.GetField('toolCatalog', $private).GetValue($window)
$create = $mainType.GetMethod('CreateFlowTool', $private)
$runRelation = $mainType.GetMethod('RunXldLineRelationTool', $private)
$runLine = $mainType.GetMethod('RunXldLineFittingTool', $private)
$execute = $mainType.GetMethod('ExecuteFlowTool', $private)
$dispose = $mainType.GetMethod('DisposeFlowTools', $private)
$captureFlow = $mainType.GetMethod('CaptureFlowRecipe', $private)
$loadDraft = $mainType.GetMethod('LoadDockConfigurationDraft', $private)
$runToSelected = $mainType.GetMethod('DebugRunToSelectedButton_Click', $private)
$debugContinue = $mainType.GetMethod('DebugContinueButton_Click', $private)
$toggleMonitorBreakpoint = $mainType.GetMethod('ToggleDebugMonitorBreakpointButton_Click', $private)
$list = $mainType.GetField('FlowToolList', $all).GetValue($window)
$monitorCombo = $mainType.GetField('DebugMonitorToolComboBox', $all).GetValue($window)

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
function Invoke-Relation($Tool, $LineA, $LineB) {
    try { $runRelation.Invoke($window, [object[]]@($Tool, $LineA, $LineB)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

foreach ($fieldName in @(
    'FlowDebugMonitorBorder','DebugMonitorStateText','DebugMonitorCurrentText','DebugMonitorNextText',
    'DebugMonitorRangeText','DebugMonitorPauseReasonText','DebugMonitorToolComboBox','DebugMonitorBreakpointList',
    'DebugRunToSelectedButton','DockXldLineRelationPanel','DockLineRelationFitAlgorithmComboBox',
    'DockLineRelationParallelToleranceTextBox','DockLineRelationCoincidentToleranceTextBox'
)) {
    Assert-True ($null -ne $mainType.GetField($fieldName, $all).GetValue($window)) "$fieldName is missing."
}
Assert-True ($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'XldLineRelationMeasurement' } | Select-Object -First 1)) 'Line-relation tool missing from toolbox.'

$metadata = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata', $true)
$getInputs = $metadata.GetMethod('GetInputPorts', [Reflection.BindingFlags]'Static,Public')
$getOutputs = $metadata.GetMethod('GetOutputPorts', [Reflection.BindingFlags]'Static,Public')
$relationKind = [Enum]::Parse($toolKindType, 'XldLineRelationMeasurement')
$relationInputs = $getInputs.Invoke($null, [object[]]@($relationKind))
$relationOutputs = $getOutputs.Invoke($null, [object[]]@($relationKind))
Assert-True (($relationInputs | Where-Object PortName -eq 'LineA').Count -eq 1 -and ($relationInputs | Where-Object PortName -eq 'LineB').Count -eq 1) 'Typed LineA/LineB XLD inputs are missing.'
foreach ($port in @('IntersectionRow','IntersectionColumn','IntersectionValid','DirectionA','DirectionB','SignedAngle','AcuteAngle','ParallelDistance','IsParallel','IsCoincident','Relation','ResidualA','ResidualB','XLD')) {
    Assert-True (($relationOutputs | Where-Object PortName -eq $port).Count -eq 1) "Line relation output $port is missing."
}

$relation = New-Tool XldLineRelationMeasurement 'S26 Line Relation'
$loadDraft.Invoke($window, [object[]]@($relation)) | Out-Null
$dock = $mainType.GetField('DockXldLineRelationPanel', $all).GetValue($window)
Assert-True ($dock.Visibility.ToString() -eq 'Visible') 'Line-relation parameters are not visible in the persistent configuration dock.'

[HalconDotNet.HObject]$horizontal = $null
[HalconDotNet.HObject]$vertical = $null
[HalconDotNet.HObject]$parallel = $null
[HalconDotNet.HObject]$nearCoincident = $null
[HalconDotNet.HObject]$short = $null
[HalconDotNet.HObject]$empty = $null
[HalconDotNet.HObject]$noisyVertical = $null
try {
    $horizontal = New-LineXld 20 0 20 100
    $vertical = New-LineXld 0 30 80 30
    $record = Invoke-Relation $relation $horizontal $vertical
    Assert-True ($record.ResultCode -eq 'OK') 'Intersecting line relation did not return OK.'
    Assert-True ([bool](Get-Out $relation IntersectionValid)) 'Intersection was not marked valid.'
    Assert-True ([Math]::Abs([double](Get-Out $relation IntersectionRow) - 20) -lt .05 -and [Math]::Abs([double](Get-Out $relation IntersectionColumn) - 30) -lt .05) 'HALCON line intersection is wrong.'
    Assert-True ([Math]::Abs([double](Get-Out $relation AcuteAngle) - 90) -lt .05) 'HALCON acute angle is wrong.'
    Assert-True (-not [bool](Get-Out $relation IsParallel)) 'Perpendicular relation was incorrectly classified as parallel.'
    Assert-True ((Get-Out $relation XLD).ObjectCount -eq 3) 'Intersecting overlay must contain two lines and a cross.'

    $noisyVertical = New-NoisyLineXld 0 30 80 30
    Invoke-Relation $relation $horizontal $noisyVertical | Out-Null
    Assert-True ([Math]::Abs([double](Get-Out $relation AcuteAngle) - 90) -lt .5) 'Robust noisy-line angle is outside tolerance.'

    $oldSnapshot = Get-Out $relation XLD
    $parallel = New-LineXld 35 0 35 100
    Invoke-Relation $relation $horizontal $parallel | Out-Null
    Assert-True $oldSnapshot.IsDisposed 'Rerun did not release the previous managed line-relation XLD.'
    Assert-True ([bool](Get-Out $relation IsParallel)) 'Parallel lines were not classified.'
    Assert-True (-not [bool](Get-Out $relation IntersectionValid)) 'Parallel lines produced a false intersection.'
    Assert-True ([Math]::Abs([double](Get-Out $relation ParallelDistance) - 15) -lt .05) 'HALCON parallel distance is wrong.'
    Assert-True ((Get-Out $relation XLD).ObjectCount -eq 2) 'Parallel overlay must contain two fitted lines.'

    $nearCoincident = New-LineXld 20.5 0 20.5 100
    $relation.Parameters.LineRelationCoincidentDistance = 1
    Invoke-Relation $relation $horizontal $nearCoincident | Out-Null
    Assert-True ([bool](Get-Out $relation IsCoincident)) 'Near-coincident lines were not classified within tolerance.'

    foreach ($case in @(
        @('LineRelationParallelToleranceDegrees',0),
        @('LineRelationParallelToleranceDegrees',16),
        @('LineRelationPerpendicularToleranceDegrees',0),
        @('LineRelationCoincidentDistance',-1),
        @('XldLineFitAlgorithm','Bad')
    )) {
        $badTool = New-Tool XldLineRelationMeasurement 'S26 Invalid'
        $badTool.Parameters.$($case[0]) = $case[1]
        $rejected = $false
        try { Invoke-Relation $badTool $horizontal $vertical | Out-Null } catch { $rejected = $true }
        Assert-True $rejected "Invalid $($case[0]) was accepted."
        $badTool.Dispose()
    }

    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$short, [HalconDotNet.HTuple]@([double]1), [HalconDotNet.HTuple]@([double]2))
    $rejected = $false
    try { Invoke-Relation $relation $short $vertical | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'One-point XLD was not rejected.'

    [HalconDotNet.HOperatorSet]::GenEmptyObj([ref]$empty)
    $rejected = $false
    try { Invoke-Relation $relation $empty $vertical | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Empty XLD was not rejected.'
}
finally {
    foreach ($obj in @($horizontal,$vertical,$parallel,$nearCoincident,$short,$empty,$noisyVertical)) { if ($null -ne $obj) { $obj.Dispose() } }
}

$relation.Parameters.XldLineFitAlgorithm = 'Huber'
$relation.Parameters.LineRelationParallelToleranceDegrees = 1.25
$relation.Parameters.LineRelationCoincidentDistance = 2.5
$relation.Parameters.LineRelationPerpendicularToleranceDegrees = 1.75
$flow.Add($relation)
$recipe = [Activator]::CreateInstance($visionRecipeType)
$recipe.ToolFlow = $captureFlow.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's26-line-relation-recipe.json'
$service.SaveRecipe($recipePath, $recipe)
$loaded = $service.LoadRecipe($recipePath)
$loadedRelation = $loaded.ToolFlow | Where-Object ToolType -eq 'XldLineRelationMeasurement' | Select-Object -First 1
Assert-True ($loadedRelation.Parameters.XldLineFitAlgorithm -eq 'Huber' -and [Math]::Abs($loadedRelation.Parameters.LineRelationParallelToleranceDegrees - 1.25) -lt .001 -and [Math]::Abs($loadedRelation.Parameters.LineRelationCoincidentDistance - 2.5) -lt .001 -and [Math]::Abs($loadedRelation.Parameters.LineRelationPerpendicularToleranceDegrees - 1.75) -lt .001) 'Line-relation parameters did not survive recipe save/load.'

$dispose.Invoke($window, @()) | Out-Null
$flow.Clear()
$sourceA = New-Tool XldLineFitting 'Relation Source A'
$sourceB = New-Tool XldLineFitting 'Relation Source B'
$flowRelation = New-Tool XldLineRelationMeasurement 'Flow Relation'
foreach ($tool in @($sourceA,$sourceB,$flowRelation)) { $flow.Add($tool) }
$flowRelation.SetInputBinding('LineA',$sourceA.ToolId,'XLD')
$flowRelation.SetInputBinding('LineB',$sourceB.ToolId,'XLD')
[HalconDotNet.HObject]$flowLineA = $null
[HalconDotNet.HObject]$flowLineB = $null
try {
    $flowLineA = New-LineXld 20 0 20 100
    $flowLineB = New-LineXld 0 30 80 30
    $runLine.Invoke($window, [object[]]@($sourceA,$flowLineA)) | Out-Null
    $sourceA.ResultCode = 'OK'
    $runLine.Invoke($window, [object[]]@($sourceB,$flowLineB)) | Out-Null
    $sourceB.ResultCode = 'OK'
    $flowRecord = $execute.Invoke($window, [object[]]@($flowRelation,'S26 flow relation'))
    Assert-True ($flowRecord.ResultCode -eq 'OK' -and [Math]::Abs([double](Get-Out $flowRelation AcuteAngle) - 90) -lt .05) 'Typed dual-XLD flow execution failed.'

    $flowRelation.SetInputBinding('LineB',$sourceA.ToolId,'XLD')
    $rejected = $false
    try { $execute.Invoke($window, [object[]]@($flowRelation,'S26 same source')) | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Same-source LineA/LineB binding was not rejected.'
    $flowRelation.SetInputBinding('LineB',$sourceB.ToolId,'XLD')

    $flow.Move(2,0)
    $rejected = $false
    try { $execute.Invoke($window, [object[]]@($flowRelation,'S26 invalid order')) | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Downstream-before-upstream XLD order was not rejected.'
    $flow.Move(0,2)

    $sourceA.ClearRuntimeOutputs()
    $sourceA.ResultCode = '--'
    $rejected = $false
    try { $execute.Invoke($window, [object[]]@($flowRelation,'S26 not run')) | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Unrun upstream XLD was not rejected.'

    $runLine.Invoke($window, [object[]]@($sourceA,$flowLineA)) | Out-Null
    $sourceA.ResultCode = 'OK'
    $disposedSnapshot = Get-Out $sourceA XLD
    $disposedSnapshot.Dispose()
    $rejected = $false
    try { $execute.Invoke($window, [object[]]@($flowRelation,'S26 released XLD')) | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Released upstream XLD was not rejected.'

    $imageSource = New-Tool ImageSource 'Wrong Type Source'
    $imageSource.Parameters.LocalImagePath = $imagePath
    $flow.Insert(0,$imageSource)
    $flowRelation.SetInputBinding('LineA',$imageSource.ToolId,'Image')
    $rejected = $false
    try { $execute.Invoke($window, [object[]]@($flowRelation,'S26 type mismatch')) | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Image-to-XLD type mismatch was not rejected.'
}
finally {
    foreach ($obj in @($flowLineA,$flowLineB)) { if ($null -ne $obj) { $obj.Dispose() } }
}

$dispose.Invoke($window, @()) | Out-Null
$flow.Clear()
$first = New-Tool ImageSource 'Monitor Source 1'
$second = New-Tool ImageSource 'Monitor Source 2'
$third = New-Tool ImageSource 'Monitor Source 3'
foreach ($tool in @($first,$second,$third)) {
    $tool.Parameters.LocalImagePath = $imagePath
    $tool.Parameters.LocalImageSerialNumber = 'S26-MONITOR'
    $flow.Add($tool)
}
$monitorCombo.SelectedItem = $second
$toggleMonitorBreakpoint.Invoke($window, [object[]]@($null, $null)) | Out-Null
Assert-True $second.IsBreakpoint 'Monitor breakpoint toggle did not update the real module.'
Assert-True ($mainType.GetField('debugBreakpointTools', $private).GetValue($window).Count -eq 1) 'Breakpoint list does not reflect the real breakpoint collection.'
$toggleMonitorBreakpoint.Invoke($window, [object[]]@($null, $null)) | Out-Null
Assert-True (-not $second.IsBreakpoint) 'Monitor breakpoint toggle did not clear the real module breakpoint.'

$monitorCombo.SelectedItem = $second
$runToSelected.Invoke($window, [object[]]@($null, $null)) | Out-Null
Wait-Condition {
    $mainType.GetField('isPauseRequested', $private).GetValue($window) -and $third.IsExecutionPointer
} 'Run-to-selected did not execute through the selected module and pause before the next module.'
Assert-True (Has-Out $first 'Image') 'Run-to-selected did not execute the preceding module.'
Assert-True (Has-Out $second 'Image') 'Run-to-selected did not execute the selected module.'
Assert-True (-not (Has-Out $third 'Image')) 'Run-to-selected executed past the selected module.'
Assert-True (($mainType.GetField('debugLastExecutedToolId', $private).GetValue($window)) -eq $second.ToolId) 'Debug monitor current module is not backed by the real executor.'
Assert-True (-not [string]::IsNullOrWhiteSpace([string]$mainType.GetField('debugPauseReason', $private).GetValue($window))) 'Debug monitor pause reason is empty.'
$debugContinue.Invoke($window, [object[]]@($null, $null)) | Out-Null
Wait-Condition { -not $mainType.GetField('isFlowExecutionActive', $private).GetValue($window) } 'Debug continue did not finish the flow.'
Assert-True (Has-Out $third 'Image') 'Debug continue did not execute the remaining module.'

$halconMethods = [HalconDotNet.HOperatorSet].GetMethods()
foreach ($method in @('IntersectionLines','AngleLl','DistancePl','GenCrossContourXld')) {
    Assert-True (($halconMethods | Where-Object Name -eq $method).Count -eq 1) "HALCON 20.11 $method .NET signature was not verified."
}

$dispose.Invoke($window, @()) | Out-Null
$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
if (Test-Path $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
Write-Output 'S26 VM debug monitor/run-to-selected and HALCON dual-XLD line relation smoke passed.'
