param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s25-breakpoint-rectangle-fit.png')
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
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
function Wait-Condition([scriptblock]$Condition, [string]$Message, [int]$TimeoutMilliseconds = 8000) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while ($watch.ElapsedMilliseconds -lt $TimeoutMilliseconds) {
        Pump-Ui 35
        if (& $Condition) { return }
    }
    throw "Timeout: $Message"
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
function Write-RotatedRectanglePgm([string]$Path) {
    $width = 640
    $height = 420
    $centerRow = 210
    $centerColumn = 320
    $phi = 0.45
    $cos = [Math]::Cos($phi)
    $sin = [Math]::Sin($phi)
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$width $height`n255`n")
    $pixels = New-Object byte[] ($width * $height)
    for ($row = 0; $row -lt $height; $row++) {
        for ($column = 0; $column -lt $width; $column++) {
            $deltaColumn = $column - $centerColumn
            $deltaRow = $row - $centerRow
            $u = $deltaColumn * $cos - $deltaRow * $sin
            $v = $deltaColumn * $sin + $deltaRow * $cos
            $inside = [Math]::Abs($u) -le 140 -and [Math]::Abs($v) -le 70
            $pixels[$row * $width + $column] = if ($inside) { 225 } else { 24 }
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s25-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp) | Out-Null
$image = Join-Path $temp 'rotated-rectangle.pgm'
Write-RotatedRectanglePgm $image
$app = [Windows.Application]::Current
if ($null -eq $app) {
    $app = New-Object Windows.Application
    $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown
}

$main = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$serviceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$kindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$runModeType = $assembly.GetType('HalconWinFormsDemo.Models.VmFlowRunRequestMode', $true)
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
$manual = $main.GetMethod('EnableManualFlowLayout', $private)
$open = $main.GetMethod('OpenSelectedToolConfiguration', $private)
$toggle = $main.GetMethod('ToggleBreakpointButton_Click', $private)
$step = $main.GetMethod('DebugStepButton_Click', $private)
$continue = $main.GetMethod('DebugContinueButton_Click', $private)
$runRange = $main.GetMethod('RunInspectionRangeAsync', $private)
function New-Tool([string]$Kind, [string]$Name) { $create.Invoke($window, [object[]]@([Enum]::Parse($kindType,$Kind),$Name,$true,$null)) }

$dispose.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool ImageSource 'Rectangle Image'
$threshold = New-Tool ImageThreshold 'Part Region'
$contour = New-Tool RegionContourMeasurement 'Part Border XLD'
$fit = New-Tool XldRectangleFitting 'Tukey Rectangle Fit'
$judge = New-Tool NumericJudge 'Length1 Judge'
$source.Parameters.LocalImagePath = $image
$source.Parameters.LocalImageSerialNumber = 'S25-RECT-001'
$threshold.Parameters.ImageThresholdMinGray = 150
$threshold.Parameters.ImageThresholdMaxGray = 255
$contour.Parameters.RegionContourMode = 'Border'
$fit.Parameters.XldRectangleFitAlgorithm = 'Tukey'
$fit.Parameters.XldRectangleFitMaxClosureDistance = 1.5
$judge.NumericOperator = 'BetweenInclusive'
$judge.NumericLowerLimit = 135
$judge.NumericUpperLimit = 145
foreach ($tool in @($source,$threshold,$contour,$fit,$judge)) { $flow.Add($tool) }
$threshold.SetInputBinding('Image',$source.ToolId,'Image')
$contour.SetInputBinding('Region',$threshold.ToolId,'Region')
$fit.SetInputBinding('XLD',$contour.ToolId,'XLD')
$judge.SetInputBinding('Value',$fit.ToolId,'Length1')
$judge.InputToolId = $fit.ToolId
$judge.InputPortName = 'Length1'

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.WindowState = [Windows.WindowState]::Normal
$window.Left = 2
$window.Top = 2
$window.Width = 1720
$window.Height = 1120
$window.Topmost = $true
$window.Show()
$window.Activate() | Out-Null
Pump-Ui 1500

$manual.Invoke($window, @()) | Out-Null
$positions = @(
    @($source,36,180),
    @($threshold,432,180),
    @($contour,828,180),
    @($fit,1224,80),
    @($judge,1620,220)
)
foreach ($entry in $positions) {
    $entry[0].GraphX = [double]$entry[1]
    $entry[0].GraphY = [double]$entry[2]
    $entry[0].HasManualGraphPosition = $true
}
$list = $main.GetField('FlowToolList', $all).GetValue($window)
$list.SelectedItems.Clear()
$list.SelectedItem = $fit
$toggle.Invoke($window, [object[]]@($null,$null)) | Out-Null

$fullMode = [Enum]::Parse($runModeType, 'Full')
$task = $runRange.Invoke($window, [object[]]@($fullMode,'S25 UI debug'))
Wait-Condition {
    $main.GetField('isPauseRequested',$private).GetValue($window) -and $fit.IsExecutionPointer
} 'Did not pause before rectangle fitting breakpoint.'
$step.Invoke($window, [object[]]@($null,$null)) | Out-Null
Wait-Condition {
    $main.GetField('isPauseRequested',$private).GetValue($window) -and $judge.IsExecutionPointer -and $fit.ResultCode -eq 'OK'
} 'Single-step did not run the rectangle fitting module and pause before the judge.'

$list.SelectedItems.Clear()
$list.SelectedItem = $fit
$open.Invoke($window, @()) | Out-Null
$main.GetField('RightTabs',$all).GetValue($window).SelectedItem = $main.GetField('DockConfigurationTab',$all).GetValue($window)
$main.GetField('DockConfigurationTabs',$all).GetValue($window).SelectedIndex = 0
$main.GetField('ImageContextComboBox',$all).GetValue($window).SelectedValue = 'ModuleInput'
$main.GetField('FlowZoomSlider',$all).GetValue($window).Value = 0.35
$refresh.Invoke($window, @()) | Out-Null
Pump-Ui 1200

$dock = $main.GetField('DockXldRectangleFittingPanel',$all).GetValue($window)
$debugText = $main.GetField('FlowDebugStateText',$all).GetValue($window).Text
$length1 = $null
$length2 = $null
$phi = $null
if (-not $fit.TryGetOutputValue('Length1',[ref]$length1) -or -not $fit.TryGetOutputValue('Length2',[ref]$length2) -or -not $fit.TryGetOutputValue('Phi',[ref]$phi)) { throw 'Rectangle outputs missing.' }
if ($dock.Visibility.ToString() -ne 'Visible' -or -not $fit.IsBreakpoint -or -not $judge.IsExecutionPointer -or -not $debugText.Contains('1') -or [Math]::Abs([double]$length1 - 140) -gt 3 -or [Math]::Abs([double]$length2 - 70) -gt 3) {
    throw "S25 visible state missing: dock=$($dock.Visibility), breakpoint=$($fit.IsBreakpoint), pointer=$($judge.IsExecutionPointer), debug=$debugText, lengths=$length1/$length2"
}
$dock.BringIntoView()
Pump-Ui 500
$size = Save-Shot $OutputPath

$continue.Invoke($window, [object[]]@($null,$null)) | Out-Null
Wait-Condition { $task.IsCompleted } 'Debug continue did not finish UI capture flow.'
if ($task.IsFaulted) { throw $task.Exception.GetBaseException() }
$main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true)
$window.Close()
$app.Shutdown()
if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
Write-Output ('S25_UI_CAPTURE=PASS; Path={0}; Window={1}; A=BREAKPOINT/F9+STEP/F10+CONTINUE/F5/NEXT_POINTER/GROUP_AND_LIST_BADGES; B=XLD_RECTANGLE_FIT_DOCK/TUKEY/CENTER_PHI_LENGTHS_CORNERS_RESIDUAL; Graph=IMAGE_TO_REGION_TO_XLD_TO_RECTANGLE_TO_LENGTH1_JUDGE; Lengths={2:0.###}/{3:0.###}; PhiDeg={4:0.###}; BSB=NOT_IMPLEMENTED_BY_USER_DIRECTIVE' -f $OutputPath,$size,[double]$length1,[double]$length2,([double]$phi*180/[Math]::PI))
