param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s15-flow-ports-region-set.png')
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
public static class S15NativeCapture
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

function New-FixtureImage([string]$Path, [int]$Left, [int]$Top, [int]$Right, [int]$Bottom) {
    $bitmap = New-Object Drawing.Bitmap 600, 420
    try {
        $graphics = [Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([Drawing.Color]::FromArgb(18, 25, 35))
            $brush = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(235, 235, 235))
            try { $graphics.FillRectangle($brush, $Left, $Top, ($Right - $Left + 1), ($Bottom - $Top + 1)) }
            finally { $brush.Dispose() }
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
    $helper = New-Object Windows.Interop.WindowInteropHelper($window)
    $handle = $helper.Handle
    $rect = New-Object S15NativeCapture+Rect
    if (-not [S15NativeCapture]::GetWindowRect($handle, [ref]$rect)) { throw 'GetWindowRect failed.' }
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    $capture = New-Object Drawing.Bitmap $width, $height
    try {
        $graphics = [Drawing.Graphics]::FromImage($capture)
        $hdc = $graphics.GetHdc()
        try {
            if (-not [S15NativeCapture]::PrintWindow($handle, $hdc, 2)) { throw 'PrintWindow failed.' }
        }
        finally {
            $graphics.ReleaseHdc($hdc)
            $graphics.Dispose()
        }
        $capture.Save($Path, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $capture.Dispose() }
    return "${width}x${height}"
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s15-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imageA = Join-Path $tempRoot 'source-a.png'
$imageB = Join-Path $tempRoot 'source-b.png'
New-FixtureImage $imageA 80 70 330 300
New-FixtureImage $imageB 230 140 520 350

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
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$refresh = $mainType.GetMethod('RefreshUiState', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$flowZoom = $mainType.GetField('FlowZoomSlider', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockTabs = $mainType.GetField('DockConfigurationTabs', $allFields).GetValue($window)
$dockSetPanel = $mainType.GetField('DockRegionSetOperationPanel', $allFields).GetValue($window)
$contextSource = $mainType.GetField('ImageContextSourceText', $allFields).GetValue($window)
$overlayStatus = $mainType.GetField('OverlayStatusText', $allFields).GetValue($window)

function New-Tool([string]$Kind, [string]$Name) {
    return $createTool.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}

function Invoke-Tool($Tool, [string]$Label) {
    try { return $executeTool.Invoke($window, [object[]]@($Tool, $Label)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$sourceA = New-Tool 'ImageSource' 'Product_A'
$sourceB = New-Tool 'ImageSource' 'Product_B'
$thresholdA = New-Tool 'ImageThreshold' 'Region_A'
$thresholdB = New-Tool 'ImageThreshold' 'Region_B'
$setTool = New-Tool 'RegionSetOperation' 'Region_Convergence'
$feature = New-Tool 'RegionFeatureFilter' 'Final_Feature'
$sourceA.Parameters.LocalImagePath = $imageA
$sourceB.Parameters.LocalImagePath = $imageB
$thresholdA.Parameters.ImageThresholdMinGray = 150
$thresholdA.Parameters.ImageThresholdMaxGray = 255
$thresholdB.Parameters.ImageThresholdMinGray = 150
$thresholdB.Parameters.ImageThresholdMaxGray = 255
$setTool.Parameters.RegionSetOperationMode = 'Intersection'
$feature.Parameters.RegionFeature = 'Area'
$feature.Parameters.RegionFeatureMin = 1000
$feature.Parameters.RegionFeatureMax = 100000
foreach ($tool in @($sourceA, $sourceB, $thresholdA, $thresholdB, $setTool, $feature)) { $flow.Add($tool) }
$thresholdA.SetInputBinding('Image', $sourceA.ToolId, 'Image')
$thresholdB.SetInputBinding('Image', $sourceB.ToolId, 'Image')
$setTool.SetInputBinding('RegionA', $thresholdA.ToolId, 'Region')
$setTool.SetInputBinding('RegionB', $thresholdB.ToolId, 'Region')
$feature.SetInputBinding('Region', $setTool.ToolId, 'Region')
foreach ($tool in @($sourceA, $sourceB, $thresholdA, $thresholdB, $setTool, $feature)) { Invoke-Tool $tool ('S15 UI ' + $tool.InstanceName) | Out-Null }
if ($setTool.ResultCode -ne 'OK') { throw 'Region set operation did not produce a visible OK result.' }
Invoke-Tool $setTool 'S15 UI selected overlay' | Out-Null

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.WindowState = [Windows.WindowState]::Normal
$window.Left = 5
$window.Top = 5
$window.Width = 1600
$window.Height = 930
$window.Topmost = $true
$window.Show()
$window.Activate() | Out-Null
Wait-Dispatcher 2300

$flowZoom.Value = 0.65
$flowToolList.SelectedItem = $setTool
$flowToolList.ScrollIntoView($setTool)
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 0
$refresh.Invoke($window, @()) | Out-Null
$dockSetPanel.BringIntoView()
Wait-Dispatcher 1000

if ($dockSetPanel.Visibility.ToString() -ne 'Visible') { throw 'Region set dock panel is not visible.' }
if ($setTool.FlowInputPorts.Count -ne 2) { throw 'Dual Region input chips are missing.' }
if (($setTool.FlowInputPorts | Where-Object StateKey -eq 'Ready').Count -ne 2) { throw 'Dual Region inputs are not visibly ready.' }
if ($thresholdA.DependencyState -ne 'Upstream' -or $thresholdB.DependencyState -ne 'Upstream' -or $feature.DependencyState -ne 'Downstream') { throw 'Direct dependency highlighting is incorrect.' }
if (-not $contextSource.Text.Contains('Product_A.Image')) { throw "Transitive image context is missing: $($contextSource.Text)" }
if (-not $overlayStatus.Text.Contains('Region_Convergence.Region')) { throw "Set-operation overlay legend is missing: $($overlayStatus.Text)" }
$size = Save-WindowCapture $OutputPath

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
$app.Shutdown()
Write-Output ('S15_UI_CAPTURE=PASS; Path={0}; Window={1}; Ports=DUAL_REGION; Dependencies=UPSTREAM/DOWNSTREAM; Dock=SET_OPERATION; Context=TRANSITIVE_IMAGE; Overlay=REGION_SET' -f $OutputPath, $size)
