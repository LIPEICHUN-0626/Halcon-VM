param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw "Assertion failed: $Message" } }
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s23-' + [Guid]::NewGuid().ToString('N')); [IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true); $recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true); $loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true); $toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'; $all = [Reflection.BindingFlags]'Instance,NonPublic,Public'; $args = New-Object 'object[]' 1; $args[0] = $tempRoot.ToString()
$service = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($args); $logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$window = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
$flow = $mainType.GetField('flowTools', $private).GetValue($window); $catalog = $mainType.GetField('toolCatalog', $private).GetValue($window); $create = $mainType.GetMethod('CreateFlowTool', $private); $runCircle = $mainType.GetMethod('RunXldCircleFittingTool', $private); $dispose = $mainType.GetMethod('DisposeFlowTools', $private)
function New-Tool([string]$Kind, [string]$Name) { $create.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null)) }
function Get-Out($Tool, [string]$Port) { $value = $null; if (-not $Tool.TryGetOutputValue($Port, [ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }; $value }
function Invoke-Circle($Tool, $Xld) { try { $runCircle.Invoke($window, [object[]]@($Tool, $Xld)) } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException } }

$dispose.Invoke($window, @()) | Out-Null; $flow.Clear()
$circleTool = New-Tool XldCircleFitting S23_Circle_Fit; $flow.Add($circleTool)
Assert-True ($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'XldCircleFitting' } | Select-Object -First 1)) 'Circle-fitting tool missing from toolbox.'
foreach ($fieldName in @('DockXldCircleFittingPanel','DockXldCircleFitAlgorithmComboBox','UndoFlowEditButton','RedoFlowEditButton','FlowEditHistoryText')) { Assert-True ($null -ne $mainType.GetField($fieldName, $all).GetValue($window)) "$fieldName is missing." }

[HalconDotNet.HObject]$whole = $null; [HalconDotNet.HObject]$arc = $null; [HalconDotNet.HObject]$line = $null; [HalconDotNet.HObject]$empty = $null; [HalconDotNet.HObject]$short = $null
try {
    [HalconDotNet.HOperatorSet]::GenCircleContourXld([ref]$whole, 80, 90, 25, 0, 6.283185307179586, 'positive', 1)
    $wholeRecord = Invoke-Circle $circleTool $whole; $wholeSnapshot = Get-Out $circleTool XLD
    Assert-True ($wholeRecord.ResultCode -eq 'OK' -and [int][double](Get-Out $circleTool CircleCount) -eq 1) 'Whole circle did not return one fitted circle.'
    Assert-True ([Math]::Abs([double](Get-Out $circleTool CenterRow) - 80) -lt .01 -and [Math]::Abs([double](Get-Out $circleTool CenterColumn) - 90) -lt .01 -and [Math]::Abs([double](Get-Out $circleTool Radius) - 25) -lt .01) 'Whole circle geometry is wrong.'
    Assert-True ($wholeSnapshot.ObjectCount -eq 1 -and [string](Get-Out $circleTool PointOrder) -match 'positive') 'Managed fitted XLD or point order missing.'

    [HalconDotNet.HOperatorSet]::GenCircleContourXld([ref]$arc, 100, 150, 40, 0, 4.71238898038469, 'positive', 1)
    $arcRecord = Invoke-Circle $circleTool $arc
    Assert-True ($arcRecord.ResultCode -eq 'OK' -and [Math]::Abs([double](Get-Out $circleTool Radius) - 40) -lt .05) 'Open arc fitting is wrong.'
    foreach ($algorithm in @('Algebraic','AHuber','ATukey','Geometric','GeoHuber','GeoTukey')) { $circleTool.Parameters.XldCircleFitAlgorithm = $algorithm; Assert-True ((Invoke-Circle $circleTool $arc).ResultCode -eq 'OK') "$algorithm failed." }

    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$line, [HalconDotNet.HTuple]@([double]10,[double]10,[double]10,[double]10), [HalconDotNet.HTuple]@([double]5,[double]45,[double]85,[double]125))
    $rejected = $false; try { Invoke-Circle $circleTool $line | Out-Null } catch { $rejected = $true }; Assert-True $rejected 'Non-circular line contour was not rejected.'

    [HalconDotNet.HOperatorSet]::GenEmptyObj([ref]$empty); $emptyRecord = Invoke-Circle $circleTool $empty
    Assert-True ($emptyRecord.ResultCode -eq 'NG' -and [int][double](Get-Out $circleTool CircleCount) -eq 0) 'Empty XLD did not return managed NG.'
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$short, [HalconDotNet.HTuple]@([double]1,[double]2), [HalconDotNet.HTuple]@([double]1,[double]2))
    $rejected = $false; try { Invoke-Circle $circleTool $short | Out-Null } catch { $rejected = $true }; Assert-True $rejected 'Two-point XLD was not rejected.'

    foreach ($case in @(@('XldCircleFitAlgorithm','Bad'),@('XldCircleFitMaxNumPoints',2),@('XldCircleFitMaxClosureDistance',-1),@('XldCircleFitClippingEndPoints',-1),@('XldCircleFitIterations',-1),@('XldCircleFitClippingFactor',0))) {
        $badTool = New-Tool XldCircleFitting S23_Invalid; $badTool.Parameters.$($case[0]) = $case[1]; $bad = $false
        try { Invoke-Circle $badTool $whole | Out-Null } catch { $bad = $true }; Assert-True $bad "Invalid $($case[0]) was accepted."; $badTool.Dispose()
    }

    $circleTool.Parameters.XldCircleFitAlgorithm='GeoTukey';$circleTool.Parameters.XldCircleFitMaxNumPoints=321;$circleTool.Parameters.XldCircleFitMaxClosureDistance=2.75;$circleTool.Parameters.XldCircleFitClippingEndPoints=2;$circleTool.Parameters.XldCircleFitIterations=7;$circleTool.Parameters.XldCircleFitClippingFactor=1.8
    $captureFlow=$mainType.GetMethod('CaptureFlowRecipe',$private);$visionRecipeType=$assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe',$true);$recipe=[Activator]::CreateInstance($visionRecipeType);$recipe.ToolFlow=$captureFlow.Invoke($window,@());$recipePath=Join-Path $tempRoot 's23-circle-recipe.json';$service.SaveRecipe($recipePath,$recipe);$loaded=$service.LoadRecipe($recipePath);$loadedCircle=$loaded.ToolFlow|Where-Object{$_.ToolType -eq 'XldCircleFitting'}|Select-Object -First 1
    Assert-True ($loadedCircle.Parameters.XldCircleFitAlgorithm -eq 'GeoTukey' -and $loadedCircle.Parameters.XldCircleFitMaxNumPoints -eq 321 -and [Math]::Abs($loadedCircle.Parameters.XldCircleFitMaxClosureDistance-2.75) -lt .001 -and $loadedCircle.Parameters.XldCircleFitClippingEndPoints -eq 2 -and $loadedCircle.Parameters.XldCircleFitIterations -eq 7 -and [Math]::Abs($loadedCircle.Parameters.XldCircleFitClippingFactor-1.8) -lt .001) 'Circle fitting parameters did not survive recipe save/load.'

    # A line: one real layout edit produces a pure DTO history entry; undo/redo restores ToolIds and releases runtime XLD.
    $source = New-Tool ImageSource S23_Source; $flow.Insert(0, $source); $circleTool.SetInputBinding('XLD', $source.ToolId, 'XLD')
    $source.GraphX = 24; $source.GraphY = 48; $circleTool.GraphX = 240; $circleTool.GraphY = 144
    $resetHistory = $mainType.GetMethod('ResetFlowEditHistory', $private); $enableManual = $mainType.GetMethod('EnableManualFlowLayout', $private); $alignH = $mainType.GetMethod('AlignFlowToolsHorizontalButton_Click', $private); $undo = $mainType.GetMethod('UndoFlowEditButton_Click', $private); $redo = $mainType.GetMethod('RedoFlowEditButton_Click', $private)
    $enableManual.Invoke($window, @()) | Out-Null; $resetHistory.Invoke($window, @()) | Out-Null
    $list = $mainType.GetField('FlowToolList', $all).GetValue($window); $list.SelectedItems.Clear(); $list.SelectedItems.Add($source) | Out-Null; $list.SelectedItems.Add($circleTool) | Out-Null
    $beforeY = $circleTool.GraphY; $sourceId = $source.ToolId; $circleId = $circleTool.ToolId
    $alignH.Invoke($window, [object[]]@($null,$null)) | Out-Null
    $undoStack = $mainType.GetField('flowUndoHistory', $private).GetValue($window); $redoStack = $mainType.GetField('flowRedoHistory', $private).GetValue($window)
    Assert-True ($undoStack.Count -eq 1 -and $circleTool.GraphY -eq $source.GraphY) 'Layout edit was not captured by undo history.'
    $runtimeSnapshot = Get-Out $circleTool XLD
    $undo.Invoke($window, [object[]]@($null,$null)) | Out-Null
    $restoredCircle = $flow | Where-Object { $_.ToolId -eq $circleId } | Select-Object -First 1
    Assert-True ($null -ne $restoredCircle -and $restoredCircle.GraphY -eq $beforeY -and $redoStack.Count -eq 1) 'Undo did not restore coordinates and ToolIds.'
    Assert-True $runtimeSnapshot.IsDisposed 'Undo did not release the old managed XLD runtime snapshot.'
    $missingRuntime = $null; Assert-True (-not $restoredCircle.TryGetOutputValue('XLD', [ref]$missingRuntime)) 'Undo incorrectly restored a runtime XLD output.'
    $redo.Invoke($window, [object[]]@($null,$null)) | Out-Null
    $redoneCircle = $flow | Where-Object { $_.ToolId -eq $circleId } | Select-Object -First 1
    Assert-True ($redoneCircle.GraphY -eq ($flow | Where-Object { $_.ToolId -eq $sourceId } | Select-Object -First 1).GraphY -and $undoStack.Count -eq 1 -and $redoStack.Count -eq 0) 'Redo did not restore the edited layout.'

    $undo.Invoke($window, [object[]]@($null,$null)) | Out-Null
    $autoLayout = $mainType.GetMethod('AutoLayoutFlowButton_Click', $private); $autoLayout.Invoke($window, [object[]]@($null,$null)) | Out-Null
    Assert-True ($redoStack.Count -eq 0 -and $undoStack.Count -eq 1) 'A new edit did not clear the redo branch.'
    $applyRecipe = $mainType.GetMethod('ApplyRecipe', $private); $applyRecipe.Invoke($window, [object[]]@([Activator]::CreateInstance($visionRecipeType))) | Out-Null
    Assert-True ($undoStack.Count -eq 0 -and $redoStack.Count -eq 0) 'Opening/new recipe baseline did not clear edit history.'

    $historyEntryType = $mainType.GetNestedType('FlowEditHistoryEntry', [Reflection.BindingFlags]'NonPublic'); $snapshotType = $mainType.GetNestedType('FlowEditSnapshot', [Reflection.BindingFlags]'NonPublic')
    Assert-True (($snapshotType.GetFields($all).FieldType.FullName -join ',') -notmatch 'HalconDotNet') 'History snapshot stores a HALCON runtime field.'
    Assert-True (($snapshotType.GetProperties($all).PropertyType.FullName -join ',') -notmatch 'HalconDotNet') 'History stores a HALCON runtime object.'
    Assert-True ($mainType.GetField('FlowEditHistoryLimit', [Reflection.BindingFlags]'Static,NonPublic').GetRawConstantValue() -eq 50) 'History bound is not 50.'
}
finally {
    foreach ($obj in @($whole,$arc,$line,$empty,$short)) { if ($null -ne $obj) { $obj.Dispose() } }
}

$dispose.Invoke($window, @()) | Out-Null; $mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true); $window.Close(); if (Test-Path $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
Write-Output 'S23 flow undo/redo and HALCON XLD circle fitting smoke passed.'
