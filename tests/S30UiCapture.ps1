param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$CircularEditorOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s30-circular-editor.png'),
    [string]$RingOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s30-ring-thickness.png'),
    [string]$CircularImageOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s30-circular-image-view.png'),
    [string]$RingImageOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s30-ring-image-view.png')
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
function Write-RingImage([string]$Path) {
    $width = 640
    $height = 420
    $centerRow = 210
    $centerColumn = 330
    $innerRadius = 105
    $outerRadius = 160
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$width $height`n255`n")
    $pixels = New-Object byte[] ($width * $height)
    for ($row = 0; $row -lt $height; $row++) {
        for ($column = 0; $column -lt $width; $column++) {
            $distance = [Math]::Sqrt(($row-$centerRow)*($row-$centerRow)+($column-$centerColumn)*($column-$centerColumn))
            $value = 25 + [int](16 * $column / $width)
            if ($distance -ge $innerRadius -and $distance -le $outerRadius) { $value = 225 }
            $pixels[$row * $width + $column] = [byte]$value
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length)
    [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}

$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s30-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp) | Out-Null
$image = Join-Path $temp 'ring.pgm'
Write-RingImage $image
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
$circle = New-Tool CircularCaliperCircleFinder 'HALCON Circular Caliper Circle Finder'
$ring = New-Tool RingWallThicknessMeasurement 'HALCON Ring Thickness / Concentricity'
$judge = New-Tool NumericJudge 'Wall Thickness Tolerance'
$source.Parameters.LocalImagePath = $image
$source.Parameters.LocalImageSerialNumber = 'S30-RING'
$cp = $circle.Parameters
$cp.CircularCaliperCenterRow = 210
$cp.CircularCaliperCenterColumn = 330
$cp.CircularCaliperSearchRadius = 160
$cp.CircularCaliperStartAngleDegrees = 0
$cp.CircularCaliperEndAngleDegrees = 360
$cp.CircularCaliperCount = 24
$cp.CircularCaliperLength1 = 28
$cp.CircularCaliperLength2 = 6
$cp.CircularCaliperSigma = 1
$cp.CircularCaliperThreshold = 25
$cp.CircularCaliperTransition = 'Negative'
$cp.CircularCaliperSelect = 'All'
$circle.SetInputBinding('Image',$source.ToolId,'Image')
$rp = $ring.Parameters
$rp.RingCenterRow = 210
$rp.RingCenterColumn = 330
$rp.RingSearchRadius = 132.5
$rp.RingStartAngleDegrees = 0
$rp.RingEndAngleDegrees = 360
$rp.RingCaliperCount = 24
$rp.RingCaliperLength1 = 36
$rp.RingCaliperLength2 = 6
$rp.RingCaliperSigma = 1
$rp.RingCaliperThreshold = 25
$rp.RingCaliperTransition = 'Positive'
$rp.RingCaliperSelect = 'All'
$ring.SetInputBinding('Image',$source.ToolId,'Image')
$judge.InputToolId = $ring.ToolId
$judge.InputPortName = 'MeanThickness'
$judge.SetInputBinding('Value',$ring.ToolId,'MeanThickness')
$judge.NumericOperator = 'BetweenInclusive'
$judge.NumericLowerLimit = 52
$judge.NumericUpperLimit = 58
foreach ($tool in @($source,$circle,$ring,$judge)) { $flow.Add($tool) }

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
foreach ($tool in @($source,$circle,$ring,$judge)) {
    try { $execute.Invoke($window,[object[]]@($tool,'S30 UI capture')) | Out-Null }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

$list = $main.GetField('FlowToolList',$all).GetValue($window)
$list.SelectedItems.Clear()
$list.SelectedItem = $circle
$open.Invoke($window,@()) | Out-Null
$main.GetField('RightTabs',$all).GetValue($window).SelectedItem = $main.GetField('DockConfigurationTab',$all).GetValue($window)
$main.GetField('DockConfigurationTabs',$all).GetValue($window).SelectedIndex = 0
$main.GetField('ImageLayerTabs',$all).GetValue($window).SelectedIndex = 1
$main.GetField('FlowZoomSlider',$all).GetValue($window).Value = 0.48
$editDomain.Invoke($window,[object[]]@($null,[Windows.RoutedEventArgs]::new())) | Out-Null
$refresh.Invoke($window,@()) | Out-Null
$layers = $main.GetField('resultOverlayLayers',$private).GetValue($window)
foreach ($layer in $layers) { $layer.IsVisible = ($layer.ToolId -eq $circle.ToolId) }
$scheduleDisplay.Invoke($window,@()) | Out-Null
Pump-Ui 900

$circularPreview = $main.GetField('circularCaliperPreview',$private).GetValue($window)
$editorBar = $main.GetField('MeasurementDomainEditorBar',$all).GetValue($window)
$circleDock = $main.GetField('DockCircularCaliperCirclePanel',$all).GetValue($window)
if ($null -eq $circularPreview -or $editorBar.Visibility.ToString() -ne 'Visible' -or $circleDock.Visibility.ToString() -ne 'Visible') {
    throw 'S30 circular editor visible state is incomplete.'
}
$circleDock.BringIntoView()
Pump-Ui 300
$circularSize = Save-Shot $CircularEditorOutputPath
Save-HalconView $CircularImageOutputPath

$main.GetMethod('MeasurementDomainCancelButton_Click',$private).Invoke($window,[object[]]@($null,[Windows.RoutedEventArgs]::new())) | Out-Null
$list.SelectedItems.Clear()
$list.SelectedItem = $ring
$open.Invoke($window,@()) | Out-Null
$main.GetField('RightTabs',$all).GetValue($window).SelectedItem = $main.GetField('DockConfigurationTab',$all).GetValue($window)
$main.GetField('DockConfigurationTabs',$all).GetValue($window).SelectedIndex = 0
$main.GetField('ImageLayerTabs',$all).GetValue($window).SelectedIndex = 1
$refresh.Invoke($window,@()) | Out-Null
$layers = $main.GetField('resultOverlayLayers',$private).GetValue($window)
foreach ($layer in $layers) { $layer.IsVisible = ($layer.ToolId -eq $ring.ToolId) }
$scheduleDisplay.Invoke($window,@()) | Out-Null
Pump-Ui 900

$ringDock = $main.GetField('DockRingWallThicknessPanel',$all).GetValue($window)
$valid = $null
$thickness = $null
$eccentricity = $null
if ($ringDock.Visibility.ToString() -ne 'Visible' -or
    -not $ring.TryGetOutputValue('ValidCount',[ref]$valid) -or
    -not $ring.TryGetOutputValue('MeanThickness',[ref]$thickness) -or
    -not $ring.TryGetOutputValue('Eccentricity',[ref]$eccentricity) -or
    [int][double]$valid -ne 24) {
    throw "S30 ring visible/runtime state is incomplete: dock=$($ringDock.Visibility), valid=$valid, thickness=$thickness"
}
$ringDock.BringIntoView()
Pump-Ui 300
$ringSize = Save-Shot $RingOutputPath
Save-HalconView $RingImageOutputPath

$main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true)
$window.Close()
$app.Shutdown()
if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
Write-Output "S30_UI_CAPTURE=PASS; CircularPath=$CircularEditorOutputPath; RingPath=$RingOutputPath; CircularImage=$CircularImageOutputPath; RingImage=$RingImageOutputPath; CircularWindow=$circularSize; RingWindow=$ringSize; A=CIRCULAR_IMAGE_EDIT; B=RING_WALL_THICKNESS_CONCENTRICITY; ValidCount=$([int][double]$valid); MeanThickness=$([double]$thickness); Eccentricity=$([double]$eccentricity); BSB=NOT_IMPLEMENTED_BY_USER_DIRECTIVE"
