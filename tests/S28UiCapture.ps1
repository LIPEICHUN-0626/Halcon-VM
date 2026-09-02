param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s28-measurement-editor-array.png')
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
        $width = [Math]::Max(1, [int][Math]::Ceiling($window.ActualWidth))
        $height = [Math]::Max(1, [int][Math]::Ceiling($window.ActualHeight))
        $render = [Activator]::CreateInstance([Windows.Media.Imaging.RenderTargetBitmap], [object[]]@($width,$height,96.0,96.0,[Windows.Media.PixelFormats]::Pbgra32))
        $render.Render($window)
        $encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder
        $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($render))
        $stream = [IO.File]::Open($Path, [IO.FileMode]::Create, [IO.FileAccess]::Write)
        try { $encoder.Save($stream) } finally { $stream.Dispose() }
    }
    "${width}x${height}"
}
function Write-ArrayImage([string]$Path) {
    $width = 640
    $height = 420
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$width $height`n255`n")
    $pixels = New-Object byte[] ($width * $height)
    for ($row = 0; $row -lt $height; $row++) {
        for ($column = 0; $column -lt $width; $column++) {
            $value = 25 + [int](24 * $column / $width)
            if (($row -ge 100 -and $row -le 125) -or ($row -ge 280 -and $row -le 305)) { $value = 225 }
            $pixels[$row * $width + $column] = [byte]$value
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s28-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp) | Out-Null
$image = Join-Path $temp 'linear-array.pgm'
Write-ArrayImage $image
$app = [Windows.Application]::Current
if ($null -eq $app) {
    $app = New-Object Windows.Application
    $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown
}

$main = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$serviceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$kindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$args = New-Object 'object[]' 1
$args[0] = $temp.ToString()
$service = $serviceType.GetConstructor([Type[]]@([string])).Invoke($args)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$window = $main.GetConstructor($private, $null, [Type[]]@($serviceType,$loggerType), $null).Invoke([object[]]@($service,$logger))
$flow = $main.GetField('flowTools', $private).GetValue($window)
$create = $main.GetMethod('CreateFlowTool', $private)
$dispose = $main.GetMethod('DisposeFlowTools', $private)
$refresh = $main.GetMethod('RefreshUiState', $private)
$open = $main.GetMethod('OpenSelectedToolConfiguration', $private)
$loadImage = $main.GetMethod('LoadImage', $private)
$execute = $main.GetMethod('ExecuteFlowTool', $private)
$editDomain = $main.GetMethod('EditMeasurementDomainButton_Click', $private)
function New-Tool([string]$Kind, [string]$Name) {
    $create.Invoke($window, [object[]]@([Enum]::Parse($kindType,$Kind),$Name,$true,$null))
}

$dispose.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool ImageSource 'Station Image Input'
$array = New-Tool LinearCaliperArrayMeasurement 'HALCON Linear Caliper Array'
$caliper = New-Tool CaliperEdgePairMeasurement 'Editable Caliper Domain'
$judge = New-Tool NumericJudge 'Mean Thickness Judge'
$source.Parameters.LocalImagePath = $image
$source.Parameters.LocalImageSerialNumber = 'S28-ARRAY'
$p = $array.Parameters
$p.LinearCaliperStartRow = 200
$p.LinearCaliperStartColumn = 80
$p.LinearCaliperEndRow = 200
$p.LinearCaliperEndColumn = 560
$p.LinearCaliperCount = 7
$p.LinearCaliperLength1 = 150
$p.LinearCaliperLength2 = 10
$p.LinearCaliperSigma = 1
$p.LinearCaliperThreshold = 25
$p.LinearCaliperTransition = 'Positive'
$p.LinearCaliperSelect = 'All'
$array.SetInputBinding('Image',$source.ToolId,'Image')
$caliper.Parameters.CaliperRow = 200
$caliper.Parameters.CaliperColumn = 320
$caliper.Parameters.CaliperPhiDegrees = 90
$caliper.Parameters.CaliperLength1 = 150
$caliper.Parameters.CaliperLength2 = 18
$caliper.Parameters.CaliperSigma = 1
$caliper.Parameters.CaliperThreshold = 25
$caliper.Parameters.CaliperTransition = 'Positive'
$caliper.Parameters.CaliperSelect = 'All'
$caliper.SetInputBinding('Image',$source.ToolId,'Image')
$judge.InputToolId = $array.ToolId
$judge.InputPortName = 'MeanDistance'
$judge.SetInputBinding('Value',$array.ToolId,'MeanDistance')
$judge.NumericOperator = 'BetweenInclusive'
$judge.NumericLowerLimit = 20
$judge.NumericUpperLimit = 35
foreach ($tool in @($source,$array,$caliper,$judge)) { $flow.Add($tool) }

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

$loadImage.Invoke($window, [object[]]@($image.ToString(),$false)) | Out-Null
foreach ($tool in @($source,$array,$caliper,$judge)) {
    try { $execute.Invoke($window, [object[]]@($tool,'S28 UI capture')) | Out-Null }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

$list = $main.GetField('FlowToolList', $all).GetValue($window)
$list.SelectedItems.Clear()
$list.SelectedItem = $caliper
$open.Invoke($window, @()) | Out-Null
$main.GetField('RightTabs',$all).GetValue($window).SelectedItem = $main.GetField('DockConfigurationTab',$all).GetValue($window)
$main.GetField('DockConfigurationTabs',$all).GetValue($window).SelectedIndex = 0
$main.GetField('ImageLayerTabs',$all).GetValue($window).SelectedIndex = 1
$main.GetField('FlowZoomSlider',$all).GetValue($window).Value = 0.48
$editDomain.Invoke($window, [object[]]@($null,[Windows.RoutedEventArgs]::new())) | Out-Null
$refresh.Invoke($window, @()) | Out-Null
Pump-Ui 1200

$validCount = $null
$pairCount = $null
if (-not $array.TryGetOutputValue('ValidCount',[ref]$validCount) -or -not $array.TryGetOutputValue('PairCount',[ref]$pairCount)) {
    throw 'Linear array outputs missing.'
}
$editorBar = $main.GetField('MeasurementDomainEditorBar',$all).GetValue($window)
$caliperDock = $main.GetField('DockCaliperEdgePairPanel',$all).GetValue($window)
$layers = $main.GetField('resultOverlayLayers',$private).GetValue($window)
$preview = $main.GetField('measurementDomainPreview',$private).GetValue($window)
if ($editorBar.Visibility.ToString() -ne 'Visible' -or $caliperDock.Visibility.ToString() -ne 'Visible' -or
    $null -eq $preview -or [int][double]$validCount -ne 7 -or $layers.Count -lt 2) {
    throw "S28 visible state missing: editor=$($editorBar.Visibility), dock=$($caliperDock.Visibility), preview=$($null -ne $preview), valid=$validCount, layers=$($layers.Count)"
}
$caliperDock.BringIntoView()
Pump-Ui 350
$size = Save-Shot $OutputPath

$main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true)
$window.Close()
$app.Shutdown()
if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
$validText = [int][double]$validCount
$pairText = [int][double]$pairCount
Write-Output "S28_UI_CAPTURE=PASS; Path=$OutputPath; Window=$size; A=IMAGE_MEASUREMENT_DOMAIN_EDIT/DRAFT/APPLY_CANCEL; B=HALCON_LINEAR_CALIPER_ARRAY/XLD/STATS; ValidCount=$validText; PairCount=$pairText"
