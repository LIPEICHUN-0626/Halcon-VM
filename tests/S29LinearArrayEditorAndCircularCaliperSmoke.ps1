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
function Write-CirclePgm(
    [string]$Path,
    [int]$Width,
    [int]$Height,
    [double]$CenterRow,
    [double]$CenterColumn,
    [double]$Radius,
    [string]$Mode
) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) {
            $deltaRow = $row - $CenterRow
            $deltaColumn = $column - $CenterColumn
            $distance = [Math]::Sqrt($deltaRow * $deltaRow + $deltaColumn * $deltaColumn)
            $effectiveRadius = $Radius
            if ($Mode -eq 'Outlier') {
                $angle = [Math]::Atan2($deltaRow, $deltaColumn) * 180.0 / [Math]::PI
                if ($angle -lt 0) { $angle += 360 }
                if ($angle -ge 65 -and $angle -le 105) { $effectiveRadius = $Radius + 18 }
            }
            $inside = $distance -le $effectiveRadius
            $value = if ($inside) { 230 } else { 20 }
            if ($Mode -eq 'Partial' -and $column -ge $CenterColumn) { $value = 20 }
            if ($Mode -eq 'LowContrast') { $value = if ($inside) { 27 } else { 20 } }
            if ($Mode -eq 'Uniform') { $value = 100 }
            $pixels[$row * $Width + $column] = [byte]$value
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s29-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$circlePath = Join-Path $tempRoot 'circle.pgm'
$partialPath = Join-Path $tempRoot 'circle-partial.pgm'
$outlierPath = Join-Path $tempRoot 'circle-outlier.pgm'
$uniformPath = Join-Path $tempRoot 'uniform.pgm'
$lowContrastPath = Join-Path $tempRoot 'circle-low-contrast.pgm'
$boundaryPath = Join-Path $tempRoot 'circle-boundary.pgm'
Write-CirclePgm $circlePath 360 300 150 180 90 Full
Write-CirclePgm $partialPath 360 300 150 180 90 Partial
Write-CirclePgm $outlierPath 360 300 150 180 90 Outlier
Write-CirclePgm $uniformPath 360 300 150 180 90 Uniform
Write-CirclePgm $lowContrastPath 360 300 150 180 90 LowContrast
Write-CirclePgm $boundaryPath 360 300 30 180 90 Full

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
$runCircle = $mainType.GetMethod('RunCircularCaliperCircleFinderTool', $private)
$loadImage = $mainType.GetMethod('LoadImage', $private)
$loadDraft = $mainType.GetMethod('LoadDockConfigurationDraft', $private)
$applyDock = $mainType.GetMethod('ApplyDockConfigurationDraft', $private)
$editDomain = $mainType.GetMethod('EditMeasurementDomainButton_Click', $private)
$trialDomain = $mainType.GetMethod('MeasurementDomainTrialRunButton_Click', $private)
$cancelDomain = $mainType.GetMethod('MeasurementDomainCancelButton_Click', $private)
$applyDomain = $mainType.GetMethod('MeasurementDomainApplyButton_Click', $private)
$mouseDown = $mainType.GetMethod('ImageWindow_MouseDown', $private)
$mouseMove = $mainType.GetMethod('ImageWindow_MouseMove', $private)
$mouseUp = $mainType.GetMethod('ImageWindow_MouseUp', $private)
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
function Invoke-Circle($Tool, $Image) {
    try { $runCircle.Invoke($window, [object[]]@($Tool, $Image)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Set-ArrayParameters($Tool) {
    $p = $Tool.Parameters
    $p.LinearCaliperStartRow = 150
    $p.LinearCaliperStartColumn = 80
    $p.LinearCaliperEndRow = 150
    $p.LinearCaliperEndColumn = 280
    $p.LinearCaliperCount = 9
    $p.LinearCaliperLength1 = 32
    $p.LinearCaliperLength2 = 6
    $p.LinearCaliperSigma = 1
    $p.LinearCaliperThreshold = 20
    $p.LinearCaliperTransition = 'All'
    $p.LinearCaliperSelect = 'All'
}
function Set-CircleParameters(
    $Tool,
    [double]$CenterRow = 150,
    [double]$CenterColumn = 180,
    [double]$Radius = 90,
    [double]$StartAngle = 0,
    [double]$EndAngle = 360,
    [int]$Count = 24
) {
    $p = $Tool.Parameters
    $p.CircularCaliperCenterRow = $CenterRow
    $p.CircularCaliperCenterColumn = $CenterColumn
    $p.CircularCaliperSearchRadius = $Radius
    $p.CircularCaliperStartAngleDegrees = $StartAngle
    $p.CircularCaliperEndAngleDegrees = $EndAngle
    $p.CircularCaliperCount = $Count
    $p.CircularCaliperLength1 = 25
    $p.CircularCaliperLength2 = 4
    $p.CircularCaliperSigma = 1
    $p.CircularCaliperThreshold = 20
    $p.CircularCaliperTransition = 'Negative'
    $p.CircularCaliperSelect = 'All'
}
function Invoke-ImageDrag(
    [double]$FromRow,
    [double]$FromColumn,
    [double]$ToRow,
    [double]$ToColumn,
    [string]$ExpectedHandle
) {
    $imageControl = $mainType.GetField('imageWindow', $private).GetValue($window)
    # Use the current aspect-preserving displayed image part, including padding.
    $view = $mainType.GetField('viewport', $private).GetValue($window)
    $part = [object[]]@($imageControl.ClientSize.Width, $imageControl.ClientSize.Height, 0.0, 0.0, 0.0, 0.0)
    $view.GetType().GetMethod('GetDisplayPart', $private).Invoke($view, $part) | Out-Null
    $columns = $part[5] - $part[3] + 1; $rows = $part[4] - $part[2] + 1
    $fromX = [int][Math]::Round(($FromColumn - $part[3]) / $columns * $imageControl.ClientSize.Width)
    $fromY = [int][Math]::Round(($FromRow - $part[2]) / $rows * $imageControl.ClientSize.Height)
    $toX = [int][Math]::Round(($ToColumn - $part[3]) / $columns * $imageControl.ClientSize.Width)
    $toY = [int][Math]::Round(($ToRow - $part[2]) / $rows * $imageControl.ClientSize.Height)
    $roundtrip = $view.WindowToImage([Drawing.Point]::new($fromX, $fromY), $imageControl)
    $pixelTolerance = [Math]::Max($columns / $imageControl.ClientSize.Width, $rows / $imageControl.ClientSize.Height)
    Assert-True ([Math]::Abs($roundtrip.X - $FromColumn) -le $pixelTolerance -and [Math]::Abs($roundtrip.Y - $FromRow) -le $pixelTolerance) 'Image/window coordinate roundtrip exceeded one screen pixel.'
    $downArgs = [Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::Left, 1, $fromX, $fromY, 0)
    $moveArgs = [Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::Left, 1, $toX, $toY, 0)
    $upArgs = [Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::Left, 1, $toX, $toY, 0)
    $mouseDown.Invoke($window, [object[]]@($imageControl, $downArgs)) | Out-Null
    Assert-True $mainType.GetField('measurementDomainDragging', $private).GetValue($window) "Array handle $ExpectedHandle was not hit."
    $actualHandle = $mainType.GetField('linearCaliperArrayEditHandle', $private).GetValue($window).ToString()
    Assert-True ($actualHandle -eq $ExpectedHandle) "Expected array handle $ExpectedHandle but hit $actualHandle."
    $mouseMove.Invoke($window, [object[]]@($imageControl, $moveArgs)) | Out-Null
    $mouseUp.Invoke($window, [object[]]@($imageControl, $upArgs)) | Out-Null
}

foreach ($fieldName in @(
    'DockLinearCaliperArrayPanel','DockLinearCaliperStartRowTextBox','DockLinearCaliperStartColumnTextBox',
    'DockLinearCaliperEndRowTextBox','DockLinearCaliperEndColumnTextBox','DockLinearCaliperCountTextBox',
    'DockLinearCaliperLength1TextBox','DockLinearCaliperLength2TextBox','EditMeasurementDomainButton',
    'DockCircularCaliperCirclePanel','DockCircularCaliperCenterRowTextBox','DockCircularCaliperCenterColumnTextBox',
    'DockCircularCaliperRadiusTextBox','DockCircularCaliperCountTextBox','DockCircularCaliperStartAngleTextBox',
    'DockCircularCaliperEndAngleTextBox','DockCircularCaliperLength1TextBox','DockCircularCaliperLength2TextBox',
    'DockCircularCaliperSigmaTextBox','DockCircularCaliperThresholdTextBox',
    'DockCircularCaliperTransitionComboBox','DockCircularCaliperSelectComboBox'
)) {
    Assert-True ($null -ne $mainType.GetField($fieldName, $all).GetValue($window)) "$fieldName is missing."
}
Assert-True ($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'CircularCaliperCircleFinder' } | Select-Object -First 1)) 'Circular caliper circle finder is missing from the toolbox.'

$metadata = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata', $true)
$circleKind = [Enum]::Parse($toolKindType, 'CircularCaliperCircleFinder')
$circleInputs = $metadata.GetMethod('GetInputPorts', $staticPublic).Invoke($null, [object[]]@($circleKind))
$circleOutputs = $metadata.GetMethod('GetOutputPorts', $staticPublic).Invoke($null, [object[]]@($circleKind))
Assert-True (($circleInputs | Where-Object { $_.PortName -eq 'Image' -and $_.DataType -eq 'Image' }).Count -eq 1) 'Circular finder typed Image input is missing.'
foreach ($port in @('XLD','CenterRow','CenterColumn','Radius','ValidCount','FailedCount','MaxResidual','MeanAmplitude','EdgeRows','EdgeColumns','EdgeAmplitudes','ValidIndices','FailedIndices','FailureDetails','Parameters','ResultCode')) {
    Assert-True (($circleOutputs | Where-Object PortName -eq $port).Count -eq 1) "Circular finder output $port is missing."
}

$app = [Windows.Application]::Current
if ($null -eq $app) {
    $app = New-Object Windows.Application
    $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown
}
$disposeFlow.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool ImageSource 'S29 Source'
$array = New-Tool LinearCaliperArrayMeasurement 'S29 Editable Array'
$circle = New-Tool CircularCaliperCircleFinder 'S29 Circle Finder'
$source.Parameters.LocalImagePath = $circlePath
Set-ArrayParameters $array
Set-CircleParameters $circle
$array.SetInputBinding('Image', $source.ToolId, 'Image')
$circle.SetInputBinding('Image', $source.ToolId, 'Image')
foreach ($tool in @($source,$array,$circle)) { $flow.Add($tool) }
$arrayId = $array.ToolId

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
$loadImage.Invoke($window, [object[]]@($circlePath.ToString(), $false)) | Out-Null
Invoke-Execute $source 'S29 source' | Out-Null

$flowList = $mainType.GetField('FlowToolList', $all).GetValue($window)
$flowList.SelectedItems.Clear()
$flowList.SelectedItem = $array
$loadDraft.Invoke($window, [object[]]@($array)) | Out-Null
Pump-Ui 200
Assert-True ($mainType.GetField('DockLinearCaliperArrayPanel', $all).GetValue($window).Visibility.ToString() -eq 'Visible') 'Linear array dock panel is not visible.'
$originalStartColumn = $array.Parameters.LinearCaliperStartColumn
$originalEndColumn = $array.Parameters.LinearCaliperEndColumn
$originalBaseline = $originalEndColumn - $originalStartColumn

$editDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
Pump-Ui 150
Assert-True $mainType.GetField('measurementDomainEditActive', $private).GetValue($window) 'Linear array image edit mode did not start.'
Assert-True ($null -ne $mainType.GetField('linearCaliperArrayPreview', $private).GetValue($window)) 'Linear array preview was not created.'
Assert-True ($null -eq $mainType.GetField('measurementDomainPreview', $private).GetValue($window)) 'Linear array editor leaked a single-caliper preview.'
Invoke-ImageDrag 150 180 150 202 Move
$draftStart = [double]::Parse($mainType.GetField('DockLinearCaliperStartColumnTextBox', $all).GetValue($window).Text, [Globalization.CultureInfo]::InvariantCulture)
$draftEnd = [double]::Parse($mainType.GetField('DockLinearCaliperEndColumnTextBox', $all).GetValue($window).Text, [Globalization.CultureInfo]::InvariantCulture)
Assert-True ($draftStart -gt $originalStartColumn + 15 -and $draftEnd -gt $originalEndColumn + 15) 'Physical whole-array drag did not update both endpoint drafts.'
Assert-True ([Math]::Abs(($draftEnd - $draftStart) - $originalBaseline) -lt .1) 'Whole-array drag changed the baseline length.'
Assert-True ([Math]::Abs($array.Parameters.LinearCaliperStartColumn - $originalStartColumn) -lt .001) 'Array drag changed applied parameters before Apply.'
Assert-True $mainType.GetField('dockDraftDirty', $private).GetValue($window) 'Array drag did not mark the draft dirty.'

$trialDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
Pump-Ui 200
Assert-True ([Math]::Abs($array.Parameters.LinearCaliperStartColumn - $originalStartColumn) -lt .001) 'Array trial run applied the draft unexpectedly.'
Assert-True ($null -ne (Get-Out $array XLD)) 'Array image-edit trial did not publish a result overlay.'
$cancelDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
Assert-True ([Math]::Abs($array.Parameters.LinearCaliperStartColumn - $originalStartColumn) -lt .001) 'Array edit Cancel changed applied parameters.'
Assert-True (-not $mainType.GetField('dockDraftDirty', $private).GetValue($window)) 'Array edit Cancel left a stale draft.'

$editDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
Invoke-ImageDrag 150 180 150 202 Move
$undoCountBefore = $mainType.GetField('flowUndoHistory', $private).GetValue($window).Count
$applyDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
$array = $flow[1]
Assert-True ($array.Parameters.LinearCaliperStartColumn -gt $originalStartColumn + 15) 'Array image edit Apply did not commit the draft.'
Assert-True ($mainType.GetField('flowUndoHistory', $private).GetValue($window).Count -eq $undoCountBefore + 1) 'Array image edit Apply did not create undo history.'
$appliedStartColumn = $array.Parameters.LinearCaliperStartColumn

$undo.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
$array = $flow[1]
Assert-True ([Math]::Abs($array.Parameters.LinearCaliperStartColumn - $originalStartColumn) -lt .01) 'Undo did not restore the array geometry.'
Assert-True ($null -eq $mainType.GetField('linearCaliperArrayPreview', $private).GetValue($window)) 'Undo left a stale array preview.'
$redo.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
$array = $flow[1]
$circle = $flow[2]
Assert-True ([Math]::Abs($array.Parameters.LinearCaliperStartColumn - $appliedStartColumn) -lt .01) 'Redo did not restore the applied array geometry.'

$flowList.SelectedItems.Clear()
$flowList.SelectedItem = $array
$loadDraft.Invoke($window, [object[]]@($array)) | Out-Null
$editDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
$preview = $mainType.GetField('linearCaliperArrayPreview', $private).GetValue($window)
Invoke-ImageDrag $preview.StartRow $preview.StartColumn $preview.StartRow ($preview.StartColumn - 12) StartPoint
$preview = $mainType.GetField('linearCaliperArrayPreview', $private).GetValue($window)
$deltaRow = $preview.EndRow - $preview.StartRow
$deltaColumn = $preview.EndColumn - $preview.StartColumn
$baselineLength = [Math]::Sqrt($deltaRow * $deltaRow + $deltaColumn * $deltaColumn)
$baselineRow = $deltaRow / $baselineLength
$baselineColumn = $deltaColumn / $baselineLength
$majorRow = -$baselineColumn
$majorColumn = $baselineRow
$centerRow = ($preview.StartRow + $preview.EndRow) * .5
$centerColumn = ($preview.StartColumn + $preview.EndColumn) * .5
$oldLength1 = $preview.Length1
Invoke-ImageDrag ($centerRow + $majorRow * $oldLength1) ($centerColumn + $majorColumn * $oldLength1) ($centerRow + $majorRow * ($oldLength1 + 10)) ($centerColumn + $majorColumn * ($oldLength1 + 10)) Length1
$preview = $mainType.GetField('linearCaliperArrayPreview', $private).GetValue($window)
Assert-True ($preview.Length1 -gt $oldLength1 + 7) 'Length1 image handle did not resize the calipers.'
$oldLength2 = $preview.Length2
$centerRow = ($preview.StartRow + $preview.EndRow) * .5
$centerColumn = ($preview.StartColumn + $preview.EndColumn) * .5
Invoke-ImageDrag ($centerRow + $baselineRow * $oldLength2) ($centerColumn + $baselineColumn * $oldLength2) ($centerRow + $baselineRow * ($oldLength2 + 10)) ($centerColumn + $baselineColumn * ($oldLength2 + 10)) Length2
$preview = $mainType.GetField('linearCaliperArrayPreview', $private).GetValue($window)
Assert-True ($preview.Length2 -gt $oldLength2 + 7) 'Length2 image handle did not resize the calipers.'
$cancelDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null

[HalconDotNet.HImage]$circleImage = $null
[HalconDotNet.HImage]$partialImage = $null
[HalconDotNet.HImage]$outlierImage = $null
[HalconDotNet.HImage]$uniformImage = $null
[HalconDotNet.HImage]$lowContrastImage = $null
[HalconDotNet.HImage]$boundaryImage = $null
try {
    $circleImage = [HalconDotNet.HImage]::new($circlePath)
    $partialImage = [HalconDotNet.HImage]::new($partialPath)
    $outlierImage = [HalconDotNet.HImage]::new($outlierPath)
    $uniformImage = [HalconDotNet.HImage]::new($uniformPath)
    $lowContrastImage = [HalconDotNet.HImage]::new($lowContrastPath)
    $boundaryImage = [HalconDotNet.HImage]::new($boundaryPath)

    Set-CircleParameters $circle
    $fullRecord = Invoke-Circle $circle $circleImage
    Assert-True ($fullRecord.ResultCode -eq 'OK') 'Full-circle caliper finder did not pass.'
    Assert-True ([int][double](Get-Out $circle ValidCount) -eq 24) 'Full-circle valid count is wrong.'
    Assert-True ([Math]::Abs([double](Get-Out $circle CenterRow) - 150) -lt 2) 'Fitted center row is inaccurate.'
    Assert-True ([Math]::Abs([double](Get-Out $circle CenterColumn) - 180) -lt 2) 'Fitted center column is inaccurate.'
    Assert-True ([Math]::Abs([double](Get-Out $circle Radius) - 90) -lt 2) 'Fitted radius is inaccurate.'
    Assert-True ([double](Get-Out $circle MaxResidual) -lt 2) 'Full-circle residual is unexpectedly large.'

    Set-CircleParameters $circle 150 180 90 360 0 24
    $reverseRecord = Invoke-Circle $circle $circleImage
    Assert-True ($reverseRecord.ResultCode -eq 'OK' -and [int][double](Get-Out $circle ValidCount) -eq 24) 'Reverse full-circle sampling failed.'

    Set-CircleParameters $circle 150 180 90 20 220 15
    $arcRecord = Invoke-Circle $circle $circleImage
    Assert-True ($arcRecord.ResultCode -eq 'OK') 'Open-arc circle fitting failed.'
    Assert-True ([Math]::Abs([double](Get-Out $circle Radius) - 90) -lt 2) 'Open-arc fitted radius is inaccurate.'

    Set-CircleParameters $circle
    $partialRecord = Invoke-Circle $circle $partialImage
    $partialValid = [int][double](Get-Out $circle ValidCount)
    Assert-True ($partialRecord.ResultCode -eq 'NG' -and $partialValid -ge 3 -and $partialValid -lt 24) 'Partial missing edge did not retain valid points and return NG.'
    Assert-True ([double](Get-Out $circle Radius) -gt 80) 'Partial result did not preserve its usable fitted circle.'
    Assert-True ((Get-Out $circle FailedIndices) -ne '[]') 'Partial result did not publish failed indices.'

    Set-CircleParameters $circle
    $outlierRecord = Invoke-Circle $circle $outlierImage
    Assert-True ([int][double](Get-Out $circle ValidCount) -eq 24) 'Outlier image lost radial measurements unexpectedly.'
    Assert-True ([Math]::Abs([double](Get-Out $circle Radius) - 90) -lt 5) 'Robust atukey fit did not resist the sector outlier.'

    Set-CircleParameters $circle
    $uniformRecord = Invoke-Circle $circle $uniformImage
    Assert-True ($uniformRecord.ResultCode -eq 'NG' -and [int][double](Get-Out $circle ValidCount) -eq 0 -and [double](Get-Out $circle Radius) -eq 0) 'Uniform image did not return a clean no-fit NG.'
    $lowRecord = Invoke-Circle $circle $lowContrastImage
    Assert-True ($lowRecord.ResultCode -eq 'NG' -and [int][double](Get-Out $circle ValidCount) -eq 0) 'Low-contrast boundary was not rejected.'

    Set-CircleParameters $circle 30 180 90 0 360 24
    $boundaryRecord = Invoke-Circle $circle $boundaryImage
    $boundaryValid = [int][double](Get-Out $circle ValidCount)
    Assert-True ($boundaryRecord.ResultCode -eq 'NG' -and $boundaryValid -ge 3 -and $boundaryValid -lt 24) 'Boundary calipers did not fail locally while retaining valid samples.'
    Assert-True ((Get-Out $circle FailureDetails) -match 'bilinear') 'Boundary failure did not explain the bilinear safety margin.'

    $invalid = New-Tool CircularCaliperCircleFinder 'S29 Invalid Circle'
    Set-CircleParameters $invalid 150 180 90 0 20 2
    $rejected = $false
    try { Invoke-Circle $invalid $circleImage | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Invalid count/angle span was accepted.'
    $invalid.Dispose()

    Set-CircleParameters $circle
    $firstRecord = Invoke-Circle $circle $circleImage
    $oldXld = Get-Out $circle XLD
    $secondRecord = Invoke-Circle $circle $circleImage
    Assert-True ($firstRecord.ResultCode -eq 'OK' -and $secondRecord.ResultCode -eq 'OK' -and $oldXld.IsDisposed) 'Rerun did not replace and release the previous managed circle XLD.'
    for ($index = 0; $index -lt 20; $index++) {
        $repeat = Invoke-Circle $circle $circleImage
        Assert-True ($repeat.ResultCode -eq 'OK') "Repeated circle measure-handle lifecycle run $index failed."
    }
}
finally {
    foreach ($image in @($circleImage,$partialImage,$outlierImage,$uniformImage,$lowContrastImage,$boundaryImage)) {
        if ($null -ne $image) { $image.Dispose() }
    }
}

$flowList.SelectedItems.Clear()
$flowList.SelectedItem = $circle
$loadDraft.Invoke($window, [object[]]@($circle)) | Out-Null
Assert-True ($mainType.GetField('DockCircularCaliperCirclePanel', $all).GetValue($window).Visibility.ToString() -eq 'Visible') 'Circular finder dock panel is not visible.'
$mainType.GetField('DockCircularCaliperCountTextBox', $all).GetValue($window).Text = '20'
Assert-True $applyDock.Invoke($window, [object[]]@($true)) 'Circular finder dock draft did not apply.'
Assert-True ($circle.Parameters.CircularCaliperCount -eq 20) 'Circular finder dock count did not persist.'

$loadDraft.Invoke($window, [object[]]@($circle)) | Out-Null
$disposeFlow.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool ImageSource 'S29 Flow Source'
$circleFlow = New-Tool CircularCaliperCircleFinder 'S29 Flow Circle'
$judge = New-Tool NumericJudge 'S29 Radius Judge'
$source.Parameters.LocalImagePath = $circlePath
$source.Parameters.LocalImageSerialNumber = 'S29-CIRCLE'
Set-CircleParameters $circleFlow
$circleFlow.SetInputBinding('Image', $source.ToolId, 'Image')
$judge.InputToolId = $circleFlow.ToolId
$judge.InputPortName = 'Radius'
$judge.SetInputBinding('Value', $circleFlow.ToolId, 'Radius')
$judge.NumericOperator = 'BetweenInclusive'
$judge.NumericLowerLimit = 88
$judge.NumericUpperLimit = 92
foreach ($tool in @($source,$circleFlow,$judge)) { $flow.Add($tool) }
Invoke-Execute $source 'S29 flow source' | Out-Null
$circleFlowRecord = Invoke-Execute $circleFlow 'S29 typed circle flow'
$judgeRecord = Invoke-Execute $judge 'S29 radius judge'
Assert-True ($circleFlowRecord.ResultCode -eq 'OK' -and $judgeRecord.ResultCode -eq 'OK') 'Typed Image → circular finder → NumericJudge flow failed.'

$recipe = [Activator]::CreateInstance($visionRecipeType)
$recipe.ToolFlow = $captureFlow.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's29-circle-recipe.json'
$service.SaveRecipe($recipePath, $recipe)
$loaded = $service.LoadRecipe($recipePath)
$loadedCircle = $loaded.ToolFlow | Where-Object ToolType -eq 'CircularCaliperCircleFinder' | Select-Object -First 1
Assert-True ($null -ne $loadedCircle -and $loadedCircle.Parameters.CircularCaliperCount -eq 24) 'Circular finder parameters did not survive recipe save/load.'
Assert-True ([Math]::Abs($loadedCircle.Parameters.CircularCaliperSearchRadius - 90) -lt .001) 'Circular finder search radius did not survive recipe save/load.'

$sourceSnapshot = Get-Out $source Image
$sourceSnapshot.Dispose()
$releasedRejected = $false
try { Invoke-Execute $circleFlow 'S29 disposed upstream image' | Out-Null } catch { $releasedRejected = $true }
Assert-True $releasedRejected 'Disposed upstream Image was not rejected.'

$emptyRecipe = [Activator]::CreateInstance($visionRecipeType)
$applyRecipe.Invoke($window, [object[]]@($emptyRecipe)) | Out-Null
Assert-True (-not $mainType.GetField('measurementDomainEditActive', $private).GetValue($window)) 'Recipe switch left array edit mode active.'
Assert-True ($null -eq $mainType.GetField('linearCaliperArrayPreview', $private).GetValue($window)) 'Recipe switch left a stale array preview.'

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
$app.Shutdown()
$resolvedTemp = [IO.Path]::GetFullPath($tempRoot)
$expectedParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
if (-not $resolvedTemp.StartsWith($expectedParent, [StringComparison]::OrdinalIgnoreCase) -or -not [IO.Path]::GetFileName($resolvedTemp).StartsWith('halcon-vm-s29-')) { throw 'Unsafe test cleanup path.' }
if (Test-Path -LiteralPath $resolvedTemp) { Remove-Item -LiteralPath $resolvedTemp -Recurse -Force }
Write-Output 'S29 image-space linear array editor and HALCON circular caliper circle finder smoke tests passed.'
