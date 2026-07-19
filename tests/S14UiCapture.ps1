param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$RotatedOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s14-rotated-feature.png'),
    [string]$PolygonOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s14-polygon-feature.png')
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

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s14-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'feature-product.png'
$bitmap = New-Object Drawing.Bitmap 900, 600
try {
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([Drawing.Color]::FromArgb(18, 25, 35))
        $graphics.FillRectangle([Drawing.Brushes]::DarkSlateBlue, 45, 42, 810, 516)
        $graphics.FillEllipse([Drawing.Brushes]::WhiteSmoke, 125, 120, 210, 210)
        $graphics.FillRectangle([Drawing.Brushes]::Gainsboro, 485, 120, 250, 160)
        $graphics.FillRectangle([Drawing.Brushes]::Silver, 520, 350, 150, 175)
        $graphics.DrawRectangle([Drawing.Pens]::DeepSkyBlue, 82, 82, 735, 430)
    }
    finally { $graphics.Dispose() }
    $bitmap.Save($imagePath, [Drawing.Imaging.ImageFormat]::Png)
}
finally { $bitmap.Dispose() }

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
$roiType = $assembly.GetType('HalconWinFormsDemo.Models.RoiData', $true)
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
$addRoiLayer = $mainType.GetMethod('AddRoiLayer', $private)
$selectRoiLayer = $mainType.GetMethod('SelectRoiLayer', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$roiLayerList = $mainType.GetField('RoiLayerList', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockTabs = $mainType.GetField('DockConfigurationTabs', $allFields).GetValue($window)
$dockFeaturePanel = $mainType.GetField('DockRegionFeaturePanel', $allFields).GetValue($window)
$contextCombo = $mainType.GetField('ImageContextComboBox', $allFields).GetValue($window)
$contextSource = $mainType.GetField('ImageContextSourceText', $allFields).GetValue($window)
$resultLegend = $mainType.GetField('OverlayStatusText', $allFields).GetValue($window)
$guidance = $mainType.GetField('RoiEditGuidanceText', $allFields).GetValue($window)

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
$feature = New-Tool 'RegionFeatureFilter' 'Part_Feature'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S14-VM-FEATURE-001'
$filter.Parameters.ImageFilterMaskWidth = 3
$filter.Parameters.ImageFilterMaskHeight = 3
$threshold.Parameters.ImageThresholdMinGray = 150
$threshold.Parameters.ImageThresholdMaxGray = 255
$morphology.Parameters.RegionMorphologyMode = 'OpeningCircle'
$morphology.Parameters.RegionMorphologyRadius = 2.5
$feature.Parameters.RegionFeature = 'Area'
$feature.Parameters.RegionFeatureMin = 20000
$feature.Parameters.RegionFeatureMax = 60000
$flow.Add($source); $flow.Add($filter); $flow.Add($threshold); $flow.Add($morphology); $flow.Add($feature)
$filter.SetInputBinding('Image', $source.ToolId, 'Image')
$threshold.SetInputBinding('Image', $filter.ToolId, 'Image')
$morphology.SetInputBinding('Region', $threshold.ToolId, 'Region')
$feature.SetInputBinding('Region', $morphology.ToolId, 'Region')
Invoke-Tool $source 'S14 UI source' | Out-Null
Invoke-Tool $filter 'S14 UI filter' | Out-Null
Invoke-Tool $threshold 'S14 UI threshold' | Out-Null
Invoke-Tool $morphology 'S14 UI morphology' | Out-Null
Invoke-Tool $feature 'S14 UI feature' | Out-Null
if ($feature.ResultCode -ne 'OK') { throw 'Feature filter did not produce a visible OK result.' }

$rotated = $roiType.GetMethod('CreateRotatedRectangle').Invoke($null, [object[]]@([double]300, [double]430, [double]0.35, [double]220, [double]115))
$polygon = $roiType.GetMethod('CreatePolygon', [Type[]]@([double[]], [double[]])).Invoke($null, [object[]]@([double[]]@(100, 145, 330, 390, 210), [double[]]@(105, 320, 350, 130, 70)))
$rotatedLayer = $addRoiLayer.Invoke($window, [object[]]@($rotated, 'Rotated_Search', $feature))
$polygonLayer = $addRoiLayer.Invoke($window, [object[]]@($polygon, 'Polygon_Search', $feature))
$rotated.Dispose(); $polygon.Dispose()

$flowToolList.SelectedItem = $feature
$contextCombo.SelectedValue = $contextType.GetField('ModuleInput').GetValue($null)
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 0
$roiLayerList.SelectedItem = $rotatedLayer
$roiLayerList.ScrollIntoView($rotatedLayer)
$selectRoiLayer.Invoke($window, [object[]]@($rotatedLayer)) | Out-Null
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

$flowToolList.SelectedItem = $feature
$contextCombo.SelectedValue = $contextType.GetField('ModuleInput').GetValue($null)
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 0
$roiLayerList.SelectedItem = $rotatedLayer
$roiLayerList.ScrollIntoView($rotatedLayer)
$selectRoiLayer.Invoke($window, [object[]]@($rotatedLayer)) | Out-Null
$refresh.Invoke($window, @()) | Out-Null
$dockFeaturePanel.BringIntoView()
Wait-Dispatcher 700
if ($dockFeaturePanel.Visibility.ToString() -ne 'Visible') { throw 'Region feature dock panel is not visible.' }
if (-not $contextSource.Text.Contains('Noise_Filter.Image')) { throw "Transitive feature image context is missing: $($contextSource.Text)" }
if (-not $resultLegend.Text.Contains('Part_Feature.Region')) { throw "Feature result legend is missing: $($resultLegend.Text)" }
if ([string]::IsNullOrWhiteSpace($guidance.Text)) { throw 'ROI edit guidance is missing.' }
$rotatedSize = Save-WindowCapture $RotatedOutputPath

$roiLayerList.SelectedItem = $polygonLayer
$roiLayerList.ScrollIntoView($polygonLayer)
$selectRoiLayer.Invoke($window, [object[]]@($polygonLayer)) | Out-Null
$refresh.Invoke($window, @()) | Out-Null
Wait-Dispatcher 650
$polygonSize = Save-WindowCapture $PolygonOutputPath

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
$app.Shutdown()
Write-Output ('S14_UI_CAPTURE=PASS; Rotated={0}; Polygon={1}; Window={2}/{3}; Context=TRANSITIVE_IMAGE; Overlay=FEATURE; Handles=ROTATE/AXES/VERTICES' -f $RotatedOutputPath, $PolygonOutputPath, $rotatedSize, $polygonSize)
