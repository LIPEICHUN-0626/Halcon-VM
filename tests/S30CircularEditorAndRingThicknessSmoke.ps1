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
function Write-RingPgm(
    [string]$Path,
    [int]$Width,
    [int]$Height,
    [double]$OuterCenterRow,
    [double]$OuterCenterColumn,
    [double]$OuterRadius,
    [double]$InnerCenterRow,
    [double]$InnerCenterColumn,
    [double]$InnerRadius,
    [string]$Mode
) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) {
            $outerDeltaRow = $row - $OuterCenterRow
            $outerDeltaColumn = $column - $OuterCenterColumn
            $outerDistance = [Math]::Sqrt($outerDeltaRow * $outerDeltaRow + $outerDeltaColumn * $outerDeltaColumn)
            $innerDeltaRow = $row - $InnerCenterRow
            $innerDeltaColumn = $column - $InnerCenterColumn
            $innerDistance = [Math]::Sqrt($innerDeltaRow * $innerDeltaRow + $innerDeltaColumn * $innerDeltaColumn)
            $effectiveOuterRadius = $OuterRadius
            $angle = [Math]::Atan2($outerDeltaRow, $outerDeltaColumn) * 180.0 / [Math]::PI
            if ($angle -lt 0) { $angle += 360 }
            if ($Mode -eq 'Outlier' -and $angle -ge 65 -and $angle -le 105) { $effectiveOuterRadius += 10 }
            $insideRing = $outerDistance -le $effectiveOuterRadius -and $innerDistance -ge $InnerRadius
            $value = if ($insideRing) { 230 } else { 20 }
            if ($Mode -eq 'Partial' -and $column -ge $OuterCenterColumn) { $value = 20 }
            if ($Mode -eq 'LowContrast') { $value = if ($insideRing) { 27 } else { 20 } }
            if ($Mode -eq 'Uniform') { $value = 100 }
            $pixels[$row * $Width + $column] = [byte]$value
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s30-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$ringPath = Join-Path $tempRoot 'ring.pgm'
$eccentricPath = Join-Path $tempRoot 'ring-eccentric.pgm'
$partialPath = Join-Path $tempRoot 'ring-partial.pgm'
$outlierPath = Join-Path $tempRoot 'ring-outlier.pgm'
$uniformPath = Join-Path $tempRoot 'uniform.pgm'
$lowContrastPath = Join-Path $tempRoot 'ring-low-contrast.pgm'
$boundaryPath = Join-Path $tempRoot 'ring-boundary.pgm'
Write-RingPgm $ringPath 360 300 150 180 110 150 180 70 Full
Write-RingPgm $eccentricPath 360 300 150 184 110 150 180 70 Full
Write-RingPgm $partialPath 360 300 150 180 110 150 180 70 Partial
Write-RingPgm $outlierPath 360 300 150 180 110 150 180 70 Outlier
Write-RingPgm $uniformPath 360 300 150 180 110 150 180 70 Uniform
Write-RingPgm $lowContrastPath 360 300 150 180 110 150 180 70 LowContrast
Write-RingPgm $boundaryPath 360 300 30 180 110 30 180 70 Full

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
$runRing = $mainType.GetMethod('RunRingWallThicknessMeasurementTool', $private)
$loadImage = $mainType.GetMethod('LoadImage', $private)
$loadDraft = $mainType.GetMethod('LoadDockConfigurationDraft', $private)
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
function Invoke-Ring($Tool, $Image) {
    try { $runRing.Invoke($window, [object[]]@($Tool, $Image)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Set-CircleParameters($Tool) {
    $p = $Tool.Parameters
    $p.CircularCaliperCenterRow = 150
    $p.CircularCaliperCenterColumn = 180
    $p.CircularCaliperSearchRadius = 90
    $p.CircularCaliperStartAngleDegrees = 0
    $p.CircularCaliperEndAngleDegrees = 360
    $p.CircularCaliperCount = 24
    $p.CircularCaliperLength1 = 24
    $p.CircularCaliperLength2 = 4
    $p.CircularCaliperSigma = 1
    $p.CircularCaliperThreshold = 20
    $p.CircularCaliperTransition = 'Negative'
    $p.CircularCaliperSelect = 'All'
}
function Set-RingParameters(
    $Tool,
    [double]$CenterRow = 150,
    [double]$CenterColumn = 180,
    [double]$Radius = 90,
    [double]$StartAngle = 0,
    [double]$EndAngle = 360,
    [int]$Count = 24
) {
    $p = $Tool.Parameters
    $p.RingCenterRow = $CenterRow
    $p.RingCenterColumn = $CenterColumn
    $p.RingSearchRadius = $Radius
    $p.RingStartAngleDegrees = $StartAngle
    $p.RingEndAngleDegrees = $EndAngle
    $p.RingCaliperCount = $Count
    $p.RingCaliperLength1 = 28
    $p.RingCaliperLength2 = 4
    $p.RingCaliperSigma = 1
    $p.RingCaliperThreshold = 20
    $p.RingCaliperTransition = 'Positive'
    $p.RingCaliperSelect = 'All'
}
function Get-WindowCoordinate([double]$ImageValue, [double]$ImageSize, [int]$ControlSize) {
    [int][Math]::Round($ImageValue / $ImageSize * $ControlSize)
}
function Invoke-CircleImageDrag(
    [double]$FromRow,
    [double]$FromColumn,
    [double]$ToRow,
    [double]$ToColumn,
    [string]$ExpectedHandle
) {
    $imageControl = $mainType.GetField('imageWindow', $private).GetValue($window)
    $fromX = Get-WindowCoordinate $FromColumn 360 $imageControl.Width
    $fromY = Get-WindowCoordinate $FromRow 300 $imageControl.Height
    $toX = Get-WindowCoordinate $ToColumn 360 $imageControl.Width
    $toY = Get-WindowCoordinate $ToRow 300 $imageControl.Height
    $downArgs = [Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::Left, 1, $fromX, $fromY, 0)
    $moveArgs = [Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::Left, 1, $toX, $toY, 0)
    $upArgs = [Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::Left, 1, $toX, $toY, 0)
    $mouseDown.Invoke($window, [object[]]@($imageControl, $downArgs)) | Out-Null
    Assert-True $mainType.GetField('measurementDomainDragging', $private).GetValue($window) "Circular handle $ExpectedHandle was not hit."
    $actualHandle = $mainType.GetField('circularCaliperEditHandle', $private).GetValue($window).ToString()
    Assert-True ($actualHandle -eq $ExpectedHandle) "Expected circular handle $ExpectedHandle but hit $actualHandle."
    $mouseMove.Invoke($window, [object[]]@($imageControl, $moveArgs)) | Out-Null
    $mouseUp.Invoke($window, [object[]]@($imageControl, $upArgs)) | Out-Null
}
function Get-CircularHandlePoint($Geometry, [string]$Handle) {
    [double]$centerRow = $Geometry.CenterRow
    [double]$centerColumn = $Geometry.CenterColumn
    [double]$geometryRadius = $Geometry.Radius
    [double]$startAngleDegrees = $Geometry.StartAngleDegrees
    [double]$endAngleDegrees = $Geometry.EndAngleDegrees
    if ($Handle -eq 'Center') { return [double[]]@($centerRow, $centerColumn) }
    [double]$middle = ($startAngleDegrees + $endAngleDegrees) * .5 * [Math]::PI / 180
    if ($Handle -eq 'Radius') {
        [double]$row = $centerRow + $geometryRadius * [Math]::Sin($middle)
        [double]$column = $centerColumn + $geometryRadius * [Math]::Cos($middle)
        return [double[]]@($row, $column)
    }
    if ($Handle -eq 'StartAngle') {
        [double]$angle = $startAngleDegrees * [Math]::PI / 180
        [double]$row = $centerRow + $geometryRadius * [Math]::Sin($angle)
        [double]$column = $centerColumn + $geometryRadius * [Math]::Cos($angle)
        return [double[]]@($row, $column)
    }
    if ($Handle -eq 'EndAngle') {
        [double]$angle = $endAngleDegrees * [Math]::PI / 180
        [double]$span = [Math]::Abs($endAngleDegrees - $startAngleDegrees)
        [double]$radius = $geometryRadius + $(if ([Math]::Abs($span - 360) -le .000001) { 14 } else { 0 })
        [double]$row = $centerRow + $radius * [Math]::Sin($angle)
        [double]$column = $centerColumn + $radius * [Math]::Cos($angle)
        return [double[]]@($row, $column)
    }
    [double]$caliperRow = $centerRow + $geometryRadius * [Math]::Sin($middle)
    [double]$caliperColumn = $centerColumn + $geometryRadius * [Math]::Cos($middle)
    if ($Handle -eq 'Length1') {
        [double]$row = $caliperRow + [double]$Geometry.Length1 * [Math]::Sin($middle)
        [double]$column = $caliperColumn + [double]$Geometry.Length1 * [Math]::Cos($middle)
        return [double[]]@($row, $column)
    }
    [double]$row = $caliperRow + [double]$Geometry.Length2 * [Math]::Cos($middle)
    [double]$column = $caliperColumn - [double]$Geometry.Length2 * [Math]::Sin($middle)
    [double[]]@($row, $column)
}

foreach ($fieldName in @(
    'DockCircularCaliperCirclePanel','DockCircularCaliperCenterRowTextBox','DockCircularCaliperCenterColumnTextBox',
    'DockCircularCaliperRadiusTextBox','DockCircularCaliperStartAngleTextBox','DockCircularCaliperEndAngleTextBox',
    'DockCircularCaliperCountTextBox','DockCircularCaliperLength1TextBox','DockCircularCaliperLength2TextBox',
    'DockRingWallThicknessPanel','DockRingCenterRowTextBox','DockRingCenterColumnTextBox','DockRingRadiusTextBox',
    'DockRingStartAngleTextBox','DockRingEndAngleTextBox','DockRingCountTextBox','DockRingLength1TextBox',
    'DockRingLength2TextBox','DockRingSigmaTextBox','DockRingThresholdTextBox','DockRingTransitionComboBox','DockRingSelectComboBox'
)) {
    Assert-True ($null -ne $mainType.GetField($fieldName, $all).GetValue($window)) "$fieldName is missing."
}
Assert-True ($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'RingWallThicknessMeasurement' } | Select-Object -First 1)) 'Ring wall-thickness tool is missing from the toolbox.'
$metadata = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata', $true)
$ringKind = [Enum]::Parse($toolKindType, 'RingWallThicknessMeasurement')
$ringInputs = $metadata.GetMethod('GetInputPorts', $staticPublic).Invoke($null, [object[]]@($ringKind))
$ringOutputs = $metadata.GetMethod('GetOutputPorts', $staticPublic).Invoke($null, [object[]]@($ringKind))
Assert-True (($ringInputs | Where-Object { $_.PortName -eq 'Image' -and $_.DataType -eq 'Image' }).Count -eq 1) 'Ring typed Image input is missing.'
foreach ($port in @('XLD','InnerCenterRow','InnerCenterColumn','InnerRadius','OuterCenterRow','OuterCenterColumn','OuterRadius','MeanThickness','MinThickness','MaxThickness','StdDevThickness','Eccentricity','ValidCount','FailedCount','MaxInnerResidual','MaxOuterResidual','Thicknesses','ValidIndices','FailedIndices','FailureDetails','Parameters','ResultCode')) {
    Assert-True (($ringOutputs | Where-Object PortName -eq $port).Count -eq 1) "Ring output $port is missing."
}

$app = [Windows.Application]::Current
if ($null -eq $app) {
    $app = New-Object Windows.Application
    $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown
}
$disposeFlow.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool ImageSource 'S30 Source'
$circle = New-Tool CircularCaliperCircleFinder 'S30 Editable Circle'
$ring = New-Tool RingWallThicknessMeasurement 'S30 Ring Thickness'
$source.Parameters.LocalImagePath = $ringPath
Set-CircleParameters $circle
Set-RingParameters $ring
$circle.SetInputBinding('Image', $source.ToolId, 'Image')
$ring.SetInputBinding('Image', $source.ToolId, 'Image')
foreach ($tool in @($source,$circle,$ring)) { $flow.Add($tool) }

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
$loadImage.Invoke($window, [object[]]@($ringPath.ToString(), $false)) | Out-Null
Invoke-Execute $source 'S30 source' | Out-Null

$flowList = $mainType.GetField('FlowToolList', $all).GetValue($window)
$flowList.SelectedItems.Clear()
$flowList.SelectedItem = $circle
$loadDraft.Invoke($window, [object[]]@($circle)) | Out-Null
$editDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
Pump-Ui 150
Assert-True $mainType.GetField('measurementDomainEditActive', $private).GetValue($window) 'Circular image editor did not start.'
Assert-True ($null -ne $mainType.GetField('circularCaliperPreview', $private).GetValue($window)) 'Circular editor preview was not created.'
Assert-True ($null -eq $mainType.GetField('linearCaliperArrayPreview', $private).GetValue($window)) 'Circular editor leaked a linear-array preview.'

$preview = $mainType.GetField('circularCaliperPreview', $private).GetValue($window)
$originalCenterColumn = $circle.Parameters.CircularCaliperCenterColumn
$point = Get-CircularHandlePoint $preview Center
Invoke-CircleImageDrag $point[0] $point[1] ($point[0] + 5) ($point[1] + 7) Center
Assert-True ([Math]::Abs($circle.Parameters.CircularCaliperCenterColumn - $originalCenterColumn) -lt .001) 'Circular drag changed applied parameters before Apply.'
$preview = $mainType.GetField('circularCaliperPreview', $private).GetValue($window)
$point = Get-CircularHandlePoint $preview Radius
Invoke-CircleImageDrag $point[0] $point[1] $point[0] ($point[1] - 10) Radius
$preview = $mainType.GetField('circularCaliperPreview', $private).GetValue($window)
$point = Get-CircularHandlePoint $preview StartAngle
$targetAngle = 20 * [Math]::PI / 180
Invoke-CircleImageDrag $point[0] $point[1] ($preview.CenterRow + $preview.Radius * [Math]::Sin($targetAngle)) ($preview.CenterColumn + $preview.Radius * [Math]::Cos($targetAngle)) StartAngle
$preview = $mainType.GetField('circularCaliperPreview', $private).GetValue($window)
$point = Get-CircularHandlePoint $preview EndAngle
$targetAngle = -20 * [Math]::PI / 180
Invoke-CircleImageDrag $point[0] $point[1] ($preview.CenterRow + $preview.Radius * [Math]::Sin($targetAngle)) ($preview.CenterColumn + $preview.Radius * [Math]::Cos($targetAngle)) EndAngle
$preview = $mainType.GetField('circularCaliperPreview', $private).GetValue($window)
$point = Get-CircularHandlePoint $preview Length1
$middle = ($preview.StartAngleDegrees + $preview.EndAngleDegrees) * .5 * [Math]::PI / 180
$caliperRow = $preview.CenterRow + $preview.Radius * [Math]::Sin($middle)
$caliperColumn = $preview.CenterColumn + $preview.Radius * [Math]::Cos($middle)
Invoke-CircleImageDrag $point[0] $point[1] ($caliperRow + ($preview.Length1 + 8) * [Math]::Sin($middle)) ($caliperColumn + ($preview.Length1 + 8) * [Math]::Cos($middle)) Length1
$preview = $mainType.GetField('circularCaliperPreview', $private).GetValue($window)
$point = Get-CircularHandlePoint $preview Length2
Invoke-CircleImageDrag $point[0] $point[1] ($caliperRow + ($preview.Length2 + 5) * [Math]::Cos($middle)) ($caliperColumn - ($preview.Length2 + 5) * [Math]::Sin($middle)) Length2
Assert-True $mainType.GetField('dockDraftDirty', $private).GetValue($window) 'Circular editor did not mark its draft dirty.'
$trialDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
Pump-Ui 200
Assert-True ([Math]::Abs($circle.Parameters.CircularCaliperCenterColumn - $originalCenterColumn) -lt .001) 'Circular trial run applied the draft unexpectedly.'
Assert-True ($null -ne (Get-Out $circle XLD)) 'Circular trial run did not refresh its XLD overlay.'
$cancelDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
Assert-True (-not $mainType.GetField('dockDraftDirty', $private).GetValue($window)) 'Circular Cancel left a stale draft.'

$editDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
$preview = $mainType.GetField('circularCaliperPreview', $private).GetValue($window)
$point = Get-CircularHandlePoint $preview Center
Invoke-CircleImageDrag $point[0] $point[1] $point[0] ($point[1] + 6) Center
$undoCountBefore = $mainType.GetField('flowUndoHistory', $private).GetValue($window).Count
$applyDomain.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
$circle = $flow[1]
Assert-True ($circle.Parameters.CircularCaliperCenterColumn -gt $originalCenterColumn + 4) 'Circular Apply did not commit the center draft.'
Assert-True ($mainType.GetField('flowUndoHistory', $private).GetValue($window).Count -eq $undoCountBefore + 1) 'Circular Apply did not create undo history.'
$appliedCenterColumn = $circle.Parameters.CircularCaliperCenterColumn
$undo.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
$circle = $flow[1]
Assert-True ([Math]::Abs($circle.Parameters.CircularCaliperCenterColumn - $originalCenterColumn) -lt .01) 'Undo did not restore circular geometry.'
Assert-True ($null -eq $mainType.GetField('circularCaliperPreview', $private).GetValue($window)) 'Undo left a stale circular preview.'
$redo.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
$circle = $flow[1]
$ring = $flow[2]
Assert-True ([Math]::Abs($circle.Parameters.CircularCaliperCenterColumn - $appliedCenterColumn) -lt .01) 'Redo did not restore circular geometry.'

[HalconDotNet.HImage]$ringImage = $null
[HalconDotNet.HImage]$eccentricImage = $null
[HalconDotNet.HImage]$partialImage = $null
[HalconDotNet.HImage]$outlierImage = $null
[HalconDotNet.HImage]$uniformImage = $null
[HalconDotNet.HImage]$lowContrastImage = $null
[HalconDotNet.HImage]$boundaryImage = $null
try {
    $ringImage = [HalconDotNet.HImage]::new($ringPath)
    $eccentricImage = [HalconDotNet.HImage]::new($eccentricPath)
    $partialImage = [HalconDotNet.HImage]::new($partialPath)
    $outlierImage = [HalconDotNet.HImage]::new($outlierPath)
    $uniformImage = [HalconDotNet.HImage]::new($uniformPath)
    $lowContrastImage = [HalconDotNet.HImage]::new($lowContrastPath)
    $boundaryImage = [HalconDotNet.HImage]::new($boundaryPath)

    Set-RingParameters $ring
    $fullRecord = Invoke-Ring $ring $ringImage
    Assert-True ($fullRecord.ResultCode -eq 'OK') 'Full-ring wall-thickness measurement did not pass.'
    Assert-True ([int][double](Get-Out $ring ValidCount) -eq 24) 'Full-ring valid count is wrong.'
    Assert-True ([Math]::Abs([double](Get-Out $ring InnerRadius) - 70) -lt 2) 'Inner fitted radius is inaccurate.'
    Assert-True ([Math]::Abs([double](Get-Out $ring OuterRadius) - 110) -lt 2) 'Outer fitted radius is inaccurate.'
    Assert-True ([Math]::Abs([double](Get-Out $ring MeanThickness) - 40) -lt 2) 'Mean wall thickness is inaccurate.'
    Assert-True ([double](Get-Out $ring Eccentricity) -lt 2) 'Concentric ring reported excessive eccentricity.'

    Set-RingParameters $ring 150 180 90 20 220 18
    $arcRecord = Invoke-Ring $ring $ringImage
    Assert-True ($arcRecord.ResultCode -eq 'OK') 'Open-arc ring measurement failed.'
    Set-RingParameters $ring 150 180 90 360 0 24
    $reverseRecord = Invoke-Ring $ring $ringImage
    Assert-True ($reverseRecord.ResultCode -eq 'OK') 'Reverse full-ring measurement failed.'

    Set-RingParameters $ring
    $eccentricRecord = Invoke-Ring $ring $eccentricImage
    Assert-True ($eccentricRecord.ResultCode -eq 'OK') 'Non-concentric ring measurement failed.'
    Assert-True ([Math]::Abs([double](Get-Out $ring Eccentricity) - 4) -lt 2) 'Non-concentric ring eccentricity is inaccurate.'

    $partialRecord = Invoke-Ring $ring $partialImage
    $partialValid = [int][double](Get-Out $ring ValidCount)
    Assert-True ($partialRecord.ResultCode -eq 'NG' -and $partialValid -ge 3 -and $partialValid -lt 24) 'Partial ring did not isolate failed calipers.'
    Assert-True ((Get-Out $ring FailedIndices) -ne '[]') 'Partial ring did not publish failed indices.'

    $outlierRecord = Invoke-Ring $ring $outlierImage
    Assert-True ([Math]::Abs([double](Get-Out $ring OuterRadius) - 110) -lt 5) 'Robust outer-circle fit did not resist the sector outlier.'

    $uniformRecord = Invoke-Ring $ring $uniformImage
    Assert-True ($uniformRecord.ResultCode -eq 'NG' -and [int][double](Get-Out $ring ValidCount) -eq 0) 'Uniform image did not return clean NG.'
    $lowRecord = Invoke-Ring $ring $lowContrastImage
    Assert-True ($lowRecord.ResultCode -eq 'NG' -and [int][double](Get-Out $ring ValidCount) -eq 0) 'Low-contrast ring was not rejected.'

    Set-RingParameters $ring 30 180 90 0 360 24
    $boundaryRecord = Invoke-Ring $ring $boundaryImage
    $boundaryValid = [int][double](Get-Out $ring ValidCount)
    Assert-True ($boundaryRecord.ResultCode -eq 'NG' -and $boundaryValid -ge 3 -and $boundaryValid -lt 24) 'Boundary calipers did not fail locally.'
    Assert-True ((Get-Out $ring FailureDetails) -match 'bilinear') 'Boundary failure did not explain the bilinear safety margin.'

    $invalid = New-Tool RingWallThicknessMeasurement 'S30 Invalid Ring'
    Set-RingParameters $invalid 150 180 90 0 20 2
    $rejected = $false
    try { Invoke-Ring $invalid $ringImage | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Invalid ring count/angle span was accepted.'
    $invalid.Dispose()

    Set-RingParameters $ring
    $firstRecord = Invoke-Ring $ring $ringImage
    $oldXld = Get-Out $ring XLD
    $secondRecord = Invoke-Ring $ring $ringImage
    Assert-True ($firstRecord.ResultCode -eq 'OK' -and $secondRecord.ResultCode -eq 'OK' -and $oldXld.IsDisposed) 'Ring rerun did not replace and release the old XLD.'
    for ($index = 0; $index -lt 20; $index++) {
        $repeat = Invoke-Ring $ring $ringImage
        Assert-True ($repeat.ResultCode -eq 'OK') "Repeated ring measure-handle lifecycle run $index failed."
    }
}
finally {
    foreach ($image in @($ringImage,$eccentricImage,$partialImage,$outlierImage,$uniformImage,$lowContrastImage,$boundaryImage)) {
        if ($null -ne $image) { $image.Dispose() }
    }
}

$flowList.SelectedItems.Clear()
$flowList.SelectedItem = $ring
$loadDraft.Invoke($window, [object[]]@($ring)) | Out-Null
Assert-True ($mainType.GetField('DockRingWallThicknessPanel', $all).GetValue($window).Visibility.ToString() -eq 'Visible') 'Ring dock panel is not visible.'
$mainType.GetField('DockRingCountTextBox', $all).GetValue($window).Text = '20'
$applyDock = $mainType.GetMethod('ApplyDockConfigurationDraft', $private)
Assert-True $applyDock.Invoke($window, [object[]]@($true)) 'Ring dock draft did not apply.'
Assert-True ($ring.Parameters.RingCaliperCount -eq 20) 'Ring dock count did not persist.'

$disposeFlow.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool ImageSource 'S30 Flow Source'
$ringFlow = New-Tool RingWallThicknessMeasurement 'S30 Flow Ring'
$judge = New-Tool NumericJudge 'S30 Thickness Judge'
$source.Parameters.LocalImagePath = $ringPath
$source.Parameters.LocalImageSerialNumber = 'S30-RING'
Set-RingParameters $ringFlow
$ringFlow.SetInputBinding('Image', $source.ToolId, 'Image')
$judge.InputToolId = $ringFlow.ToolId
$judge.InputPortName = 'MeanThickness'
$judge.SetInputBinding('Value', $ringFlow.ToolId, 'MeanThickness')
$judge.NumericOperator = 'BetweenInclusive'
$judge.NumericLowerLimit = 38
$judge.NumericUpperLimit = 42
foreach ($tool in @($source,$ringFlow,$judge)) { $flow.Add($tool) }
Invoke-Execute $source 'S30 flow source' | Out-Null
$ringFlowRecord = Invoke-Execute $ringFlow 'S30 typed ring flow'
$judgeRecord = Invoke-Execute $judge 'S30 thickness judge'
Assert-True ($ringFlowRecord.ResultCode -eq 'OK' -and $judgeRecord.ResultCode -eq 'OK') 'Typed Image -> ring -> NumericJudge flow failed.'

$recipe = [Activator]::CreateInstance($visionRecipeType)
$recipe.ToolFlow = $captureFlow.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's30-ring-recipe.json'
$service.SaveRecipe($recipePath, $recipe)
$loaded = $service.LoadRecipe($recipePath)
$loadedRing = $loaded.ToolFlow | Where-Object ToolType -eq 'RingWallThicknessMeasurement' | Select-Object -First 1
Assert-True ($null -ne $loadedRing -and $loadedRing.Parameters.RingCaliperCount -eq 24) 'Ring parameters did not survive recipe save/load.'
Assert-True ([Math]::Abs($loadedRing.Parameters.RingSearchRadius - 90) -lt .001) 'Ring radius did not survive recipe save/load.'

$sourceSnapshot = Get-Out $source Image
$sourceSnapshot.Dispose()
$releasedRejected = $false
try { Invoke-Execute $ringFlow 'S30 disposed upstream image' | Out-Null } catch { $releasedRejected = $true }
Assert-True $releasedRejected 'Disposed upstream Image was not rejected.'

$emptyRecipe = [Activator]::CreateInstance($visionRecipeType)
$applyRecipe.Invoke($window, [object[]]@($emptyRecipe)) | Out-Null
Assert-True (-not $mainType.GetField('measurementDomainEditActive', $private).GetValue($window)) 'Recipe switch left circular edit mode active.'
Assert-True ($null -eq $mainType.GetField('circularCaliperPreview', $private).GetValue($window)) 'Recipe switch left a stale circular preview.'

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
$app.Shutdown()
if (Test-Path $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
Write-Output 'S30 circular image editor and HALCON ring wall-thickness/concentricity smoke tests passed.'
