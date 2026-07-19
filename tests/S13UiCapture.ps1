param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$NormalOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s13-image-workspace-normal.png'),
    [string]$ExpandedOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s13-image-workspace-expanded.png')
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) {
    throw 'Run this UI capture with powershell.exe -STA.'
}

$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s13-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'morphology-product.png'
$bitmap = New-Object Drawing.Bitmap 900, 600
try {
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([Drawing.Color]::FromArgb(20, 27, 38))
        $graphics.FillRectangle([Drawing.Brushes]::DarkSlateBlue, 70, 55, 760, 490)
        $graphics.FillEllipse([Drawing.Brushes]::WhiteSmoke, 155, 135, 210, 210)
        $graphics.FillRectangle([Drawing.Brushes]::Gainsboro, 505, 160, 205, 250)
        $graphics.FillEllipse([Drawing.Brushes]::DarkSlateBlue, 230, 210, 45, 45)
        $graphics.FillEllipse([Drawing.Brushes]::DarkSlateBlue, 575, 245, 55, 55)
        $graphics.DrawRectangle([Drawing.Pens]::DeepSkyBlue, 105, 90, 690, 400)
    }
    finally {
        $graphics.Dispose()
    }
    $bitmap.Save($imagePath, [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $bitmap.Dispose()
}

$app = [Windows.Application]::Current
if ($null -eq $app) {
    $app = New-Object Windows.Application
    $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown
}

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$contextType = $assembly.GetType('HalconWinFormsDemo.Models.VmImageContextOption', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$allFields = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$windowConstructor = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null)
$window = $windowConstructor.Invoke([object[]]@($recipeService, $logger))

$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$refresh = $mainType.GetMethod('RefreshUiState', $private)
$expandImage = $mainType.GetMethod('ImageWorkspaceExpandButton_Click', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockTabs = $mainType.GetField('DockConfigurationTabs', $allFields).GetValue($window)
$dockMorphPanel = $mainType.GetField('DockRegionMorphologyPanel', $allFields).GetValue($window)
$contextCombo = $mainType.GetField('ImageContextComboBox', $allFields).GetValue($window)
$contextSource = $mainType.GetField('ImageContextSourceText', $allFields).GetValue($window)
$resultLegend = $mainType.GetField('OverlayStatusText', $allFields).GetValue($window)
$halconHost = $mainType.GetField('HalconHost', $allFields).GetValue($window)
$expandButton = $mainType.GetField('ImageWorkspaceExpandButton', $allFields).GetValue($window)
$showRoi = $mainType.GetField('ShowRoiOverlayCheckBox', $allFields).GetValue($window)
$showResult = $mainType.GetField('ShowResultOverlayCheckBox', $allFields).GetValue($window)

function New-Tool([string]$Kind, [string]$Name) {
    return $createTool.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}

function Invoke-Tool($Tool, [string]$Label) {
    try { return $executeTool.Invoke($window, [object[]]@($Tool, $Label)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
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
    $sourceVisual = [Windows.PresentationSource]::FromVisual($window)
    $scale = $sourceVisual.CompositionTarget.TransformToDevice
    $x = [int][Math]::Round($window.Left * $scale.M11)
    $y = [int][Math]::Round($window.Top * $scale.M22)
    $width = [int][Math]::Round($window.ActualWidth * $scale.M11)
    $height = [int][Math]::Round($window.ActualHeight * $scale.M22)
    $capture = New-Object Drawing.Bitmap $width, $height
    try {
        $graphics = [Drawing.Graphics]::FromImage($capture)
        try { $graphics.CopyFromScreen($x, $y, 0, 0, [Drawing.Size]::new($width, $height)) }
        finally { $graphics.Dispose() }
        $capture.Save($Path, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $capture.Dispose() }
    return "${width}x${height}"
}

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool 'ImageSource' 'Product_Image'
$filter = New-Tool 'ImageFilter' 'Noise_Filter'
$threshold = New-Tool 'ImageThreshold' 'Bright_Region'
$morphology = New-Tool 'RegionMorphology' 'Region_Cleanup'
$gray = New-Tool 'GrayStat' 'Gray_In_Cleanup'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S13-VM-MORPH-001'
$filter.Parameters.ImageFilterMaskWidth = 3
$filter.Parameters.ImageFilterMaskHeight = 3
$threshold.Parameters.ImageThresholdMinGray = 150
$threshold.Parameters.ImageThresholdMaxGray = 255
$morphology.Parameters.RegionMorphologyMode = 'ClosingCircle'
$morphology.Parameters.RegionMorphologyRadius = 7.5
$gray.Parameters.GrayMin = 120
$gray.Parameters.GrayMax = 255
$flow.Add($source); $flow.Add($filter); $flow.Add($threshold); $flow.Add($morphology); $flow.Add($gray)
$filter.SetInputBinding('Image', $source.ToolId, 'Image')
$threshold.SetInputBinding('Image', $filter.ToolId, 'Image')
$morphology.SetInputBinding('Region', $threshold.ToolId, 'Region')
$gray.SetInputBinding('Image', $filter.ToolId, 'Image')
$gray.SetInputBinding('ROI', $morphology.ToolId, 'Region')
Invoke-Tool $source 'S13 UI source' | Out-Null
Invoke-Tool $filter 'S13 UI filter' | Out-Null
Invoke-Tool $threshold 'S13 UI threshold' | Out-Null
Invoke-Tool $morphology 'S13 UI morphology' | Out-Null

$flowToolList.SelectedItem = $morphology
$contextCombo.SelectedValue = $contextType.GetField('ModuleInput').GetValue($null)
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 0

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

$flowToolList.SelectedItem = $morphology
$contextCombo.SelectedValue = $contextType.GetField('ModuleInput').GetValue($null)
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 0
$refresh.Invoke($window, @()) | Out-Null
$dockMorphPanel.BringIntoView()
Wait-Dispatcher 550

if ($dockMorphPanel.Visibility.ToString() -ne 'Visible') { throw 'Morphology dock panel is not visible.' }
if (-not $contextSource.Text.Contains('Noise_Filter.Image')) { throw "Transitive morphology image context is missing: $($contextSource.Text)" }
if (-not $resultLegend.Text.Contains('Region_Cleanup.Region')) { throw "Morphology result legend is missing: $($resultLegend.Text)" }
if ($showRoi.IsChecked -ne $true -or $showResult.IsChecked -ne $true) { throw 'Independent ROI/result overlay switches are not visible and enabled.' }

$normalHostHeight = $halconHost.ActualHeight
$normalSize = Save-WindowCapture $NormalOutputPath
$expandImage.Invoke($window, [object[]]@($expandButton, [Windows.RoutedEventArgs]::new())) | Out-Null
Wait-Dispatcher 900
$refresh.Invoke($window, @()) | Out-Null
Wait-Dispatcher 350
$expandedHostHeight = $halconHost.ActualHeight
$expandedSize = Save-WindowCapture $ExpandedOutputPath
if ($expandedHostHeight -le ($normalHostHeight + 100)) { throw "Image workspace did not materially grow: $normalHostHeight -> $expandedHostHeight" }

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
$app.Shutdown()
Write-Output ('S13_UI_CAPTURE=PASS; Normal={0}; Expanded={1}; Window={2}/{3}; HostHeight={4:F0}->{5:F0}; Context=TRANSITIVE_IMAGE; Overlay=ROI+RESULT; Legend=MORPHOLOGY' -f $NormalOutputPath, $ExpandedOutputPath, $normalSize, $expandedSize, $normalHostHeight, $expandedHostHeight)
