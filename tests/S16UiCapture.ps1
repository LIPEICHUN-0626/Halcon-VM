param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s16-flow-graph-image-enhancement.png')
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) {
    throw 'Run this UI capture with powershell.exe -STA.'
}

$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class S16NativeCapture
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
}
'@
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function New-FixtureImage([string]$Path) {
    $bitmap = New-Object Drawing.Bitmap 720, 500
    try {
        $graphics = [Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([Drawing.Color]::FromArgb(28, 35, 46))
            $brushA = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(90, 100, 120))
            $brushB = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(185, 195, 215))
            try {
                $graphics.FillRectangle($brushA, 90, 70, 520, 340)
                $graphics.FillEllipse($brushB, 245, 135, 230, 230)
            }
            finally { $brushA.Dispose(); $brushB.Dispose() }
        }
        finally { $graphics.Dispose() }
        $bitmap.Save($Path, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $bitmap.Dispose() }
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
    try {
        $encoder.Save($stream)
    }
    finally { $stream.Dispose() }
    return "${width}x${height}"
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s16-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'contrast-source.png'
New-FixtureImage $imagePath

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
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$flowZoom = $mainType.GetField('FlowZoomSlider', $allFields).GetValue($window)
$graphViewport = $mainType.GetField('FlowGraphViewport', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockTabs = $mainType.GetField('DockConfigurationTabs', $allFields).GetValue($window)
$dockPanel = $mainType.GetField('DockImageEnhancementPanel', $allFields).GetValue($window)
$dockMode = $mainType.GetField('DockImageEnhancementModeComboBox', $allFields).GetValue($window)

function New-Tool([string]$Kind, [string]$Name) {
    return $createTool.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}

function Invoke-Tool($Tool, [string]$Label) {
    try { return $executeTool.Invoke($window, [object[]]@($Tool, $Label)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool 'ImageSource' 'Product_Image'
$enhance = New-Tool 'ImageEnhancement' 'Local_Contrast'
$threshold = New-Tool 'ImageThreshold' 'Defect_Region'
$morphology = New-Tool 'RegionMorphology' 'Region_Cleanup'
$blob = New-Tool 'Blob' 'Defect_Blob'
$gray = New-Tool 'GrayStat' 'Contrast_Gray'
$edge = New-Tool 'EdgeMeasure' 'Contour_Edge'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S16-UI'
$enhance.Parameters.ImageEnhancementMode = 'LocalContrast'
$enhance.Parameters.ImageEnhancementMaskWidth = 9
$enhance.Parameters.ImageEnhancementMaskHeight = 9
$enhance.Parameters.ImageEnhancementFactor = 1.8
$threshold.Parameters.ImageThresholdMinGray = 160
$threshold.Parameters.ImageThresholdMaxGray = 255
$morphology.Parameters.RegionMorphologyMode = 'ClosingCircle'
$morphology.Parameters.RegionMorphologyRadius = 3.5
$blob.Parameters.BlobMinGray = 150
$blob.Parameters.BlobMaxGray = 255
$blob.Parameters.BlobMinArea = 100
$gray.Parameters.GrayMin = 0
$gray.Parameters.GrayMax = 255
$edge.Parameters.EdgeThreshold = 8
foreach ($tool in @($source, $enhance, $threshold, $morphology, $blob, $gray, $edge)) { $flow.Add($tool) }
$enhance.SetInputBinding('Image', $source.ToolId, 'Image')
$threshold.SetInputBinding('Image', $enhance.ToolId, 'Image')
$morphology.SetInputBinding('Region', $threshold.ToolId, 'Region')
foreach ($consumer in @($blob, $gray, $edge)) {
    $consumer.SetInputBinding('Image', $enhance.ToolId, 'Image')
    $consumer.SetInputBinding('ROI', $morphology.ToolId, 'Region')
}
foreach ($tool in @($source, $enhance, $threshold, $morphology, $blob, $gray, $edge)) { Invoke-Tool $tool ('S16 UI ' + $tool.InstanceName) | Out-Null }
Invoke-Tool $enhance 'S16 UI selected output' | Out-Null

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

$flowToolList.SelectedItem = $enhance
$flowZoom.Value = 0.4
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 0
$refresh.Invoke($window, @()) | Out-Null
$dockScroller = $dockTabs.SelectedContent -as [Windows.Controls.ScrollViewer]
if ($null -ne $dockScroller) { $dockScroller.ScrollToVerticalOffset(125) }
$graphViewport.ScrollToHorizontalOffset(0)
$graphViewport.ScrollToVerticalOffset(0)
Wait-Dispatcher 1200

if ($graphViewport.Visibility.ToString() -ne 'Visible') { throw 'Connection canvas is not visible.' }
if ($dockPanel.Visibility.ToString() -ne 'Visible') { throw 'Image enhancement dock panel is not visible.' }
if ($connections.Count -ne 9) { throw "Expected 9 connection paths, got $($connections.Count)." }
if ($enhance.ResultCode -ne 'OK') { throw 'Image enhancement output is not OK.' }
$size = Save-WindowCapture $OutputPath

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
$app.Shutdown()
Write-Output ('S16_UI_CAPTURE=PASS; Path={0}; Window={1}; Graph=DEPENDENCY_LAYERS/TYPED_PATHS/BRANCH; Dock=IMAGE_ENHANCEMENT; HALCON=EMPHASIZE; Output=OK' -f $OutputPath, $size)
