param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s10-docked-channel.png')
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) {
    throw 'Run this UI capture with powershell.exe -STA.'
}

$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s10-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'rgb-inspection.png'
$bitmap = New-Object Drawing.Bitmap 640, 420
try {
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([Drawing.Color]::FromArgb(22, 38, 74))
        $graphics.FillRectangle([Drawing.Brushes]::OrangeRed, 130, 80, 170, 240)
        $graphics.FillRectangle([Drawing.Brushes]::LimeGreen, 340, 120, 180, 180)
        $graphics.DrawRectangle([Drawing.Pens]::White, 105, 55, 440, 300)
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
$channelModeType = $assembly.GetType('HalconWinFormsDemo.Models.VmImageChannelMode', $true)
$contextType = $assembly.GetType('HalconWinFormsDemo.Models.VmImageContextOption', $true)
$ctorTypes = [Type[]]@([string])
$ctorArgs = New-Object 'object[]' 1
$ctorArgs[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor($ctorTypes).Invoke($ctorArgs)
$logger = $loggerType.GetConstructor($ctorTypes).Invoke($ctorArgs)
$windowCtor = $mainType.GetConstructor([Reflection.BindingFlags]'Instance,NonPublic', $null, [Type[]]@($recipeServiceType, $loggerType), $null)
$window = $windowCtor.Invoke(@($recipeService, $logger))

$private = [Reflection.BindingFlags]'Instance,NonPublic'
$allFields = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$refresh = $mainType.GetMethod('RefreshUiState', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$contextCombo = $mainType.GetField('ImageContextComboBox', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockTabs = $mainType.GetField('DockConfigurationTabs', $allFields).GetValue($window)
$dockChannelPanel = $mainType.GetField('DockImageChannelPanel', $allFields).GetValue($window)

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
function New-Tool([string]$Kind) {
    return $createTool.Invoke($window, @([Enum]::Parse($toolKindType, $Kind), $null, $true, $null))
}

$source = New-Tool 'ImageSource'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S10-RGB-0001'
$channel = New-Tool 'ImageChannel'
$channel.Parameters.ImageChannelMode = [string]$channelModeType.GetField('ToGray').GetValue($null)
$channel.Parameters.ImageChannelIndex = 1
$channel.SetInputBinding('Image', $source.ToolId, 'Image')
$blob = New-Tool 'Blob'
$blob.Parameters.BlobMinGray = 40
$blob.Parameters.BlobMaxGray = 255
$blob.Parameters.BlobMinArea = 200
$blob.SetInputBinding('Image', $channel.ToolId, 'Image')
$flow.Add($source)
$flow.Add($channel)
$flow.Add($blob)
$executeTool.Invoke($window, @($source, 'S10 UI source')) | Out-Null
$executeTool.Invoke($window, @($channel, 'S10 UI channel')) | Out-Null
$executeTool.Invoke($window, @($blob, 'S10 UI blob')) | Out-Null
$flowToolList.SelectedItem = $channel
$contextCombo.SelectedValue = $contextType.GetField('ModuleOutput').GetValue($null)
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 0
$refresh.Invoke($window, @()) | Out-Null

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.WindowState = [Windows.WindowState]::Normal
$window.Left = 10
$window.Top = 10
$window.Width = 1280
$window.Height = 760
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
$flowToolList.SelectedItem = $channel
$contextCombo.SelectedValue = $contextType.GetField('ModuleOutput').GetValue($null)
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 0
$refresh.Invoke($window, @()) | Out-Null
$dockChannelPanel.BringIntoView()
Wait-Dispatcher 250

$imageWindow = $mainType.GetField('imageWindow', $private).GetValue($window)
$mouseMove = $mainType.GetMethod('ImageWindow_MouseMove', $private)
$mouseMove.Invoke($window, @($imageWindow, [Windows.Forms.MouseEventArgs]::new([Windows.Forms.MouseButtons]::None, 0, [int]($imageWindow.ClientSize.Width / 2), [int]($imageWindow.ClientSize.Height / 2), 0))) | Out-Null
Wait-Dispatcher 200
$pixelText = $mainType.GetField('ImagePixelStatusText', $allFields).GetValue($window).Text

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
Write-Output "S10_UI_CAPTURE=PASS; $OutputPath; ${width}x${height}; $pixelText"
