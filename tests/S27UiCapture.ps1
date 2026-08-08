param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s27-result-layers-caliper.png')
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
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
    $width = [Math]::Max(1, [int][Math]::Ceiling($window.ActualWidth))
    $height = [Math]::Max(1, [int][Math]::Ceiling($window.ActualHeight))
    $capture = [Activator]::CreateInstance([Windows.Media.Imaging.RenderTargetBitmap], [object[]]@($width,$height,96.0,96.0,[Windows.Media.PixelFormats]::Pbgra32))
    $capture.Render($window)
    $encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($capture))
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Create, [IO.FileAccess]::Write)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
    "${width}x${height}"
}
function Write-CaliperImage([string]$Path) {
    $width = 640
    $height = 420
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$width $height`n255`n")
    $pixels = New-Object byte[] ($width * $height)
    for ($row = 0; $row -lt $height; $row++) {
        for ($column = 0; $column -lt $width; $column++) {
            $value = 25 + [int](18 * $row / $height)
            if (($column -ge 155 -and $column -le 220) -or ($column -ge 395 -and $column -le 470)) { $value = 225 }
            $pixels[$row * $width + $column] = [byte]$value
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s27-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp) | Out-Null
$image = Join-Path $temp 'caliper-pairs.pgm'
Write-CaliperImage $image
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
function New-Tool([string]$Kind, [string]$Name) { $create.Invoke($window, [object[]]@([Enum]::Parse($kindType,$Kind),$Name,$true,$null)) }

$dispose.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool ImageSource 'Station Image Input'
$threshold = New-Tool ImageThreshold 'Bright Region Threshold'
$caliper = New-Tool CaliperEdgePairMeasurement 'HALCON Caliper Pair'
$judge = New-Tool NumericJudge 'Primary Distance Judge'
$source.Parameters.LocalImagePath = $image
$source.Parameters.LocalImageSerialNumber = 'S27-CALIPER'
$threshold.Parameters.ImageThresholdMinGray = 150
$threshold.Parameters.ImageThresholdMaxGray = 255
$threshold.SetInputBinding('Image',$source.ToolId,'Image')
$caliper.Parameters.CaliperRow = 210
$caliper.Parameters.CaliperColumn = 320
$caliper.Parameters.CaliperPhiDegrees = 0
$caliper.Parameters.CaliperLength1 = 200
$caliper.Parameters.CaliperLength2 = 42
$caliper.Parameters.CaliperSigma = 1
$caliper.Parameters.CaliperThreshold = 25
$caliper.Parameters.CaliperTransition = 'Positive'
$caliper.Parameters.CaliperSelect = 'All'
$caliper.SetInputBinding('Image',$source.ToolId,'Image')
$judge.InputToolId = $caliper.ToolId
$judge.InputPortName = 'PrimaryDistance'
$judge.SetInputBinding('Value',$caliper.ToolId,'PrimaryDistance')
$judge.NumericOperator = 'BetweenInclusive'
$judge.NumericLowerLimit = 55
$judge.NumericUpperLimit = 80
foreach ($tool in @($source,$threshold,$caliper,$judge)) { $flow.Add($tool) }

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.WindowState = [Windows.WindowState]::Normal
$window.Left = 2
$window.Top = 2
$window.Width = 1900
$window.Height = 1120
$window.Topmost = $true
$window.Show()
$window.Activate() | Out-Null
Pump-Ui 1300

$loadArgs = New-Object 'object[]' 2
$loadArgs[0] = $image.ToString()
$loadArgs[1] = $false
$loadImage.Invoke($window, $loadArgs) | Out-Null
foreach ($tool in @($source,$threshold,$caliper,$judge)) {
    try { $execute.Invoke($window, [object[]]@($tool,'S27 UI capture')) | Out-Null }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

$list = $main.GetField('FlowToolList', $all).GetValue($window)
$list.SelectedItems.Clear()
$list.SelectedItem = $caliper
$open.Invoke($window, @()) | Out-Null
$main.GetField('RightTabs',$all).GetValue($window).SelectedItem = $main.GetField('DockConfigurationTab',$all).GetValue($window)
$main.GetField('DockConfigurationTabs',$all).GetValue($window).SelectedIndex = 0
$main.GetField('ImageLayerTabs',$all).GetValue($window).SelectedIndex = 1
$main.GetField('ImageContextComboBox',$all).GetValue($window).SelectedValue = 'GlobalInput'
$main.GetField('FlowZoomSlider',$all).GetValue($window).Value = 0.55
$refresh.Invoke($window, @()) | Out-Null
Pump-Ui 1000

$layers = $main.GetField('resultOverlayLayers',$private).GetValue($window)
$caliperDock = $main.GetField('DockCaliperEdgePairPanel',$all).GetValue($window)
$pairCount = $null
$primaryDistance = $null
if (-not $caliper.TryGetOutputValue('PairCount',[ref]$pairCount) -or -not $caliper.TryGetOutputValue('PrimaryDistance',[ref]$primaryDistance)) { throw 'Caliper outputs missing.' }
if ($caliperDock.Visibility.ToString() -ne 'Visible' -or $layers.Count -lt 2 -or [double]$pairCount -lt 2) {
    throw "S27 visible state missing: dock=$($caliperDock.Visibility), layers=$($layers.Count), pairs=$pairCount"
}
$caliperDock.BringIntoView()
Pump-Ui 400
$size = Save-Shot $OutputPath

$main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true)
$window.Close()
$app.Shutdown()
if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
$pairText = [int][double]$pairCount
$distanceText = ([double]$primaryDistance).ToString('0.###',[Globalization.CultureInfo]::InvariantCulture)
Write-Output "S27_UI_CAPTURE=PASS; Path=$OutputPath; Window=$size; A=MULTI_RESULT_LAYER_LIST/VISIBILITY/ISOLATE/ORDER/LOCATE/CONFIG; B=HALCON_GEN_MEASURE_RECTANGLE2/MEASURE_PAIRS/CLOSE_MEASURE/XLD/NUMBER; PairCount=$pairText; PrimaryDistance=$distanceText; BSB=NOT_IMPLEMENTED_BY_USER_DIRECTIVE"
