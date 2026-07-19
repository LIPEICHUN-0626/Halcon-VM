param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s09-image-context.png')
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

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s09-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'inspection-source.png'
$bitmap = New-Object Drawing.Bitmap 640, 420
try {
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([Drawing.Color]::FromArgb(28, 36, 50))
        $graphics.FillRectangle([Drawing.Brushes]::White, 150, 90, 280, 190)
        $graphics.FillEllipse([Drawing.Brushes]::DarkGray, 230, 140, 120, 90)
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
$constructorTypes = [Type[]]@([string])
$arguments = New-Object 'object[]' 1
$arguments[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor($constructorTypes).Invoke($arguments)
$logger = $loggerType.GetConstructor($constructorTypes).Invoke($arguments)
$windowConstructor = $mainType.GetConstructor(
    [Reflection.BindingFlags]'Instance,NonPublic',
    $null,
    [Type[]]@($recipeServiceType, $loggerType),
    $null)
$window = $windowConstructor.Invoke(@($recipeService, $logger))

$private = [Reflection.BindingFlags]'Instance,NonPublic'
$allFields = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$refresh = $mainType.GetMethod('RefreshUiState', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$contextCombo = $mainType.GetField('ImageContextComboBox', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$ioTab = $mainType.GetField('IoTab', $allFields).GetValue($window)

function New-Tool([string]$Kind, [string]$Name) {
    return $createTool.Invoke($window, @([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}

$source = New-Tool 'ImageSource' $null
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S09-DEMO-0001'
$blob = New-Tool 'Blob' $null
$blob.Parameters.BlobMinGray = 180
$blob.Parameters.BlobMaxGray = 255
$blob.Parameters.BlobMinArea = 100
$blob.SetInputBinding('Image', $source.ToolId, 'Image')
$flow.Add($source)
$flow.Add($blob)
$executeTool.Invoke($window, @($source, 'S09 UI capture')) | Out-Null
$executeTool.Invoke($window, @($blob, 'S09 UI capture')) | Out-Null
$flowToolList.SelectedItem = $blob
$contextCombo.SelectedValue = $contextType.GetField('ModuleInput').GetValue($null)
$rightTabs.SelectedItem = $ioTab
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

$frame = New-Object Windows.Threading.DispatcherFrame
$timer = New-Object Windows.Threading.DispatcherTimer
$timer.Interval = [TimeSpan]::FromSeconds(2)
$timer.Add_Tick({
    $timer.Stop()
    $frame.Continue = $false
})
$timer.Start()
[Windows.Threading.Dispatcher]::PushFrame($frame)

$presentationSource = [Windows.PresentationSource]::FromVisual($window)
$scale = $presentationSource.CompositionTarget.TransformToDevice
$resizeFrame = New-Object Windows.Threading.DispatcherFrame
$resizeTimer = New-Object Windows.Threading.DispatcherTimer
$resizeTimer.Interval = [TimeSpan]::FromSeconds(1)
$resizeTimer.Add_Tick({
    $resizeTimer.Stop()
    $resizeFrame.Continue = $false
})
$resizeTimer.Start()
[Windows.Threading.Dispatcher]::PushFrame($resizeFrame)
$flowToolList.SelectedItem = $blob
$contextCombo.SelectedValue = $contextType.GetField('ModuleInput').GetValue($null)
$rightTabs.SelectedItem = $ioTab
$refresh.Invoke($window, @()) | Out-Null
$imageWindowControl = $mainType.GetField('imageWindow', $private).GetValue($window)
$mouseMoveMethod = $mainType.GetMethod('ImageWindow_MouseMove', $private)
$mouseArguments = New-Object 'object[]' 2
$mouseArguments[0] = $imageWindowControl
$mouseArguments[1] = [Windows.Forms.MouseEventArgs]::new(
    [Windows.Forms.MouseButtons]::None,
    0,
    [int]($imageWindowControl.ClientSize.Width / 2),
    [int]($imageWindowControl.ClientSize.Height / 2),
    0)
$mouseMoveMethod.Invoke($window, $mouseArguments) | Out-Null
$pixelText = $mainType.GetField('ImagePixelStatusText', $private).GetValue($window).Text
$pixelFrame = New-Object Windows.Threading.DispatcherFrame
$pixelTimer = New-Object Windows.Threading.DispatcherTimer
$pixelTimer.Interval = [TimeSpan]::FromMilliseconds(200)
$pixelTimer.Add_Tick({
    $pixelTimer.Stop()
    $pixelFrame.Continue = $false
})
$pixelTimer.Start()
[Windows.Threading.Dispatcher]::PushFrame($pixelFrame)
$window.Topmost = $false

$scale = $presentationSource.CompositionTarget.TransformToDevice
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

$closeConfirmed = $mainType.GetField('recipeCloseConfirmed', $private)
$closeConfirmed.SetValue($window, $true)
$window.Close()
$app.Shutdown()
Write-Output "S09_UI_CAPTURE=PASS; $OutputPath; ${width}x${height}; $pixelText"
