param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s26-debug-monitor-line-relation.png')
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
function Write-LineImage([string]$Path) {
    $width = 640
    $height = 420
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$width $height`n255`n")
    $pixels = New-Object byte[] ($width * $height)
    for ($i = 0; $i -lt $pixels.Length; $i++) { $pixels[$i] = 22 }
    foreach ($column in 70..570) {
        foreach ($offset in -2..2) { $pixels[(210 + $offset) * $width + $column] = 220 }
    }
    foreach ($index in 0..360) {
        $row = 30 + $index
        $column = [int](430 - $index * 0.45)
        foreach ($offset in -2..2) {
            $target = $column + $offset
            if ($target -ge 0 -and $target -lt $width) { $pixels[$row * $width + $target] = 235 }
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}
function New-LineXld([double]$Row1, [double]$Column1, [double]$Row2, [double]$Column2) {
    $rows = New-Object 'System.Collections.Generic.List[double]'
    $columns = New-Object 'System.Collections.Generic.List[double]'
    foreach ($index in 0..30) {
        $t = $index / 30.0
        $rows.Add($Row1 + ($Row2 - $Row1) * $t)
        $columns.Add($Column1 + ($Column2 - $Column1) * $t)
    }
    [HalconDotNet.HObject]$line = $null
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$line, [HalconDotNet.HTuple]$rows.ToArray(), [HalconDotNet.HTuple]$columns.ToArray())
    $line
}

$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s26-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp) | Out-Null
$image = Join-Path $temp 'dual-lines.pgm'
Write-LineImage $image
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
$manual = $main.GetMethod('EnableManualFlowLayout', $private)
$open = $main.GetMethod('OpenSelectedToolConfiguration', $private)
$loadImage = $main.GetMethod('LoadImage', $private)
$runLine = $main.GetMethod('RunXldLineFittingTool', $private)
$runRelation = $main.GetMethod('RunXldLineRelationTool', $private)
$execute = $main.GetMethod('ExecuteFlowTool', $private)
$runToSelected = $main.GetMethod('DebugRunToSelectedButton_Click', $private)
$debugContinue = $main.GetMethod('DebugContinueButton_Click', $private)
function New-Tool([string]$Kind, [string]$Name) { $create.Invoke($window, [object[]]@([Enum]::Parse($kindType,$Kind),$Name,$true,$null)) }

$dispose.Invoke($window, @()) | Out-Null
$flow.Clear()
$source1 = New-Tool ImageSource 'Debug Input 1'
$source2 = New-Tool ImageSource 'Debug Input 2'
$source3 = New-Tool ImageSource 'Debug Input 3'
$lineA = New-Tool XldLineFitting 'Fitted Line A'
$lineB = New-Tool XldLineFitting 'Fitted Line B'
$relation = New-Tool XldLineRelationMeasurement 'HALCON Line Relation'
$judge = New-Tool NumericJudge 'Angle Judge'
foreach ($source in @($source1,$source2,$source3)) {
    $source.Parameters.LocalImagePath = $image
    $source.Parameters.LocalImageSerialNumber = 'S26-DEBUG'
}
$relation.Parameters.LineRelationParallelToleranceDegrees = 1
$relation.Parameters.LineRelationPerpendicularToleranceDegrees = 1
$relation.Parameters.LineRelationCoincidentDistance = 1
$judge.NumericOperator = 'BetweenInclusive'
$judge.NumericLowerLimit = 60
$judge.NumericUpperLimit = 80
foreach ($tool in @($source1,$source2,$source3,$lineA,$lineB,$relation,$judge)) { $flow.Add($tool) }
$relation.SetInputBinding('LineA',$lineA.ToolId,'XLD')
$relation.SetInputBinding('LineB',$lineB.ToolId,'XLD')
$judge.SetInputBinding('Value',$relation.ToolId,'AcuteAngle')
$judge.InputToolId = $relation.ToolId
$judge.InputPortName = 'AcuteAngle'

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
    @($source1,36,520),@($source2,432,520),@($source3,828,520),
    @($lineA,36,60),@($lineB,36,280),@($relation,470,150),@($judge,910,150)
)
foreach ($entry in $positions) {
    $entry[0].GraphX = [double]$entry[1]
    $entry[0].GraphY = [double]$entry[2]
    $entry[0].HasManualGraphPosition = $true
}

$monitorCombo = $main.GetField('DebugMonitorToolComboBox', $all).GetValue($window)
$monitorCombo.SelectedItem = $source2
$runToSelected.Invoke($window, [object[]]@($null,$null)) | Out-Null
Wait-Condition {
    $main.GetField('isPauseRequested',$private).GetValue($window) -and $source3.IsExecutionPointer
} 'Run-to-selected did not pause before the next real module.'

$loadImageArgs = New-Object 'object[]' 2
$loadImageArgs[0] = $image.ToString()
$loadImageArgs[1] = $false
$loadImage.Invoke($window, $loadImageArgs) | Out-Null
[HalconDotNet.HObject]$xldA = $null
[HalconDotNet.HObject]$xldB = $null
try {
    $xldA = New-LineXld 210 70 210 570
    $xldB = New-LineXld 30 430 390 268
    $runLine.Invoke($window, [object[]]@($lineA,$xldA)) | Out-Null
    $lineA.ResultCode = 'OK'
    $lineA.RunStatus = 'Completed'
    $runLine.Invoke($window, [object[]]@($lineB,$xldB)) | Out-Null
    $lineB.ResultCode = 'OK'
    $lineB.RunStatus = 'Completed'
    $runRelation.Invoke($window, [object[]]@($relation,$xldA,$xldB)) | Out-Null
    $relation.ResultCode = 'OK'
    $relation.RunStatus = 'Completed'
    $execute.Invoke($window, [object[]]@($judge,'S26 UI capture')) | Out-Null
}
finally {
    if ($null -ne $xldA) { $xldA.Dispose() }
    if ($null -ne $xldB) { $xldB.Dispose() }
}

$relation.IsBreakpoint = $true
$list = $main.GetField('FlowToolList', $all).GetValue($window)
$list.SelectedItems.Clear()
$list.SelectedItem = $relation
$open.Invoke($window, @()) | Out-Null
$main.GetField('RightTabs',$all).GetValue($window).SelectedItem = $main.GetField('DockConfigurationTab',$all).GetValue($window)
$main.GetField('DockConfigurationTabs',$all).GetValue($window).SelectedIndex = 0
$main.GetField('ImageContextComboBox',$all).GetValue($window).SelectedValue = 'GlobalInput'
$main.GetField('FlowZoomSlider',$all).GetValue($window).Value = 0.35
$monitorCombo.SelectedItem = $source2
$refresh.Invoke($window, @()) | Out-Null
Pump-Ui 1200

$dock = $main.GetField('DockXldLineRelationPanel',$all).GetValue($window)
$currentText = $main.GetField('DebugMonitorCurrentText',$all).GetValue($window).Text
$nextText = $main.GetField('DebugMonitorNextText',$all).GetValue($window).Text
$acute = $null
$intersectionRow = $null
$intersectionColumn = $null
if (-not $relation.TryGetOutputValue('AcuteAngle',[ref]$acute) -or -not $relation.TryGetOutputValue('IntersectionRow',[ref]$intersectionRow) -or -not $relation.TryGetOutputValue('IntersectionColumn',[ref]$intersectionColumn)) { throw 'Line relation outputs missing.' }
if ($dock.Visibility.ToString() -ne 'Visible' -or -not $source3.IsExecutionPointer -or -not $currentText.Contains('Debug Input 2') -or -not $nextText.Contains('Debug Input 3') -or [double]$acute -lt 60 -or [double]$acute -gt 80) {
    throw "S26 visible state missing: dock=$($dock.Visibility), pointer=$($source3.IsExecutionPointer), current=$currentText, next=$nextText, angle=$acute"
}
$dock.BringIntoView()
Pump-Ui 500
$size = Save-Shot $OutputPath

$main.GetField('isStopRequested',$private).SetValue($window,$true)
$debugContinue.Invoke($window, [object[]]@($null,$null)) | Out-Null
Wait-Condition { -not $main.GetField('isFlowExecutionActive',$private).GetValue($window) } 'Stopped debug flow did not terminate.'
$main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true)
$window.Close()
$app.Shutdown()
if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
Write-Output ('S26_UI_CAPTURE=PASS; Path={0}; Window={1}; A=DOCKED_DEBUG_MONITOR/CURRENT_NEXT/RANGE/PAUSE_REASON/IO_SNAPSHOT/BREAKPOINT_LIST/RUN_TO_SELECTED; B=HALCON_DUAL_XLD_LINE_RELATION/INTERSECTION/ANGLE/PARALLEL_DISTANCE/RESIDUAL/OVERLAY; AcuteAngle={2:0.###}; Intersection={3:0.###},{4:0.###}' -f $OutputPath,$size,[double]$acute,[double]$intersectionRow,[double]$intersectionColumn)
