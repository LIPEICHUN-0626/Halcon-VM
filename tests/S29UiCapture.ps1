param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$ArrayOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s29-array-editor.png'),
    [string]$CircleOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s29-circle-finder.png'),
    [string]$ArrayImageOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s29-array-image-view.png'),
    [string]$CircleImageOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s29-circle-image-view.png')
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Pump-Ui([int]$Milliseconds) {
    $frame = New-Object Windows.Threading.DispatcherFrame
    $timer = New-Object Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds)
    $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false })
    $timer.Start()
    [Windows.Threading.Dispatcher]::PushFrame($frame)
}
function Save-Shot([string]$Path) {
    $window.UpdateLayout()
    $topLeft = $window.PointToScreen([Windows.Point]::new(0,0))
    $bottomRight = $window.PointToScreen([Windows.Point]::new($window.ActualWidth,$window.ActualHeight))
    $width = [Math]::Max(1, [int][Math]::Ceiling($bottomRight.X - $topLeft.X))
    $height = [Math]::Max(1, [int][Math]::Ceiling($bottomRight.Y - $topLeft.Y))
    $capture = [Drawing.Bitmap]::new($width,$height)
    $graphics = [Drawing.Graphics]::FromImage($capture)
    $screenSaved = $false
    try {
        $graphics.CopyFromScreen([int]$topLeft.X,[int]$topLeft.Y,0,0,[Drawing.Size]::new($width,$height))
        $capture.Save($Path,[Drawing.Imaging.ImageFormat]::Png)
        $screenSaved = $true
    }
    catch { $screenSaved = $false }
    finally {
        $graphics.Dispose()
        $capture.Dispose()
    }
    if (-not $screenSaved) {
        $width = [Math]::Max(1,[int][Math]::Ceiling($window.ActualWidth))
        $height = [Math]::Max(1,[int][Math]::Ceiling($window.ActualHeight))
        $render = [Activator]::CreateInstance(
            [Windows.Media.Imaging.RenderTargetBitmap],
            [object[]]@($width,$height,96.0,96.0,[Windows.Media.PixelFormats]::Pbgra32))
        $render.Render($window)
        $encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder
        $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($render))
        $stream = [IO.File]::Open($Path,[IO.FileMode]::Create,[IO.FileAccess]::Write)
        try { $encoder.Save($stream) } finally { $stream.Dispose() }
    }
    "${width}x${height}"
}
function Save-HalconView([string]$Path) {
    $imageControl = $main.GetField('imageWindow',$private).GetValue($window)
    $view = $imageControl.HalconWindow.DumpWindowImage()
    try { $view.WriteImage('png',0,$Path) }
    finally { $view.Dispose() }
}
function Write-CircleImage([string]$Path) {
    $width = 640
    $height = 420
    $centerRow = 210
    $centerColumn = 330
    $radius = 145
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$width $height`n255`n")
    $pixels = New-Object byte[] ($width * $height)
    for ($row = 0; $row -lt $height; $row++) {
        for ($column = 0; $column -lt $width; $column++) {
            $distance = [Math]::Sqrt(($row-$centerRow)*($row-$centerRow)+($column-$centerColumn)*($column-$centerColumn))
            $value = 28 + [int](20 * $column / $width)
            if ($distance -le $radius) { $value = 220 }
            $pixels[$row * $width + $column] = [byte]$value
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length)
    [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}

$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s29-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp) | Out-Null
$image = Join-Path $temp 'circle.pgm'
Write-CircleImage $image
$app = [Windows.Application]::Current
if ($null -eq $app) {
    $app = New-Object Windows.Application
    $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown
}

$main = $assembly.GetType('HalconWinFormsDemo.MainWindow',$true)
$serviceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$kindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$args = New-Object 'object[]' 1
$args[0] = $temp.ToString()
$service = $serviceType.GetConstructor([Type[]]@([string])).Invoke($args)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$window = $main.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
$flow = $main.GetField('flowTools',$private).GetValue($window)
$create = $main.GetMethod('CreateFlowTool',$private)
$dispose = $main.GetMethod('DisposeFlowTools',$private)
$refresh = $main.GetMethod('RefreshUiState',$private)
$scheduleDisplay = $main.GetMethod('ScheduleRefreshDisplay',$private)
$open = $main.GetMethod('OpenSelectedToolConfiguration',$private)
$loadImage = $main.GetMethod('LoadImage',$private)
$execute = $main.GetMethod('ExecuteFlowTool',$private)
$editDomain = $main.GetMethod('EditMeasurementDomainButton_Click',$private)
function New-Tool([string]$Kind,[string]$Name) {
    $create.Invoke($window,[object[]]@([Enum]::Parse($kindType,$Kind),$Name,$true,$null))
}

$dispose.Invoke($window,@()) | Out-Null
$flow.Clear()
$source = New-Tool ImageSource 'Station Image Input'
$array = New-Tool LinearCaliperArrayMeasurement 'HALCON Linear Caliper Array'
$circle = New-Tool CircularCaliperCircleFinder 'HALCON Circular Caliper Circle Finder'
$judge = New-Tool NumericJudge 'Radius Tolerance Judge'
$source.Parameters.LocalImagePath = $image
$source.Parameters.LocalImageSerialNumber = 'S29-CIRCLE'
$ap = $array.Parameters
$ap.LinearCaliperStartRow = 210
$ap.LinearCaliperStartColumn = 110
$ap.LinearCaliperEndRow = 210
$ap.LinearCaliperEndColumn = 550
$ap.LinearCaliperCount = 9
$ap.LinearCaliperLength1 = 170
$ap.LinearCaliperLength2 = 9
$ap.LinearCaliperSigma = 1
$ap.LinearCaliperThreshold = 25
$ap.LinearCaliperTransition = 'All'
$ap.LinearCaliperSelect = 'All'
$array.SetInputBinding('Image',$source.ToolId,'Image')
$cp = $circle.Parameters
$cp.CircularCaliperCenterRow = 210
$cp.CircularCaliperCenterColumn = 330
$cp.CircularCaliperSearchRadius = 145
$cp.CircularCaliperStartAngleDegrees = 0
$cp.CircularCaliperEndAngleDegrees = 360
$cp.CircularCaliperCount = 20
$cp.CircularCaliperLength1 = 32
$cp.CircularCaliperLength2 = 6
$cp.CircularCaliperSigma = 1
$cp.CircularCaliperThreshold = 25
$cp.CircularCaliperTransition = 'Negative'
$cp.CircularCaliperSelect = 'All'
$circle.SetInputBinding('Image',$source.ToolId,'Image')
$judge.InputToolId = $circle.ToolId
$judge.InputPortName = 'Radius'
$judge.SetInputBinding('Value',$circle.ToolId,'Radius')
$judge.NumericOperator = 'BetweenInclusive'
$judge.NumericLowerLimit = 140
$judge.NumericUpperLimit = 150
foreach ($tool in @($source,$array,$circle,$judge)) { $flow.Add($tool) }

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.WindowState = [Windows.WindowState]::Normal
$window.Left = 2
$window.Top = 2
$window.Width = 1900
$window.Height = 1060
$window.Topmost = $true
$window.Show()
$window.Activate() | Out-Null
Pump-Ui 1200
$loadImage.Invoke($window,[object[]]@($image.ToString(),$false)) | Out-Null
foreach ($tool in @($source,$array,$circle,$judge)) {
    try { $execute.Invoke($window,[object[]]@($tool,'S29 UI capture')) | Out-Null }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

$list = $main.GetField('FlowToolList',$all).GetValue($window)
$list.SelectedItems.Clear()
$list.SelectedItem = $array
$open.Invoke($window,@()) | Out-Null
$main.GetField('RightTabs',$all).GetValue($window).SelectedItem = $main.GetField('DockConfigurationTab',$all).GetValue($window)
$main.GetField('DockConfigurationTabs',$all).GetValue($window).SelectedIndex = 0
$main.GetField('ImageLayerTabs',$all).GetValue($window).SelectedIndex = 1
$main.GetField('FlowZoomSlider',$all).GetValue($window).Value = 0.48
$editDomain.Invoke($window,[object[]]@($null,[Windows.RoutedEventArgs]::new())) | Out-Null
$refresh.Invoke($window,@()) | Out-Null
$layers = $main.GetField('resultOverlayLayers',$private).GetValue($window)
foreach ($layer in $layers) { $layer.IsVisible = ($layer.ToolId -eq $array.ToolId) }
$scheduleDisplay.Invoke($window,@()) | Out-Null
Pump-Ui 900

$arrayPreview = $main.GetField('linearCaliperArrayPreview',$private).GetValue($window)
$editorBar = $main.GetField('MeasurementDomainEditorBar',$all).GetValue($window)
$arrayDock = $main.GetField('DockLinearCaliperArrayPanel',$all).GetValue($window)
if ($null -eq $arrayPreview -or $editorBar.Visibility.ToString() -ne 'Visible' -or $arrayDock.Visibility.ToString() -ne 'Visible') {
    throw 'S29 array editor visible state is incomplete.'
}
$arrayDock.BringIntoView()
Pump-Ui 300
$arraySize = Save-Shot $ArrayOutputPath
Save-HalconView $ArrayImageOutputPath

$main.GetMethod('MeasurementDomainCancelButton_Click',$private).Invoke($window,[object[]]@($null,[Windows.RoutedEventArgs]::new())) | Out-Null
$list.SelectedItems.Clear()
$list.SelectedItem = $circle
$open.Invoke($window,@()) | Out-Null
$main.GetField('RightTabs',$all).GetValue($window).SelectedItem = $main.GetField('DockConfigurationTab',$all).GetValue($window)
$main.GetField('DockConfigurationTabs',$all).GetValue($window).SelectedIndex = 0
$main.GetField('ImageLayerTabs',$all).GetValue($window).SelectedIndex = 1
$refresh.Invoke($window,@()) | Out-Null
$layers = $main.GetField('resultOverlayLayers',$private).GetValue($window)
foreach ($layer in $layers) { $layer.IsVisible = ($layer.ToolId -eq $circle.ToolId) }
$scheduleDisplay.Invoke($window,@()) | Out-Null
Pump-Ui 900

$circleDock = $main.GetField('DockCircularCaliperCirclePanel',$all).GetValue($window)
$valid = $null
$radius = $null
if ($circleDock.Visibility.ToString() -ne 'Visible' -or
    -not $circle.TryGetOutputValue('ValidCount',[ref]$valid) -or
    -not $circle.TryGetOutputValue('Radius',[ref]$radius) -or
    [int][double]$valid -ne 20) {
    throw "S29 circle finder visible/runtime state is incomplete: dock=$($circleDock.Visibility), valid=$valid, radius=$radius"
}
$circleDock.BringIntoView()
Pump-Ui 300
$circleSize = Save-Shot $CircleOutputPath
Save-HalconView $CircleImageOutputPath

$main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true)
$window.Close()
$app.Shutdown()
if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
Write-Output "S29_UI_CAPTURE=PASS; ArrayPath=$ArrayOutputPath; CirclePath=$CircleOutputPath; ArrayImage=$ArrayImageOutputPath; CircleImage=$CircleImageOutputPath; ArrayWindow=$arraySize; CircleWindow=$circleSize; A=LINEAR_ARRAY_IMAGE_EDIT; B=CIRCULAR_CALIPER_CIRCLE_FINDER; ValidCount=$([int][double]$valid); Radius=$([double]$radius)"
