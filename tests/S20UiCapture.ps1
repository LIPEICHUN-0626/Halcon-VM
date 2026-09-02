param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s20-flow-selection-xld-filter.png')
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

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s20-ui-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'part-two-contours.pgm'
Write-Pgm $imagePath 640 420 {
    param($row, $column)
    if ($row -ge 70 -and $row -le 345 -and $column -ge 90 -and $column -le 545) {
        if ($row -ge 165 -and $row -le 255 -and $column -ge 255 -and $column -le 385) { return 45 }
        return 215
    }
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
$toolInstanceType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolInstance', $true)
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
$applySelection = $mainType.GetMethod('ApplyFlowToolSelection', $private)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)
$setFlowView = $mainType.GetMethod('SetFlowEditorView', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$flowZoom = $mainType.GetField('FlowZoomSlider', $allFields).GetValue($window)
$graphViewport = $mainType.GetField('FlowGraphViewport', $allFields).GetValue($window)
$selectionRectangle = $mainType.GetField('FlowGraphSelectionRectangle', $allFields).GetValue($window)
$selectionText = $mainType.GetField('FlowSelectionStatusText', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockTabs = $mainType.GetField('DockConfigurationTabs', $allFields).GetValue($window)
$dockPanel = $mainType.GetField('DockXldContourFilterPanel', $allFields).GetValue($window)
$imageContext = $mainType.GetField('ImageContextComboBox', $allFields).GetValue($window)

function New-Tool([string]$Kind, [string]$Name) {
    return $createTool.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}

function Invoke-Tool($Tool, [string]$Label) {
    try { return $executeTool.Invoke($window, [object[]]@($Tool, $Label)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

function Select-Tools([object[]]$Tools) {
    $typedTools = [Array]::CreateInstance($toolInstanceType, $Tools.Count)
    for ($index = 0; $index -lt $Tools.Count; $index++) { $typedTools.SetValue($Tools[$index], $index) }
    $primary = if ($Tools.Count -gt 0) { $Tools[$Tools.Count - 1] } else { $null }
    $applySelection.Invoke($window, [object[]]@($typedTools, $primary, $true)) | Out-Null
}

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool 'ImageSource' 'Part_Image'
$threshold = New-Tool 'ImageThreshold' 'Bright_Region'
$grayBranch = New-Tool 'GrayStat' 'Gray_Inspection_Branch'
$contour = New-Tool 'RegionContourMeasurement' 'Outer_And_Hole_XLD'
$filter = New-Tool 'XldContourFilter' 'Keep_Long_Contours'
$judge = New-Tool 'NumericJudge' 'Filtered_Length_Judge'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S20-UI'
$threshold.Parameters.ImageThresholdMinGray = 150
$threshold.Parameters.ImageThresholdMaxGray = 255
$contour.Parameters.RegionContourMode = 'BorderHoles'
$filter.Parameters.XldContourFeature = 'ContourLength'
$filter.Parameters.XldContourMin = 1000
$filter.Parameters.XldContourMax = 2500
$judge.NumericOperator = 'BetweenInclusive'
$judge.NumericLowerLimit = 1000
$judge.NumericUpperLimit = 2500
foreach ($tool in @($source, $threshold, $grayBranch, $contour, $filter, $judge)) { $flow.Add($tool) }
$threshold.SetInputBinding('Image', $source.ToolId, 'Image')
$grayBranch.SetInputBinding('Image', $source.ToolId, 'Image')
$contour.SetInputBinding('Region', $threshold.ToolId, 'Region')
$filter.SetInputBinding('XLD', $contour.ToolId, 'XLD')
$judge.InputToolId = $filter.ToolId
$judge.InputPortName = 'TotalLength'
foreach ($tool in @($source, $threshold, $grayBranch, $contour, $filter, $judge)) { Invoke-Tool $tool ('S20 UI ' + $tool.InstanceName) | Out-Null }

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

$flowZoom.Value = 0.42
$graphViewport.ScrollToHorizontalOffset(0)
$graphViewport.ScrollToVerticalOffset(0)
Select-Tools @($threshold, $contour, $filter)
$flowToolList.ScrollIntoView($filter)
$openConfiguration.Invoke($window, @()) | Out-Null
$rightTabs.SelectedItem = $dockTab
$dockTabs.SelectedIndex = 0
$imageContext.SelectedValue = 'ModuleInput'
$refresh.Invoke($window, @()) | Out-Null
Wait-Dispatcher 700

$left = [Math]::Min($threshold.GraphX, $filter.GraphX) - 12
$top = [Math]::Min($threshold.GraphY, $filter.GraphY) - 12
$right = [Math]::Max($threshold.GraphX + 360, $filter.GraphX + 360) + 12
$bottom = [Math]::Max($threshold.GraphY + $threshold.GraphHeight, $filter.GraphY + $filter.GraphHeight) + 12
[Windows.Controls.Canvas]::SetLeft($selectionRectangle, $left)
[Windows.Controls.Canvas]::SetTop($selectionRectangle, $top)
$selectionRectangle.Width = $right - $left
$selectionRectangle.Height = $bottom - $top
$selectionRectangle.Visibility = [Windows.Visibility]::Visible
$dockScroller = $dockTabs.SelectedContent -as [Windows.Controls.ScrollViewer]
if ($null -ne $dockScroller) { $dockScroller.ScrollToVerticalOffset(115) }
Wait-Dispatcher 900
if ($dockPanel.Visibility.ToString() -ne 'Visible') { throw 'XLD filter parameter panel is not visible.' }
if (-not $selectionText.Text.Contains('3/')) { throw 'Flow selection count is not visible.' }
$graphSize = Save-WindowCapture $OutputPath

$listPath = [IO.Path]::Combine([IO.Path]::GetDirectoryName($OutputPath), [IO.Path]::GetFileNameWithoutExtension($OutputPath) + '-sequence-list.png')
$selectionRectangle.Visibility = [Windows.Visibility]::Collapsed
$setFlowView.Invoke($window, [object[]]@($false)) | Out-Null
$flowToolList.ScrollIntoView($filter)
Wait-Dispatcher 900
$listSize = Save-WindowCapture $listPath

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
$app.Shutdown()
Write-Output ('S20_UI_CAPTURE=PASS; GraphPath={0}; ListPath={1}; Window={2}/{3}; Selection=3_VISIBLE/RUBBER_RECT/GRAPH_AND_LIST; Actions=COPY/DUPLICATE/ENABLE/DISABLE/DELETE; Dock=XLD_FEATURE_AND_RANGE; Graph=REGION_TO_XLD_TO_FILTERED_XLD_TO_NUMBER; Overlay=FILTERED_XLD' -f $OutputPath, $listPath, $graphSize, $listSize)
