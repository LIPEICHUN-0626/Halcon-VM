param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s18-port-subscription-image-difference.png')
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) {
    throw 'Run this UI capture with powershell.exe -STA.'
}

$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
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
    $width = [Math]::Max(1, [int][Math]::Ceiling($window.ActualWidth))
    $height = [Math]::Max(1, [int][Math]::Ceiling($window.ActualHeight))
    $capture = [Activator]::CreateInstance(
        [Windows.Media.Imaging.RenderTargetBitmap],
        [object[]]@($width, $height, 96.0, 96.0, [Windows.Media.PixelFormats]::Pbgra32))
    $capture.Render($window)
    $encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($capture))
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Create, [IO.FileAccess]::Write)
    try { $encoder.Save($stream) }
    finally { $stream.Dispose() }
    return "${width}x${height}"
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s18-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imageAPath = Join-Path $tempRoot 'surface-current.pgm'
$imageBPath = Join-Path $tempRoot 'surface-reference.pgm'
Write-Pgm $imageAPath 640 420 {
    param($row, $column)
    if ($row -ge 95 -and $row -le 234 -and $column -ge 115 -and $column -le 324) { return 190 }
    if ($row -ge 255 -and $row -le 354 -and $column -ge 405 -and $column -le 554) { return 150 }
    return 105
}
Write-Pgm $imageBPath 640 420 { param($row, $column) return 105 }

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
$consumers = $mainType.GetField('flowPortConsumers', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$refresh = $mainType.GetMethod('RefreshUiState', $private)
$updatePortInspector = $mainType.GetMethod('UpdateFlowPortInspector', $private)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$flowZoom = $mainType.GetField('FlowZoomSlider', $allFields).GetValue($window)
$graphViewport = $mainType.GetField('FlowGraphViewport', $allFields).GetValue($window)
$portInspector = $mainType.GetField('FlowPortInspectorBorder', $allFields).GetValue($window)
$inputPanel = $mainType.GetField('FlowPortInputEditorPanel', $allFields).GetValue($window)
$outputPanel = $mainType.GetField('FlowPortOutputConsumerPanel', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockTabs = $mainType.GetField('DockConfigurationTabs', $allFields).GetValue($window)
$dockPanel = $mainType.GetField('DockImageDifferencePanel', $allFields).GetValue($window)
$imageContext = $mainType.GetField('ImageContextComboBox', $allFields).GetValue($window)

function New-Tool([string]$Kind, [string]$Name) {
    return $createTool.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}

function Invoke-Tool($Tool, [string]$Label) {
    try { return $executeTool.Invoke($window, [object[]]@($Tool, $Label)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

function Select-Port([object]$Tool, [string]$PortName, [string]$Direction) {
    $mainType.GetField('selectedFlowConnectionKey', $private).SetValue($window, $null)
    $mainType.GetField('selectedFlowPortToolId', $private).SetValue($window, $Tool.ToolId)
    $mainType.GetField('selectedFlowPortName', $private).SetValue($window, $PortName)
    $mainType.GetField('selectedFlowPortDirection', $private).SetValue($window, $Direction)
    $flowToolList.SelectedItem = $Tool
    $refresh.Invoke($window, @()) | Out-Null
    $updatePortInspector.Invoke($window, @()) | Out-Null
}

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$sourceA = New-Tool 'ImageSource' 'Current_Surface'
$sourceB = New-Tool 'ImageSource' 'Reference_Surface'
$difference = New-Tool 'ImageDifference' 'Surface_Difference'
$threshold = New-Tool 'ImageThreshold' 'Difference_Threshold'
$dynamic = New-Tool 'DynamicThreshold' 'Local_Difference_Check'
$blob = New-Tool 'Blob' 'Changed_Area'
$gray = New-Tool 'GrayStat' 'Difference_Level'
$edge = New-Tool 'EdgeMeasure' 'Change_Contour'
$sourceA.Parameters.LocalImagePath = $imageAPath
$sourceA.Parameters.LocalImageSerialNumber = 'S18-UI-CURRENT'
$sourceB.Parameters.LocalImagePath = $imageBPath
$sourceB.Parameters.LocalImageSerialNumber = 'S18-UI-REFERENCE'
$difference.Parameters.ImageDifferenceMode = 'AbsoluteDifference'
$difference.Parameters.ImageDifferenceMultiplier = 2
$threshold.Parameters.ImageThresholdMinGray = 60
$threshold.Parameters.ImageThresholdMaxGray = 255
$dynamic.Parameters.DynamicThresholdPolarity = 'Light'
$dynamic.Parameters.DynamicThresholdOffset = 20
$blob.Parameters.BlobMinGray = 60
$blob.Parameters.BlobMaxGray = 255
$blob.Parameters.BlobMinArea = 100
$gray.Parameters.GrayMin = 0
$gray.Parameters.GrayMax = 255
$edge.Parameters.EdgeThreshold = 8
foreach ($tool in @($sourceA, $sourceB, $difference, $threshold, $dynamic, $blob, $gray, $edge)) { $flow.Add($tool) }
$difference.SetInputBinding('ImageA', $sourceA.ToolId, 'Image')
$difference.SetInputBinding('ImageB', $sourceB.ToolId, 'Image')
$threshold.SetInputBinding('Image', $difference.ToolId, 'Image')
$dynamic.SetInputBinding('OriginalImage', $difference.ToolId, 'Image')
$dynamic.SetInputBinding('ReferenceImage', $sourceB.ToolId, 'Image')
foreach ($consumer in @($blob, $gray, $edge)) {
    $consumer.SetInputBinding('Image', $difference.ToolId, 'Image')
    $consumer.SetInputBinding('ROI', $threshold.ToolId, 'Region')
}
foreach ($tool in @($sourceA, $sourceB, $difference, $threshold, $dynamic, $blob, $gray, $edge)) { Invoke-Tool $tool ('S18 UI ' + $tool.InstanceName) | Out-Null }

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

$flowZoom.Value = 0.34
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 2
$imageContext.SelectedValue = 'ModuleOutput'
Select-Port $difference 'ImageA' 'IN'
$graphViewport.ScrollToHorizontalOffset(0)
$graphViewport.ScrollToVerticalOffset(0)
Wait-Dispatcher 1000
if ($portInspector.Visibility.ToString() -ne 'Visible' -or $inputPanel.Visibility.ToString() -ne 'Visible') { throw 'Input port subscription inspector is not visible.' }
$inputSize = Save-WindowCapture $OutputPath

$consumerPath = [IO.Path]::Combine([IO.Path]::GetDirectoryName($OutputPath), [IO.Path]::GetFileNameWithoutExtension($OutputPath) + '-consumers.png')
Select-Port $difference 'Image' 'OUT'
Wait-Dispatcher 800
if ($portInspector.Visibility.ToString() -ne 'Visible' -or $outputPanel.Visibility.ToString() -ne 'Visible' -or $consumers.Count -ne 5) { throw 'Output consumer navigator is incomplete.' }
$consumerSize = Save-WindowCapture $consumerPath

$parameterPath = [IO.Path]::Combine([IO.Path]::GetDirectoryName($OutputPath), [IO.Path]::GetFileNameWithoutExtension($OutputPath) + '-parameters.png')
$mainType.GetField('selectedFlowPortToolId', $private).SetValue($window, $null)
$mainType.GetField('selectedFlowPortName', $private).SetValue($window, $null)
$mainType.GetField('selectedFlowPortDirection', $private).SetValue($window, $null)
$flowToolList.SelectedItem = $difference
$openConfiguration.Invoke($window, @()) | Out-Null
$dockTabs.SelectedIndex = 0
$refresh.Invoke($window, @()) | Out-Null
$dockScroller = $dockTabs.SelectedContent -as [Windows.Controls.ScrollViewer]
if ($null -ne $dockScroller) { $dockScroller.ScrollToVerticalOffset(105) }
Wait-Dispatcher 800
if ($dockPanel.Visibility.ToString() -ne 'Visible') { throw 'Image difference parameter panel is not visible.' }
$parameterSize = Save-WindowCapture $parameterPath

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
$app.Shutdown()
Write-Output ('S18_UI_CAPTURE=PASS; InputPath={0}; ConsumerPath={1}; ParameterPath={2}; Window={3}/{4}/{5}; Graph=CLICKABLE_PORTS/TWO_IMAGE_CONVERGENCE; PortInput=CURRENT/OPTIONS/REPLACE/CLEAR; PortOutput=FIVE_CONSUMERS/LOCATE; Dock=IMAGE_DIFFERENCE_PARAMS; HALCON=ABS_DIFF_IMAGE; Output=OK' -f $OutputPath, $consumerPath, $parameterPath, $inputSize, $consumerSize, $parameterSize)
