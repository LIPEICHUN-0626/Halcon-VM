param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s21-port-drag-xld-segmentation.png')
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

function Write-Pgm([string]$Path, [int]$Width, [int]$Height, [scriptblock]$PixelFactory) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) {
            $pixels[$row * $Width + $column] = [byte](& $PixelFactory $row $column)
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

function Wait-Dispatcher([int]$Milliseconds) {
    $frame = New-Object Windows.Threading.DispatcherFrame
    $timer = New-Object Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds)
    $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false })
    $timer.Start()
    [Windows.Threading.Dispatcher]::PushFrame($frame)
}

function Save-WindowCapture([string]$Path) {
    $window.UpdateLayout()
    $source = [Windows.PresentationSource]::FromVisual($window)
    $transform = $source.CompositionTarget.TransformToDevice
    $origin = $window.PointToScreen([Windows.Point]::new(0, 0))
    $width = [Math]::Max(1, [int][Math]::Ceiling($window.ActualWidth * $transform.M11))
    $height = [Math]::Max(1, [int][Math]::Ceiling($window.ActualHeight * $transform.M22))
    $bitmap = New-Object Drawing.Bitmap $width, $height
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen([int]$origin.X, [int]$origin.Y, 0, 0, [Drawing.Size]::new($width, $height))
        $bitmap.Save($Path, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
    return "${width}x${height}"
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s21-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'rectangle.pgm'
Write-Pgm $imagePath 640 420 {
    param($row, $column)
    if ($row -ge 70 -and $row -le 345 -and $column -ge 95 -and $column -le 540) { return 215 }
    return 45
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
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$allFields = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$args = New-Object 'object[]' 1
$args[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($args)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$windowConstructor = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null)
$window = $windowConstructor.Invoke([object[]]@($recipeService, $logger))

$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$refresh = $mainType.GetMethod('RefreshUiState', $private)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)
$beginDrag = $mainType.GetMethod('BeginFlowPortDrag', $private)
$updateDrag = $mainType.GetMethod('UpdateFlowPortDragPreview', $private)
$cancelDrag = $mainType.GetMethod('CancelFlowPortDrag', $private)
$evaluateBinding = $mainType.GetMethod('EvaluateFlowPortConnection', $private)
$tryApplyBinding = $mainType.GetMethod('TryApplyFlowPortBinding', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$flowZoom = $mainType.GetField('FlowZoomSlider', $allFields).GetValue($window)
$graphViewport = $mainType.GetField('FlowGraphViewport', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockTabs = $mainType.GetField('DockConfigurationTabs', $allFields).GetValue($window)
$dockPanel = $mainType.GetField('DockXldContourSegmentationPanel', $allFields).GetValue($window)
$previewPath = $mainType.GetField('FlowPortDragPreviewPath', $allFields).GetValue($window)
$dragStatus = $mainType.GetField('FlowPortDragStatusBorder', $allFields).GetValue($window)
$dragValidation = $mainType.GetField('FlowPortDragValidationText', $allFields).GetValue($window)
$imageContext = $mainType.GetField('ImageContextComboBox', $allFields).GetValue($window)

function New-Tool([string]$Kind, [string]$Name) {
    return $createTool.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}

function Invoke-Tool($Tool, [string]$Label) {
    try { return $executeTool.Invoke($window, [object[]]@($Tool, $Label)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

function Get-Port($Tool, [string]$Direction, [string]$PortName) {
    $ports = if ($Direction -eq 'OUT') { $Tool.FlowOutputPorts } else { $Tool.FlowInputPorts }
    $port = $ports | Where-Object PortName -eq $PortName | Select-Object -First 1
    return $port.PSObject.BaseObject
}

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool 'ImageSource' 'Part_Image'
$threshold = New-Tool 'ImageThreshold' 'Bright_Rectangle'
$contour = New-Tool 'RegionContourMeasurement' 'Rectangle_XLD'
$segment = New-Tool 'XldContourSegmentation' 'Four_Line_Segments'
$judge = New-Tool 'NumericJudge' 'Segment_Count_Judge'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S21-UI'
$threshold.Parameters.ImageThresholdMinGray = 150
$threshold.Parameters.ImageThresholdMaxGray = 255
$contour.Parameters.RegionContourMode = 'Border'
$segment.Parameters.XldSegmentMode = 'LinesCircles'
$segment.Parameters.XldSegmentSmooth = 5
$segment.Parameters.XldSegmentMaxLineDistance1 = 4
$segment.Parameters.XldSegmentMaxLineDistance2 = 2
$judge.NumericOperator = 'GreaterOrEqual'
$judge.NumericLowerLimit = 4
foreach ($tool in @($source, $threshold, $contour, $segment, $judge)) { $flow.Add($tool) }
$threshold.SetInputBinding('Image', $source.ToolId, 'Image')
$contour.SetInputBinding('Region', $threshold.ToolId, 'Region')
$segment.SetInputBinding('XLD', $contour.ToolId, 'XLD')
$judge.InputToolId = $segment.ToolId
$judge.InputPortName = 'SegmentCount'
$judge.SetInputBinding('Value', $segment.ToolId, 'SegmentCount')
foreach ($tool in @($source, $threshold, $contour, $segment, $judge)) { Invoke-Tool $tool ('S21 UI ' + $tool.InstanceName) | Out-Null }

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.WindowState = [Windows.WindowState]::Normal
$window.Left = 4
$window.Top = 4
$window.Width = 1660
$window.Height = 1100
$window.Topmost = $true
$window.Show()
$window.Activate() | Out-Null
Wait-Dispatcher 2200

$flowZoom.Value = 0.4
$graphViewport.ScrollToHorizontalOffset(0)
$graphViewport.ScrollToVerticalOffset(0)
$flowToolList.SelectedItem = $segment
$openConfiguration.Invoke($window, @()) | Out-Null
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 0
$imageContext.SelectedValue = 'ModuleInput'
$refresh.Invoke($window, @()) | Out-Null
Wait-Dispatcher 900

$contourXld = Get-Port $contour 'OUT' 'XLD'
$segmentXld = Get-Port $segment 'IN' 'XLD'
$judgeValue = Get-Port $judge 'IN' 'Value'
$dummy = (New-Object Windows.Controls.Border).PSObject.BaseObject
$start = [Windows.Point]::new($contour.GraphX + 360, $contour.GraphY + 87)
$validEnd = [Windows.Point]::new($segment.GraphX, $segment.GraphY + 87)
$invalidEnd = [Windows.Point]::new($judge.GraphX, $judge.GraphY + 87)

$started = [bool]$beginDrag.Invoke($window, [object[]]@($contourXld, $dummy, $start))
if (-not $started) { throw 'Could not start visible OUT-to-IN drag preview.' }
$validArgs = [object[]]@($contourXld, $segmentXld, $null)
$valid = [bool]$evaluateBinding.Invoke($window, $validArgs)
if (-not $valid) { throw ('Compatible XLD ports were rejected: ' + $validArgs[2]) }
$updateDrag.Invoke($window, [object[]]@($validEnd, $contourXld, $segmentXld, $true, $true, [string]$validArgs[2])) | Out-Null
$mainType.GetField('flowPortDragSession', $private).SetValue($window, $null)
Wait-Dispatcher 700
if ($dockPanel.Visibility.ToString() -ne 'Visible' -or $previewPath.Visibility.ToString() -ne 'Visible' -or $dragStatus.Visibility.ToString() -ne 'Visible') {
    throw ('S21 dock or valid drag preview is not visible. Dock={0}; Path={1}; Status={2}' -f $dockPanel.Visibility, $previewPath.Visibility, $dragStatus.Visibility)
}
if ($previewPath.Stroke.Color.ToString() -ne '#FF10B981' -or -not $dragValidation.Text.Contains('InputBindings')) {
    throw ('Compatible drag preview did not expose the expected green/write-back state: {0} / {1}' -f $previewPath.Stroke.Color, $dragValidation.Text)
}
$validSize = Save-WindowCapture $OutputPath
$applyArgs = [object[]]@($contourXld, $segmentXld, $null)
if (-not [bool]$tryApplyBinding.Invoke($window, $applyArgs)) { throw ('Could not complete the compatible binding: ' + $applyArgs[2]) }
$cancelDrag.Invoke($window, [object[]]@($null, $false)) | Out-Null

$refresh.Invoke($window, @()) | Out-Null
$contourXld = Get-Port $contour 'OUT' 'XLD'
$judgeValue = Get-Port $judge 'IN' 'Value'
$beginDrag.Invoke($window, [object[]]@($contourXld, $dummy, $start)) | Out-Null
$invalidArgs = [object[]]@($contourXld, $judgeValue, $null)
$invalid = [bool]$evaluateBinding.Invoke($window, $invalidArgs)
if ($invalid -or [string]::IsNullOrWhiteSpace([string]$invalidArgs[2])) { throw 'Incompatible XLD-to-Number preview did not produce a rejection.' }
$updateDrag.Invoke($window, [object[]]@($invalidEnd, $contourXld, $judgeValue, $true, $false, [string]$invalidArgs[2])) | Out-Null
$mainType.GetField('flowPortDragSession', $private).SetValue($window, $null)
Wait-Dispatcher 700
if ($previewPath.Visibility.ToString() -ne 'Visible' -or $dragStatus.Visibility.ToString() -ne 'Visible' -or $previewPath.Stroke.Color.ToString() -ne '#FFDC2626' -or [string]::IsNullOrWhiteSpace($dragValidation.Text)) {
    throw ('Rejected drag preview did not expose the expected red/actionable state: {0} / {1} / {2}' -f $previewPath.Visibility, $previewPath.Stroke.Color, $dragValidation.Text)
}
$rejectPath = [IO.Path]::Combine([IO.Path]::GetDirectoryName($OutputPath), [IO.Path]::GetFileNameWithoutExtension($OutputPath) + '-rejected.png')
$rejectSize = Save-WindowCapture $rejectPath
$cancelDrag.Invoke($window, [object[]]@('S21 rejected preview cancel', $false)) | Out-Null

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
$app.Shutdown()
Write-Output ('S21_UI_CAPTURE=PASS; ValidPath={0}; RejectPath={1}; Window={2}/{3}; Drag=OUT_TO_IN_PREVIEW/VALID_GREEN/REJECT_RED/COMPLETE/CANCEL; Dock=XLD_SEGMENT_MODE_SMOOTH_DISTANCES; Graph=IMAGE_TO_REGION_TO_XLD_TO_SEGMENTS_TO_NUMBER; Overlay=SEGMENTED_XLD' -f $OutputPath, $rejectPath, $validSize, $rejectSize)
