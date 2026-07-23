param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s17-connection-inspector-dynamic-threshold.png')
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) {
    throw 'Run this UI capture with powershell.exe -STA.'
}

$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Write-Pgm([string]$Path, [int]$Width, [int]$Height, [scriptblock]$PixelFactory) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) {
            $pixels[$row * $Width + $column] = [byte](& $PixelFactory $row $column)
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

function Wait-Dispatcher([int]$Milliseconds) {
    $frame = New-Object Windows.Threading.DispatcherFrame
    $timer = New-Object Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds)
    $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false })
    $timer.Start()
    [Windows.Threading.Dispatcher]::PushFrame($frame)
}

function Save-WindowCapture([string]$Path) {
    $window.UpdateLayout()
    $width = [Math]::Max(1, [int][Math]::Ceiling($window.ActualWidth))
    $height = [Math]::Max(1, [int][Math]::Ceiling($window.ActualHeight))
    $capture = [Activator]::CreateInstance(
        [Windows.Media.Imaging.RenderTargetBitmap],
        [object[]]@($width, $height, 96.0, 96.0, [Windows.Media.PixelFormats]::Pbgra32))
    $capture.Render($window)
    $encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($capture))
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Create, [IO.FileAccess]::Write)
    try { $encoder.Save($stream) }
    finally { $stream.Dispose() }
    return "${width}x${height}"
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s17-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$originalPath = Join-Path $tempRoot 'original.pgm'
$referencePath = Join-Path $tempRoot 'reference.pgm'
Write-Pgm $originalPath 640 420 {
    param($row, $column)
    if ($row -ge 90 -and $row -le 229 -and $column -ge 120 -and $column -le 319) { return 190 }
    if ($row -ge 240 -and $row -le 349 -and $column -ge 390 -and $column -le 559) { return 45 }
    return 105
}
Write-Pgm $referencePath 640 420 { param($row, $column) return 105 }

$app = [Windows.Application]::Current
if ($null -eq $app) {
    $app = New-Object Windows.Application
    $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown
}

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$allFields = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$args = New-Object 'object[]' 1
$args[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($args)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$windowConstructor = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null)
$window = $windowConstructor.Invoke([object[]]@($recipeService, $logger))

$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$connections = $mainType.GetField('flowConnections', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$refresh = $mainType.GetMethod('RefreshUiState', $private)
$selectConnection = $mainType.GetMethod('SelectFlowConnection', $private)
$openTargetInput = $mainType.GetMethod('OpenFlowConnectionTargetInputButton_Click', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$flowZoom = $mainType.GetField('FlowZoomSlider', $allFields).GetValue($window)
$graphViewport = $mainType.GetField('FlowGraphViewport', $allFields).GetValue($window)
$inspector = $mainType.GetField('FlowConnectionInspectorBorder', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockTabs = $mainType.GetField('DockConfigurationTabs', $allFields).GetValue($window)
$dockPanel = $mainType.GetField('DockDynamicThresholdPanel', $allFields).GetValue($window)

function New-Tool([string]$Kind, [string]$Name) {
    return $createTool.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}

function Invoke-Tool($Tool, [string]$Label) {
    try { return $executeTool.Invoke($window, [object[]]@($Tool, $Label)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$original = New-Tool 'ImageSource' 'Original_Image'
$reference = New-Tool 'ImageSource' 'Local_Reference'
$dynamic = New-Tool 'DynamicThreshold' 'Bright_Defect_Dynamic'
$morphology = New-Tool 'RegionMorphology' 'Defect_Cleanup'
$gray = New-Tool 'GrayStat' 'Defect_Gray'
$edge = New-Tool 'EdgeMeasure' 'Defect_Contour'
$original.Parameters.LocalImagePath = $originalPath
$original.Parameters.LocalImageSerialNumber = 'S17-UI-ORIGINAL'
$reference.Parameters.LocalImagePath = $referencePath
$reference.Parameters.LocalImageSerialNumber = 'S17-UI-REFERENCE'
$dynamic.Parameters.DynamicThresholdPolarity = 'Light'
$dynamic.Parameters.DynamicThresholdOffset = 25
$morphology.Parameters.RegionMorphologyMode = 'ClosingCircle'
$morphology.Parameters.RegionMorphologyRadius = 2.5
$gray.Parameters.GrayMin = 0
$gray.Parameters.GrayMax = 255
$edge.Parameters.EdgeThreshold = 8
foreach ($tool in @($original, $reference, $dynamic, $morphology, $gray, $edge)) { $flow.Add($tool) }
$dynamic.SetInputBinding('OriginalImage', $original.ToolId, 'Image')
$dynamic.SetInputBinding('ReferenceImage', $reference.ToolId, 'Image')
$morphology.SetInputBinding('Region', $dynamic.ToolId, 'Region')
foreach ($consumer in @($gray, $edge)) {
    $consumer.SetInputBinding('Image', $original.ToolId, 'Image')
    $consumer.SetInputBinding('ROI', $morphology.ToolId, 'Region')
}
foreach ($tool in @($original, $reference, $dynamic, $morphology, $gray, $edge)) { Invoke-Tool $tool ('S17 UI ' + $tool.InstanceName) | Out-Null }

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.WindowState = [Windows.WindowState]::Normal
$window.Left = 4
$window.Top = 4
$window.Width = 1660
$window.Height = 1100
$window.Topmost = $true
$window.Show()
$window.Activate() | Out-Null
Wait-Dispatcher 2200

$flowToolList.SelectedItem = $dynamic
$flowZoom.Value = 0.38
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 2
$refresh.Invoke($window, @()) | Out-Null
$selectedPath = $connections | Where-Object { $_.TargetToolId -eq $dynamic.ToolId -and $_.TargetPortName -eq 'OriginalImage' } | Select-Object -First 1
$selectConnection.Invoke($window, [object[]]@($selectedPath.PSObject.BaseObject)) | Out-Null
$openTargetInput.Invoke($window, @($null, $null)) | Out-Null
$graphViewport.ScrollToHorizontalOffset(0)
$graphViewport.ScrollToVerticalOffset(0)
Wait-Dispatcher 1200

if ($graphViewport.Visibility.ToString() -ne 'Visible') { throw 'Connection canvas is not visible.' }
if ($inspector.Visibility.ToString() -ne 'Visible') { throw 'Connection inspector is not visible.' }
if ($dockTabs.SelectedIndex -ne 2) { throw 'Target input page is not visible.' }
if (($connections | Where-Object IsSelected).Count -ne 1) { throw 'A real connection path is not selected.' }
if ($dynamic.ResultCode -ne 'OK') { throw 'Dynamic threshold output is not OK.' }
$inputSize = Save-WindowCapture $OutputPath

$parameterPath = [IO.Path]::Combine([IO.Path]::GetDirectoryName($OutputPath), [IO.Path]::GetFileNameWithoutExtension($OutputPath) + '-parameters.png')
$dockTabs.SelectedIndex = 0
$dockScroller = $dockTabs.SelectedContent -as [Windows.Controls.ScrollViewer]
if ($null -ne $dockScroller) { $dockScroller.ScrollToVerticalOffset(105) }
Wait-Dispatcher 700
if ($dockPanel.Visibility.ToString() -ne 'Visible') { throw 'Dynamic threshold parameter panel is not visible.' }
$parameterSize = Save-WindowCapture $parameterPath

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
$app.Shutdown()
Write-Output ('S17_UI_CAPTURE=PASS; InputPath={0}; ParameterPath={1}; Window={2}/{3}; Graph=SELECTED_TYPED_PATH/TWO_IMAGE_CONVERGENCE; Inspector=ENDPOINTS/TYPE/VALUE/ERROR/ACTIONS; Dock=INPUTS/DYNAMIC_THRESHOLD_PARAMS; HALCON=DYN_THRESHOLD; Output=OK' -f $OutputPath, $parameterPath, $inputSize, $parameterSize)
