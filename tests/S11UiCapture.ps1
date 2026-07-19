param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s11-roi-filter.png'),
    [string]$RoiOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s11-roi-workbench.png')
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

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s11-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'filter-inspection.png'
$bitmap = New-Object Drawing.Bitmap 760, 500
try {
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([Drawing.Color]::FromArgb(28, 34, 48))
        $graphics.FillRectangle([Drawing.Brushes]::SteelBlue, 90, 70, 580, 350)
        $graphics.FillEllipse([Drawing.Brushes]::Orange, 180, 135, 150, 150)
        $graphics.FillRectangle([Drawing.Brushes]::LimeGreen, 430, 155, 150, 185)
        $graphics.DrawRectangle([Drawing.Pens]::White, 140, 105, 480, 275)
        for ($index = 0; $index -lt 70; $index++) {
            $x = 100 + (($index * 83) % 550)
            $y = 80 + (($index * 47) % 330)
            $graphics.FillRectangle([Drawing.Brushes]::White, $x, $y, 2, 2)
        }
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
$filterModeType = $assembly.GetType('HalconWinFormsDemo.Models.VmImageFilterMode', $true)
$contextType = $assembly.GetType('HalconWinFormsDemo.Models.VmImageContextOption', $true)
$roiType = $assembly.GetType('HalconWinFormsDemo.Models.RoiData', $true)
$constructorTypes = [Type[]]@([string])
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor($constructorTypes).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor($constructorTypes).Invoke($constructorArgs)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$allFields = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$windowConstructor = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null)
$window = $windowConstructor.Invoke(@($recipeService, $logger))

$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$addRoiLayer = $mainType.GetMethod('AddRoiLayer', $private)
$selectMode = $mainType.GetMethod('SelectImageToolButton_Click', $private)
$refresh = $mainType.GetMethod('RefreshUiState', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$roiLayerList = $mainType.GetField('RoiLayerList', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$roiTab = $mainType.GetField('RoiTab', $allFields).GetValue($window)
$dockTabs = $mainType.GetField('DockConfigurationTabs', $allFields).GetValue($window)
$dockFilterPanel = $mainType.GetField('DockImageFilterPanel', $allFields).GetValue($window)
$contextCombo = $mainType.GetField('ImageContextComboBox', $allFields).GetValue($window)
$toggleRoiLockButton = $mainType.GetField('ToggleSelectedRoiLockButton', $allFields).GetValue($window)

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
function New-Tool([string]$Kind, [string]$Name) {
    return $createTool.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}

$source = New-Tool 'ImageSource' 'Product_Image'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S11-ROI-FILTER-001'
$filter = New-Tool 'ImageFilter' 'Noise_Filter'
$filter.Parameters.ImageFilterMode = [string]$filterModeType.GetField('Mean').GetValue($null)
$filter.Parameters.ImageFilterMaskWidth = 3
$filter.Parameters.ImageFilterMaskHeight = 3
$filter.SetInputBinding('Image', $source.ToolId, 'Image')
$blob = New-Tool 'Blob' 'Target_Region'
$blob.Parameters.BlobMinGray = 70
$blob.Parameters.BlobMaxGray = 255
$blob.Parameters.BlobMinArea = 100
$blob.SetInputBinding('Image', $filter.ToolId, 'Image')
$flow.Add($source); $flow.Add($filter); $flow.Add($blob)
$executeTool.Invoke($window, @($source, 'S11 UI source')) | Out-Null
$executeTool.Invoke($window, @($filter, 'S11 UI filter')) | Out-Null
$executeTool.Invoke($window, @($blob, 'S11 UI blob')) | Out-Null

$rectangle = $roiType.GetMethod('CreateRectangle').Invoke($null, [object[]]@([double]105, [double]140, [double]380, [double]620))
$circle = $roiType.GetMethod('CreateCircle').Invoke($null, [object[]]@([double]235, [double]255, [double]95))
try {
    $rectLayer = $addRoiLayer.Invoke($window, [object[]]@($rectangle, 'Inspection_ROI', $blob))
    $circleLayer = $addRoiLayer.Invoke($window, [object[]]@($circle, 'Locked_ROI', $blob))
}
finally {
    $rectangle.Dispose(); $circle.Dispose()
}
$circleLayer.IsLocked = $true
$roiLayerList.SelectedItem = $rectLayer
$flowToolList.SelectedItem = $filter
$contextCombo.SelectedValue = $contextType.GetField('ModuleOutput').GetValue($null)
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 0
$selectMode.Invoke($window, @($null, $null)) | Out-Null
$refresh.Invoke($window, @()) | Out-Null

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.WindowState = [Windows.WindowState]::Normal
$window.Left = 5
$window.Top = 5
$window.Width = 1580
$window.Height = 920
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
$flowToolList.SelectedItem = $filter
$roiLayerList.SelectedItem = $rectLayer
$contextCombo.SelectedValue = $contextType.GetField('ModuleOutput').GetValue($null)
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 0
$selectMode.Invoke($window, @($null, $null)) | Out-Null
$refresh.Invoke($window, @()) | Out-Null
$dockFilterPanel.BringIntoView()
Wait-Dispatcher 350

if ($dockFilterPanel.Visibility.ToString() -ne 'Visible') { throw 'ImageFilter dock panel is not visible.' }
if (-not [object]::ReferenceEquals($roiLayerList.SelectedItem, $rectLayer)) { throw 'Editable ROI is not selected.' }
if (-not $circleLayer.IsLocked) { throw 'Locked ROI state is not visible.' }

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

$rightTabs.SelectedItem = $roiTab
$roiLayerList.SelectedItem = $rectLayer
$refresh.Invoke($window, @()) | Out-Null
Wait-Dispatcher 120
$roiTab.Content.ScrollToVerticalOffset(95)
$toggleRoiLockButton.BringIntoView()
Wait-Dispatcher 300
$roiCapture = New-Object Drawing.Bitmap $width, $height
try {
    $graphics = [Drawing.Graphics]::FromImage($roiCapture)
    try {
        $graphics.CopyFromScreen($x, $y, 0, 0, [Drawing.Size]::new($width, $height))
    }
    finally {
        $graphics.Dispose()
    }
    $roiCapture.Save($RoiOutputPath, [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $roiCapture.Dispose()
}

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
$app.Shutdown()
Write-Output "S11_UI_CAPTURE=PASS; $OutputPath; $RoiOutputPath; ${width}x${height}; ROI=SELECTED+LOCKED; FILTER=VISIBLE"
