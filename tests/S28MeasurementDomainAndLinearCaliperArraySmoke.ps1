param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Windows.Forms
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}
function Pump-Ui([int]$Milliseconds) {
    $frame = New-Object Windows.Threading.DispatcherFrame
    $timer = New-Object Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds)
    $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false })
    $timer.Start()
    [Windows.Threading.Dispatcher]::PushFrame($frame)
}
function Write-ArrayPgm([string]$Path, [int]$Width, [int]$Height, [string]$Mode) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) {
            $value = 20
            if ($Mode -eq 'HorizontalFull' -and (($row -ge 70 -and $row -le 86) -or ($row -ge 154 -and $row -le 170))) { $value = 230 }
            if ($Mode -eq 'HorizontalPartial' -and $column -lt 132 -and (($row -ge 70 -and $row -le 86) -or ($row -ge 154 -and $row -le 170))) { $value = 230 }
            if ($Mode -eq 'Rotated') {
                $normal = $row - $column
                if ([Math]::Abs($normal - 50) -le 8 -or [Math]::Abs($normal + 50) -le 8) { $value = 230 }
            }
            if ($Mode -eq 'LowContrast' -and (($row -ge 70 -and $row -le 86) -or ($row -ge 154 -and $row -le 170))) { $value = 25 }
            if ($Mode -eq 'Uniform') { $value = 100 }
            $pixels[$row * $Width + $column] = [byte]$value
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s28-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$horizontalPath = Join-Path $tempRoot 'horizontal-full.pgm'
$partialPath = Join-Path $tempRoot 'horizontal-partial.pgm'
$rotatedPath = Join-Path $tempRoot 'rotated.pgm'
$uniformPath = Join-Path $tempRoot 'uniform.pgm'
$lowContrastPath = Join-Path $tempRoot 'low-contrast.pgm'
Write-ArrayPgm $horizontalPath 320 260 HorizontalFull
Write-ArrayPgm $partialPath 320 260 HorizontalPartial
Write-ArrayPgm $rotatedPath 320 320 Rotated
Write-ArrayPgm $uniformPath 320 260 Uniform
Write-ArrayPgm $lowContrastPath 320 260 LowContrast

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$visionRecipeType = $assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$staticPublic = [Reflection.BindingFlags]'Static,Public'
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$service = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$window = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$catalog = $mainType.GetField('toolCatalog', $private).GetValue($window)
$create = $mainType.GetMethod('CreateFlowTool', $private)
$execute = $mainType.GetMethod('ExecuteFlowTool', $private)
$runArray = $mainType.GetMethod('RunLinearCaliperArrayTool', $private)
$loadImage = $mainType.GetMethod('LoadImage', $private)
$loadDraft = $mainType.GetMethod('LoadDockConfigurationDraft', $private)
$applyDock = $mainType.GetMethod('ApplyDockConfigurationDraft', $private)
$tryReadDock = $mainType.GetMethod('TryReadDockConfigurationDraft', $private)
$applyDockInputs = $mainType.GetMethod('ApplyDockInputDraft', $private)
$editDomain = $mainType.GetMethod('EditMeasurementDomainButton_Click', $private)
$mouseDown = $mainType.GetMethod('ImageWindow_MouseDown', $private)
$mouseMove = $mainType.GetMethod('ImageWindow_MouseMove', $private)
$mouseUp = $mainType.GetMethod('ImageWindow_MouseUp', $private)
$cancelDomain = $mainType.GetMethod('MeasurementDomainCancelButton_Click', $private)
$applyDomain = $mainType.GetMethod('MeasurementDomainApplyButton_Click', $private)
$undo = $mainType.GetMethod('UndoFlowEditButton_Click', $private)
$redo = $mainType.GetMethod('RedoFlowEditButton_Click', $private)
$captureFlow = $mainType.GetMethod('CaptureFlowRecipe', $private)
$applyRecipe = $mainType.GetMethod('ApplyRecipe', $private)
$disposeFlow = $mainType.GetMethod('DisposeFlowTools', $private)

function New-Tool([string]$Kind, [string]$Name) {
    $create.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}
function Get-Out($Tool, [string]$Port) {
    $value = $null
    if (-not $Tool.TryGetOutputValue($Port, [ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }
    $value
}
function Invoke-Execute($Tool, [string]$Source) {
    try { $execute.Invoke($window, [object[]]@($Tool, $Source)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Invoke-Array($Tool, $Image) {
    try { $runArray.Invoke($window, [object[]]@($Tool, $Image)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Set-HorizontalArray($Tool, [int]$Count = 5) {
    $p = $Tool.Parameters
    $p.LinearCaliperStartRow = 120
    $p.LinearCaliperStartColumn = 40
    $p.LinearCaliperEndRow = 120
    $p.LinearCaliperEndColumn = 260
    $p.LinearCaliperCount = $Count
    $p.LinearCaliperLength1 = 75
    $p.LinearCaliperLength2 = 6
    $p.LinearCaliperSigma = 1
    $p.LinearCaliperThreshold = 20
    $p.LinearCaliperTransition = 'Positive'
    $p.LinearCaliperSelect = 'All'
}

foreach ($fieldName in @(
    'EditMeasurementDomainButton','MeasurementDomainEditorBar','MeasurementDomainEditorToolText','MeasurementDomainEditorHintText',
    'DockLinearCaliperArrayPanel','DockLinearCaliperStartRowTextBox','DockLinearCaliperStartColumnTextBox',
    'DockLinearCaliperEndRowTextBox','DockLinearCaliperEndColumnTextBox','DockLinearCaliperCountTextBox',
    'DockLinearCaliperLength1TextBox','DockLinearCaliperLength2TextBox','DockLinearCaliperSigmaTextBox',
    'DockLinearCaliperThresholdTextBox','DockLinearCaliperTransitionComboBox','DockLinearCaliperSelectComboBox'
)) {
    Assert-True ($null -ne $mainType.GetField($fieldName, $all).GetValue($window)) "$fieldName is missing."
}
Assert-True ($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'LinearCaliperArrayMeasurement' } | Select-Object -First 1)) 'Linear caliper array tool is missing from the toolbox.'

$metadata = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata', $true)
$arrayKind = [Enum]::Parse($toolKindType, 'LinearCaliperArrayMeasurement')
$arrayInputs = $metadata.GetMethod('GetInputPorts', $staticPublic).Invoke($null, [object[]]@($arrayKind))
$arrayOutputs = $metadata.GetMethod('GetOutputPorts', $staticPublic).Invoke($null, [object[]]@($arrayKind))
Assert-True (($arrayInputs | Where-Object { $_.PortName -eq 'Image' -and $_.DataType -eq 'Image' }).Count -eq 1) 'Linear array typed Image input is missing.'
foreach ($port in @('XLD','MeanDistance','ValidCount','PairCount','MinDistance','MaxDistance','StdDev','Distances','ValidIndices','FailedIndices','FailureDetails','Parameters','ResultCode')) {
    Assert-True (($arrayOutputs | Where-Object PortName -eq $port).Count -eq 1) "Linear array output $port is missing."
}

$app = [Windows.Application]::Current
if ($null -eq $app) {
    $app = New-Object Windows.Application
    $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown
}
$disposeFlow.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool ImageSource 'S28 Source'
$caliper = New-Tool CaliperEdgePairMeasurement 'S28 Editable Caliper'
$array = New-Tool LinearCaliperArrayMeasurement 'S28 Linear Array'
$sourceId = $source.ToolId
$caliperId = $caliper.ToolId
$arrayId = $array.ToolId
$source.Parameters.LocalImagePath = $horizontalPath
$caliper.Parameters.CaliperRow = 120
$caliper.Parameters.CaliperColumn = 160
$caliper.Parameters.CaliperPhiDegrees = 90
$caliper.Parameters.CaliperLength1 = 75
$caliper.Parameters.CaliperLength2 = 12
$caliper.Parameters.CaliperSigma = 1
$caliper.Parameters.CaliperThreshold = 20
$caliper.Parameters.CaliperTransition = 'Positive'
$caliper.Parameters.CaliperSelect = 'All'
$caliper.SetInputBinding('Image', $source.ToolId, 'Image')
Set-HorizontalArray $array 5
$array.SetInputBinding('Image', $source.ToolId, 'Image')
foreach ($tool in @($source,$caliper,$array)) { $flow.Add($tool) }

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.WindowState = [Windows.WindowState]::Normal
$window.Left = 2
$window.Top = 2
$window.Width = 1800
$window.Height = 1050
$window.Topmost = $true
$window.Show()
$window.Activate() | Out-Null
Pump-Ui 900
$loadImage.Invoke($window, [object[]]@($horizontalPath.ToString(), $false)) | Out-Null
Invoke-Execute $source 'S28 source' | Out-Null

$flowList = $mainType.GetField('FlowToolList', $all).GetValue($window)
$flowList.SelectedItems.Clear()
$flowList.SelectedItem = $caliper
$loadDraft.Invoke($window, [object[]]@($caliper)) | Out-Null
Pump-Ui 250
$originalColumn = $caliper.Parameters.CaliperColumn
$editDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
Pump-Ui 150
Assert-True $mainType.GetField('measurementDomainEditActive', $private).GetValue($window) 'Measurement-domain edit mode did not start.'
Assert-True ($mainType.GetField('MeasurementDomainEditorBar', $all).GetValue($window).Visibility.ToString() -eq 'Visible') 'Measurement-domain editor bar is not visible.'
Assert-True ($null -ne $mainType.GetField('measurementDomainPreview', $private).GetValue($window)) 'Measurement-domain preview was not created.'

$halconWindow = $mainType.GetField('imageWindow', $private).GetValue($window)
$startX = [int](160.0 / 320.0 * $halconWindow.Width)
$startY = [int](120.0 / 260.0 * $halconWindow.Height)
$endX = $startX + [Math]::Max(12, [int](24.0 / 320.0 * $halconWindow.Width))
$downArgs = [Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::Left, 1, $startX, $startY, 0)
$moveArgs = [Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::Left, 1, $endX, $startY, 0)
$upArgs = [Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::Left, 1, $endX, $startY, 0)
$mouseDown.Invoke($window, [object[]]@($halconWindow, $downArgs)) | Out-Null
Assert-True $mainType.GetField('measurementDomainDragging', $private).GetValue($window) "Measurement-domain center hit missed at window ($startX,$startY), control $($halconWindow.Width)x$($halconWindow.Height)."
$activeHandle = $mainType.GetField('measurementDomainEditHandle', $private).GetValue($window).ToString()
Assert-True ($activeHandle -eq 'Move') "Measurement-domain center resolved to $activeHandle instead of Move."
$mouseMove.Invoke($window, [object[]]@($halconWindow, $moveArgs)) | Out-Null
$mouseUp.Invoke($window, [object[]]@($halconWindow, $upArgs)) | Out-Null
$draftColumn = [double]::Parse($mainType.GetField('DockCaliperColumnTextBox', $all).GetValue($window).Text, [Globalization.CultureInfo]::InvariantCulture)
Assert-True ($draftColumn -gt $originalColumn + 5) 'Physical image drag did not update the dock draft.'
Assert-True ([Math]::Abs($caliper.Parameters.CaliperColumn - $originalColumn) -lt .001) 'Image drag changed applied parameters before Apply.'
Assert-True $mainType.GetField('dockDraftDirty', $private).GetValue($window) 'Image drag did not mark the dock draft dirty.'

$cancelDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
Assert-True ([Math]::Abs($caliper.Parameters.CaliperColumn - $originalColumn) -lt .001) 'Cancel did not preserve the applied measurement domain.'
Assert-True (-not $mainType.GetField('dockDraftDirty', $private).GetValue($window)) 'Cancel left a stale dock draft.'

$editDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
$null = Pump-Ui 150
$startX = [int](160.0 / 320.0 * $halconWindow.Width)
$startY = [int](120.0 / 260.0 * $halconWindow.Height)
$endX = $startX + [Math]::Max(12, [int](24.0 / 320.0 * $halconWindow.Width))
$downArgs = [Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::Left, 1, $startX, $startY, 0)
$moveArgs = [Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::Left, 1, $endX, $startY, 0)
$upArgs = [Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::Left, 1, $endX, $startY, 0)
$mouseDown.Invoke($window, [object[]]@($halconWindow, $downArgs)) | Out-Null
$mouseMove.Invoke($window, [object[]]@($halconWindow, $moveArgs)) | Out-Null
$mouseUp.Invoke($window, [object[]]@($halconWindow, $upArgs)) | Out-Null
$secondDraftColumn = [double]::Parse($mainType.GetField('DockCaliperColumnTextBox', $all).GetValue($window).Text, [Globalization.CultureInfo]::InvariantCulture)
Assert-True ($secondDraftColumn -gt $originalColumn + 5) 'Second image drag did not update the dock draft.'
$undoCountBefore = $mainType.GetField('flowUndoHistory', $private).GetValue($window).Count
$applyDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
$caliper = $flow[1]
Assert-True ($caliper.ToolId -eq $caliperId) 'Applied flow no longer preserves the caliper identity.'
$validationMessage = $mainType.GetField('DockValidationText', $all).GetValue($window).Text
Assert-True ($caliper.Parameters.CaliperColumn -gt $originalColumn + 5) "Apply did not commit the image-edited measurement domain (actual $($caliper.Parameters.CaliperColumn), draft $secondDraftColumn, validation '$validationMessage')."
Assert-True ($mainType.GetField('flowUndoHistory', $private).GetValue($window).Count -eq $undoCountBefore + 1) 'Measurement-domain Apply did not create undo history.'
$appliedColumn = $caliper.Parameters.CaliperColumn

$undo.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
$caliper = $flow[1]
Assert-True ($caliper.ToolId -eq $caliperId) 'Undo no longer preserves the caliper identity.'
Assert-True ([Math]::Abs($caliper.Parameters.CaliperColumn - $originalColumn) -lt .001) 'Undo did not restore the previous measurement domain.'
Assert-True ($null -eq $mainType.GetField('measurementDomainPreview', $private).GetValue($window)) 'Undo left a stale measurement-domain preview.'
$redo.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
$source = $flow[0]
$caliper = $flow[1]
$array = $flow[2]
Assert-True ($source.ToolId -eq $sourceId -and $caliper.ToolId -eq $caliperId -and $array.ToolId -eq $arrayId) 'Redo did not restore the original measurement flow identities.'
Assert-True ([Math]::Abs($caliper.Parameters.CaliperColumn - $appliedColumn) -lt .01) 'Redo did not restore the applied measurement domain.'

[HalconDotNet.HImage]$horizontal = $null
[HalconDotNet.HImage]$partial = $null
[HalconDotNet.HImage]$rotated = $null
[HalconDotNet.HImage]$uniform = $null
[HalconDotNet.HImage]$lowContrast = $null
try {
    $horizontal = [HalconDotNet.HImage]::new($horizontalPath)
    $partial = [HalconDotNet.HImage]::new($partialPath)
    $rotated = [HalconDotNet.HImage]::new($rotatedPath)
    $uniform = [HalconDotNet.HImage]::new($uniformPath)
    $lowContrast = [HalconDotNet.HImage]::new($lowContrastPath)

    foreach ($count in @(3,5,10)) {
        Set-HorizontalArray $array $count
        $record = Invoke-Array $array $horizontal
        $array.ResultCode = $record.ResultCode
        Assert-True ($record.ResultCode -eq 'OK') "Horizontal $count-caliper array did not pass."
        Assert-True ([int][double](Get-Out $array ValidCount) -eq $count) "Horizontal $count-caliper array valid count is wrong."
        $meanDistance = [double](Get-Out $array MeanDistance)
        Assert-True ($meanDistance -gt 10 -and $meanDistance -lt 25) "Horizontal $count-caliper mean distance is implausible ($meanDistance)."
    }

    $p = $array.Parameters
    $p.LinearCaliperStartRow = 100; $p.LinearCaliperStartColumn = 100
    $p.LinearCaliperEndRow = 220; $p.LinearCaliperEndColumn = 220
    $p.LinearCaliperCount = 5; $p.LinearCaliperLength1 = 70; $p.LinearCaliperLength2 = 6
    $p.LinearCaliperSigma = 1; $p.LinearCaliperThreshold = 20
    $p.LinearCaliperTransition = 'Positive'; $p.LinearCaliperSelect = 'All'
    $rotatedRecord = Invoke-Array $array $rotated
    Assert-True ($rotatedRecord.ResultCode -eq 'OK' -and [int][double](Get-Out $array ValidCount) -eq 5) 'Rotated linear array did not measure all calipers.'

    Set-HorizontalArray $array 5
    $partialRecord = Invoke-Array $array $partial
    $partialValid = [int][double](Get-Out $array ValidCount)
    Assert-True ($partialRecord.ResultCode -eq 'NG' -and $partialValid -gt 0 -and $partialValid -lt 5) 'Partial missing-edge case did not retain valid calipers and return NG.'
    Assert-True ((Get-Out $array FailedIndices) -ne '[]' -and (Get-Out $array Distances) -match '--') 'Partial result did not publish failed indices and per-caliper placeholders.'

    $uniformRecord = Invoke-Array $array $uniform
    Assert-True ($uniformRecord.ResultCode -eq 'NG' -and [int][double](Get-Out $array ValidCount) -eq 0) 'Uniform image did not return a clean all-failed NG.'
    $lowRecord = Invoke-Array $array $lowContrast
    Assert-True ($lowRecord.ResultCode -eq 'NG' -and [int][double](Get-Out $array ValidCount) -eq 0) 'Low-contrast image was not rejected by the edge threshold.'

    Set-HorizontalArray $array 5
    $p.LinearCaliperStartColumn = 2
    $p.LinearCaliperEndColumn = 260
    $boundsRecord = Invoke-Array $array $horizontal
    Assert-True ($boundsRecord.ResultCode -eq 'NG' -and [int][double](Get-Out $array ValidCount) -gt 0) 'Out-of-bounds member polluted or aborted the whole array.'
    Assert-True ((Get-Out $array FailureDetails) -match 'bilinear') 'Out-of-bounds member did not explain the bilinear boundary.'

    $invalid = New-Tool LinearCaliperArrayMeasurement 'S28 Invalid Array'
    Set-HorizontalArray $invalid 5
    $invalid.Parameters.LinearCaliperCount = 1
    $rejected = $false
    try { Invoke-Array $invalid $horizontal | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Invalid array count was accepted.'
    $invalid.Dispose()

    Set-HorizontalArray $array 5
    $firstRecord = Invoke-Array $array $horizontal
    $oldXld = Get-Out $array XLD
    $secondRecord = Invoke-Array $array $horizontal
    Assert-True ($firstRecord.ResultCode -eq 'OK' -and $secondRecord.ResultCode -eq 'OK' -and $oldXld.IsDisposed) 'Rerun did not replace and release the previous managed array XLD.'
    for ($index = 0; $index -lt 25; $index++) {
        $repeat = Invoke-Array $array $horizontal
        Assert-True ($repeat.ResultCode -eq 'OK') "Repeated array handle lifecycle run $index failed."
    }
}
finally {
    foreach ($image in @($horizontal,$partial,$rotated,$uniform,$lowContrast)) { if ($null -ne $image) { $image.Dispose() } }
}

$flowList.SelectedItems.Clear()
$flowList.SelectedItem = $array
$loadDraft.Invoke($window, [object[]]@($array)) | Out-Null
Assert-True ($mainType.GetField('DockLinearCaliperArrayPanel', $all).GetValue($window).Visibility.ToString() -eq 'Visible') 'Linear array dock panel is not visible when selected.'
$mainType.GetField('DockLinearCaliperCountTextBox', $all).GetValue($window).Text = '7'
$appliedArrayDraft = $applyDock.Invoke($window, [object[]]@($true))
Assert-True ($appliedArrayDraft -and $array.Parameters.LinearCaliperCount -eq 7) 'Linear array dock draft did not apply through the real configuration path.'
Invoke-Execute $source 'S28 dock trial upstream' | Out-Null
$mainType.GetField('DockLinearCaliperCountTextBox', $all).GetValue($window).Text = '6'
$draftArguments = New-Object 'object[]' 7
$draftArguments[2] = $false
$draftArguments[4] = 0.0
$draftArguments[5] = 0.0
$draftArguments[6] = 0.0
Assert-True $tryReadDock.Invoke($window, $draftArguments) 'Linear array dock trial draft did not pass the real configuration parser.'
$appliedParameters = $array.Parameters
try {
    $array.Parameters = $draftArguments[0]
    $applyDockInputs.Invoke($window, [object[]]@($array)) | Out-Null
    $draftTrialRecord = Invoke-Execute $array 'S28 dock draft trial'
    Assert-True ($draftTrialRecord.ResultCode -eq 'OK' -and [int][double](Get-Out $array ValidCount) -eq 6) 'Linear array dock draft trial did not execute with six calipers.'
}
finally {
    $array.Parameters = $appliedParameters
}
Assert-True ($array.Parameters.LinearCaliperCount -eq 7) 'Linear array dock draft trial did not restore the applied parameter object.'
$loadDraft.Invoke($window, [object[]]@($array)) | Out-Null
Assert-True (-not $mainType.GetField('dockDraftDirty', $private).GetValue($window)) 'Resetting the trial draft left a stale dock change.'

$disposeFlow.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool ImageSource 'S28 Flow Source'
$arrayFlow = New-Tool LinearCaliperArrayMeasurement 'S28 Flow Array'
$judge = New-Tool NumericJudge 'S28 Mean Judge'
$source.Parameters.LocalImagePath = $horizontalPath
$source.Parameters.LocalImageSerialNumber = 'S28-ARRAY'
Set-HorizontalArray $arrayFlow 5
$arrayFlow.SetInputBinding('Image', $source.ToolId, 'Image')
$judge.InputToolId = $arrayFlow.ToolId
$judge.InputPortName = 'MeanDistance'
$judge.SetInputBinding('Value', $arrayFlow.ToolId, 'MeanDistance')
$judge.NumericOperator = 'BetweenInclusive'
$judge.NumericLowerLimit = 10
$judge.NumericUpperLimit = 25
foreach ($tool in @($source,$arrayFlow,$judge)) { $flow.Add($tool) }
Invoke-Execute $source 'S28 flow source' | Out-Null
$flowArrayRecord = Invoke-Execute $arrayFlow 'S28 typed array flow'
$judgeRecord = Invoke-Execute $judge 'S28 numeric judge'
Assert-True ($flowArrayRecord.ResultCode -eq 'OK' -and $judgeRecord.ResultCode -eq 'OK') 'Typed Image → linear array → NumericJudge flow failed.'

$recipe = [Activator]::CreateInstance($visionRecipeType)
$recipe.ToolFlow = $captureFlow.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's28-array-recipe.json'
$service.SaveRecipe($recipePath, $recipe)
$loaded = $service.LoadRecipe($recipePath)
$loadedArray = $loaded.ToolFlow | Where-Object ToolType -eq 'LinearCaliperArrayMeasurement' | Select-Object -First 1
Assert-True ($null -ne $loadedArray -and $loadedArray.Parameters.LinearCaliperCount -eq 5 -and [Math]::Abs($loadedArray.Parameters.LinearCaliperEndColumn - 260) -lt .001) 'Linear array parameters did not survive recipe save/load.'

$sourceSnapshot = Get-Out $source Image
$sourceSnapshot.Dispose()
$releasedRejected = $false
try { Invoke-Execute $arrayFlow 'S28 disposed upstream image' | Out-Null } catch { $releasedRejected = $true }
Assert-True $releasedRejected 'Disposed upstream Image was not rejected.'

$emptyRecipe = [Activator]::CreateInstance($visionRecipeType)
$applyRecipe.Invoke($window, [object[]]@($emptyRecipe)) | Out-Null
Assert-True (-not $mainType.GetField('measurementDomainEditActive', $private).GetValue($window) -and $null -eq $mainType.GetField('measurementDomainPreview', $private).GetValue($window)) 'Recipe switch left stale measurement-domain controls.'

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
$app.Shutdown()
if (Test-Path $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
Write-Output 'S28 measurement-domain editor and HALCON linear caliper array smoke tests passed.'
