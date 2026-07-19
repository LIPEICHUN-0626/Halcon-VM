param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s12-toolbox-threshold.png')
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

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s12-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'threshold-product.png'
$bitmap = New-Object Drawing.Bitmap 760, 500
try {
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([Drawing.Color]::FromArgb(24, 32, 44))
        $graphics.FillRectangle([Drawing.Brushes]::DarkSlateBlue, 55, 45, 650, 410)
        $graphics.FillEllipse([Drawing.Brushes]::WhiteSmoke, 145, 120, 175, 175)
        $graphics.FillRectangle([Drawing.Brushes]::Gainsboro, 430, 155, 175, 205)
        $graphics.DrawRectangle([Drawing.Pens]::DeepSkyBlue, 90, 80, 575, 335)
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
$constructorTypes = [Type[]]@([string])
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor($constructorTypes).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor($constructorTypes).Invoke($constructorArgs)
$windowConstructor = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null)
$window = $windowConstructor.Invoke([object[]]@($recipeService, $logger))

$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$favorites = $mainType.GetField('favoriteToolKinds', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$recordUsage = $mainType.GetMethod('RecordToolCatalogUsage', $private)
$refresh = $mainType.GetMethod('RefreshUiState', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockTabs = $mainType.GetField('DockConfigurationTabs', $allFields).GetValue($window)
$dockThresholdPanel = $mainType.GetField('DockImageThresholdPanel', $allFields).GetValue($window)
$contextCombo = $mainType.GetField('ImageContextComboBox', $allFields).GetValue($window)
$catalogList = $mainType.GetField('ToolCatalogList', $allFields).GetValue($window)

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
function New-Tool([string]$Kind, [string]$Name) {
    return $createTool.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}

$source = New-Tool 'ImageSource' 'Product_Image'
$filter = New-Tool 'ImageFilter' 'Noise_Filter'
$threshold = New-Tool 'ImageThreshold' 'Bright_Parts'
$gray = New-Tool 'GrayStat' 'Gray_In_Parts'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S12-VM-THRESHOLD-001'
$filter.Parameters.ImageFilterMaskWidth = 3
$filter.Parameters.ImageFilterMaskHeight = 3
$threshold.Parameters.ImageThresholdMinGray = 170
$threshold.Parameters.ImageThresholdMaxGray = 255
$gray.Parameters.GrayMin = 150
$gray.Parameters.GrayMax = 255
$filter.SetInputBinding('Image', $source.ToolId, 'Image')
$threshold.SetInputBinding('Image', $filter.ToolId, 'Image')
$gray.SetInputBinding('Image', $filter.ToolId, 'Image')
$gray.SetInputBinding('ROI', $threshold.ToolId, 'Region')
$flow.Add($source); $flow.Add($filter); $flow.Add($threshold); $flow.Add($gray)
$executeTool.Invoke($window, [object[]]@($source, 'S12 UI source')) | Out-Null
$executeTool.Invoke($window, [object[]]@($filter, 'S12 UI filter')) | Out-Null
$executeTool.Invoke($window, [object[]]@($threshold, 'S12 UI threshold')) | Out-Null
$executeTool.Invoke($window, [object[]]@($gray, 'S12 UI gray')) | Out-Null

$thresholdKind = [Enum]::Parse($toolKindType, 'ImageThreshold')
$favorites.Add($thresholdKind) | Out-Null
$recordUsage.Invoke($window, [object[]]@($thresholdKind)) | Out-Null
$flowToolList.SelectedItem = $threshold
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

function Wait-Dispatcher([int]$Milliseconds) {
    $frame = New-Object Windows.Threading.DispatcherFrame
    $timer = New-Object Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds)
    $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false })
    $timer.Start()
    [Windows.Threading.Dispatcher]::PushFrame($frame)
}

Wait-Dispatcher 2200
$flowToolList.SelectedItem = $threshold
$contextCombo.SelectedValue = $contextType.GetField('ModuleInput').GetValue($null)
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 0
$refresh.Invoke($window, @()) | Out-Null
$dockThresholdPanel.BringIntoView()
$thresholdCatalogItem = $catalogList.Items | Where-Object Kind -eq $thresholdKind | Select-Object -First 1
$catalogList.ScrollIntoView($thresholdCatalogItem)
Wait-Dispatcher 450

if ($dockThresholdPanel.Visibility.ToString() -ne 'Visible') { throw 'Threshold dock panel is not visible.' }
if ($catalogList.Items.Count -lt 10) { throw 'The categorized toolbox did not expose the expected tools.' }

$sourceVisual = [Windows.PresentationSource]::FromVisual($window)
$scale = $sourceVisual.CompositionTarget.TransformToDevice
$x = [int][Math]::Round($window.Left * $scale.M11)
$y = [int][Math]::Round($window.Top * $scale.M22)
$width = [int][Math]::Round($window.ActualWidth * $scale.M11)
$height = [int][Math]::Round($window.ActualHeight * $scale.M22)
$capture = New-Object Drawing.Bitmap $width, $height
try {
    $graphics = [Drawing.Graphics]::FromImage($capture)
    try {
        $graphics.CopyFromScreen($x, $y, 0, 0, [Drawing.Size]::new($width, $height))
    }
    finally {
        $graphics.Dispose()
    }
    $capture.Save($OutputPath, [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $capture.Dispose()
}

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
$app.Shutdown()
Write-Output "S12_UI_CAPTURE=PASS; $OutputPath; ${width}x${height}; TOOLBOX=CATEGORY+FAVORITE+RECENT; THRESHOLD=VISIBLE; REGION=OVERLAY"
