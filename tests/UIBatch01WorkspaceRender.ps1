param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$Phase = 'baseline',
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') { throw 'Run with powershell.exe -STA.' }
if ($Phase -notmatch '^[a-zA-Z0-9-]+$') { throw 'Invalid artifact phase.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot ('bin\x64\' + $Configuration + '\HalconWinFormsDemo.exe')))
$main = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$serviceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-ui-batch01-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp) | Out-Null
Write-Output ('ISOLATED_TEST_PID=' + $PID + '; STATE=' + $temp)
$service = $serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$app = New-Object Windows.Application
$app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown
$window = $null
function F([string]$name) {
    $field = $main.GetField($name, $all)
    if ($null -eq $field) { throw ('Missing field: ' + $name) }
    Write-Output -NoEnumerate ($field.GetValue($window))
}
function M([string]$name, [object[]]$arguments = @()) {
    $method = $main.GetMethods($private) | Where-Object { $_.Name -eq $name -and $_.GetParameters().Count -eq $arguments.Count } | Select-Object -First 1
    if ($null -eq $method) { throw ('Missing method: ' + $name) }
    $actual = [object[]]::new($arguments.Count)
    for ($index = 0; $index -lt $arguments.Count; $index++) {
        if ($null -eq $arguments[$index]) { $actual[$index] = $null }
        else { $actual[$index] = $arguments[$index].PSObject.BaseObject }
    }
    try { Write-Output -NoEnumerate ($method.Invoke($window, $actual)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Pump([int]$milliseconds) {
    $frame = New-Object Windows.Threading.DispatcherFrame
    $timer = New-Object Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromMilliseconds($milliseconds)
    $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false })
    $timer.Start(); [Windows.Threading.Dispatcher]::PushFrame($frame)
}
function Shot([string]$name, $surface = $window) {
    $directory = Join-Path $ProjectRoot $(if ($Phase -like 'ui02*') { 'artifacts\ui-batch-02' } else { 'artifacts\ui-batch-01' })
    [IO.Directory]::CreateDirectory($directory) | Out-Null
    $path = Join-Path $directory ($Phase + '-' + $name + '.png')
    $surface.UpdateLayout()
    $renderSurface=$surface
    if ($Phase -eq 'ui02block02' -and $surface -is [Windows.Window]) {
        if (-not $surface.IsVisible -or $null -eq $surface.Content) { throw 'Template window must remain visible with actual content.' }
        # Rendering the real content avoids transparent HWND snapshots when the
        # off-screen window compositor skips an inactive window after async work.
        $renderSurface=$surface.Content; $renderSurface.UpdateLayout()
    }
    $render = [Windows.Media.Imaging.RenderTargetBitmap]::new([int][Math]::Ceiling($renderSurface.ActualWidth), [int][Math]::Ceiling($renderSurface.ActualHeight), 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
    $render.Render($renderSurface)
    $encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($render))
    $stream = [IO.File]::Open($path, [IO.FileMode]::Create)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
    Write-Output ('WPF_RENDER=' + $path + '; NotDesktopCapture=True; NativeHalconSurfaceNotCaptured=True')
}
function CategoryExpanders {
    $stack = [Collections.Generic.Stack[Windows.DependencyObject]]::new()
    $stack.Push((F 'ToolCatalogList'))
    while ($stack.Count -gt 0) {
        $current = $stack.Pop()
        if ($current -is [Windows.Controls.Expander] -and $current.Name -eq 'ToolCatalogCategoryExpander') { Write-Output $current }
        for ($i = 0; $i -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($current); $i++) {
            $stack.Push([Windows.Media.VisualTreeHelper]::GetChild($current, $i))
        }
    }
}
try {
    $window = $main.GetConstructor($private, $null, [Type[]]@($serviceType, $loggerType), $null).Invoke([object[]]@($service.PSObject.BaseObject, $logger.PSObject.BaseObject))
    $window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
    $window.WindowState = [Windows.WindowState]::Normal
    $window.ShowActivated = $false
    $window.Left = -12000; $window.Top = -12000; $window.Width = 1366; $window.Height = 768
    $window.Show(); Pump 550
    if ((F 'flowTools').Count -ne 0) { throw 'Startup must be an explicit blank project.' }
    $right = F 'RightTabs'
    Write-Output ('RIGHT_TASK=' + $right.SelectedIndex + '; Content=' + ($null -ne $right.SelectedContent))
    if ($Phase -ne 'baseline') {
        if ($right.SelectedItem -ne (F 'ModuleResultTab') -or $null -eq $right.SelectedContent) { throw 'Default task must show real result content.' }
        if ((F 'DeveloperMetricStrip').Visibility -ne [Windows.Visibility]::Collapsed) { throw 'Engineering statistics must be hidden by default.' }
        if ((F 'WorkspaceRunDetailsExpander').IsExpanded) { throw 'Low-frequency run detail must start collapsed.' }
        M 'RightParametersTaskButton_Click' @($null, $null) | Out-Null
        if ((F 'centralModuleConfigurationOpenS77') -or $right.SelectedItem -ne (F 'ModuleResultTab')) { throw 'Empty parameter action must not open an empty editor.' }
    }
    Shot 'blank-1366x768'
    if ($Phase -eq 'block05') {
        if ((F 'ImageOneToOneButton').IsEnabled -or (F 'RoiToolsToggleButton').IsEnabled) { throw 'Empty image commands must be disabled.' }
        M 'ImageOriginalSizeButton_Click' @($null,$null) | Out-Null
        M 'RectangleRoiButton_Click' @($null,$null) | Out-Null
        if ((F 'pendingRoi') -ne $null -or (F 'roiLayers').Count -ne 0) { throw 'Blank image action created geometry.' }
    }
    if ($Phase -eq 'block07') {
        if ((F 'FlowRunSelectedButtonS89').IsEnabled -or (F 'FlowRunFullButtonS89').IsEnabled) { throw 'Blank workflow run actions must be disabled.' }
        M 'TryHandleRunShortcutS89' @([Windows.Input.Key]::F5,[Windows.Input.ModifierKeys]::None) | Out-Null
        if ((F 'isFlowExecutionActive') -or (F 'flowTools').Count -ne 0) { throw 'Blank F5 started work or created tools.' }
    }
    if ($Phase -eq 'block02') {
        $groups = @(CategoryExpanders)
        if ($groups.Count -ne 10 -or @($groups | Where-Object IsExpanded).Count -ne 1) { throw 'Default catalog must expose ten categories with only acquisition expanded.' }
        $list = F 'ToolCatalogList'
        foreach ($group in $groups) {
            $y = $group.TransformToAncestor($list).Transform([Windows.Point]::new(0,0)).Y
            if ($y -lt 0 -or $y + 28 -gt $list.ActualHeight) { throw ('Category not discoverable on first screen: ' + $group.DataContext.Name) }
        }
        $collapsed = @($groups | Where-Object { -not $_.IsExpanded })[0]
        $category = $collapsed.DataContext.Name
        $collapsed.IsExpanded = $true
        M 'RefreshToolCatalogView' | Out-Null
        Pump 100
        if (-not (@(CategoryExpanders) | Where-Object { $_.DataContext.Name -eq $category }).IsExpanded) { throw 'Refresh lost user category state.' }
        (F 'ToolSearchTextBox').Text = 'NCC'
        Pump 100
        if ($list.Items.Count -ne 1 -or @((CategoryExpanders) | Where-Object { -not $_.IsExpanded }).Count -ne 0) { throw 'Search must reveal the actual NCC result.' }
        Shot 'search-1366x768'
        $list.SelectedItem = $list.Items[0]
        # Invoke the original double-click handler without sending physical mouse input.
        M 'ToolCatalogList_MouseDoubleClick' @($null, $null) | Out-Null
        if ((F 'flowTools').Count -ne 1 -or (F 'flowTools')[0].Kind.ToString() -ne 'NccTemplateMatch') { throw 'Original catalog add action failed.' }
        if ((F 'centralModuleConfigurationOpenS77') -or (F 'isFlowExecutionActive')) { throw 'Adding must not open parameters or run.' }
        $addedId = (F 'flowTools')[0].ToolId
        $recipe = M 'CaptureRecipe'
        $savedPath = Join-Path $temp 'catalog-add.json'
        $service.SaveRecipe($savedPath, $recipe)
        $loaded = $service.LoadRecipe($savedPath)
        $savedFlow = @($loaded.Flows | Where-Object { $_.FlowId -eq $loaded.ActiveFlowId })
        if ($savedFlow.Count -ne 1 -or $savedFlow[0].Tools.Count -ne 1 -or $savedFlow[0].Tools[0].ToolId -ne $addedId -or $savedFlow[0].Tools[0].ToolType -ne 'NccTemplateMatch') { throw 'Saved catalog module identity was not preserved in the active schema3 flow.' }
        (F 'ToolSearchTextBox').Clear(); Pump 100
        if (-not (@(CategoryExpanders) | Where-Object { $_.DataContext.Name -eq $category }).IsExpanded) { throw 'Clearing search lost user category state.' }
        M 'ToolCatalogModeButton_Click' @((F 'ToolCatalogRecentButton'), $null) | Out-Null
        Pump 100
        if ($list.Items.Count -ne 1 -or @((CategoryExpanders) | Where-Object { -not $_.IsExpanded }).Count -ne 0) { throw 'Recent tool must be directly visible.' }
        $list.SelectedItem = $list.Items[0]
        M 'ToolCatalogToggleFavoriteMenuItem_Click' @($null, $null) | Out-Null
        M 'ToolCatalogModeButton_Click' @((F 'ToolCatalogFavoriteButton'), $null) | Out-Null
        Pump 100
        if ($list.Items.Count -ne 1 -or @((CategoryExpanders) | Where-Object { -not $_.IsExpanded }).Count -ne 0) { throw 'Favorite tool must be directly visible.' }
        M 'ToolCatalogModeButton_Click' @((F 'ToolCatalogAllButton'), $null) | Out-Null
        (F 'ToolSearchTextBox').Text = 'no-such-tool-unique-fixture'; Pump 100
        if ($list.Items.Count -ne 0 -or (F 'ToolCatalogEmptyText').Visibility -ne [Windows.Visibility]::Visible) { throw 'Empty search must explain no match.' }
        (F 'ToolSearchTextBox').Clear(); Pump 100
        # Dispose the added non-executed fixture before the independent render graph below.
        $added = (F 'flowTools')[0]
        (F 'FlowToolList').SelectedItem = $null
        (F 'flowTools').Clear(); $added.Dispose()
        Write-Output 'UI_BLOCK02_CATALOG=PASS; TenCategoriesOnFirstScreen=True; SearchVisible=True; ExpansionRestored=True; OriginalAddAndSave=True; RecentVisible=True; EmptySearch=True; NoFlowExecution=True'
    }
    $enumType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
    $flow = F 'flowTools'
    foreach ($definition in @(@('ImageSource','01 图像源'), @('ImageThreshold','02 阈值分割'), @('ConnectedObjectAnalysisS144','03 对象分析'), @('NumericJudge','04 数量判定'))) {
        $kind = [Enum]::Parse($enumType, $definition[0])
        if ($Phase -eq 'block10') {
            (F 'ToolCatalogList').SelectedItem = @((F 'toolCatalog') | Where-Object Kind -eq $kind)[0]
            M 'ToolCatalogList_MouseDoubleClick' @($null,$null) | Out-Null
            if ($flow[$flow.Count-1].Kind -ne $kind -or (F 'centralModuleConfigurationOpenS77') -or (F 'isFlowExecutionActive')) { throw 'Original catalog add did not preserve task scope.' }
        } else {
            $tool = M 'CreateFlowTool' @($kind, $definition[1], $true, $null)
            $flow.Add($tool)
        }
    }
    if ($Phase -eq 'block10') {
        $bindingArgs = [object[]]::new(3)
        $bindingArgs[0] = @($flow[0].FlowOutputPorts | Where-Object PortName -eq 'Image')[0].PSObject.BaseObject
        $bindingArgs[1] = @($flow[1].FlowInputPorts | Where-Object PortName -eq 'Image')[0].PSObject.BaseObject
        $bindingArgs[2] = ''
        if (-not $main.GetMethod('TryApplyFlowPortBinding',$private).Invoke($window,$bindingArgs)) { throw ('Original image binding failed: '+$bindingArgs[2]) }
    } else { $flow[1].SetInputBinding('Image', $flow[0].ToolId, 'Image') }
    $flow[2].SetInputBinding('Region', $flow[1].ToolId, 'Region')
    $flow[3].SetInputBinding('Value', $flow[2].ToolId, 'Count')
    $flow[3].InputToolId = $flow[2].ToolId
    $flow[3].InputPortName = 'Count'
    M 'RefreshFlowGraphLayout' | Out-Null
    if ($Phase -ne 'baseline') {
        (F 'FlowToolList').SelectedItem = $flow[1]
        $columnWidth = (F 'ToolCatalogColumn').Width
        $imageHeight = (F 'ImageWorkspaceRow').Height
        M 'RightResultTaskButton_Click' @($null, $null) | Out-Null
        if ((F 'centralModuleConfigurationOpenS77') -or $right.SelectedItem -ne (F 'ModuleResultTab')) { throw 'Result action must be read-only, not open parameters.' }
        if ((F 'ToolCatalogColumn').Width -ne $columnWidth -or (F 'ImageWorkspaceRow').Height -ne $imageHeight) { throw 'Result navigation changed workspace layout.' }
        if ((F 'FlowToolList').SelectedItem -ne $flow[1]) { throw 'Result navigation changed selected module.' }
        $flow[1].ErrorMessage = 'Isolated diagnostic visibility test'
        M 'RefreshInspector' | Out-Null
        if ((F 'SelectedResultErrorText').Visibility -ne [Windows.Visibility]::Visible -or (F 'SelectedResultErrorText').Text -ne $flow[1].ErrorMessage) { throw 'Actual module error must remain visible.' }
        $flow[1].ErrorMessage = ''
        M 'RefreshInspector' | Out-Null
        if ((F 'SelectedResultErrorText').Visibility -ne [Windows.Visibility]::Collapsed) { throw 'Empty error must not occupy default result space.' }
        Write-Output 'UI_BLOCK01_TASK_ROUTING=PASS; EmptyEditorRejected=True; ResultReadOnly=True; LayoutPreserved=True'
    }
    Pump 150
    Shot 'flow-1366x768'
    $window.Width = 1600; $window.Height = 900; Pump 100
    Shot 'flow-1600x900'
    if ($Phase -like 'block04*' -or $Phase -eq 'block05' -or $Phase -eq 'block06' -or $Phase -eq 'block07' -or $Phase -eq 'block10' -or $Phase -eq 'ui02block01') {
        $window.Width = 1366; $window.Height = 768; Pump 100
        $tool = $flow[1]
        if ($Phase -eq 'block05') {
            $tool = M 'CreateFlowTool' @([Enum]::Parse($enumType,'Blob'),'ROI对象检测',$true,$null)
            $tool.SetInputBinding('Image',$flow[0].ToolId,'Image')
            $flow.Insert(1,$tool)
            M 'RefreshUiState' | Out-Null
        }
        $beforeCatalog = (F 'ToolCatalogColumn').Width
        $beforeZoom = (F 'FlowGraphScaleTransform').ScaleX
        if ($Phase -eq 'block04' -or $Phase -eq 'block05' -or $Phase -eq 'block06' -or $Phase -eq 'block07' -or $Phase -eq 'block10' -or $Phase -eq 'ui02block01') {
            $pixels = [byte[]]::new(64*64)
            for ($y = 0; $y -lt 64; $y++) { for ($x = 0; $x -lt 64; $x++) { $pixels[$y*64+$x] = [byte]($x*4) } }
            $bitmap = [Windows.Media.Imaging.BitmapSource]::Create(64,64,96,96,[Windows.Media.PixelFormats]::Gray8,$null,$pixels,64)
            $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
            $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
            $imagePath = Join-Path $temp 'gray-ramp.png'
            $stream = [IO.File]::Create($imagePath)
            try { $encoder.Save($stream) } finally { $stream.Dispose() }
            $flow[0].Parameters.ImageSourceModeS114 = 'File'
            $flow[0].Parameters.LocalImagePath = $imagePath
            M 'RunStandaloneTool' @($flow[0], 'Parameter editor isolated image fixture') | Out-Null
            if ($flow[0].ResultCode -ne 'OK') { throw ('Image fixture failed: ' + $flow[0].ErrorMessage) }
        }
        M 'OpenCentralModuleConfigurationS77' @($tool, $false) | Out-Null
        Pump 100
        $editor = F 'moduleParameterWindowS89'
        if ($null -eq $editor -or $editor.ShowActivated -or $editor.IsActive -or $editor.Left -gt -10000) { throw 'Editor diagnostics must stay off-screen and non-active.' }
        Write-Output ('PARAMETER_SIZE=' + $editor.ActualWidth + 'x' + $editor.ActualHeight + '; MIN_WIDTH=' + (F 'DockImageThresholdMinTextBox').ActualWidth)
        Shot 'parameters-common' $editor
        if ($Phase -eq 'block07') {
            if ((F 'FlowRunShortcutHintS89').Text -notlike '*试运行*') { throw 'Open editor shortcut hint is not draft scoped.' }
            (F 'DockImageThresholdMinTextBox').Text = '100'; (F 'DockImageThresholdMaxTextBox').Text = '200'
            (F 'FlowToolList').SelectedItem = $flow[0]
            $sourceVersion = $flow[0].RuntimeOutputVersion
            M 'TryHandleRunShortcutS89' @([Windows.Input.Key]::F5,[Windows.Input.ModifierKeys]::None) | Out-Null
            Pump 100
            [double]$area = 0
            if (-not $tool.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 1664 -or $flow[0].RuntimeOutputVersion -ne $sourceVersion -or $tool.Parameters.ImageThresholdMinGray -ne 128 -or $flow[2].ResultCode -ne '--') { throw 'Shared F5 did not run only editing draft.' }
            $main.GetField('isContinuousRunning',$all).SetValue($window,$true)
            M 'RefreshUiState' | Out-Null
            if ((F 'FlowRunSelectedButtonS89').IsEnabled -or (F 'FlowRunFullButtonS89').IsEnabled) { throw 'Busy buttons remain enabled.' }
            $version = $tool.RuntimeOutputVersion
            M 'TryHandleRunShortcutS89' @([Windows.Input.Key]::F5,[Windows.Input.ModifierKeys]::None) | Out-Null
            if ($tool.RuntimeOutputVersion -ne $version -or -not (F 'isContinuousRunning')) { throw 'Busy F5 executed or silently changed scope.' }
            $task = M 'StartManualFlowRunAsync' @([Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmFlowRunRequestMode',$true),'Full'),'Isolated busy scope check',[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmRunOriginS135',$true),'Debug'))
            if (-not $task.IsCompleted -or -not (F 'isContinuousRunning')) { throw 'Busy manual command did not reject without implicit stop.' }
            M 'TryHandleRunShortcutS89' @([Windows.Input.Key]::F5,[Windows.Input.ModifierKeys]::Shift) | Out-Null
            if ((F 'isContinuousRunning') -or -not (F 'isStopRequested')) { throw 'Shift-F5 did not request original stop path.' }
            if (M 'TryHandleRunShortcutS89' @([Windows.Input.Key]::F5,[Windows.Input.ModifierKeys]::Control)) { throw 'Ctrl-F5 must not accidentally execute a run shortcut.' }
            M 'DockRevertButton_Click' @($null,$null) | Out-Null
            M 'CloseCentralModuleConfigurationS77' | Out-Null
            if ((F 'FlowRunShortcutHintS89').Text -notlike '*当前流程*') { throw 'Close editor did not restore flow shortcut hint.' }
            # Keep this real scope fixture to two local tools; no device or unrelated algorithm runs.
            while ($flow.Count -gt 2) { $removed = $flow[$flow.Count-1]; $flow.RemoveAt($flow.Count-1); $removed.Dispose() }
            (F 'FlowToolList').SelectedItem = $tool
            M 'RefreshUiState' | Out-Null
            $sourceVersion = $flow[0].RuntimeOutputVersion
            M 'RunCurrentToolButton_Click' @($null,$null) | Out-Null
            for ($i=0; $i -lt 40 -and (F 'isFlowExecutionActive'); $i++) { Pump 50 }
            [double]$area = 0
            if ((F 'isFlowExecutionActive') -or -not $tool.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 2048 -or $flow[0].RuntimeOutputVersion -ne $sourceVersion) { throw 'Selected module command reran source or used draft rather than applied parameters.' }
            $targetVersion = $tool.RuntimeOutputVersion
            M 'TryHandleRunShortcutS89' @([Windows.Input.Key]::F5,[Windows.Input.ModifierKeys]::None) | Out-Null
            for ($i=0; $i -lt 40 -and (F 'isFlowExecutionActive'); $i++) { Pump 50 }
            if ((F 'isFlowExecutionActive') -or $flow[0].RuntimeOutputVersion -le $sourceVersion -or $tool.RuntimeOutputVersion -le $targetVersion -or $tool.ResultCode -ne 'OK') { throw 'Closed-editor F5 did not run actual current two-tool flow.' }
            M 'RefreshUiState' | Out-Null
            Shot 'run-scope-1366x768'
            $sourceVersion = $flow[0].RuntimeOutputVersion
            $main.GetField('historicalImageMode',$all).SetValue($window,$true)
            M 'TryHandleRunShortcutS89' @([Windows.Input.Key]::F5,[Windows.Input.ModifierKeys]::None) | Out-Null
            $main.GetField('historicalImageMode',$all).SetValue($window,$false)
            if ($flow[0].RuntimeOutputVersion -ne $sourceVersion) { throw 'Historical F5 started live execution.' }
            M 'RefreshUiState' | Out-Null
            Write-Output 'UI_BLOCK07_RUN_SCOPE=PASS; BlankSafe=True; DraftOnlyArea1664=True; BusyNoImplicitSwitch=True; ShiftStopRequest=True; UnknownModifierIgnored=True; SelectedAppliedArea2048=True; FullTwoToolFlow=True; HistoricalReadOnly=True; NoMouse=True; NoDeviceConnection=True; NoAutomaticDraftApply=True'
        }
        if ($Phase -eq 'block05') {
            $optionType = $assembly.GetType('HalconWinFormsDemo.Models.VmImageContextOption',$true)
            $moduleInput = $optionType.GetField('ModuleInput').GetRawConstantValue()
            $sourcePrefix = $optionType.GetField('SourcePrefix').GetRawConstantValue()
            M 'SetImageContextSelection' @($moduleInput) | Out-Null
            M 'UpdateImageContextViewport' @($true) | Out-Null
            (F 'DockBlobMinTextBox').Text = '100'
            (F 'FlowToolList').SelectedItem = $flow[0]
            $imageContext = M 'ResolveImageContext'
            if (-not $imageContext.HasImage -or (M 'GetImageTaskToolS89') -ne $tool) { throw 'Open editor must own image task context.' }
            $sourceVersion = $flow[0].RuntimeOutputVersion
            M 'ImageOriginalSizeButton_Click' @($null,$null) | Out-Null
            Pump 50
            if ((F 'viewport').ZoomText -ne 'Zoom: 100%') { throw 'Original size is not actual 1:1.' }
            M 'FitImageButton_Click' @($null,$null) | Out-Null
            if ($flow[0].RuntimeOutputVersion -ne $sourceVersion -or (F 'DockBlobMinTextBox').Text -ne '100') { throw 'View navigation changed source or draft.' }
            M 'RectangleRoiButton_Click' @($null,$null) | Out-Null
            $roiType = $assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
            $rectangle = $roiType.GetMethod('CreateRectangle').Invoke($null,[object[]]@([double]5,[double]25,[double]15,[double]35))
            M 'SetPendingRoi' @($rectangle) | Out-Null
            M 'RefreshUiState' | Out-Null
            if (-not (F 'ConfirmImageRoiMenuS112').IsEnabled) { throw 'Valid pending ROI confirmation must be available.' }
            M 'ConfirmRoiButton_Click' @($null,$null) | Out-Null
            $layers = F 'roiLayers'
            if ($layers.Count -ne 1) { throw 'ROI was not created.' }
            $layer = $layers[0]
            Write-Output ('ROI_CONTEXT=' + $layer.ImageContextId + '; Target=' + (M 'ResolveTargetImageContextIdS86' @($tool)) + '; Bound=' + $tool.IsRoiBound($layer.RoiId) + '; SourceBound=' + $flow[0].IsRoiBound($layer.RoiId) + '; Editor=' + (F 'dockEditingTool').ToolId)
            if (-not $tool.IsRoiBound($layer.RoiId) -or $flow[0].IsRoiBound($layer.RoiId)) { throw 'ROI bound to ordinary selection instead of editing module.' }
            if ([string]::IsNullOrWhiteSpace($layer.ImageContextId) -or (F 'DockBlobMinTextBox').Text -ne '100' -or -not (F 'dockDraftDirty')) { throw 'ROI confirmation lost coordinate or unsaved scalar draft.' }
            if (@((F 'dockRoiBindingRows') | Where-Object { $_.RoiId -eq $layer.RoiId -and $_.IsBound }).Count -ne 1) { throw 'Applied editor would drop newly confirmed ROI.' }
            M 'TryHandleModuleParameterShortcutS89' @([Windows.Input.Key]::Enter,[Windows.Input.ModifierKeys]::Control) | Out-Null
            if (-not $tool.IsRoiBound($layer.RoiId)) { throw 'Draft apply dropped confirmed ROI.' }
            M 'RunStandaloneTool' @($tool,'Isolated ROI effect fixture') | Out-Null
            [double]$area = 0
            if ($tool.ResultCode -ne 'OK' -or -not $tool.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 121) { throw ('Actual ROI Blob area differs: '+$area+'; '+$tool.ErrorMessage) }
            M 'SetDualImageMode' @($true) | Out-Null
            $leftContext = M 'ResolveDualInputImageContext'
            $rightContext = M 'ResolveImageContext'
            if (-not $leftContext.HasImage -or (M 'GetImageCoordinateContextS112' @($leftContext)) -ne (M 'GetImageCoordinateContextS112' @($rightContext))) { throw 'Dual views mixed input coordinates.' }
            M 'SetDualImageMode' @($false) | Out-Null
            M 'LoadImage' @($imagePath,$false) | Out-Null
            if ((F 'currentImage') -eq $null) { throw 'Global image fallback fixture missing.' }
            M 'SetImageContextSelection' @($moduleInput) | Out-Null
            M 'UpdateImageContextViewport' @($true) | Out-Null
            M 'RectangleRoiButton_Click' @($null,$null) | Out-Null
            $rectangle = $roiType.GetMethod('CreateRectangle').Invoke($null,[object[]]@([double]10,[double]10,[double]20,[double]20))
            M 'SetPendingRoi' @($rectangle) | Out-Null
            $missing = M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageSource'),'未运行来源',$true,$null)
            $flow.Insert(1,$missing); M 'RefreshUiState' | Out-Null
            (F 'ImageContextComboBox').SelectedValue = $sourcePrefix + $missing.ToolId
            Pump 100
            Write-Output ('MISSING_SOURCE=' + (F 'ImageContextComboBox').SelectedValue + '; HasImage=' + (M 'ResolveImageContext').HasImage + '; Pending=' + ((F 'pendingRoi') -ne $null) + '; OriginalSizeEnabled=' + (F 'ImageOneToOneButton').IsEnabled)
            if ((M 'ResolveImageContext').HasImage -or (F 'pendingRoi') -ne $null -or (F 'ImageOneToOneButton').IsEnabled) { throw 'Missing explicit source reused global image or old pending drawing.' }
            M 'ConfirmRoiButton_Click' @($null,$null) | Out-Null
            if ($layers.Count -ne 1 -or -not $tool.IsRoiBound($layer.RoiId)) { throw 'Stale drawing committed or existing ROI was deleted.' }
            $flow.Remove($missing); $missing.Dispose()
            M 'RefreshUiState' | Out-Null
            if ((M 'ResolveImageContext').HasImage -or (M 'GetImageContextSelectionKeyS112') -ne ($sourcePrefix + $missing.ToolId)) { throw 'Removed explicit source silently fell back to another image.' }
            $recipe = M 'CaptureRecipe'; $savedPath = Join-Path $temp 'roi-context.json'
            $service.SaveRecipe($savedPath,$recipe); $loaded = $service.LoadRecipe($savedPath)
            $savedFlow = @($loaded.Flows | Where-Object { $_.FlowId -eq $loaded.ActiveFlowId })[0]
            $savedTool = @($savedFlow.Tools | Where-Object ToolId -eq $tool.ToolId)[0]
            if ($loaded.RoiLayers.Count -ne 1 -or $loaded.RoiLayers[0].ImageContextId -ne $layer.ImageContextId -or $savedTool.RoiIds -notcontains $layer.RoiId) { throw 'ROI coordinate/binding save roundtrip failed.' }
            Shot 'image-source-guard-1366x768'
            M 'SetImageContextSelection' @($moduleInput) | Out-Null
            M 'UpdateImageContextViewport' @($true) | Out-Null
            M 'RefreshUiState' | Out-Null
            Shot 'image-roi-1366x768'
            if ((F 'ToolCatalogColumn').Width -ne $beforeCatalog -or (F 'FlowGraphScaleTransform').ScaleX -ne $beforeZoom) { throw 'Image/ROI tasks changed main layout.' }
            Write-Output 'UI_BLOCK05_IMAGE_ROI=PASS; EmptyActionsSafe=True; OriginalSize100=True; EditorOwnsRoi=True; DraftPreserved=True; AppliedBindingPreserved=True; ActualHalconArea=121; DualCoordinateSame=True; StalePendingRejected=True; MissingAndRemovedSourceNoGlobalFallback=True; SaveCoordinates=True; NoMouse=True; NoDeviceConnection=True; NoFullFlowExecution=True'
        }
        if ($Phase -eq 'block04' -or $Phase -eq 'block06' -or $Phase -eq 'block10' -or $Phase -eq 'ui02block01') {
            if ($editor.Width -ne 520 -or $editor.Height -ne 480 -or (F 'DockImageThresholdMinTextBox').ActualWidth -gt 140) { throw 'Parameter window or numeric field is not compact.' }
            if ((F 'DockAdvancedParameterToggleButton').Visibility -ne [Windows.Visibility]::Collapsed) { throw 'Empty advanced command must not occupy the common page.' }
            if ((F 'ToolCatalogColumn').Width -ne $beforeCatalog -or (F 'FlowGraphScaleTransform').ScaleX -ne $beforeZoom) { throw 'Parameter editor squeezed or zoomed main workspace.' }
            (F 'DockImageThresholdMinTextBox').Text = '100'
            (F 'DockImageThresholdMaxTextBox').Text = '200'
            $ctrl = [Windows.Input.ModifierKeys]::Control
            $taskNames = @('DockParametersTaskTab','DockIoTaskTab','DockRoiTaskTab','DockResultTaskTab')
            for ($index = 0; $index -lt 4; $index++) {
                $key = [Enum]::Parse([Windows.Input.Key], ('D'+($index+1)))
                if (-not (M 'TryHandleModuleParameterShortcutS89' @($key,$ctrl)) -or (F 'DockConfigurationTabs').SelectedItem -ne (F $taskNames[$index])) { throw 'Four-task shortcut did not select original page.' }
                if ((F 'dockEditingTool') -ne $tool -or (F 'DockImageThresholdMinTextBox').Text -ne '100' -or $tool.Parameters.ImageThresholdMinGray -ne 128) { throw 'Task switching changed module or applied draft.' }
                Pump 50; Shot ('parameters-task'+($index+1)) $editor
            }
            (F 'FlowToolList').SelectedItem = $flow[0]
            if ((F 'dockEditingTool') -ne $tool) { throw 'Ordinary selection switched the open editor.' }
            $sourceVersion = $flow[0].RuntimeOutputVersion
            M 'DockTrialRunButton_Click' @($null,$null) | Out-Null
            Pump 100
            [double]$area = 0
            if ($tool.ResultCode -ne 'OK' -or -not $tool.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 1664) { throw ('Actual draft threshold result differs: ' + $area + '; ' + $tool.ErrorMessage) }
            if ($tool.Parameters.ImageThresholdMinGray -ne 128 -or -not (F 'dockDraftDirty') -or $flow[0].RuntimeOutputVersion -ne $sourceVersion -or $flow[2].ResultCode -ne '--') { throw 'Current-module trial applied draft or executed another module.' }
            Shot 'parameters-draft-result' $editor
            if ($Phase -eq 'block06') {
                if ($tool.RuntimeResultOrigin.ToString() -ne 'DraftTrial' -or (F 'DockResultOriginTextS88').Text -notlike '*草稿试运行*') { throw 'Draft result origin is missing.' }
                (F 'FlowToolList').SelectedItem = $tool
                $tool.OutputSummary = 'Area=999999 fabricated summary fixture'
                M 'RefreshInspector' | Out-Null
                if ((F 'SelectedResultOutputText').Text -notlike '*1664*' -or (F 'SelectedResultOutputText').Text -like '*999999*' -or (F 'SelectedResultOriginTextS88').Text -notlike '*草稿试运行*') { throw 'Overview must use typed keys and explicit draft origin, not summary text.' }
            }
            if (-not (M 'TryHandleModuleParameterShortcutS89' @([Windows.Input.Key]::Enter,$ctrl))) { throw 'Apply shortcut not handled.' }
            if ($tool.Parameters.ImageThresholdMinGray -ne 100 -or $tool.Parameters.ImageThresholdMaxGray -ne 200 -or (F 'dockDraftDirty')) { throw 'Apply shortcut did not commit original draft.' }
            $recipe = M 'CaptureRecipe'; $savedPath = Join-Path $temp 'parameter-draft.json'
            $service.SaveRecipe($savedPath,$recipe); $loaded = $service.LoadRecipe($savedPath)
            $savedFlow = @($loaded.Flows | Where-Object { $_.FlowId -eq $loaded.ActiveFlowId })[0]
            $savedTool = @($savedFlow.Tools | Where-Object ToolId -eq $tool.ToolId)[0]
            if ($savedTool.Parameters.ImageThresholdMinGray -ne 100 -or $savedTool.Parameters.ImageThresholdMaxGray -ne 200) { throw 'Applied parameter save roundtrip failed.' }
            Shot 'parameters-result' $editor
            if ($Phase -eq 'block06') {
                if ($tool.ElapsedMilliseconds -ne 0 -or (F 'DockResultElapsedTextS88').Text -ne '等待运行' -or (F 'DockTaskResultItemsControlS88').Visibility -ne [Windows.Visibility]::Collapsed -or $tool.RuntimeResultOrigin.ToString() -ne 'Unspecified') { throw 'Apply kept stale timing, keys or origin.' }
                M 'RunStandaloneTool' @($tool,'Isolated applied result fixture') | Out-Null
                if ($tool.RuntimeResultOrigin.ToString() -ne 'ModuleExecution' -or (F 'DockResultOriginTextS88').Text -notlike '*模块执行*') { throw 'Applied module result origin not distinguished.' }
                $tool.SetOutputValue('Area','999')
                M 'RefreshDockConfigurationRuntime' @($tool) | Out-Null
                $row = @((F 'dockTaskResultRowsS88') | Where-Object PortName -eq 'Area')[0]
                if ($row.StateKey -ne 'Invalid' -or $row.ValueText -like '*999*' -or (F 'DockResultTaskErrorBorderS88').Visibility -ne [Windows.Visibility]::Visible) { throw 'Wrong scalar type was parsed or hidden.' }
                $tool.SetOutputValue('Area',[double]::NaN)
                M 'RefreshDockConfigurationRuntime' @($tool) | Out-Null
                if (@((F 'dockTaskResultRowsS88') | Where-Object { $_.PortName -eq 'Area' -and $_.StateKey -eq 'Invalid' }).Count -ne 1) { throw 'Nonfinite scalar displayed as valid.' }
                M 'RunStandaloneTool' @($tool,'Isolated recovery of genuine typed result') | Out-Null
                Shot 'result-applied' $editor
                $tool.SetOutputValue('Area',[double]123456)
                $flow[0].ClearRuntimeOutputs()
                $failedAsExpected = $false
                try { M 'ExecuteFlowTool' @($tool,'Isolated missing upstream error fixture') | Out-Null }
                catch { $failedAsExpected = $true }
                if (-not $failedAsExpected) { throw 'Missing upstream did not fail real execution.' }
                if ([string]::IsNullOrWhiteSpace($tool.ErrorMessage) -or (F 'DockResultVerdictTextS88').Text -ne '异常' -or (F 'DockResultTaskErrorBorderS88').Visibility -ne [Windows.Visibility]::Visible -or @((F 'dockTaskResultRowsS88') | Where-Object { $_.ValueText -like '*123456*' }).Count -ne 0) { throw 'Failure reused old value or masked exception as business NG.' }
                Shot 'result-error' $editor
                Write-Output 'UI_BLOCK06_RESULTS=PASS; GenuineHalconArea=1664; DraftOrigin=True; AppliedOrigin=True; TypedOverviewNotSummary=True; ApplyClearsTimingAndKeys=True; InvalidAndNonfiniteScalarRejected=True; FailureClearsOldValues=True; NoMouse=True; NoFullFlowExecution=True'
            }
            Write-Output 'UI_BLOCK04_PARAMETERS=PASS; Compact=True; FourTasks=True; SingleDraft=True; SelectionDoesNotSwitch=True; ActualHalconArea=1664; TrialNotApplied=True; ApplyAndSave=True; MainLayoutPreserved=True; NoMouse=True; NoFullFlowExecution=True'
        }
        $main.GetField('dockDraftDirty', $all).SetValue($window, $false)
        M 'CloseCentralModuleConfigurationS77' | Out-Null
    }
    if ($Phase -eq 'ui02block10') {
        $beforeIds=@((F 'flowTools')|ForEach-Object ToolId) -join ','
        $threshold=(F 'flowTools')[1]
        M 'OpenCentralModuleConfigurationS77' @($threshold,$false)|Out-Null
        (F 'FlowToolList').SelectedItem=(F 'flowTools')[0]
        M 'RecipeAutosaveTimer_Tick' @($null,$null)|Out-Null
        $dirtyBefore=F 'isRecipeDirty';$draftBefore=F 'dockDraftDirty'
        $definitionBefore=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
        M 'OpenCurrentModuleHelpMenuItem_ClickS78' @($null,$null)|Out-Null;Pump 80
        $help=F 'helpCenterWindowS78';$ht=$help.GetType()
        function HF10([string]$name) { Write-Output -NoEnumerate ($ht.GetField($name,$all).GetValue($help)) }
        function HelpText10 { $d=HF10 'HelpDocument';[Windows.Documents.TextRange]::new($d.ContentStart,$d.ContentEnd).Text }
        if((HF10 'HelpTopicList').SelectedItem.ToolKind -ne $threshold.Kind){throw 'Current help followed single-selected source instead of edited threshold.'}
        $toolTopics=@((HF10 'allTopics')|Where-Object {$null -ne $_.ToolKind})
        foreach($topic in $toolTopics) {
            (HF10 'HelpTopicList').SelectedItem=$topic
            $sections=HF10 'helpSectionsUI10';$doc=HF10 'HelpDocument'
            if($sections.Count -lt 3 -or @($sections|Where-Object Title -eq '输入').Count -ne 1 -or @($sections|Where-Object Title -eq '输出').Count -ne 1){throw ('Actual module sections incomplete: '+$topic.Title)}
            foreach($section in $sections){if(-not $doc.Blocks.Contains($section.Target)){throw 'Section points outside the current document.'}}
        }
        $longTopic=@($toolTopics|Where-Object {$_.ToolKind.ToString() -eq 'CSharpDllScript'})[0]
        (HF10 'HelpTopicList').SelectedItem=$longTopic;Pump 30
        $picker=HF10 'HelpSectionPickerUI10';$viewer=HF10 'HelpDocumentViewer'
        $queue=New-Object 'System.Collections.Generic.Queue[System.Windows.DependencyObject]';$queue.Enqueue($viewer);$scroll=$null
        while($queue.Count -gt 0 -and $null -eq $scroll) {
            $visual=$queue.Dequeue()
            if($visual -is [Windows.Controls.ScrollViewer]){$scroll=$visual;break}
            for($i=0;$i -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($visual);$i++){$queue.Enqueue([Windows.Media.VisualTreeHelper]::GetChild($visual,$i))}
        }
        if($null -eq $scroll){throw 'Actual help scroll viewer not found.'}
        $textBefore=HelpText10
        $picker.SelectedItem=@($picker.Items|Where-Object Title -eq '常见问题')[0];Pump 60
        if($scroll.VerticalOffset -le 0 -or (HelpText10) -ne $textBefore){throw 'Section selection did not scroll the existing document or replaced its content.'}
        Shot 'help-section-jump' $help
        $oldSection=$picker.SelectedItem
        $picker.SelectedIndex=0;Pump 40
        if($scroll.VerticalOffset -gt (HF10 'HelpDocument').PagePadding.Top+1){throw ('Overview did not return to the first heading: '+$scroll.VerticalOffset)}
        (HF10 'HelpTopicList').SelectedItem=@((HF10 'allTopics')|Where-Object Key -eq 'Workspace')[0];Pump 40
        if($picker.SelectedIndex -ne 0 -or $scroll.VerticalOffset -gt (HF10 'HelpDocument').PagePadding.Top+1 -or (HF10 'helpSectionsUI10').Contains($oldSection)){throw 'New topic reused old anchors or scroll position.'}
        $text=HelpText10
        if($text -notlike '*普通阈值支持检测ROI和屏蔽*' -or $text -like '*普通阈值分割目前不支持*'){throw 'Workspace help still contradicts implemented ROI behavior.'}
        Shot 'help-current-workspace' $help
        $help.SelectToolTopicUI10($null);Pump 20
        $text=HelpText10
        if($text -notlike '*多种组合明确选择*' -or $text -notlike '*按顶部所选运行范围*' -or $text -notlike '*应用会清旧输出*'){throw 'Quick start still describes old binding, F5 scope or apply behavior.'}
        (HF10 'HelpSearchTextBox').Text='ui10-no-such-help';Pump 20
        if((HF10 'helpSectionsUI10').Count -ne 0 -or (HF10 'HelpSectionBarUI10').Visibility -ne 'Collapsed' -or (HelpText10) -notlike '*没有匹配*'){throw 'Empty search retained old section navigation.'}
        $help.SelectToolTopicUI10($threshold.Kind);Pump 30
        if((HF10 'HelpSectionBarUI10').Visibility -ne 'Visible' -or $picker.SelectedIndex -ne 0){throw 'Reopening actual tool help did not recover current sections.'}
        if((@((F 'flowTools')|ForEach-Object ToolId) -join ',') -ne $beforeIds -or (F 'dockEditingTool') -ne $threshold -or (F 'dockDraftDirty') -ne $draftBefore -or (F 'isRecipeDirty') -ne $dirtyBefore -or $service.GetRecipeFingerprint((M 'CaptureRecipe')) -ne $definitionBefore -or @((F 'flowTools')|Where-Object {$null -ne $_.LastOutputUtc}).Count -ne 0){throw ('Read-only help changed project, active draft or ran tools. DirtyBefore='+$dirtyBefore+' DirtyAfter='+(F 'isRecipeDirty')+' DraftBefore='+$draftBefore+' DraftAfter='+(F 'dockDraftDirty'))}
        if($help.IsActive -or $help.ShowActivated -or $help.Left -gt -10000 -or $window.IsActive){throw 'Help validation activated foreground.'}
        $help.Close();M 'CloseCentralModuleConfigurationS77'|Out-Null
        Write-Output ('UI02_BLOCK10_HELP=PASS; ActualModulePages='+$toolTopics.Count+'; CurrentEditedTool=True; ActualAnchorScroll=True; OverviewAndTopicReset=True; EmptySearchClearsAnchors=True; RoiBindingF5InstructionsCorrected=True; NoProjectMutation=True; NoMouse=True')
    }
    if ($Phase -eq 'ui02block09') {
        $window.Width=1366;$window.Height=768
        $menu=F 'ExampleProgramsMenuS78'
        foreach($prefix in @('检测入门','定位应用','测量应用')) {
            $entry=@($menu.Items|Where-Object {$_ -is [Windows.Controls.MenuItem] -and $_.Header -like ($prefix+'*')})
            if($entry.Count -ne 1 -or $entry[0].HasItems){throw ('Learning task is not directly reachable: '+$prefix)}
        }
        function Load09([string]$key) {
            $main.GetField('isRecipeDirty',$all).SetValue($window,$false)
            M 'LoadExampleProgramS78' @($key)|Out-Null;Pump 100
            if(@((F 'flowTools')|Where-Object {$null -ne $_.LastOutputUtc}).Count -ne 0 -or (F 'isFlowExecutionActive')){throw 'Loading example ran tools.'}
            $guide=F 'learningGuideWindowS146'
            if($null -eq $guide -or $guide.ShowActivated -or $guide.IsActive -or $guide.Left -gt -10000){throw ('Learning window isolation failed: key='+$key+' Exists='+($null -ne $guide)+' ShowActivated='+$guide.ShowActivated+' IsActive='+$guide.IsActive+' Left='+$guide.Left+' OwnerActive='+$window.IsActive+' OwnerLeft='+$window.Left)}
            if((F 'learningStepDetailsS148').IsExpanded -or -not (F 'learningStepsListS146').IsVisible -or (F 'learningStepsListS146') -isnot [Windows.Controls.ComboBox]){throw 'Step selection still needs expanded technical detail.'}
        }
        Load09 'project-object-production'
        $flow=F 'flowTools';$source=$flow[0];$threshold=$flow[1];$objects=$flow[2];$stats=$flow[3];$judge=$flow[4];$gray=$flow[5]
        if($flow.Count -ne 6 -or (F 'learningStepsListS146').Items.Count -ne 9){throw 'Original object project/learning steps changed.'}
        $source.Parameters.LocalImagePath='';M 'RefreshLearningGuideS146'|Out-Null
        if((F 'learningStepGapS148').Text -notlike '先选择图片*'){throw 'Missing source has no nearby actionable instruction.'}
        (F 'learningStepsListS146').SelectedIndex=2;Pump 40
        if((F 'learningStepTitleS146').Text -ne '学习步骤 3 / 9' -or (F 'centralModuleConfigurationOpenS77') -or @($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count -ne 0){throw 'Selecting a learning step opened/ran a module.'}
        (F 'learningStepsListS146').SelectedIndex=0
        M 'InvokeLearningStepS146'|Out-Null;Pump 80
        if((F 'dockEditingTool') -ne $source){throw 'Explicit learning action did not open the source.'}
        $path=Join-Path $temp 'ui09-learning-image.png';[HalconDotNet.HObject]$base=$null;[HalconDotNet.HObject]$image=$null
        try {
            [HalconDotNet.HOperatorSet]::GenImageConst([ref]$base,'byte',32,32)
            [HalconDotNet.HOperatorSet]::ScaleImage($base,[ref]$image,1,64)
            [HalconDotNet.HOperatorSet]::WriteImage($image,'png',0,$path)
        } finally {if($image){$image.Dispose()};if($base){$base.Dispose()}}
        $lease=M 'CaptureEngineeringEditLeaseS137' @('UI09 source choice')
        M 'SelectSourceFileDraftUI03' @($path,$lease)|Out-Null
        M 'RefreshLearningGuideS146'|Out-Null
        if((F 'learningStepGapS148').Text -notlike '当前有未应用参数*'){throw 'Source draft was reported as applied.'}
        (F 'learningStepsListS146').SelectedIndex=1;Pump 20
        if((F 'dockEditingTool') -ne $source -or -not (F 'dockDraftDirty') -or (F 'DockLocalImagePathTextBox').Text -ne $path){throw 'Step choice replaced the active source draft.'}
        (F 'learningStepsListS146').SelectedIndex=0
        M 'DockTrialRunButton_Click' @($null,$null)|Out-Null
        if(-not [string]::IsNullOrWhiteSpace($source.Parameters.LocalImagePath)){throw 'Learning preview applied source without consent.'}
        M 'ApplyDockConfigurationDraft' @($true)|Out-Null
        M 'RefreshLearningGuideS146'|Out-Null
        if($source.Parameters.LocalImagePath -ne $path -or $null -ne $source.LastOutputUtc -or (F 'learningStepGapS148').Text -notlike '*应用后重新运行*'){throw 'Source apply kept draft output or guide did not ask for rerun.'}
        Shot 'learning-source-after-apply' (F 'learningGuideWindowS146')
        M 'DockTrialRunButton_Click' @($null,$null)|Out-Null
        if($null -eq $source.LastOutputUtc){throw 'Explicit post-apply source read produced no output.'}
        M 'CloseCentralModuleConfigurationS77'|Out-Null
        $full=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmFlowRunRequestMode',$true),'Full')
        $manual=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmRunOriginS135',$true),'ManualFull')
        $task=M 'RunInspectionRangeAsync' @($full,'UI09 explicit object learning run',$false,$true,$manual)
        $deadline=[DateTime]::UtcNow.AddSeconds(20)
        while(-not $task.IsCompleted -and [DateTime]::UtcNow -lt $deadline){Pump 50}
        if(-not $task.IsCompleted){throw 'Learning full run timeout.'}
        $outcome=$task.GetAwaiter().GetResult()
        if($outcome.ExecutionError -or $outcome.ResultCode -ne 'OK'){throw ('Actual learning run failed: '+$outcome.StopReason)}
        [double]$value=0
        if(-not $objects.TryGetNumericOutput('Count',[ref]$value) -or $value -ne 1){throw 'Actual object count is not 1.'}
        if(-not $stats.TryGetNumericOutput('Sum',[ref]$value) -or $value -ne 1024){throw 'Actual area-array downstream sum is not 1024.'}
        if(-not $gray.TryGetNumericOutput('Mean',[ref]$value) -or $value -ne 64){throw 'Actual conditional gray downstream is not 64.'}
        $saved=Join-Path $temp 'ui09-object.json';$service.SaveRecipe($saved,(M 'CaptureRecipe'));$loaded=$service.LoadRecipe($saved)
        $active=@($loaded.Flows|Where-Object FlowId -eq $loaded.ActiveFlowId)[0]
        if($active.Tools.Count -ne 6 -or $loaded.ProductionRunLayout.ResultItemsS141.Count -ne 4){throw 'Learning save lost tools or production cards.'}
        M 'ApplyRecipe' @($loaded)|Out-Null;M 'RefreshLearningGuideS146'|Out-Null
        if((M 'IsLearningSessionCurrentS146') -or (F 'learningStepsListS146').IsEnabled -or @((F 'flowTools')|Where-Object {$null -ne $_.LastOutputUtc}).Count -ne 0){throw 'Reopen restored runtime values or stale navigation identity.'}
        foreach($key in @('project-alignment-line','project-world-distance')) {
            Load09 $key
            $nodes=F 'flowTools';$step=if($key -eq 'project-alignment-line'){1}else{3}
            (F 'learningStepsListS146').SelectedIndex=$step;Pump 40
            $expected=if($step -eq 1){'配置模板'}else{'配置标定'}
            Write-Output ('UI09_PREREQUISITE='+[string](F 'learningStepActionS146').Content)
            if((F 'learningStepGapS148').Text -notlike '先处理*' -or (F 'learningStepActionS146').Content -ne $expected){throw 'Missing model/calibration lacks its precise prerequisite action.'}
            if($step -eq 1 -and $nodes[1].Parameters.NccMatchingS131.HasModel){throw 'Loading example fabricated a model.'}
            if($step -eq 3 -and $nodes[3].Parameters.PlanarCalibrationSamplesS124.Count -ne 0){throw 'Loading example fabricated calibration samples.'}
            M 'InvokeLearningStepS146'|Out-Null;Pump 80
            if((F 'dockEditingTool') -ne $nodes[$step] -or @($nodes|Where-Object {$null -ne $_.LastOutputUtc}).Count -ne 0){throw 'Prerequisite action opened wrong module or executed an algorithm.'}
            Shot ('learning-'+$key) (F 'learningGuideWindowS146')
            M 'CloseCentralModuleConfigurationS77'|Out-Null
            if($step -eq 1) {
                (F 'learningStepsListS146').SelectedIndex=2;Pump 20
                if((F 'learningStepActionS146').Content -ne '配置固定参考' -or $nodes[2].GetInputBinding('Pose').SourcePortName -ne 'SelectedPose' -or $nodes[3].GetInputBinding('Image').SourceToolId -ne $nodes[2].ToolId){throw 'Alignment example lost explicit reference or typed pose/image chain.'}
            } else {
                if($nodes[2].GetInputBinding('Points').SourcePortName -ne 'KeptPoints' -or $nodes[3].GetInputBinding('PointA').SourcePortName -ne 'StartPoint' -or $nodes[4].GetInputBinding('PointB').SourcePortName -ne 'WorldPointB'){throw 'Measurement example lost its real typed geometry chain.'}
            }
            $file=Join-Path $temp ($key+'.json');$service.SaveRecipe($file,(M 'CaptureRecipe'));$reopened=$service.LoadRecipe($file)
            $restoredTools=@($reopened.Flows|Where-Object FlowId -eq $reopened.ActiveFlowId)[0].Tools
            if($restoredTools.Count -ne $nodes.Count){throw 'Learning configuration failed save/load.'}
            for($index=0;$index -lt $nodes.Count;$index++) {
                if($restoredTools[$index].ToolId -ne $nodes[$index].ToolId -or (ConvertTo-Json -Compress -InputObject @($restoredTools[$index].InputBindings)) -ne (ConvertTo-Json -Compress -InputObject @($nodes[$index].InputBindings))){throw 'Save/reopen changed typed subscriptions or module identity.'}
            }
            M 'ApplyRecipe' @($reopened)|Out-Null;M 'RefreshLearningGuideS146'|Out-Null
            if((M 'IsLearningSessionCurrentS146') -or @((F 'flowTools')|Where-Object {$null -ne $_.LastOutputUtc}).Count -ne 0){throw 'Reopened learning example retained stale navigation or outputs.'}
        }
        Write-Output 'UI02_BLOCK09_LEARNING=PASS; DirectTaskMenus=True; CompactStepChoice=True; ExplicitNavigationOnly=True; SourceDraftApplyRerun=True; ActualObjectCount1AreaSum1024ConditionalGray64=True; Schema3SaveReopen=True; MissingModelCalibrationNoAutoExecution=True; NoMouse=True'
    }
    if ($Phase -eq 'ui02block08') {
        $window.Width=1366;$window.Height=768
        $flows=F 'projectFlowsS104';$first=$flows[0]
        $full=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmFlowRunRequestMode',$true),'Full')
        $manual=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmRunOriginS135',$true),'ManualFull')
        function Full08 {
            $task=M 'RunInspectionRangeAsync' @($full,'UI08 explicit full run',$false,$true,$manual)
            $deadline=[DateTime]::UtcNow.AddSeconds(20)
            while(-not $task.IsCompleted -and [DateTime]::UtcNow -lt $deadline){Pump 50}
            if(-not $task.IsCompleted){throw 'UI08 full execution did not finish in the bounded test wait.'}
            $outcome=$task.GetAwaiter().GetResult()
            if($outcome.ResultCode -ne 'OK' -or $outcome.ExecutionError){throw ('UI08 actual run failed: '+$outcome.StopReason)}
        }
        $path=Join-Path $temp 'ui08-image.png';[HalconDotNet.HObject]$image=$null
        try{[HalconDotNet.HOperatorSet]::GenImageConst([ref]$image,'byte',32,32);[HalconDotNet.HOperatorSet]::WriteImage($image,'png',0,$path)}finally{if($image){$image.Dispose()}}
        $flow[0].Parameters.ImageSourceModeS114='File';$flow[0].Parameters.LocalImagePath=$path
        $flow[1].Parameters.ImageThresholdMinGray=0;$flow[1].Parameters.ImageThresholdMaxGray=255
        (F 'AutoSendMatchResultCheckBox').IsChecked=$false
        Full08
        $store=F 'productionRunStoreS135';$records=@($store.ReadRecent(20).Records)
        if($records.Count -ne 1 -or $records[0].FlowId -ne $first.FlowId -or $records[0].PageMode -ne 'Developer'){throw 'Engineering root run did not preserve its original Flow/PageMode in the real journal.'}
        $firstRunId=$records[0].RunId;$version=$flow[0].RuntimeOutputVersion
        M 'SetProductionRunMode' @($true)|Out-Null;Pump 60
        if(-not (F 'productionRunMode') -or (F 'activeProjectFlowIdS104') -ne $first.FlowId -or $flow[0].RuntimeOutputVersion -ne $version -or $store.ReadRecent(20).Records.Count -ne 1){throw 'Entering production changed flow, executed, or relabeled an existing journal record.'}
        if((F 'productionResultCycleS141') -ne $null){throw 'Engineering run became a formal production result merely by changing page.'}
        if((F 'ProductionModeStateText').Text -ne ('当前流程：'+$first.Name) -or (F 'ProductionRunOnceButtonUI08').ToolTip -notlike ('*'+$first.Name+'*')){throw 'Production page lost its exact current flow label.'}
        if((F 'ProductionStatisticsWidget').Header -eq '生产统计' -or (F 'ProductionOverallResultWidget').Header -eq '总判定'){throw 'Default result/statistics still imply production-only values.'}
        Shot 'production-current-flow'
        Full08
        $records=@($store.ReadRecent(20).Records)
        if($records.Count -ne 2 -or @($records|Where-Object PageMode -eq 'Production').Count -ne 1 -or @($records|Where-Object {$_.RunId -eq $firstRunId -and $_.PageMode -eq 'Developer'}).Count -ne 1){throw 'Explicit production run mixed its origin with engineering history.'}
        M 'SetProductionRunMode' @($false)|Out-Null;Pump 50
        if((F 'productionRunMode') -or (F 'activeProjectFlowIdS104') -ne $first.FlowId -or (F 'FlowRunResultText').Text -ne 'OK'){throw 'Returning to engineering lost the current flow/result.'}
        $second=[Activator]::CreateInstance($assembly.GetType('HalconWinFormsDemo.Models.VmProjectFlowData',$true))
        $second.Name='第二工位 · 待配置';$flows.Add($second)
        M 'RefreshProjectFlowPickerS104'|Out-Null
        (F 'ProjectFlowPickerS104').SelectedItem=$second;Pump 100
        if((F 'activeProjectFlowIdS104') -ne $second.FlowId -or (F 'ProjectFlowPickerS104').SelectedItem.FlowId -ne $second.FlowId){throw ('Actual flow selector failed: selected='+ (F 'ProjectFlowPickerS104').SelectedItem.FlowId+' active='+(F 'activeProjectFlowIdS104')+' expected='+$second.FlowId)}
        if((F 'flowTools').Count -ne 0 -or $first.Tools.Count -ne 4 -or (F 'isFlowExecutionActive')){throw 'Selecting flow did not preserve definition or started execution.'}
        if((F 'ProductionRunSaveStatusTextS135').Text -notlike '当前流程尚未运行*' -or (F 'FlowRunResultText').Text -ne '--' -or (F 'ProductionRecentResultsGrid').Items.Count -ne 0){throw 'New flow retained the old flow cycle status, result, or recent steps.'}
        $main.GetField('isFlowExecutionActive',$private).SetValue($window,$true)
        try{(F 'ProjectFlowPickerS104').SelectedItem=$first;Pump 50;if((F 'activeProjectFlowIdS104') -ne $second.FlowId -or (F 'ProjectFlowPickerS104').SelectedItem.FlowId -ne $second.FlowId){throw 'Busy flow selection did not restore the actual active flow.'}}
        finally{$main.GetField('isFlowExecutionActive',$private).SetValue($window,$false)}
        foreach($guard in @('engineeringPackageBusyS139','tcpTriggerPendingS104')){
            $busyValue=if($guard -eq 'tcpTriggerPendingS104'){[object]1}else{[object]$true}
            $idleValue=if($guard -eq 'tcpTriggerPendingS104'){[object]0}else{[object]$false}
            $main.GetField($guard,$private).SetValue($window,$busyValue)
            try{(F 'ProjectFlowPickerS104').SelectedItem=$first;Pump 30;if((F 'activeProjectFlowIdS104') -ne $second.FlowId -or (F 'ProjectFlowPickerS104').SelectedItem.FlowId -ne $second.FlowId){throw ('Pending task allowed flow switch: '+$guard)}}
            finally{$main.GetField($guard,$private).SetValue($window,$idleValue)}
        }
        M 'SetProductionRunMode' @($true)|Out-Null;Pump 50
        if((F 'ProductionModeStateText').Text -ne ('当前流程：'+$second.Name) -or (F 'ProductionOverallResultText').Text -ne '--' -or $store.ReadRecent(20).Records.Count -ne 2){throw 'New flow presentation substituted or deleted old records.'}
        Shot 'production-new-flow'
        $saved=Join-Path $temp 'two-flow-ui08.json';$service.SaveRecipe($saved,(M 'CaptureRecipe'));$loaded=$service.LoadRecipe($saved)
        if($loaded.ActiveFlowId -ne $second.FlowId -or $loaded.Flows.Count -ne 2 -or @($loaded.Flows|Where-Object FlowId -eq $first.FlowId)[0].Tools.Count -ne 4){throw 'Saving after page/flow switches lost definitions or active flow.'}
        M 'SetProductionRunMode' @($false)|Out-Null
        (F 'ProjectFlowPickerS104').SelectedItem=$first;Pump 60
        if((F 'flowTools').Count -ne 4 -or @((F 'flowTools')|Where-Object {$null -ne $_.LastOutputUtc}).Count -ne 0){throw 'Returning to the first flow restored old runtime outputs or lost its definition.'}
        if($window.IsActive -or $store.ReadRecent(20).Records.Count -ne 2){throw 'UI08 activated foreground or changed the persisted journal while navigating.'}
        Write-Output 'UI02_BLOCK08_CONTEXT=PASS; ActualEngineeringAndProductionFullRuns=True; JournalOriginsFrozen=True; PageSwitchNoRun=True; ExactCurrentFlow=True; NewFlowClearsCycleLabelAndResults=True; BusySelectionRejected=True; Schema3TwoFlowsSaved=True; HistoryPreserved=True; NoMouse=True; NoDeviceConnection=True'
    }
    if ($Phase -eq 'ui02block07') {
        $window.Width=1366;$window.Height=768
        function Open07($tool) {
            M 'OpenCentralModuleConfigurationS77' @($tool,$false)|Out-Null
            M 'SelectCentralConfigurationTaskS77' @((F 'DockParametersTaskTab'),'Parameters')|Out-Null
            Pump 70
            $editor=F 'moduleParameterWindowS89'
            if($null -eq $editor -or $editor.ShowActivated -or $editor.IsActive -or $window.IsActive -or $editor.Left -gt -10000){throw 'Parameter test must remain off-screen and non-active.'}
        }
        function Choose07([string]$field,[string]$key) {
            $combo=F $field;$choice=@($combo.Items|Where-Object Key -eq $key)
            if($choice.Count -ne 1){throw ('Missing actual mode option: '+$field+' / '+$key)}
            $editing=F 'dockEditingTool';$applied=$editing.Parameters;$version=$editing.RuntimeOutputVersion
            $modeBefore=@($applied.ImageFilterMode,$applied.ImageEnhancementMode,$applied.ImageChannelMode,$applied.ImageDifferenceMode,$applied.ImageProjectiveOutputMode,$applied.NumericCalculationOperation)-join '|'
            # Real SelectionChanged, including its pre-SelectedValue timing; no handler shortcut.
            $combo.SelectedItem=$choice[0];Pump 40
            $modeAfter=@($applied.ImageFilterMode,$applied.ImageEnhancementMode,$applied.ImageChannelMode,$applied.ImageDifferenceMode,$applied.ImageProjectiveOutputMode,$applied.NumericCalculationOperation)-join '|'
            if(-not [object]::ReferenceEquals($editing.Parameters,$applied) -or $modeAfter -ne $modeBefore -or $editing.RuntimeOutputVersion -ne $version){throw 'Mode selection applied parameters or ran the module.'}
        }
        function Common07([string]$field) {
            $control=F $field;$row=$control.Parent.RowDefinitions[[Windows.Controls.Grid]::GetRow($control)]
            if($control.Visibility -ne [Windows.Visibility]::Visible -or $row.ActualHeight -le 0 -or $control.ActualWidth -gt 140){throw ('Mode field is hidden or too wide: '+$field)}
        }
        function Read07([bool]$expected=$true,[string]$errorField='') {
            $arguments=[object[]]@($null,$null,$false,$null,0.0,0.0,0.0)
            $ok=[bool]$main.GetMethod('TryReadDockConfigurationDraft',$private).Invoke($window,$arguments)
            if($ok -ne $expected){throw ('Unexpected draft validation: '+$ok+' / '+(F 'DockValidationText').Text)}
            if(-not $ok -and $errorField){
                $field=F $errorField
                if(-not [object]::ReferenceEquals((F 'invalidParameterFieldUI07'),$field) -or $field.BorderThickness.Left -ne 2 -or [string]::IsNullOrWhiteSpace((F 'DockValidationText').Text)){throw ('Validation did not identify the exact field: '+$errorField)}
                if((F 'DockConfigurationTabs').SelectedItem -ne (F 'DockParametersTaskTab')){throw 'Field error did not select the common task.'}
                if((F 'DockValidationText').Visibility -ne [Windows.Visibility]::Collapsed -or (F 'DockAdvancedParameterValidationText').Visibility -ne [Windows.Visibility]::Visible){throw 'Field error must have one visible explanation, not duplicate messages.'}
            }
            if($ok){Write-Output -NoEnumerate $arguments[0]}
        }
        function AssertImage07($tool,$expected,[string]$name) {
            $snapshot=$null
            if($tool.ResultCode -ne 'OK' -or -not $tool.TryGetOutputValue('Image',[ref]$snapshot) -or $snapshot.IsDisposed -or $snapshot.Width -ne 32 -or $snapshot.Height -ne 32){throw ('No actual Image output: '+$name+' / '+$tool.ErrorMessage)}
            $actual=$snapshot.CreateImageCopy()
            try {
                foreach($y in @(0,1,7,15,16,17,24,30,31)){foreach($x in @(0,1,7,15,16,17,24,30,31)){
                    $a=$actual.GetGrayval($y,$x);$b=$expected.GetGrayval($y,$x)
                    try{if($a.D -ne $b.D){throw ($name+' pixel differs at '+$y+','+$x+': '+$a.D+' vs '+$b.D)}}finally{$a.Dispose();$b.Dispose()}
                }}
            } finally {$actual.Dispose()}
        }
        function Trial07($tool) {
            $before=$source.RuntimeOutputVersion
            M 'DockTrialRunButton_Click' @($null,$null)|Out-Null;Pump 50
            if($tool.ResultCode -ne 'OK' -or $source.RuntimeOutputVersion -ne $before -or $tool.RuntimeResultOrigin.ToString() -ne 'DraftTrial'){throw ('F5 draft did not stay local: '+$tool.ErrorMessage)}
        }
        $pixels=[byte[]]::new(1024)
        for($y=0;$y -lt 32;$y++){for($x=0;$x -lt 32;$x++){$pixels[$y*32+$x]=[byte](20+(($x*4+$y*3)%150))}}
        $pixels[16*32+16]=240
        $bitmap=[Windows.Media.Imaging.BitmapSource]::Create(32,32,96,96,[Windows.Media.PixelFormats]::Gray8,$null,$pixels,32)
        $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
        $path=Join-Path $temp 'parameter-modes.png';$stream=[IO.File]::Create($path);try{$encoder.Save($stream)}finally{$stream.Dispose()}
        $source=$flow[0];$source.Parameters.ImageSourceModeS114='File';$source.Parameters.LocalImagePath=$path
        M 'RunStandaloneToolCoreUI02' @($source,'UI07 actual Image source',$false)|Out-Null
        $sourceSnapshot=$null;if(-not $source.TryGetOutputValue('Image',[ref]$sourceSnapshot)){throw 'Actual source snapshot missing.'}
        $filter=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageFilter'),'模式滤波',$true,$null);$flow.Add($filter);$filter.SetInputBinding('Image',$source.ToolId,'Image')
        $enhancement=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageEnhancement'),'模式增强',$true,$null);$flow.Add($enhancement);$enhancement.SetInputBinding('Image',$source.ToolId,'Image')
        M 'RefreshUiState'|Out-Null
        $catalogWidth=(F 'ToolCatalogColumn').Width;$imageHeight=(F 'ImageWorkspaceRow').Height;$zoom=(F 'FlowGraphScaleTransform').ScaleX
        Open07 $filter
        (F 'DockImageFilterWidthTextBox').Text='5';(F 'DockImageFilterRadiusTextBox').Text='hidden-invalid'
        $draft=Read07
        if($draft.ImageFilterMaskWidth -ne 5 -or $draft.ImageFilterRadius -ne 1 -or (F 'DockImageFilterRadiusTextBox').Visibility -ne [Windows.Visibility]::Collapsed){throw 'Mean parsed or wrote hidden median radius.'}
        Trial07 $filter
        $inputImage=$sourceSnapshot.CreateImageCopy();$expected=$inputImage.MeanImage(5,3)
        try{AssertImage07 $filter $expected 'Mean 5x3'}finally{$expected.Dispose();$inputImage.Dispose()}
        if($filter.Parameters.ImageFilterMaskWidth -ne 3){throw 'Mean F5 applied its draft.'}
        Choose07 'DockImageFilterModeComboBox' 'Median'
        if((F 'DockImageFilterWidthTextBox').Visibility -ne [Windows.Visibility]::Collapsed -or (F 'DockImageFilterRadiusTextBox').Visibility -ne [Windows.Visibility]::Visible){throw 'Actual median selection left the old mode fields visible.'}
        (F 'DockImageFilterRadiusTextBox').Text='2';(F 'DockImageFilterWidthTextBox').Text='7'
        $draft=Read07
        if($draft.ImageFilterMaskWidth -ne 3 -or $draft.ImageFilterMaskHeight -ne 3 -or $draft.ImageFilterRadius -ne 2){throw 'Hidden valid mean draft leaked into median parameters.'}
        (F 'DockImageFilterWidthTextBox').Text='hidden-invalid';(F 'DockImageFilterHeightTextBox').Text='hidden-invalid'
        Trial07 $filter
        $inputImage=$sourceSnapshot.CreateImageCopy();$expected=$inputImage.MedianImage('circle',2,'mirrored')
        try{AssertImage07 $filter $expected 'Median R2'}finally{$expected.Dispose();$inputImage.Dispose()}
        if($filter.Parameters.ImageFilterMode -ne 'Mean' -or $filter.Parameters.ImageFilterRadius -ne 1){throw 'Median trial changed applied mode/radius.'}
        $version=$filter.RuntimeOutputVersion
        (F 'DockImageFilterRadiusTextBox').Text='0';Read07 $false 'DockImageFilterRadiusTextBox'
        M 'DockTrialRunButton_Click' @($null,$null)|Out-Null
        if($filter.RuntimeOutputVersion -ne $version -or (F 'DockAdvancedParameterToggleButton').Visibility -ne [Windows.Visibility]::Collapsed){throw 'Bad common radius executed or opened unrelated advanced fields.'}
        Shot 'median-field-error' (F 'moduleParameterWindowS89')
        (F 'DockImageFilterRadiusTextBox').Text='2'
        if((F 'invalidParameterFieldUI07') -ne $null){throw 'Editing the invalid field did not clear its marker.'}
        Choose07 'DockImageFilterModeComboBox' 'Mean'
        (F 'DockImageFilterWidthTextBox').Text='4';(F 'DockImageFilterHeightTextBox').Text='3';Read07 $false 'DockImageFilterWidthTextBox'
        (F 'DockImageFilterWidthTextBox').Text='0';Read07 $false 'DockImageFilterWidthTextBox'
        M 'DockRevertButton_Click' @($null,$null)|Out-Null;Pump 50
        if((F 'dockDraftDirty') -or (F 'invalidParameterFieldUI07') -ne $null -or (F 'DockImageFilterWidthTextBox').Text -ne '3'){throw 'Cancel did not restore applied fields and clear validation.'}
        (F 'DockImageFilterWidthTextBox').Text='5';(F 'DockImageFilterRadiusTextBox').Text='hidden-invalid'
        if(-not (M 'ApplyDockConfigurationDraft' @($true)) -or $filter.Parameters.ImageFilterMaskWidth -ne 5 -or $filter.Parameters.ImageFilterRadius -ne 1){throw 'Explicit mean apply failed.'}
        $filterId=$filter.ToolId
        Open07 $enhancement
        if((F 'dockDraftDirty') -or (F 'invalidParameterFieldUI07') -ne $null){throw 'Switching from a clean applied module leaked draft/error state.'}
        Choose07 'DockImageEnhancementModeComboBox' 'LocalContrast'
        foreach($field in @('DockImageEnhancementMaskWidthTextBox','DockImageEnhancementMaskHeightTextBox','DockImageEnhancementFactorTextBox')){
            Common07 $field
        }
        if((F 'DockImageEnhancementScaleTextBox').Visibility -ne [Windows.Visibility]::Collapsed -or (F 'DockAdvancedParameterToggleButton').Visibility -ne [Windows.Visibility]::Collapsed){throw 'Local primary fields still need More or expose the linear mode.'}
        (F 'DockImageEnhancementScaleTextBox').Text='hidden-invalid';(F 'DockImageEnhancementOffsetTextBox').Text='hidden-invalid'
        (F 'DockImageEnhancementMaskWidthTextBox').Text='5';(F 'DockImageEnhancementMaskHeightTextBox').Text='5';(F 'DockImageEnhancementFactorTextBox').Text='1.2'
        $draft=Read07
        if($draft.ImageEnhancementScale -ne 1 -or $draft.ImageEnhancementOffset -ne 0 -or $draft.ImageEnhancementFactor -ne 1.2){throw 'Local enhancement parsed or wrote inactive linear values.'}
        Trial07 $enhancement
        $inputImage=$sourceSnapshot.CreateImageCopy();$expected=$inputImage.Emphasize(5,5,1.2)
        try{AssertImage07 $enhancement $expected 'Local 5x5 F1.2'}finally{$expected.Dispose();$inputImage.Dispose()}
        if($enhancement.Parameters.ImageEnhancementMode -ne 'LinearScale' -or $enhancement.Parameters.ImageEnhancementMaskWidth -ne 7){throw 'Local enhancement F5 applied draft values.'}
        M 'SelectCentralConfigurationTaskS77' @((F 'DockParametersTaskTab'),'Parameters')|Out-Null;Pump 50;Shot 'local-common-four' (F 'moduleParameterWindowS89')
        foreach($bad in @('0','NaN','21')){(F 'DockImageEnhancementFactorTextBox').Text=$bad;Read07 $false 'DockImageEnhancementFactorTextBox'}
        M 'DockRevertButton_Click' @($null,$null)|Out-Null
        (F 'DockImageEnhancementScaleTextBox').Text='1.5';(F 'DockImageEnhancementOffsetTextBox').Text='10'
        foreach($field in @('DockImageEnhancementMaskWidthTextBox','DockImageEnhancementMaskHeightTextBox','DockImageEnhancementFactorTextBox')){(F $field).Text='hidden-invalid'}
        $draft=Read07
        if($draft.ImageEnhancementMaskWidth -ne 7 -or $draft.ImageEnhancementMaskHeight -ne 7 -or $draft.ImageEnhancementFactor -ne 1){throw 'Linear enhancement wrote hidden local values.'}
        Trial07 $enhancement
        $inputImage=$sourceSnapshot.CreateImageCopy();$expected=$inputImage.ScaleImage(1.5,10)
        try{AssertImage07 $enhancement $expected 'Linear 1.5 plus10'}finally{$expected.Dispose();$inputImage.Dispose()}
        if($enhancement.Parameters.ImageEnhancementScale -ne 1){throw 'Linear F5 applied draft.'}
        (F 'DockImageEnhancementScaleTextBox').Text='0';Read07 $false 'DockImageEnhancementScaleTextBox'
        M 'DockRevertButton_Click' @($null,$null)|Out-Null
        # Other mode families share the original parser; no simulated images/outputs are injected.
        # ImageA/B must use different real endpoints, not the forbidden duplicate source.
        M 'RunStandaloneToolCoreUI02' @($filter,'UI07 applied second Image endpoint',$false)|Out-Null
        foreach($kind in @('ImageChannel','ImageDifference','ImageProjectiveTransform','NumericCalculation')){
            $tool=M 'CreateFlowTool' @([Enum]::Parse($enumType,$kind),('模式检查 '+$kind),$true,$null);$flow.Add($tool)
            if($kind -eq 'ImageDifference'){$tool.SetInputBinding('ImageA',$source.ToolId,'Image');$tool.SetInputBinding('ImageB',$filter.ToolId,'Image')}
            elseif($kind -eq 'NumericCalculation'){$tool.SetInputBinding('NumberA',$source.ToolId,'Width');$tool.SetInputBinding('NumberB',$source.ToolId,'Height')}
            else{$tool.SetInputBinding('Image',$source.ToolId,'Image')}
            M 'RefreshUiState'|Out-Null;Open07 $tool
            if($kind -eq 'ImageChannel'){
                (F 'DockImageChannelIndexTextBox').Text='hidden-invalid';$draft=Read07
                if($draft.ImageChannelIndex -ne 1 -or (F 'DockImageChannelIndexTextBox').Visibility -ne [Windows.Visibility]::Collapsed){throw 'Keep-channel read inactive index.'}
                Choose07 'DockImageChannelModeComboBox' 'Extract';(F 'DockImageChannelIndexTextBox').Text='0';Read07 $false 'DockImageChannelIndexTextBox'
                Choose07 'DockImageChannelModeComboBox' 'Keep';Read07|Out-Null
            } elseif($kind -eq 'ImageDifference'){
                (F 'DockImageDifferenceAddTextBox').Text='hidden-invalid';$draft=Read07
                if($draft.ImageDifferenceAdd -ne 0 -or (F 'DockImageDifferenceAddTextBox').Visibility -ne [Windows.Visibility]::Collapsed){throw 'Absolute difference read inactive add.'}
                Choose07 'DockImageDifferenceModeComboBox' 'SubtractAB';Common07 'DockImageDifferenceAddTextBox';Read07 $false 'DockImageDifferenceAddTextBox'
                Choose07 'DockImageDifferenceModeComboBox' 'AbsoluteDifference';Read07|Out-Null
                (F 'DockImageDifferenceMultiplierTextBox').Text='0';Read07 $false 'DockImageDifferenceMultiplierTextBox'
            } elseif($kind -eq 'ImageProjectiveTransform'){
                (F 'DockProjectiveOutputWidthTextBox').Text='hidden-invalid';(F 'DockProjectiveOutputHeightTextBox').Text='hidden-invalid';$draft=Read07
                $previewArgs=[object[]]@($null,$null)
                if(-not $main.GetMethod('TryBuildProjectiveGeometryPreviewFromDock',$private).Invoke($window,$previewArgs) -or $draft.ImageProjectiveOutputWidth -ne $tool.Parameters.ImageProjectiveOutputWidth -or $previewArgs[0].ImageProjectiveOutputWidth -ne $tool.Parameters.ImageProjectiveOutputWidth){throw 'Keep-size preview/parser consumed inactive dimensions.'}
                Choose07 'DockProjectiveOutputModeComboBox' 'ExplicitSize';Common07 'DockProjectiveOutputWidthTextBox';Common07 'DockProjectiveOutputHeightTextBox'
                (F 'DockProjectiveOutputWidthTextBox').Text='32';(F 'DockProjectiveOutputHeightTextBox').Text='32';Read07|Out-Null
                $coordinateGroup=(F 'DockProjectiveSourceRow4TextBox').Parent.Parent.Parent
                if($coordinateGroup.ActualHeight -gt 0){throw 'Fourth point bypassed the collapsed advanced coordinate group.'}
                M 'DockAdvancedParameterToggleButton_Click' @($null,$null)|Out-Null;Pump 40
                if($coordinateGroup.ActualHeight -le 0){throw 'Original More no longer reveals the complete four-point coordinate editor.'}
                M 'DockAdvancedParameterToggleButton_Click' @($null,$null)|Out-Null;Pump 40
                Pump 40;Shot 'projective-common' (F 'moduleParameterWindowS89')
                (F 'DockProjectiveOutputWidthTextBox').Text='0';(F 'DockProjectiveOutputHeightTextBox').Text='32';Read07 $false 'DockProjectiveOutputWidthTextBox'
                if((F 'dockAdvancedParameterExpandedByKind')[$tool.Kind]){throw 'Common size error expanded unrelated four-point geometry fields.'}
                if($main.GetMethod('TryBuildProjectiveGeometryPreviewFromDock',$private).Invoke($window,$previewArgs)){throw 'Preview accepted the explicit zero size rejected by the parser.'}
                Choose07 'DockProjectiveOutputModeComboBox' 'KeepInputSize';Read07|Out-Null
            } else {
                (F 'DockNumericCalculationRoundDigitsTextBox').Text='hidden-invalid';$draft=Read07
                if($draft.NumericCalculationRoundDigits -ne $tool.Parameters.NumericCalculationRoundDigits -or (F 'DockNumericCalculationRoundDigitsTextBox').Visibility -ne [Windows.Visibility]::Collapsed){throw 'Add read inactive rounding digits.'}
                Trial07 $tool;[double]$number=0
                if(-not $tool.TryGetNumericOutput('Number',[ref]$number) -or $number -ne 64){throw 'Actual source Width + Height must equal 64.'}
                $legacy=$tool.Parameters.Clone();$legacy.NumericCalculationRoundDigits=99
                if(-not [string]::IsNullOrEmpty($legacy.Validate($tool.Kind))){throw 'Add model validation still checks inactive rounding digits.'}
                $legacy.NumericCalculationOperation='Round'
                if([string]::IsNullOrEmpty($legacy.Validate($tool.Kind))){throw 'Round model validation accepted digits99.'}
                M 'SelectCentralConfigurationTaskS77' @((F 'DockParametersTaskTab'),'Parameters')|Out-Null
                Choose07 'DockNumericCalculationOperationComboBox' 'Round';Common07 'DockNumericCalculationRoundDigitsTextBox';(F 'DockNumericCalculationRoundDigitsTextBox').Text='16';Read07 $false 'DockNumericCalculationRoundDigitsTextBox'
                Choose07 'DockNumericCalculationOperationComboBox' 'Add';Read07|Out-Null
            }
            M 'DockRevertButton_Click' @($null,$null)|Out-Null
            if((F 'dockDraftDirty') -or (F 'invalidParameterFieldUI07') -ne $null){throw ('Cancel retained mode draft/error for '+$kind)}
        }
        # End with an actual applied edit, then exercise original history and schema3 persistence.
        Open07 $enhancement;(F 'DockImageEnhancementScaleTextBox').Text='1.5';(F 'DockImageEnhancementOffsetTextBox').Text='10'
        if(-not (M 'ApplyDockConfigurationDraft' @($true))){throw 'Explicit enhancement apply failed.'}
        $enhancementId=$enhancement.ToolId
        M 'CloseCentralModuleConfigurationS77'|Out-Null
        M 'UndoFlowEditButton_Click' @($null,$null)|Out-Null
        $enhancement=@((F 'flowTools')|Where-Object ToolId -eq $enhancementId)[0]
        if($enhancement.Parameters.ImageEnhancementScale -ne 1 -or $enhancement.Parameters.ImageEnhancementOffset -ne 0){throw 'Original undo did not restore applied enhancement.'}
        M 'RedoFlowEditButton_Click' @($null,$null)|Out-Null
        $enhancement=@((F 'flowTools')|Where-Object ToolId -eq $enhancementId)[0]
        if($enhancement.Parameters.ImageEnhancementScale -ne 1.5 -or $enhancement.Parameters.ImageEnhancementOffset -ne 10 -or $enhancement.Parameters.ImageEnhancementMaskWidth -ne 7){throw 'Redo lost active values or changed inactive values.'}
        $save=Join-Path $temp 'mode-parameters.json';$service.SaveRecipe($save,(M 'CaptureRecipe'));$loaded=$service.LoadRecipe($save)
        $savedFlow=@($loaded.Flows|Where-Object FlowId -eq $loaded.ActiveFlowId)[0]
        $savedFilter=@($savedFlow.Tools|Where-Object ToolId -eq $filterId)[0];$savedEnhancement=@($savedFlow.Tools|Where-Object ToolId -eq $enhancementId)[0]
        if($savedFilter.Parameters.ImageFilterMaskWidth -ne 5 -or $savedFilter.Parameters.ImageFilterRadius -ne 1 -or $savedEnhancement.Parameters.ImageEnhancementScale -ne 1.5 -or $savedEnhancement.Parameters.ImageEnhancementMaskWidth -ne 7){throw 'Schema3 save lost applied active/inactive mode values.'}
        if((F 'ToolCatalogColumn').Width -ne $catalogWidth -or (F 'ImageWorkspaceRow').Height -ne $imageHeight -or (F 'FlowGraphScaleTransform').ScaleX -ne $zoom -or $window.IsActive){throw 'Parameter work changed main layout or activated foreground.'}
        Write-Output 'UI02_BLOCK07_PARAMETERS=PASS; SixRealModeSelectors=True; HiddenInvalidAndValidDraftValuesIgnored=True; LocalFourCommonFields=True; ExactFieldErrors=True; ZeroOddNonfiniteRejected=True; ActualHalconMeanMedianLinearLocal81PixelsEach=True; DraftF5NotApplied=True; ActualNumericAdd64=True; ProjectivePreviewConsistent=True; CancelAndCleanSwitch=True; ApplyUndoRedoSchema3Save=True; MainLayoutPreserved=True; NoMouse=True; NoDeviceConnection=True'
    }
    if ($Phase -eq 'ui02block06') {
        $window.Width=1366;$window.Height=768
        function DetailValue($row) {
            $args=[object[]]@($row.PSObject.BaseObject,$null,$null,$null)
            if(-not $main.GetMethod('TryResolveDetailValueUI06',$private).Invoke($window,$args)){throw 'Current row did not resolve its actual source value.'}
            Write-Output -NoEnumerate $args
        }
        function ShowDetail($detail,$name) {
            $detail.ShowActivated=$false;$detail.WindowStartupLocation=[Windows.WindowStartupLocation]::Manual;$detail.Left=-12000;$detail.Top=-12000
            $detail.Show();Pump 80
            if($detail.IsActive -or $window.IsActive){throw 'Read-only detail activated foreground.'}
            Shot $name $detail
        }
        $pixels=[byte[]]::new(256)
        for($y=1;$y -le 3;$y++){for($x=1;$x -le 3;$x++){$pixels[$y*16+$x]=200}}
        for($y=8;$y -le 11;$y++){for($x=8;$x -le 11;$x++){$pixels[$y*16+$x]=200}}
        $bitmap=[Windows.Media.Imaging.BitmapSource]::Create(16,16,96,96,[Windows.Media.PixelFormats]::Gray8,$null,$pixels,16)
        $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
        $path=Join-Path $temp 'two-objects.png';$stream=[IO.File]::Create($path);try{$encoder.Save($stream)}finally{$stream.Dispose()}
        $source=$flow[0];$threshold=$flow[1];$objects=$flow[2]
        $source.Parameters.ImageSourceModeS114='File';$source.Parameters.LocalImagePath=$path
        $threshold.Parameters.ImageThresholdMinGray=100;$threshold.Parameters.ImageThresholdMaxGray=255
        foreach($tool in @($source,$threshold,$objects)){M 'RunStandaloneToolCoreUI02' @($tool,'UI06 actual object chain',$false)|Out-Null;if($tool.ResultCode -ne 'OK'){throw $tool.ErrorMessage}}
        M 'OpenCentralModuleConfigurationS77' @($objects,$false)|Out-Null
        M 'SelectCentralConfigurationTaskS77' @((F 'DockResultTaskTab'),'Result')|Out-Null
        $keys=@((F 'dockTaskResultRowsS88')|ForEach-Object PortName)
        if(($keys-join ',') -ne 'Count,SelectedArea,SelectedPoint,Areas'){throw ('Object key outputs differ: '+($keys-join ','))}
        $areaRow=@((F 'dockTaskResultRowsS88')|Where-Object PortName -eq 'Areas')[0];$resolved=DetailValue $areaRow
        if($resolved[3].Count -ne 2 -or $resolved[3][0] -ne 16 -or $resolved[3][1] -ne 9){throw 'HALCON ordered areas must be 16,9.'}
        Pump 60;Shot 'object-key-results' (F 'moduleParameterWindowS89')
        $detail=M 'BuildArrayDetailWindowUI06' @($resolved[1],$resolved[2],$resolved[3])
        try {ShowDetail $detail 'object-areas';$grid=@($detail.Content.Children|Where-Object {$_ -is [Windows.Controls.DataGrid]})[0];if($grid.Items[0].Value -ne '16' -or $grid.Items[1].Index -ne 1){throw 'Array detail lost original index/value.'}}
        finally{$detail.Close()}
        $stats=M 'CreateFlowTool' @([Enum]::Parse($enumType,'NumberArrayStatistics'),'面积统计',$true,$null);$flow.Add($stats);$stats.SetInputBinding('Array',$objects.ToolId,'Areas');M 'RefreshUiState'|Out-Null
        M 'OpenCentralModuleConfigurationS77' @($stats,$false)|Out-Null;M 'SelectCentralConfigurationTaskS77' @((F 'DockIoTaskTab'),'Io')|Out-Null
        $input=@((F 'dockInputPortRows')|Where-Object PortName -eq 'Array')[0];$resolved=DetailValue $input
        if(-not [object]::ReferenceEquals($resolved[1],$objects) -or $resolved[2].PortName -ne 'Areas' -or $input.StatusText -notlike '最近执行*'){throw 'Input detail did not resolve selected Areas source.'}
        Pump 80;Shot 'array-input' (F 'moduleParameterWindowS89')
        if(F 'dockDraftDirty'){throw 'Viewing an existing array source marked the draft modified.'}
        $nodes=[Collections.Generic.Stack[Windows.DependencyObject]]::new();$nodes.Push((F 'DockInputPortItemsControl'));$combo=$null
        while($nodes.Count -gt 0){$node=$nodes.Pop();if($node -is [Windows.Controls.ComboBox] -and [object]::ReferenceEquals($node.DataContext,$input.PSObject.BaseObject)){$combo=$node;break};for($i=0;$i -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($node);$i++){$nodes.Push([Windows.Media.VisualTreeHelper]::GetChild($node,$i))}}
        if($null -eq $combo){throw 'Actual input source selector missing.'}
        $combo.SelectedItem=@($input.SourceOptions|Where-Object SourcePortName -eq 'SourceIndices')[0];Pump 50
        if(-not (F 'dockDraftDirty') -or $input.SelectedSourceKey -notlike '*SourceIndices' -or $stats.GetInputBinding('Array').SourcePortName -ne 'Areas'){throw 'Actual source selection did not edit only the input draft.'}
        M 'DockRevertButton_Click' @($null,$null)|Out-Null;Pump 60
        $input=@((F 'dockInputPortRows')|Where-Object PortName -eq 'Array')[0]
        if((F 'dockDraftDirty') -or $input.SelectedSourceKey -notlike '*Areas'){throw 'Cancel source draft did not restore applied binding cleanly.'}
        $sourceVersion=$objects.RuntimeOutputVersion
        (F 'DockArrayIndexS105').Text='1';M 'DockTrialRunButton_Click' @($null,$null)|Out-Null
        [double]$value=0
        if($stats.ResultCode -ne 'OK' -or -not $stats.TryGetNumericOutput('SelectedValue',[ref]$value) -or $value -ne 9 -or $stats.Parameters.ArrayIndex -ne 0 -or $objects.RuntimeOutputVersion -ne $sourceVersion){throw 'Draft array-index trial applied parameters or used wrong array.'}
        if(-not (M 'TryHandleModuleParameterShortcutS89' @([Windows.Input.Key]::Enter,[Windows.Input.ModifierKeys]::Control)) -or $stats.Parameters.ArrayIndex -ne 1){throw 'Apply array index failed.'}
        M 'RunStandaloneToolCoreUI02' @($stats,'UI06 applied array consumer',$false)|Out-Null
        if(-not $stats.TryGetNumericOutput('SelectedValue',[ref]$value) -or $value -ne 9){throw 'Applied array consumer differs.'}
        $recipe=M 'CaptureRecipe';$save=Join-Path $temp 'array-use.json';$service.SaveRecipe($save,$recipe);$loaded=$service.LoadRecipe($save)
        $saved=@(@($loaded.Flows|Where-Object FlowId -eq $loaded.ActiveFlowId)[0].Tools|Where-Object ToolId -eq $stats.ToolId)[0]
        if($saved.Parameters.ArrayIndex -ne 1 -or $saved.InputBindings[0].SourcePortName -ne 'Areas'){throw 'Array input/index save failed.'}
        $objects.RuntimeResultOrigin=[HalconWinFormsDemo.Models.VmRuntimeResultOrigin]::DraftTrial
        M 'RefreshDockInputRowState' @($input)|Out-Null
        if($input.IsReady -or $input.StateKey -ne 'Preview' -or $input.StatusText -notlike '草稿预览*'){throw 'Draft input was presented as applied ready.'}
        $objects.SetOutputValue('Areas','16,9');M 'RefreshDockInputRowState' @($input)|Out-Null
        if($input.IsReady -or $input.StateKey -ne 'Error' -or $input.CurrentValue -like '*16,9*'){throw 'Text masqueraded as typed input array.'}
        $args=[object[]]@($input.PSObject.BaseObject,$null,$null,$null)
        if($main.GetMethod('TryResolveDetailValueUI06',$private).Invoke($window,$args)){throw 'Bad array type opened detail.'}
        $objects.ClearRuntimeOutputs();M 'RefreshDockInputRowState' @($input)|Out-Null
        if($input.IsReady -or -not $input.IsConfigured -or $input.CurrentValue -ne '--' -or $input.StateKey -ne 'Waiting'){throw 'Invalidation kept a value or removed static connection.'}
        $failed=$false;try{M 'RunStandaloneToolCoreUI02' @($stats,'UI06 missing source',$false)|Out-Null}catch{$failed=$true}
        if(-not $failed){throw 'Missing array did not fail execution.'}
        if([string]::IsNullOrWhiteSpace($stats.ErrorMessage) -or $stats.TryGetNumericOutput('SelectedValue',[ref]$value)){throw 'Missing array retained old downstream result.'}
        M 'RunStandaloneToolCoreUI02' @($objects,'UI06 restore actual objects',$false)|Out-Null
        M 'RunStandaloneToolCoreUI02' @($stats,'UI06 recover actual consumer',$false)|Out-Null
        if($stats.ResultCode -ne 'OK' -or -not $stats.TryGetNumericOutput('SelectedValue',[ref]$value) -or $value -ne 9){throw 'Array recovery failed.'}
        M 'OpenCentralModuleConfigurationS77' @($objects,$false)|Out-Null
        $pointsRow=@((F 'dockTechnicalResultRowsS88')|Where-Object PortName -eq 'Centers')[0];$resolved=DetailValue $pointsRow
        $detail=M 'BuildPointDetailWindowUI06' @($resolved[1],$resolved[2],$resolved[3])
        try {ShowDetail $detail 'object-centers';$grid=@($detail.Content.Children|Where-Object {$_ -is [Windows.Controls.DataGrid]})[0];if($grid.Items.Count -ne 2 -or $grid.Items[0].Row -ne '9.5' -or $grid.Items[1].Row -ne '2'){throw 'Same-order centers do not match areas.'}}
        finally{$detail.Close()}
        $objects.SetOutputValue('SelectedPoint','Row=999;Column=999');M 'RefreshDockConfigurationRuntime' @($objects)|Out-Null
        if(@((F 'dockTaskResultRowsS88')|Where-Object {$_.PortName -eq 'SelectedPoint' -and $_.StateKey -eq 'Invalid'}).Count -ne 1){throw 'Fake geometry displayed as valid.'}
        $args=[object[]]@($pointsRow.PSObject.BaseObject,$null,$null,$null)
        if($main.GetMethod('TryResolveDetailValueUI06',$private).Invoke($window,$args)){throw 'Stale result row survived refresh.'}
        M 'RunStandaloneToolCoreUI02' @($objects,'UI06 restore geometry',$false)|Out-Null
        $geometry=M 'CreateFlowTool' @([Enum]::Parse($enumType,'PointSetGeometryStatistics'),'中心点集统计',$true,$null);$flow.Add($geometry)
        $geometry.Parameters.PointSetMinimumCount=2;$geometry.SetInputBinding('Points',$objects.ToolId,'Centers');M 'RefreshUiState'|Out-Null
        M 'OpenCentralModuleConfigurationS77' @($geometry,$false)|Out-Null;M 'SelectCentralConfigurationTaskS77' @((F 'DockIoTaskTab'),'Io')|Out-Null
        $pointInput=@((F 'dockInputPortRows')|Where-Object PortName -eq 'Points')[0];$resolved=DetailValue $pointInput
        if($resolved[2].PortName -ne 'Centers' -or $pointInput.StatusText -notlike '*2 个点*'){throw 'Point input does not show actual compact count.'}
        Pump 70;Shot 'point-input' (F 'moduleParameterWindowS89')
        if(F 'dockDraftDirty'){throw 'Viewing an existing point source marked the draft modified.'}
        M 'RunStandaloneToolCoreUI02' @($geometry,'UI06 actual center consumer',$false)|Out-Null
        if($geometry.ResultCode -ne 'OK' -or -not $geometry.TryGetNumericOutput('CentroidRow',[ref]$value) -or $value -ne 5.75){throw 'Actual typed-point downstream centroid differs.'}
        M 'OpenCentralModuleConfigurationS77' @($objects,$false)|Out-Null;M 'SelectCentralConfigurationTaskS77' @((F 'DockResultTaskTab'),'Result')|Out-Null
        $points=$null;if(-not $objects.TryGetOutputValue('Centers',[ref]$points)){throw 'Actual Centers missing.'}
        $badPoints=$points.Clone();$badPoints.Points[0].CoordinateContextId='different-image-fixture';$objects.SetOutputValue('Centers',$badPoints)
        M 'RefreshDockConfigurationRuntime' @($objects)|Out-Null;(F 'DockResultTechnicalExpanderS88').IsExpanded=$true;Pump 80
        $badRow=@((F 'dockTechnicalResultRowsS88')|Where-Object PortName -eq 'Centers')[0]
        if($badRow.StateKey -ne 'Invalid' -or -not $badRow.CanInspectValueUI06){throw 'Invalid point container must remain inspectable, not ready.'}
        $nodes=[Collections.Generic.Stack[Windows.DependencyObject]]::new();$nodes.Push((F 'DockTechnicalResultItemsControlS88'));$found=$false
        while($nodes.Count -gt 0){$node=$nodes.Pop();if($node -is [Windows.Controls.Button] -and $node.Content -eq '点集明细…' -and [object]::ReferenceEquals($node.DataContext,$badRow.PSObject.BaseObject)){$found=$node.IsEnabled -and $node.Visibility -eq [Windows.Visibility]::Visible};for($i=0;$i -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($node);$i++){$nodes.Push([Windows.Media.VisualTreeHelper]::GetChild($node,$i))}}
        if(-not $found){throw 'Actual invalid-point detail button was disabled or missing.'}
        $resolved=DetailValue $badRow;$detail=M 'BuildPointDetailWindowUI06' @($resolved[1],$resolved[2],$resolved[3])
        try{ShowDetail $detail 'invalid-point-diagnostic';$grid=@($detail.Content.Children|Where-Object {$_ -is [Windows.Controls.DataGrid]})[0];if($grid.Items[0].State -notlike '*坐标来源不一致*'){throw 'Invalid point detail hid coordinate error.'}}
        finally{$detail.Close()}
        M 'RunStandaloneToolCoreUI02' @($objects,'UI06 final source recovery',$false)|Out-Null
        Write-Output 'UI02_BLOCK06_RESULTS=PASS; RealHalconAreas16And9=True; SameOrderCenters=True; KeyGeometryAndArray=True; InputDetailExactSource=True; DraftIndexTrialAndApply=True; Downstream9=True; Schema3Saved=True; DraftOriginNotReady=True; WrongArrayAndGeometryRejected=True; MissingKeepsBindingNotOldValue=True; StaleRowRejected=True; OffscreenDetails=True; NoMouse=True'
    }
    if ($Phase -eq 'ui02block05') {
        $window.Width=1366;$window.Height=768
        for($index=5;$index -le 40;$index++) {
            $tool=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageSource'),('调试模块'+$index),$true,$null);$flow.Add($tool)
        }
        M 'RefreshUiState' | Out-Null;Pump 120
        if ((F 'flowTaskLensModeS76') -or (F 'FlowAllCardsModeRadioS76').IsChecked -ne $true -or @($flow | Where-Object {$_.GraphHeight -ne 52 -or $_.IsTaskLensCompactS76}).Count -gt 0) { throw 'Default must be fixed 52px cards without automatic folding.' }
        $originalOrder=@($flow | ForEach-Object ToolId);$positions=@($flow | ForEach-Object GraphY)
        (F 'FlowToolList').SelectedItem=$flow[28];M 'RefreshUiState' | Out-Null
        if((@($flow | ForEach-Object GraphY)-join ',') -ne ($positions-join ',')){throw 'Selecting a module moved fixed-layout cards.'}
        M 'SetFlowModuleSearchUI05' @('30') | Out-Null
        $navigator=F 'FlowModuleNavigatorUI05'
        if($navigator.Items.Count -ne 1 -or $navigator.Text -ne '30' -or -not [object]::ReferenceEquals($navigator.Items[0].Tool.Target,$flow[29]) -or $window.IsActive){throw 'Sequence search was cleared on opening or resolved/activated incorrectly.'}
        $navigator.SelectedItem=$navigator.Items[0];Pump 100
        if(-not [object]::ReferenceEquals((F 'FlowToolList').SelectedItem,$flow[29]) -or (F 'centralModuleConfigurationOpenS77') -or (F 'isFlowExecutionActive')){throw 'Locator changed task scope instead of selecting step30.'}
        $viewport=F 'FlowGraphViewport';$zoom=(F 'FlowZoomSlider').Value
        $center=($flow[29].GraphY+$flow[29].GraphHeight/2)*$zoom-$viewport.VerticalOffset
        if($center -lt 0 -or $center -gt $viewport.ViewportHeight){throw 'Selected module is not in visible long-flow viewport.'}
        M 'RefreshFlowModuleNavigatorUI05' @('调试模块40') | Out-Null
        if($navigator.Items.Count -ne 1 -or -not [object]::ReferenceEquals($navigator.Items[0].Tool.Target,$flow[39])){throw 'Name locator failed.'}
        $navigator.SelectedItem=$navigator.Items[0];Pump 100
        M 'SelectAdjacentFlowToolS72' @(-1,'Isolated adjacent navigation') | Out-Null;Pump 100
        if(-not [object]::ReferenceEquals((F 'FlowToolList').SelectedItem,$flow[38])){throw 'Adjacent navigation did not move to step39.'}
        M 'RefreshFlowModuleNavigatorUI05' @('not-a-module-fixture') | Out-Null
        if($navigator.Items.Count -ne 0 -or (F 'FlowNavigationEmptyUI05').Visibility -ne [Windows.Visibility]::Visible -or -not [object]::ReferenceEquals((F 'FlowToolList').SelectedItem,$flow[38])){throw 'Empty search retained stale candidates or changed selection.'}
        M 'RefreshFlowModuleNavigatorUI05' @($null) | Out-Null
        if($navigator.Items.Count -ne 40 -or (F 'FlowNavigationEmptyUI05').Visibility -ne [Windows.Visibility]::Collapsed){throw 'Clearing locator did not restore current flow.'}
        $stale=$navigator.Items[20];$original=$flow[20];$replacement=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageSource'),'替换实例',$true,$null)
        $replacement.ToolId=$original.ToolId;$flow[20]=$replacement
        $navigator.SelectedItem=$stale;Pump 80
        if([object]::ReferenceEquals((F 'FlowToolList').SelectedItem,$replacement)){throw 'Stale locator remapped same ID to new instance.'}
        $flow[20]=$original;$replacement.Dispose();M 'RefreshUiState' | Out-Null
        # Two queued requests: only the newest current selection may scroll.
        M 'FocusFlowGraphTool' @($flow[32].ToolId,'Old navigation') | Out-Null
        M 'FocusFlowGraphTool' @($flow[5].ToolId,'Newest navigation') | Out-Null;Pump 120
        $center=($flow[5].GraphY+$flow[5].GraphHeight/2)*$zoom-$viewport.VerticalOffset
        if(-not [object]::ReferenceEquals((F 'FlowToolList').SelectedItem,$flow[5]) -or $center -lt 0 -or $center -gt $viewport.ViewportHeight){throw 'Late navigation stole newer selection/viewport.'}
        M 'FocusFlowGraphTool' @($flow[35].ToolId,'Stale workspace navigation') | Out-Null
        $session=F 'projectSessionIdS104';$main.GetField('projectSessionIdS104',$all).SetValue($window,'isolated-new-session')
        $viewport.ScrollToVerticalOffset(100);Pump 80
        if([Math]::Abs($viewport.VerticalOffset-100) -gt 1){throw 'Stale workspace callback scrolled current view.'}
        $main.GetField('projectSessionIdS104',$all).SetValue($window,$session)
        $main.GetField('isFlowExecutionActive',$all).SetValue($window,$true)
        try {M 'SelectAdjacentFlowToolS72' @(-1,'Manual navigation during execution') | Out-Null;if(-not (F 'flowExecutionFollowPausedS72')){throw 'Manual adjacent navigation did not pause auto-follow.'}}
        finally {$main.GetField('isFlowExecutionActive',$all).SetValue($window,$false)}
        # Exercise original position completion/history without capturing physical mouse input.
        $moving=$flow[1];M 'FocusFlowGraphTool' @($moving.ToolId,'Position fixture') | Out-Null;Pump 80
        $before=M 'CaptureFlowEditSnapshot';$x=$moving.GraphX;$y=$moving.GraphY;$id=$moving.ToolId
        $type=$main.GetNestedType('FlowNodeDragSession',[Reflection.BindingFlags]'NonPublic');$drag=[Activator]::CreateInstance($type,$true)
        $drag.Tool=$moving;$drag.EngineeringLeaseS137=M 'CaptureEngineeringEditLeaseS137' @('Position fixture');$drag.HasMoved=$true;$drag.BeforeSnapshot=$before
        $dictionaryType=[Collections.Generic.Dictionary``2].MakeGenericType(@($moving.GetType(),[Windows.Point]));$positionsMap=[Activator]::CreateInstance($dictionaryType);$positionsMap.Add($moving,[Windows.Point]::new($x,$y));$drag.InitialPositions=$positionsMap
        $main.GetField('flowNodeDragSession',$all).SetValue($window,$drag)
        M 'EnableManualFlowLayout' | Out-Null;$moving.GraphX+=24;$moving.GraphY+=48
        M 'CompleteFlowNodeDrag' @($false) | Out-Null
        if((@($flow | ForEach-Object ToolId)-join '|') -ne ($originalOrder-join '|') -or $moving.GetInputBinding('Image').SourceToolId -ne $flow[0].ToolId){throw 'Position completion altered order/binding.'}
        M 'UndoFlowEditButton_Click' @($null,$null) | Out-Null;$moving=@((F 'flowTools')|Where-Object ToolId -eq $id)[0]
        if($moving.GraphX -ne $x -or $moving.GraphY -ne $y -or (F 'isManualFlowLayout')){throw 'Position undo failed.'}
        M 'RedoFlowEditButton_Click' @($null,$null) | Out-Null;$flow=F 'flowTools';$moving=@($flow|Where-Object ToolId -eq $id)[0]
        if($moving.GraphX -ne $x+24 -or $moving.GraphY -ne $y+48){throw 'Position redo failed.'}
        $args=[object[]]@($moving.PSObject.BaseObject,0,$null,'')
        if($main.GetMethod('TryBuildFlowReorderS77',$private).Invoke($window,$args)){throw 'Execution reorder broke existing Image dependency.'}
        $recipe=M 'CaptureRecipe';$path=Join-Path $temp 'long-flow-position.json';$service.SaveRecipe($path,$recipe);$loaded=$service.LoadRecipe($path);$savedFlow=@($loaded.Flows|Where-Object FlowId -eq $loaded.ActiveFlowId)[0];$savedTool=@($savedFlow.Tools|Where-Object ToolId -eq $id)[0]
        if($savedTool.GraphX -ne $x+24 -or $savedTool.GraphY -ne $y+48 -or (@($savedFlow.Tools|ForEach-Object ToolId)-join '|') -ne ($originalOrder-join '|')){throw 'Long-flow position/order did not save.'}
        foreach($node in $flow){if($node.LastOutputUtc -ne $null){throw 'Navigation/layout executed an algorithm.'}}
        M 'FocusFlowGraphTool' @($moving.ToolId,'Final compact flow') | Out-Null;Pump 100;Shot 'compact-long-flow'
        $button=[Windows.Controls.Button]::new();$button.Tag=$moving
        M 'ToggleFlowNodeDetailsButton_Click' @($button,[Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent,$button)) | Out-Null
        if(-not $moving.ShowFlowDetails -or $moving.GraphHeight -lt 132){throw 'Existing detailed port view no longer expands.'}
        Pump 80
        $presenter=(F 'FlowGraphNodeItemsControl').ItemContainerGenerator.ContainerFromItem($moving)
        if([Windows.Controls.Panel]::GetZIndex($presenter) -lt 20 -or $moving.GraphX -ne $x+24 -or $moving.GraphY -ne $y+48){throw 'Expanded ports are behind overlapping cards or changed manual coordinates.'}
        Shot 'explicit-port-details'
        M 'ToggleFlowNodeDetailsButton_Click' @($button,[Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent,$button)) | Out-Null
        if($moving.GraphHeight -ne 52){throw 'Explicit details did not return to compact card.'}
        Write-Output 'UI02_BLOCK05_LONG_FLOW=PASS; Fixed52pxCards=True; SelectionKeepsPositions=True; FortyModuleSequenceAndNameSearch=True; EmptySearchNoStale=True; LocatorNoParametersOrRun=True; AdjacentNavigation=True; LateRequestAndSessionRejected=True; WeakInstanceRejected=True; ManualNavigationPausesFollow=True; OriginalPositionCompletionUndoRedoSave=True; OrderAndBindingsPreserved=True; InvalidReorderRejected=True; ExplicitPortsPreserved=True; NoMouse=True; NoDeviceConnection=True'
    }
    if ($Phase -eq 'ui02block04') {
        $window.Width=1366; $window.Height=768
        function ConnectionSession($origin) {
            $type=$main.GetNestedType('FlowPortDragSession',[Reflection.BindingFlags]'NonPublic')
            $session=[Activator]::CreateInstance($type,$true)
            $session.EngineeringLeaseS137=M 'CaptureEngineeringEditLeaseS137' @('Isolated connection choice')
            $session.AggregateOriginToolId=$origin.ToolId; $session.AggregateOriginDirection='OUT'; $session.IsClickArmed=$true
            $main.GetField('flowPortDragSession',$all).SetValue($window,$session)
        }
        function ResolveConnection($target) {
            $args=[object[]]@($target.PSObject.BaseObject,'IN','')
            Write-Output -NoEnumerate ($main.GetMethod('ResolveAggregateHoverPortS75',$private).Invoke($window,$args))
        }
        function ChoiceButton($choice) {
            $nodes=[Collections.Generic.Stack[Windows.DependencyObject]]::new();$nodes.Push((F 'FlowConnectionPairItemsUI04'))
            while($nodes.Count -gt 0) {
                $node=$nodes.Pop()
                if($node -is [Windows.Controls.Button] -and [object]::ReferenceEquals($node.Tag,$choice.PSObject.BaseObject)){return $node}
                for($index=0;$index -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($node);$index++){$nodes.Push([Windows.Media.VisualTreeHelper]::GetChild($node,$index))}
            }
            throw 'Actual connection choice button is missing.'
        }
        $source=$flow[0];$threshold=$flow[1];$judge=$flow[3]
        ConnectionSession $source; $port=ResolveConnection $threshold
        if ($null -eq $port -or $port.PortName -ne 'Image') { throw 'Unique Image pair must resolve directly.' }
        M 'CompleteArmedFlowConnectionS79' @($port) | Out-Null
        if ($threshold.GetInputBinding('Image').SourceToolId -ne $source.ToolId -or (F 'flowPortDragSession') -ne $null) { throw 'Unique Image binding failed.' }
        $previous=$judge.GetInputBinding('Value').SourceToolId
        ConnectionSession $source; $port=ResolveConnection $judge
        if ($null -ne $port -or (F 'pendingConnectionPairsUI04').Count -lt 2 -or $judge.GetInputBinding('Value').SourceToolId -ne $previous) { throw 'Numeric ambiguity silently selected a source.' }
        if (-not (M 'OpenPendingConnectionChoicesUI04')) { throw 'Original connection picker failed.' }
        $choices=@((F 'FlowConnectionPairItemsUI04').Items)
        if (@($choices | Where-Object SourcePortName -eq 'Width').Count -ne 1 -or @($choices | Where-Object SourcePortName -eq 'Height').Count -ne 1 -or (F 'FlowAggregatePortItemsS75').Visibility -ne [Windows.Visibility]::Collapsed) { throw 'Picker lacks exact numeric choices.' }
        if ((F 'isFlowExecutionActive') -or $source.LastOutputUtc -ne $null -or $judge.LastOutputUtc -ne $null) { throw 'Opening choices executed tools.' }
        Pump 100
        $flyout=F 'FlowAggregatePortFlyoutS75';$viewport=F 'FlowGraphViewport'
        $rect=$flyout.TransformToAncestor($viewport).TransformBounds([Windows.Rect]::new(0,0,$flyout.ActualWidth,$flyout.ActualHeight))
        if ($rect.Left -lt 8 -or $rect.Top -lt 8 -or $rect.Right -gt $viewport.ViewportWidth -or $rect.Bottom -gt $viewport.ViewportHeight -or (F 'FlowConnectionInspectorBorder').Visibility -ne [Windows.Visibility]::Collapsed) { throw 'Connection choices clipped or overlapped binding inspector.' }
        Shot 'numeric-connection-choices'
        M 'CloseFlowAggregatePortFlyoutButton_ClickS75' @($null,$null) | Out-Null
        if ((F 'flowPortDragSession') -ne $null -or $judge.GetInputBinding('Value').SourceToolId -ne $previous) { throw 'Cancel changed binding or left armed session.' }
        ConnectionSession $source;ResolveConnection $judge | Out-Null;M 'OpenPendingConnectionChoicesUI04' | Out-Null
        $choice=@((F 'FlowConnectionPairItemsUI04').Items | Where-Object SourcePortName -eq 'Width')[0]
        Pump 50;$button=ChoiceButton $choice
        $up=[Windows.Input.MouseButtonEventArgs]::new([Windows.Input.Mouse]::PrimaryDevice,0,[Windows.Input.MouseButton]::Left);$up.RoutedEvent=[Windows.UIElement]::PreviewMouseLeftButtonUpEvent;$up.Source=$button
        if(-not [object]::ReferenceEquals($up.OriginalSource,$button)){throw 'Synthetic preview source missing.'}
        M 'FlowGraphCanvas_PreviewMouseLeftButtonUp' @((F 'FlowGraphCanvas'),$up) | Out-Null
        if($up.Handled){throw 'Armed connection swallowed choice button MouseUp.'}
        $button.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent,$button))
        if ($judge.GetInputBinding('Value').SourcePortName -ne 'Width') { throw 'Actual Width button did not update binding.' }
        M 'OpenFlowBindingSummaryS79' @($judge) | Out-Null
        $combo=F 'FlowConnectionBindingComboS79'
        if ((F 'FlowConnectionCandidateStatusUI04').Text -notlike '*兼容来源*' -or $combo.SelectedItem.SourcePortName -ne 'Width') { throw 'Binding bar lacks exact source/count.' }
        $combo.SelectedItem=@($combo.Items | Where-Object { $_.SourceToolId -eq $source.ToolId -and $_.SourcePortName -eq 'Height' })[0]
        if ($judge.GetInputBinding('Value').SourcePortName -ne 'Height') { throw 'Reselect did not apply Height.' }
        $judgeId=$judge.ToolId;$sourceId=$source.ToolId
        M 'UndoFlowEditButton_Click' @($null,$null) | Out-Null
        $judge=@((F 'flowTools') | Where-Object ToolId -eq $judgeId)[0]
        if ($judge.GetInputBinding('Value').SourcePortName -ne 'Width') { throw 'Undo reselect failed.' }
        M 'RedoFlowEditButton_Click' @($null,$null) | Out-Null
        $source=@((F 'flowTools') | Where-Object ToolId -eq $sourceId)[0];$judge=@((F 'flowTools') | Where-Object ToolId -eq $judgeId)[0]
        if ($judge.GetInputBinding('Value').SourcePortName -ne 'Height') { throw 'Redo reselect failed.' }
        $difference=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageDifference'),'图像差分',$true,$null)
        (F 'flowTools').Add($difference);M 'RefreshUiState' | Out-Null
        ConnectionSession $source;$port=ResolveConnection $difference
        if ($null -ne $port -or (F 'pendingConnectionPairsUI04').Count -ne 2) { throw 'ImageA/ImageB ambiguity silently selected target.' }
        M 'OpenPendingConnectionChoicesUI04' | Out-Null
        $choice=@((F 'FlowConnectionPairItemsUI04').Items | Where-Object TargetPortName -eq 'ImageB')[0]
        if (-not (M 'ApplyConnectionChoiceUI04' @($choice)) -or $difference.GetInputBinding('ImageB').SourceToolId -ne $source.ToolId -or $difference.GetInputBinding('ImageA') -ne $null) { throw 'Exact ImageB binding failed.' }
        ConnectionSession $source;$port=ResolveConnection $difference
        if ($null -eq $port -or $port.PortName -ne 'ImageB' -or $difference.GetInputBinding('ImageA') -ne $null) { throw 'Duplicate endpoint was offered for the other input.' }
        M 'CancelFlowPortDrag' @($null,$false) | Out-Null
        ConnectionSession $source;ResolveConnection $judge | Out-Null;M 'OpenPendingConnectionChoicesUI04' | Out-Null
        $choice=@((F 'FlowConnectionPairItemsUI04').Items | Where-Object SourcePortName -eq 'Width')[0]
        $main.GetField('isFlowExecutionActive',$all).SetValue($window,$true)
        try { if (M 'ApplyConnectionChoiceUI04' @($choice)) { throw 'Busy picker applied a binding.' } }
        finally { $main.GetField('isFlowExecutionActive',$all).SetValue($window,$false) }
        if ($judge.GetInputBinding('Value').SourcePortName -ne 'Height') { throw 'Busy rejection changed binding.' }
        ConnectionSession $source;ResolveConnection $judge | Out-Null;M 'OpenPendingConnectionChoicesUI04' | Out-Null
        $choice=@((F 'FlowConnectionPairItemsUI04').Items | Where-Object SourcePortName -eq 'Width')[0]
        (F 'flowPortDragSession').EngineeringLeaseS137=-1
        if (M 'ApplyConnectionChoiceUI04' @($choice)) { throw 'Invalid editing lease applied a binding.' }
        ConnectionSession $source;ResolveConnection $judge | Out-Null;M 'OpenPendingConnectionChoicesUI04' | Out-Null
        $choice=@((F 'FlowConnectionPairItemsUI04').Items | Where-Object SourcePortName -eq 'Width')[0]
        $source.IsEnabled=$false
        try {if (M 'ApplyConnectionChoiceUI04' @($choice)) { throw 'Disabled source applied from stale choices.' }} finally {$source.IsEnabled=$true}
        if ($judge.GetInputBinding('Value').SourcePortName -ne 'Height') { throw 'Permission/disabled rejection changed binding.' }
        ConnectionSession $source;ResolveConnection $judge | Out-Null;M 'OpenPendingConnectionChoicesUI04' | Out-Null
        $stale=@((F 'FlowConnectionPairItemsUI04').Items | Where-Object SourcePortName -eq 'Width')[0]
        $position=(F 'flowTools').IndexOf($source);$replacement=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageSource'),'替代实例',$true,$null)
        $replacement.ToolId=$source.ToolId;(F 'flowTools')[$position]=$replacement
        if (M 'ApplyConnectionChoiceUI04' @($stale)) { throw 'Old picker remapped same ID to replacement instance.' }
        (F 'flowTools')[$position]=$source;$replacement.Dispose();M 'RefreshUiState' | Out-Null
        $path=Join-Path $temp 'binding-real-16x12.png';[HalconDotNet.HObject]$image=$null
        try { [HalconDotNet.HOperatorSet]::GenImageConst([ref]$image,'byte',16,12);[HalconDotNet.HOperatorSet]::WriteImage($image,'png',0,$path) } finally {if($image){$image.Dispose()}}
        $source.Parameters.LocalImagePath=$path
        M 'RunStandaloneToolCoreUI02' @($source,'Explicit real source',$false) | Out-Null
        M 'RunStandaloneToolCoreUI02' @($judge,'Explicit downstream',$false) | Out-Null
        $value=$null;if(-not $judge.TryGetOutputValue('Value',[ref]$value) -or [double]$value -ne 12) { throw 'Real Height downstream is not 12.' }
        $recipe=M 'CaptureRecipe';$saved=Join-Path $temp 'connection-choice.json';$service.SaveRecipe($saved,$recipe);$loaded=$service.LoadRecipe($saved)
        $savedFlow=@($loaded.Flows | Where-Object FlowId -eq $loaded.ActiveFlowId)[0];$savedJudge=@($savedFlow.Tools | Where-Object ToolId -eq $judgeId)[0]
        if (@($savedJudge.InputBindings | Where-Object TargetPortName -eq 'Value')[0].SourcePortName -ne 'Height') { throw 'Reselected input did not save.' }
        M 'OpenFlowBindingSummaryS79' @($judge) | Out-Null;Pump 100;Shot 'binding-source-reselect'
        Write-Output 'UI02_BLOCK04_CONNECTION=PASS; UniqueImageDirect=True; NumericAmbiguityExplicit=True; SameTypeInputsExplicit=True; CancelPreserves=True; NoReadOnChoice=True; ExactBindings=True; ReselectUndoRedo=True; BusyRejected=True; WeakInstanceRejected=True; RealHeight12Downstream=True; Save=True; NoMouse=True; NoDeviceConnection=True'
    }
    if ($Phase -eq 'block03') {
        $window.Width = 1366; $window.Height = 768; Pump 100
        $target = $flow[1]; $targetId = $target.ToolId; $firstId = $flow[0].ToolId
        $alternate = M 'CreateFlowTool' @([Enum]::Parse($enumType, 'ImageSource'), '备用图像源', $true, $null)
        $alternateId = $alternate.ToolId
        $flow.Insert(1, $alternate)
        M 'RefreshUiState' | Out-Null
        $sourcePort = @($alternate.FlowOutputPorts | Where-Object PortName -eq 'Image')[0]
        $targetPort = @($target.FlowInputPorts | Where-Object PortName -eq 'Image')[0]
        $pairs = M 'BuildCompatiblePortPairsS79' @($alternate.FlowOutputPorts, $target.FlowInputPorts)
        Write-Output ('AGGREGATE_PAIRS=' + $pairs.Count + '; SourcePorts=' + $alternate.FlowOutputPorts.Count + '; TargetPorts=' + $target.FlowInputPorts.Count)
        if ($pairs.Count -eq 0 -or $pairs[0].Item1.PortName -ne 'Image' -or $pairs[0].Item2.PortName -ne 'Image') { throw 'Automatic aggregate matching must choose Image to Image.' }
        $method = $main.GetMethod('TryApplyFlowPortBinding', $private)
        $arguments = [object[]]@($sourcePort.PSObject.BaseObject, $targetPort.PSObject.BaseObject, '')
        if (-not $method.Invoke($window, $arguments)) { throw ('Real binding failed: ' + $arguments[2]) }
        $recommendationVisibility = (F 'ToolRecommendationContextBorder').Visibility
        M 'OpenFlowBindingSummaryS79' @($target) | Out-Null
        if ((F 'ToolRecommendationContextBorder').Visibility -ne $recommendationVisibility) { throw 'Reading a binding must not replace catalog with recommendations.' }
        if ((F 'FlowConnectionInspectorBorder').Visibility -ne [Windows.Visibility]::Visible -or (F 'centralModuleConfigurationOpenS77')) { throw 'Card binding summary must show original binding bar without parameters.' }
        $combo = F 'FlowConnectionBindingComboS79'
        $originalOption = @($combo.Items | Where-Object { $_.SourceToolId -eq $firstId -and $_.SourcePortName -eq 'Image' })[0]
        if ($null -eq $originalOption) { throw 'Compatible original source must remain selectable.' }
        $combo.SelectedItem = $originalOption
        if ($target.GetInputBinding('Image').SourceToolId -ne $firstId) { throw 'Original binding selector did not apply reselected source.' }
        $badPort = @($alternate.FlowOutputPorts | Where-Object PortName -eq 'Width')[0]
        $arguments = [object[]]@($badPort.PSObject.BaseObject, $targetPort.PSObject.BaseObject, '')
        if ($method.Invoke($window, $arguments) -or $target.GetInputBinding('Image').SourceToolId -ne $firstId) { throw 'Incompatible source must preserve the real binding.' }
        M 'UndoFlowEditButton_Click' @($null,$null) | Out-Null
        $target = @((F 'flowTools') | Where-Object ToolId -eq $targetId)[0]
        if ($target.GetInputBinding('Image').SourceToolId -ne $alternateId) { throw 'Source reselect undo failed.' }
        M 'RedoFlowEditButton_Click' @($null,$null) | Out-Null
        $target = @((F 'flowTools') | Where-Object ToolId -eq $targetId)[0]
        if ($target.GetInputBinding('Image').SourceToolId -ne $firstId) { throw 'Source reselect redo failed.' }
        # Layout-position persistence check; this is not a physical drag simulation.
        $orderedIds = @((F 'flowTools') | ForEach-Object ToolId)
        M 'EnableManualFlowLayout' | Out-Null
        $target.GraphX += 24; $target.GraphY += 24
        M 'RefreshFlowGraphLayout' | Out-Null
        if ((@((F 'flowTools') | ForEach-Object ToolId) -join '|') -ne ($orderedIds -join '|') -or $target.GetInputBinding('Image').SourceToolId -ne $firstId) { throw 'Layout position change altered sequence or binding.' }
        $recipe = M 'CaptureRecipe'
        $savedPath = Join-Path $temp 'binding-layout.json'
        $service.SaveRecipe($savedPath, $recipe)
        $loaded = $service.LoadRecipe($savedPath)
        $savedFlow = @($loaded.Flows | Where-Object { $_.FlowId -eq $loaded.ActiveFlowId })[0]
        $savedTarget = @($savedFlow.Tools | Where-Object ToolId -eq $targetId)[0]
        if ((@($savedFlow.Tools | ForEach-Object ToolId) -join '|') -ne ($orderedIds -join '|') -or $savedTarget.GraphX -ne $target.GraphX -or $savedTarget.GraphY -ne $target.GraphY -or @($savedTarget.InputBindings | Where-Object TargetPortName -eq 'Image')[0].SourceToolId -ne $firstId) { throw 'Binding/layout save roundtrip failed.' }
        M 'OpenFlowBindingSummaryS79' @($target) | Out-Null
        $button = [Windows.Controls.Button]::new()
        $child = [Windows.Controls.TextBlock]::new(); $button.Content = $child
        if (-not (M 'IsFlowNodeCommandHitS79' @($child))) { throw 'Explicit node commands must not be hijacked by nearby anchors.' }
        if ((F 'isFlowExecutionActive') -or (F 'centralModuleConfigurationOpenS77')) { throw 'Binding/layout operations must not run or open parameters.' }
        Pump 100; Shot 'binding-1366x768'
        Write-Output 'UI_BLOCK03_BINDING=PASS; AutoImagePair=True; SummaryShowsBinding=True; Reselect=True; InvalidPreserves=True; UndoRedo=True; LayoutOrderAndSave=True; CommandPriority=True; NoMouse=True; NoFlowExecution=True'
    }
    if ($Phase -eq 'block08') {
        $window.Width = 1366; $window.Height = 768; Pump 50
        $tool = $flow[1]
        $recordType = $assembly.GetType('HalconWinFormsDemo.Models.InspectionRecord',$true)
        $store = F 'resultStore'
        $record = $recordType.GetConstructor([Type[]]@()).Invoke(@())
        $record.Timestamp = [DateTime]::Now; $record.WorkspaceSessionId = F 'projectSessionIdS104'
        $record.FlowId = F 'activeProjectFlowIdS104'; $record.FlowName = 'Original flow name'
        $record.ToolId = $tool.ToolId; $record.ToolName = 'Original module name'; $record.IsRootTool = $true
        $record.ResultCode = 'OK'; $record.Message = 'EXACT_RECORD_A'
        $store.Add($record) | Out-Null
        $wrong = $recordType.GetConstructor([Type[]]@()).Invoke(@())
        $wrong.Timestamp = [DateTime]::Now; $wrong.WorkspaceSessionId = $record.WorkspaceSessionId
        $wrong.FlowId = 'OTHER_FLOW'; $wrong.ToolId = $tool.ToolId; $wrong.ToolName = $tool.InstanceName; $wrong.IsRootTool = $true; $wrong.Message = 'WRONG_FLOW_RECORD'
        $store.Add($wrong) | Out-Null
        (F 'BottomLogTextBox').Text += "`r`n" + $tool.InstanceName + ' NAME_ONLY_FALSE_ASSOCIATION'
        $selectedBefore = (F 'FlowToolList').SelectedItem
        $widthBefore = (F 'ToolCatalogColumn').Width; $zoomBefore = (F 'FlowGraphScaleTransform').ScaleX
        M 'OpenFlowStepActivityS71' @($tool) | Out-Null
        Pump 80
        $viewer = F 'flowStepActivityWindowS71'
        if ($null -eq $viewer -or $viewer.IsActive -or $viewer.ShowActivated -or $viewer.Left -gt -10000) { throw 'Step viewer must stay non-activating and off-screen.' }
        if ((F 'FlowToolList').SelectedItem -ne $selectedBefore -or (F 'ToolCatalogColumn').Width -ne $widthBefore -or (F 'FlowGraphScaleTransform').ScaleX -ne $zoomBefore) { throw 'Opening records changed project selection or layout.' }
        if ((F 'FlowStepLogTextBox').Text -notlike '*EXACT_RECORD_A*' -or (F 'FlowStepLogTextBox').Text -notlike '*Original module name*' -or (F 'FlowStepLogTextBox').Text -like '*WRONG_FLOW_RECORD*' -or (F 'FlowStepLogTextBox').Text -like '*NAME_ONLY_FALSE_ASSOCIATION*') { throw 'Step records did not isolate exact execution identity.' }
        (F 'FlowStepFollowS71').IsChecked = $false
        $frozen = (F 'FlowStepLogTextBox').Text
        $later = $recordType.GetConstructor([Type[]]@()).Invoke(@())
        $later.Timestamp = [DateTime]::Now; $later.WorkspaceSessionId = $record.WorkspaceSessionId; $later.FlowId = $record.FlowId
        $later.ToolId = $tool.ToolId; $later.ToolName = $tool.InstanceName; $later.IsRootTool = $true; $later.Message = 'LATER_RECORD_B'
        $store.Add($later) | Out-Null
        Pump 550
        if ((F 'FlowStepLogTextBox').Text -ne $frozen -or $store.Records.Count -ne 3) { throw 'Pause view stopped collecting or changed frozen text.' }
        (F 'FlowStepSearchS71').Text = 'EXACT_RECORD_A'
        if ((F 'FlowStepLogTextBox').Text -notlike '*EXACT_RECORD_A*' -or (F 'FlowStepLogTextBox').Text -like '*[输入]*') { throw 'Frozen snapshot search failed.' }
        if ((M 'GetFlowStepViewCopyTextS71') -ne (F 'FlowStepLogTextBox').Text) { throw 'Copy payload differs from visible filtered page.' }
        $exportPath = Join-Path $temp 'step-view.txt'
        M 'SaveFlowStepViewSnapshotS71' @($exportPath) | Out-Null
        $exported = [IO.File]::ReadAllText($exportPath)
        if ($exported -notlike '*EXACT_RECORD_A*' -or $exported -like '*LATER_RECORD_B*' -or -not $exported.Contains($tool.ToolId) -or -not $exported.Contains($record.FlowId)) { throw 'Export did not freeze visible identity/text.' }
        (F 'FlowStepSearchS71').Clear(); (F 'FlowStepFollowS71').IsChecked = $true; Pump 550
        if ((F 'FlowStepLogTextBox').Text -notlike '*LATER_RECORD_B*') { throw 'Resume follow did not show new exact record.' }
        Shot 'step-records' $viewer
        $comm = M 'CreateFlowTool' @([Enum]::Parse($enumType,'TcpClientExchange'),'离线通讯记录',$true,$null)
        $comm.Parameters.CommunicationSimulationS121 = $true; $comm.Parameters.TcpSimulationResponseS121 = 'REPLY_RECORD_C'
        $flow.Add($comm); M 'RefreshUiState' | Out-Null
        $task = M 'RunStandaloneCommunicationAsyncS120' @($comm,'Isolated offline communication record')
        for ($i=0; $i -lt 40 -and -not $task.IsCompleted; $i++) { Pump 50 }
        if (-not $task.IsCompleted -or $task.IsFaulted -or $comm.ResultCode -ne 'OK') { throw 'Offline communication fixture did not complete.' }
        if (@($store.Records | Where-Object { $_.ToolId -eq $comm.ToolId -and $_.IsRootTool -eq $true -and $_.FlowId -eq $record.FlowId }).Count -eq 0) { throw 'Real execution did not stamp tool/flow identity.' }
        M 'OpenFlowStepActivityS71' @($comm) | Out-Null
        (F 'FlowStepActivityTabsS71').SelectedIndex = 1
        Write-Output ('COMM_VIEW=' + (F 'FlowStepCommunicationTextBox').Text)
        if ((F 'FlowStepCommunicationTextBox').Text -notlike '*离线模拟*' -or (F 'FlowStepCommunicationTextBox').Text -notlike '*REPLY_RECORD_C*') { throw 'Communication view omitted actual sample output or simulation distinction.' }
        Shot 'step-communication' $viewer
        $flow.Remove($comm) | Out-Null; $comm.Dispose()
        M 'RefreshFlowStepActivityS71' | Out-Null
        if ((F 'FlowStepActivityTitleText').Text -notlike '*失效*' -or (F 'FlowStepLogTextBox').Text -ne '--') { throw 'Deleted instance reused another step or old records.' }
        M 'CloseFlowStepActivityButton_Click' @($null,$null) | Out-Null
        if ((F 'flowStepActivityWindowS71') -ne $null -or (F 'FlowStepActivityPanel').Parent -eq $null) { throw 'Closing records did not return original surface.' }
        Write-Output 'UI_BLOCK08_STEP_VIEW=PASS; DetachedNoLayoutChange=True; ExactFlowToolSession=True; NoNameGuessing=True; FrozenViewStillCollects=True; SearchCopyExportSnapshot=True; ResumeFollow=True; RealOfflineCommunication=True; DeletedInstanceRejected=True; NoMouse=True; NoDeviceConnection=True'
    }
    if ($Phase -eq 'block09') {
        $interactive = $main.GetMethod('IsFlowGraphInteractiveElement',[Reflection.BindingFlags]'Static,NonPublic')
        foreach ($buttonName in @('EmptyExamplesUI09','EmptyOpenRecipeUI09')) {
            $button = F $buttonName; $argsUI09 = [object[]]::new(1); $argsUI09[0] = $button.PSObject.BaseObject
            if (-not $interactive.Invoke($null,$argsUI09)) { throw 'Blank project commands could be intercepted by marquee selection.' }
        }
        $beforeIds = @((F 'flowTools') | ForEach-Object ToolId) -join ','
        $beforeExamples = F 'exampleProgramLoadCountS78'
        $main.GetField('isFlowExecutionActive', $all).SetValue($window, $true)
        try {
            M 'NewRecipeButton_Click' @($null,$null) | Out-Null
            M 'LoadRecipeButton_Click' @($null,$null) | Out-Null
            M 'OpenRecentRecipeUI09' @($null) | Out-Null
            M 'LoadExampleProgramS78' @('basic-segmentation') | Out-Null
        } finally { $main.GetField('isFlowExecutionActive', $all).SetValue($window, $false) }
        if ((@((F 'flowTools') | ForEach-Object ToolId) -join ',') -ne $beforeIds -or (F 'exampleProgramLoadCountS78') -ne $beforeExamples) { throw 'Busy project commands changed the current project.' }
        $recovery = [Activator]::CreateInstance($assembly.GetType('HalconWinFormsDemo.Models.RecipeRecoveryData',$true))
        $recovery.Recipe = M 'CaptureRecipe'; $recovery.CapturedAtUtc = [DateTime]::UtcNow.ToString('O')
        $service.SaveRecovery($recovery)
        $main.GetField('pendingRecovery',$all).SetValue($window,$recovery)
        $main.GetField('isRecipeDirty',$all).SetValue($window,$true)
        $recoveryHash = (Get-FileHash -LiteralPath $service.RecoveryPath).Hash
        if (M 'ResolveUnsavedRecipeChoiceUI09' @([Windows.MessageBoxResult]::Cancel)) { throw 'Cancel accepted project replacement.' }
        if ((F 'pendingRecovery') -ne $recovery -or (Get-FileHash -LiteralPath $service.RecoveryPath).Hash -ne $recoveryHash -or -not (F 'isRecipeDirty') -or (@((F 'flowTools') | ForEach-Object ToolId) -join ',') -ne $beforeIds) { throw 'Cancel altered project or recovery.' }
        # Explicitly arrange a no-confirmation fixture; never display native dialogs.
        $main.GetField('pendingRecovery',$all).SetValue($window,$null)
        $main.GetField('isRecipeDirty',$all).SetValue($window,$false)
        M 'NewRecipeButton_Click' @($null,$null) | Out-Null
        if ((F 'flowTools').Count -ne 0 -or (F 'currentRecipePath') -ne $null -or -not (F 'isRecipeDirty')) { throw 'New project was not blank and unsaved.' }
        Pump 100; Shot 'new-blank'
        $blankPath = Join-Path $temp 'blank-user.json'
        $main.GetField('currentRecipePath',$all).SetValue($window,$blankPath)
        if (-not (M 'SaveCurrentRecipe' @($false))) { throw 'Original existing-path save failed.' }
        $blankSaved = $service.LoadRecipe($blankPath)
        $blankActive = @($blankSaved.Flows | Where-Object FlowId -eq $blankSaved.ActiveFlowId)
        if ($blankActive.Count -ne 1 -or $blankActive[0].Tools.Count -ne 0 -or (F 'isRecipeDirty')) { throw 'Blank save revived default modules or remained dirty.' }
        $menu = F 'RecentRecipesMenuUI09'
        M 'AddRecentRecipe' @((Join-Path $temp 'missing.json')) | Out-Null
        $menu.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.MenuItem]::SubmenuOpenedEvent,$menu))
        if ($menu.Items.Count -ne 2 -or $menu.Items[0].IsEnabled -or -not $menu.Items[1].IsEnabled -or $menu.Items[1].ToolTip -ne $blankPath) { throw 'Recent menu omitted valid or missing project facts.' }
        M 'LoadExampleProgramS78' @('basic-segmentation') | Out-Null
        $exampleFlow = F 'flowTools'
        if ($exampleFlow.Count -ne 8 -or (F 'exampleProgramLoadCountS78') -ne ($beforeExamples + 1) -or (F 'currentRecipePath') -ne $null -or -not (F 'isRecipeDirty')) { throw 'Explicit editable example load failed.' }
        foreach ($tool in $exampleFlow) { if ($tool.LastOutputUtc -ne $null -or $tool.ElapsedMilliseconds -ne 0) { throw 'Example load automatically executed a tool.' } }
        if ($exampleFlow[3].GetInputBinding('Image').SourceToolId -ne $exampleFlow[2].ToolId) { throw 'Example lost real image subscription.' }
        Pump 100; Shot 'editable-example'
        $examplePath = Join-Path $temp 'example-user.json'
        $main.GetField('currentRecipePath',$all).SetValue($window,$examplePath)
        if (-not (M 'SaveCurrentRecipe' @($false))) { throw 'Example existing-path save failed.' }
        $saved = $service.LoadRecipe($examplePath)
        $savedActive = @($saved.Flows | Where-Object FlowId -eq $saved.ActiveFlowId)
        if ($savedActive.Count -ne 1 -or $savedActive[0].Tools.Count -ne 8 -or (F 'isRecipeDirty')) { throw 'Example save lost real tools.' }
        M 'NewRecipeButton_Click' @($null,$null) | Out-Null
        $main.GetField('isRecipeDirty',$all).SetValue($window,$false)
        $menu.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.MenuItem]::SubmenuOpenedEvent,$menu))
        (F 'RecentRecipeList').SelectedItem = $null
        $menu.Items[0].RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.MenuItem]::ClickEvent,$menu.Items[0]))
        if ((F 'flowTools').Count -ne 8 -or (F 'currentRecipePath') -ne $examplePath) { throw 'Recent menu did not use its captured file independently of list selection.' }
        Write-Output 'UI_BLOCK09_PROJECT_LEARNING=PASS; BlankNewAndSave=True; BusySwitchRejected=True; CancelChoiceKeepsRecovery=True; RecentMenuCapturedPath=True; MissingFileDisabled=True; RealEightToolExample=True; NoAutoRun=True; SaveAndReopen=True; NoPhysicalDialogs=True; NoMouse=True'
    }
    if ($Phase -eq 'ui02block03') {
        $window.Width=1366; $window.Height=768
        $folder=Join-Path $temp 'replay-images';[IO.Directory]::CreateDirectory($folder) | Out-Null
        $pathA=Join-Path $folder '01-a.png';$pathB=Join-Path $folder '02-b.png'
        foreach($entry in @(@($pathA,40),@($pathB,200))) {
            [HalconDotNet.HObject]$image=$null;[HalconDotNet.HObject]$filled=$null
            try {
                [HalconDotNet.HOperatorSet]::GenImageConst([ref]$image,'byte',16,16)
                [HalconDotNet.HOperatorSet]::ScaleImage($image,[ref]$filled,0,[int]$entry[1])
                [HalconDotNet.HOperatorSet]::WriteImage($filled,'png',0,[string]$entry[0])
            } finally {if($filled){$filled.Dispose()};if($image){$image.Dispose()}}
        }
        $source=$flow[0]; (F 'FlowToolList').SelectedItem=$source
        M 'OpenCentralModuleConfigurationS77' @($source,$false) | Out-Null
        $editor=F 'moduleParameterWindowS89'
        Write-Output ('SOURCE_OPEN_ACTION='+(F 'DockTrialRunButton').Content+'; LAST_OUTPUT='+$source.LastOutputUtc)
        if ((F 'DockTrialRunButton').Content -notlike '*读取一帧*' -or $source.LastOutputUtc -ne $null) { throw 'Source task did not expose explicit read or auto-read on open.' }
        $lease=M 'CaptureEngineeringEditLeaseS137' @('Source file isolated choice')
        M 'SelectSourceFileDraftUI03' @($pathA,$lease) | Out-Null
        if ($source.LastOutputUtc -ne $null -or -not [string]::IsNullOrWhiteSpace($source.Parameters.LocalImagePath) -or (F 'DockSourceFileBriefUI03').Text -ne '01-a.png' -or -not [string]::IsNullOrWhiteSpace((F 'DockLocalImageSerialTextBox').Text)) { throw 'File selection read/applied or fixed an automatic SN.' }
        M 'DockTrialRunButton_Click' @($null,$null) | Out-Null
        $raw=$null; if (-not $source.TryGetOutputValue('Path',[ref]$raw) -or $raw -ne $pathA -or (F 'DockSourceCurrentFrameUI03').Text -notlike '*草稿帧[[]文件[]]*01-a.png*') { throw 'F5 file did not show actual draft frame provenance.' }
        M 'TryHandleModuleParameterShortcutS89' @([Windows.Input.Key]::Enter,[Windows.Input.ModifierKeys]::Control) | Out-Null
        if ($source.LastOutputUtc -ne $null -or (F 'DockSourceCurrentFrameUI03').Text -notlike '*无有效帧*') { throw 'Apply retained previous frame as current.' }
        M 'RunStandaloneTool' @($source,'Applied file frame A') | Out-Null
        $version=$source.RuntimeOutputVersion
        M 'SelectSourceFileDraftUI03' @($pathB,$lease) | Out-Null
        if ($source.RuntimeOutputVersion -ne $version -or $source.Parameters.LocalImagePath -ne $pathA -or (F 'DockSourceCurrentFrameUI03').Text -notlike '*01-a.png*') { throw 'Selecting B reused label as B or executed a read.' }
        M 'DockTrialRunButton_Click' @($null,$null) | Out-Null
        $raw=$null; $source.TryGetOutputValue('SN',[ref]$raw) | Out-Null
        if ($raw -ne '02-b' -or $source.Parameters.LocalImagePath -ne $pathA -or (F 'DockSourceCurrentFrameUI03').Text -notlike '*02-b.png*') { throw 'Automatic SN or draft file ownership stayed on old file.' }
        Pump 100; Shot 'source-file-current' $editor
        M 'DockRevertButton_Click' @($null,$null) | Out-Null
        (F 'DockImageSourceModeS114').SelectedValue='Replay'; (F 'DockReplayFolderS114').Text=$folder
        if ((F 'DockImageSourceReplayS114').Visibility -ne [Windows.Visibility]::Visible -or (F 'DockImageSourceFileS114').Visibility -ne [Windows.Visibility]::Collapsed) { throw 'Replay mode still displays file controls.' }
        $version=$source.RuntimeOutputVersion
        if ($source.Parameters.ImageSourceModeS114 -eq 'Replay' -or (F 'cameraSessionsS114').Count -ne 0 -or $source.RuntimeOutputVersion -ne $version) { throw 'Mode choice applied/read/connected.' }
        M 'TryHandleModuleParameterShortcutS89' @([Windows.Input.Key]::Enter,[Windows.Input.ModifierKeys]::Control) | Out-Null
        (F 'DockReplayListExpanderUI03').IsExpanded=$true
        $choice=F 'DockReplayNextChoiceUI03'
        if ($choice.Items.Count -ne 2 -or $source.LastOutputUtc -ne $null) { throw 'Replay list failed sorted metadata load or decoded images.' }
        if ($choice.Items[0].Path -ne $pathA -or $choice.Items[1].Path -ne $pathB) { throw 'Replay ordering differs from original filename order.' }
        $version=$source.RuntimeOutputVersion; $choice.SelectedIndex=1
        if ($source.RuntimeOutputVersion -ne $version -or $source.LastOutputUtc -ne $null -or (F 'dockDraftDirty')) { throw 'Selecting next frame read image or modified recipe draft.' }
        M 'DockTrialRunButton_Click' @($null,$null) | Out-Null
        [double]$index=0; if (-not $source.TryGetNumericOutput('FrameIndex',[ref]$index) -or $index -ne 2 -or $choice.SelectedIndex -ne 0) { throw 'Replay did not use selected frame and advance original cursor.' }
        M 'RunStandaloneTool' @($flow[1],'Actual replay B downstream threshold') | Out-Null
        [double]$area=0; if (-not $flow[1].TryGetNumericOutput('Area',[ref]$area) -or $area -ne 256) { throw 'Replay B image did not reach genuine HALCON downstream.' }
        Pump 100; Shot 'source-replay-list' $editor
        M 'DockTrialRunButton_Click' @($null,$null) | Out-Null
        $raw=$null;$source.TryGetOutputValue('Path',[ref]$raw) | Out-Null
        if ($raw -ne $pathA -or -not $source.TryGetNumericOutput('FrameIndex',[ref]$index) -or $index -ne 1) { throw 'Replay did not wrap to first frame.' }
        $version=$source.RuntimeOutputVersion; (F 'DockImageSourceModeS114').SelectedValue='Camera'
        if ((F 'DockImageSourceCameraS114').Visibility -ne [Windows.Visibility]::Visible -or (F 'DockImageSourceReplayS114').Visibility -ne [Windows.Visibility]::Collapsed) { throw 'Camera mode still displays replay controls.' }
        if ($source.RuntimeOutputVersion -ne $version -or (F 'cameraSessionsS114').Count -ne 0 -or $source.Parameters.ImageSourceModeS114 -ne 'Replay' -or (F 'DockSourceCurrentFrameUI03').Text -notlike '*[[]回放[]]*') { throw 'Camera choice connected/read or mislabeled previous replay as camera.' }
        M 'DockRevertButton_Click' @($null,$null) | Out-Null
        (F 'DockReplayListExpanderUI03').IsExpanded=$true
        (F 'DockReplayFolderS114').Text='bad::folder'
        M 'RefreshImageSourceTaskUI03' | Out-Null
        if ((F 'DockReplayNextChoiceUI03').IsEnabled) { throw 'Malformed draft folder retained selectable applied list.' }
        M 'DockRevertButton_Click' @($null,$null) | Out-Null
        $savedPath=Join-Path $temp 'source-replay-user.json'; $sourceId=$source.ToolId
        $service.SaveRecipe($savedPath,(M 'CaptureRecipe'))
        M 'CloseCentralModuleConfigurationS77' | Out-Null
        M 'LoadRecipeFromPath' @($savedPath) | Out-Null
        $source=@((F 'flowTools') | Where-Object ToolId -eq $sourceId)[0]
        if ($source.LastOutputUtc -ne $null -or (F 'cameraSessionsS114').Count -ne 0 -or (F 'replayCursorsS114').Count -ne 0 -or $source.Parameters.ImageReplayFolderS114 -ne $folder) { throw 'Reopen restored runtime cursor/old frame rather than definitions.' }
        (F 'FlowToolList').SelectedItem=$source;M 'OpenCentralModuleConfigurationS77' @($source,$false) | Out-Null
        (F 'DockReplayListExpanderUI03').IsExpanded=$true
        if ((F 'DockReplayNextChoiceUI03').SelectedIndex -ne 0 -or $source.LastOutputUtc -ne $null) { throw 'Reopen list did not start at first without reading.' }
        M 'DockTrialRunButton_Click' @($null,$null) | Out-Null
        if (-not $source.TryGetNumericOutput('FrameIndex',[ref]$index) -or $index -ne 1) { throw 'Explicit reopened read is not first frame.' }
        $source.Parameters.ImageReplayFolderS114=Join-Path $temp 'missing-replay-folder'
        $failed=$false;try {M 'ExecuteFlowTool' @($source,'Missing replay source fixture') | Out-Null}catch{$failed=$true}
        M 'RefreshDockConfigurationRuntime' @($source) | Out-Null
        $image=$null
        if (-not $failed -or $source.TryGetOutputValue('Image',[ref]$image) -or (F 'DockSourceCurrentFrameUI03').Text -notlike '*无有效帧*') { throw 'Missing folder reused old image/current-frame text.' }
        Write-Output 'UI02_BLOCK03_IMAGE_SOURCE=PASS; ShortPathAndActualFrame=True; ExplicitFileChoiceNoReadApply=True; AutomaticSnFollowsFilename=True; DraftFrameProvenance=True; ApplyClearsFrame=True; ReplayListNoDecode=True; SelectNextNoReadOrRecipeChange=True; SelectedSecondAndWrapFirst=True; ActualDownstreamArea256=True; CameraChoiceNoConnection=True; InvalidFolderSafe=True; SaveReopenNoOldFrameCursor=True; ExplicitReopenFrame1=True; MissingSourceClearsImage=True; NoMouse=True; NoDeviceConnection=True'
    }
    if ($Phase -eq 'ui02block02') {
        $window.Width = 1366; $window.Height = 768
        $targetPath = Join-Path $temp 'template-target.png'
        [HalconDotNet.HObject]$baseImage=$null; [HalconDotNet.HObject]$vertical=$null; [HalconDotNet.HObject]$horizontal=$null; [HalconDotNet.HObject]$targetRegion=$null; [HalconDotNet.HObject]$painted=$null
        try {
            [HalconDotNet.HOperatorSet]::GenImageConst([ref]$baseImage,'byte',320,240)
            [HalconDotNet.HOperatorSet]::GenRectangle1([ref]$vertical,40,45,75,54)
            [HalconDotNet.HOperatorSet]::GenRectangle1([ref]$horizontal,66,45,75,85)
            [HalconDotNet.HOperatorSet]::Union2($vertical,$horizontal,[ref]$targetRegion)
            [HalconDotNet.HOperatorSet]::PaintRegion($targetRegion,$baseImage,[ref]$painted,255,'fill')
            [HalconDotNet.HOperatorSet]::WriteImage($painted,'png',0,$targetPath)
        } finally { foreach($obj in @($painted,$targetRegion,$horizontal,$vertical,$baseImage)) { if($null -ne $obj){$obj.Dispose()} } }
        $source = $flow[0]; $source.Parameters.LocalImagePath=$targetPath; $source.Parameters.ImageSourceModeS114='File'
        $shape=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ShapeMatch'),'Shape模板匹配',$true,$null)
        $flow.Add($shape); $shape.SetInputBinding('Image',$source.ToolId,'Image')
        M 'RefreshUiState' | Out-Null; (F 'FlowToolList').SelectedItem=$shape
        M 'OpenCentralModuleConfigurationS77' @($shape,$false) | Out-Null
        $editor=F 'moduleParameterWindowS89'
        if (-not [string]::IsNullOrWhiteSpace([string](F 'DockShapeTrainingRoiComboBox').SelectedValue)) { throw 'Shape silently selected a training ROI.' }
        (F 'DockShapeMinScoreTextBox').Text='0.75'
        $originalScore=$shape.Parameters.ShapeMinScore
        M 'ShapeReadInputButton_ClickS89' @($null,$null) | Out-Null
        if ($shape.Parameters.ShapeMinScore -ne $originalScore -or -not (F 'dockDraftDirty') -or (F 'ShapeTrainingImageS89').Source -eq $null) { throw 'Reading Shape image applied unrelated draft or omitted actual image.' }
        $inputRow=@((F 'dockInputPortRows') | Where-Object PortName -eq 'Image')[0]
        $savedKey=$inputRow.SelectedSourceKey; $version=$source.RuntimeOutputVersion
        $inputRow.SelectedSourceKey=@($inputRow.SourceOptions | Where-Object IsDefault)[0].Key
        M 'ShapeReadInputButton_ClickS89' @($null,$null) | Out-Null
        if ($source.RuntimeOutputVersion -ne $version -or $shape.GetInputBinding('Image').SourceToolId -ne $source.ToolId) { throw 'Shape read committed changed input or read wrong source.' }
        $inputRow.SelectedSourceKey=$savedKey
        M 'ShapeReadInputButton_ClickS89' @($null,$null) | Out-Null
        $main.GetField('shapeTrainingDraftS89',$all).SetValue($window,[Windows.Rect]::new(35,30,60,60))
        M 'TrainShapeFromPreviewS89' | Out-Null
        $path1=F 'shapeDraftModelPathUI02'
        if (-not (Test-Path -LiteralPath $path1) -or -not [string]::IsNullOrWhiteSpace($shape.Parameters.ShapeModelPath) -or $shape.Parameters.ShapeMinScore -ne $originalScore -or -not (F 'dockDraftDirty') -or (M 'HasShapeModelResource' @($shape))) { throw 'Shape training replaced formal parameters/model instead of drafting.' }
        Pump 75; Shot 'shape-model-draft' $editor
        M 'DockTrialRunButton_Click' @($null,$null) | Out-Null
        [double]$score=0
        if (-not $shape.TryGetNumericOutput('Score',[ref]$score) -or $score -lt 0.95 -or -not [string]::IsNullOrWhiteSpace($shape.Parameters.ShapeModelPath) -or (M 'HasShapeModelResource' @($shape))) { throw 'Shape F5 failed genuine matching or leaked draft cache as formal model.' }
        $draftPoseRejected=$false; try { M 'RequireAppliedTemplateTargetS145' @($shape) | Out-Null } catch {$draftPoseRejected=$true}
        if (-not $draftPoseRejected) { throw 'Shape draft target gained applied output qualification.' }
        M 'TryHandleModuleParameterShortcutS89' @([Windows.Input.Key]::Enter,[Windows.Input.ModifierKeys]::Control) | Out-Null
        if ($shape.Parameters.ShapeModelPath -ne $path1 -or $shape.Parameters.ShapeMinScore -ne 0.75) { throw 'Shape apply did not commit the original draft.' }
        M 'RunStandaloneTool' @($shape,'Applied Shape matching') | Out-Null
        if (-not $shape.TryGetNumericOutput('Score',[ref]$score) -or $score -lt 0.95) { throw 'Applied Shape result failed.' }
        M 'RequireAppliedTemplateTargetS145' @($shape) | Out-Null
        $hash1=(Get-FileHash -LiteralPath $path1).Hash
        M 'ShapeReadInputButton_ClickS89' @($null,$null) | Out-Null
        $main.GetField('shapeTrainingDraftS89',$all).SetValue($window,[Windows.Rect]::new(35,30,60,60))
        M 'RunStandaloneTool' @($source,'Deliberate source version change') | Out-Null
        $rejected=$false; try { M 'TrainShapeFromPreviewS89' | Out-Null } catch { $rejected=$true }
        if (-not $rejected -or $shape.Parameters.ShapeModelPath -ne $path1 -or (Get-FileHash -LiteralPath $path1).Hash -ne $hash1) { throw 'Stale Shape preview changed the applied model.' }
        M 'ShapeReadInputButton_ClickS89' @($null,$null) | Out-Null
        $main.GetField('shapeTrainingDraftS89',$all).SetValue($window,[Windows.Rect]::new(35,30,60,60))
        M 'TrainShapeFromPreviewS89' | Out-Null
        $path2=F 'shapeDraftModelPathUI02'
        if ($path2 -eq $path1 -or $shape.Parameters.ShapeModelPath -ne $path1 -or (Get-FileHash -LiteralPath $path1).Hash -ne $hash1) { throw 'Shape retrain overwrote applied version.' }
        M 'DockRevertButton_Click' @($null,$null) | Out-Null
        if ((F 'shapeDraftModelPathUI02') -ne $path1 -or (F 'dockDraftDirty')) { throw 'Revert did not restore applied Shape model selection.' }
        $lease=M 'CaptureEngineeringEditLeaseS137' @('Model import isolated fixture')
        M 'ImportShapeModelDraftUI02' @($shape,$path2,$lease) | Out-Null
        if ((F 'shapeDraftModelPathUI02') -ne $path2 -or $shape.Parameters.ShapeModelPath -ne $path1) { throw 'Model import bypassed original draft.' }
        M 'DockRevertButton_Click' @($null,$null) | Out-Null
        $item=M 'GetShapeTemplateItem' @($shape,$true)
        $exportPath=Join-Path $temp 'shape-export-copy.shm'; $item.Service.SaveCopy($exportPath)
        if ($item.TemplatePath -ne $path1 -or $shape.Parameters.ShapeModelPath -ne $path1 -or -not (Test-Path -LiteralPath $exportPath)) { throw 'Export mutated active resource identity.' }
        M 'CloseCentralModuleConfigurationS77' | Out-Null
        $ncc=M 'CreateFlowTool' @([Enum]::Parse($enumType,'NccTemplateMatch'),'NCC模板匹配',$true,$null)
        $flow.Add($ncc); $ncc.SetInputBinding('Image',$source.ToolId,'Image')
        M 'RefreshUiState' | Out-Null; (F 'FlowToolList').SelectedItem=$ncc
        M 'OpenCentralModuleConfigurationS77' @($ncc,$false) | Out-Null
        M 'DockNccReadS131_Click' @($null,$null) | Out-Null
        $main.GetField('nccTrainingBoxS131',$all).SetValue($window,[Windows.Rect]::new(35,30,60,60))
        M 'DrawNccTrainingBoxS131' | Out-Null
        M 'DockNccTrainS131_Click' @($null,$null) | Out-Null
        for($i=0;$i -lt 200 -and (F 'nccTrainingCancellationS131') -ne $null;$i++){Pump 50}
        $nccDraft=F 'nccDraftS131'
        if (-not $nccDraft.HasModel -or $ncc.Parameters.NccMatchingS131.HasModel -or (F 'DockNccModelStateS131').Text -notlike '*草稿*' -or (F 'DockNccTrainingS131').IsExpanded) { throw 'NCC template failed, applied early, or obscured match settings with preview.' }
        Pump 75; Shot 'ncc-model-draft' (F 'moduleParameterWindowS89')
        M 'DockTrialRunButton_Click' @($null,$null) | Out-Null
        if (-not $ncc.TryGetNumericOutput('Score',[ref]$score) -or $score -lt 0.95 -or $ncc.Parameters.NccMatchingS131.HasModel) { throw 'NCC trial failed matching or applied early.' }
        $draftPoseRejected=$false; try { M 'RequireAppliedTemplateTargetS145' @($ncc) | Out-Null } catch {$draftPoseRejected=$true}
        if (-not $draftPoseRejected) { throw 'NCC draft target gained applied output qualification.' }
        M 'TryHandleModuleParameterShortcutS89' @([Windows.Input.Key]::Enter,[Windows.Input.ModifierKeys]::Control) | Out-Null
        M 'RunStandaloneTool' @($ncc,'Applied NCC matching') | Out-Null
        if (-not $ncc.TryGetNumericOutput('Score',[ref]$score) -or $score -lt 0.95 -or -not $ncc.Parameters.NccMatchingS131.HasModel) { throw 'Applied NCC result failed.' }
        M 'RequireAppliedTemplateTargetS145' @($ncc) | Out-Null
        [HalconDotNet.HObject]$rotateInput=$null;[HalconDotNet.HObject]$rotated=$null
        $rotatedPath=Join-Path $temp 'template-target-179.png'
        try {
            [HalconDotNet.HOperatorSet]::ReadImage([ref]$rotateInput,$targetPath)
            [HalconDotNet.HOperatorSet]::RotateImage($rotateInput,[ref]$rotated,179,'constant')
            [HalconDotNet.HOperatorSet]::WriteImage($rotated,'png',0,$rotatedPath)
        } finally {if($rotated){$rotated.Dispose()};if($rotateInput){$rotateInput.Dispose()}}
        $source.Parameters.LocalImagePath=$rotatedPath
        M 'RunStandaloneTool' @($source,'NCC full-circle seam image') | Out-Null
        M 'RunStandaloneTool' @($ncc,'NCC genuine 179 degree match') | Out-Null
        if (-not $ncc.TryGetNumericOutput('Score',[ref]$score) -or $score -lt 0.7) { throw 'Full-circle NCC lost target at representation seam.' }
        Write-Output ('NCC_FULL_TURN_SEAM_SCORE='+$score)
        $source.Parameters.LocalImagePath=$targetPath
        M 'RunStandaloneTool' @($source,'Restore explicit source image') | Out-Null
        (F 'DockNccAngleStartS131').Text='0'; (F 'DockNccAngleExtentS131').Text='0'
        M 'DockNccReadS131_Click' @($null,$null) | Out-Null
        $main.GetField('nccTrainingBoxS131',$all).SetValue($window,[Windows.Rect]::new(35,30,60,60))
        M 'DockNccTrainS131_Click' @($null,$null) | Out-Null
        for($i=0;$i -lt 200 -and (F 'nccTrainingCancellationS131') -ne $null;$i++){Pump 50}
        if (-not (F 'nccDraftS131').HasModel) { throw 'Limited-angle model was not made.' }
        (F 'DockNccAngleExtentS131').Text='10'
        M 'DockTrialRunButton_Click' @($null,$null) | Out-Null
        if ($ncc.TryGetNumericOutput('Score',[ref]$score) -or (F 'DockFooterText').Text -notlike '*搜索角度超出*') { throw 'Limited model search escaped range or failed to show inline error.' }
        M 'DockRevertButton_Click' @($null,$null) | Out-Null
        $saved=M 'CaptureRecipe'; $modelRecipe=Join-Path $temp 'models-user.json'; $service.SaveRecipe($modelRecipe,$saved)
        $loaded=$service.LoadRecipe($modelRecipe); $active=@($loaded.Flows | Where-Object FlowId -eq $loaded.ActiveFlowId)[0]
        $savedShape=@($active.Tools | Where-Object ToolId -eq $shape.ToolId)[0]; $savedNcc=@($active.Tools | Where-Object ToolId -eq $ncc.ToolId)[0]
        if ($savedShape.Parameters.ShapeModelPath -ne $path1 -or $savedNcc.Parameters.NccMatchingS131.ModelPath -ne $nccDraft.ModelPath) { throw 'Saved project omitted applied model versions.' }
        Write-Output 'UI02_BLOCK02_TEMPLATE_DRAFT=PASS; ShapeReadNoAutoApply=True; InputDraftNotCommitted=True; ShapeTrainingDraftOnly=True; GenuineShapeScoreAbove095=True; NoDraftCacheAsFormalModel=True; ShapeApply=True; StalePreviewRejected=True; VersionPreserved=True; RevertAndImportDraft=True; ExportIdentityPreserved=True; NccDraftAndApply=True; GenuineNccScoreAbove095=True; Actual179DegreeMatch=True; LimitedAngleRejectedInline=True; DraftPoseQualificationRejected=True; AppliedQualificationAccepted=True; ModelVersionsSaved=True; NoMouse=True'
    }
    if ($Phase -eq 'ui02block01') {
        $window.Width = 1366; $window.Height = 768
        $tool = $flow[1]; $layers = F 'roiLayers'
        $roiType = $assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
        $layerType = $assembly.GetType('HalconWinFormsDemo.Models.VmRoiLayer',$true)
        function NewThresholdRoi([string]$name,[double]$r1,[double]$c1,[double]$r2,[double]$c2,[bool]$mask) {
            $layer = [Activator]::CreateInstance($layerType)
            $layer.Name = $name; $layer.IsMask = $mask
            $layer.Geometry = $roiType.GetMethod('CreateRectangle').Invoke($null,[object[]]@($r1,$c1,$r2,$c2))
            $layer.ImageContextId = M 'ResolveTargetImageContextIdS86' @($tool)
            $layers.Add($layer); return $layer
        }
        function ThresholdArea([double]$expected, $node = $tool) {
            [double]$value = 0
            if (-not $node.TryGetNumericOutput('Area',[ref]$value) -or $value -ne $expected) { throw ('Threshold area '+$value+' expected '+$expected+'; '+$node.ErrorMessage) }
        }
        function ThresholdMustFail([string]$reason, $node = $tool) {
            $failed = $false
            try { M 'ExecuteFlowTool' @($node,('ROI failure: '+$reason)) | Out-Null } catch { $failed = $true }
            [double]$oldArea = 0
            if (-not $failed -or [string]::IsNullOrWhiteSpace($node.ErrorMessage) -or $node.TryGetNumericOutput('Area',[ref]$oldArea)) { throw ('ROI failure reused a value: '+$reason) }
        }
        M 'RunStandaloneTool' @($tool,'Threshold baseline full domain') | Out-Null; ThresholdArea 1664
        $detect = NewThresholdRoi '阈值检测区' 5 25 15 35 $false
        (F 'FlowToolList').SelectedItem = $tool
        M 'OpenCentralModuleConfigurationS77' @($tool,$false) | Out-Null
        $editor = F 'moduleParameterWindowS89'
        M 'TryHandleModuleParameterShortcutS89' @([Windows.Input.Key]::D3,[Windows.Input.ModifierKeys]::Control) | Out-Null
        if ((F 'DockRoiSelectionTaskUI02').Visibility -ne [Windows.Visibility]::Visible -or (F 'DockRoiLocalDetailsExpander').IsExpanded) { throw 'Common ROI selection remains hidden in advanced details.' }
        $row = @((F 'dockRoiBindingRows') | Where-Object RoiId -eq $detect.RoiId)[0]
        $row.IsBound = $true; $box = [Windows.Controls.CheckBox]::new(); $box.DataContext = $row; $box.IsChecked = $true
        M 'DockRoiBindingCheckBox_Click' @($box,$null) | Out-Null
        if ($tool.IsRoiBound($detect.RoiId) -or (F 'DockRoiTaskDecisionPanelS86').Visibility -ne [Windows.Visibility]::Visible) { throw 'ROI selection applied early or original task is hidden.' }
        $roiColumnBefore = (F 'RoiLayerPanelColumn').Width
        M 'DockOpenRoiWorkspaceButton_Click' @($null,$null) | Out-Null
        if ($tool.IsRoiBound($detect.RoiId) -or -not (F 'dockDraftDirty') -or (F 'RoiLayerPanelColumn').Width -ne $roiColumnBefore) { throw 'Drawing entry auto-applied ROI draft or expanded legacy panel.' }
        M 'CancelRoiDrawingButton_Click' @($null,$null) | Out-Null
        M 'DockTrialRunButton_Click' @($null,$null) | Out-Null; ThresholdArea 121
        if ($tool.IsRoiBound($detect.RoiId) -or -not (F 'dockDraftDirty')) { throw 'Threshold ROI trial applied the draft.' }
        if ((F 'DockImageThresholdScopeUI02').Text -notlike '*本地检测区 1*') { throw 'Common page omitted current draft scope.' }
        Pump 75; Shot 'threshold-roi-task' $editor
        M 'TryHandleModuleParameterShortcutS89' @([Windows.Input.Key]::Enter,[Windows.Input.ModifierKeys]::Control) | Out-Null
        if (-not $tool.IsRoiBound($detect.RoiId)) { throw 'Threshold ROI apply lost its binding.' }
        M 'RunStandaloneTool' @($tool,'Threshold applied local ROI') | Out-Null; ThresholdArea 121
        M 'RunStandaloneTool' @($flow[2],'Threshold actual Region downstream') | Out-Null
        [double]$total = 0
        if (-not $flow[2].TryGetNumericOutput('TotalArea',[ref]$total) -or $total -ne 121) { throw 'Downstream did not receive actual cropped threshold Region.' }
        # HALCON input already has a reduced domain: ROI must not enlarge it.
        $inputCopy = M 'CreateToolInputImage' @($tool)
        $tiny = $roiType.GetMethod('CreateRectangle').Invoke($null,[object[]]@([double]10,[double]28,[double]12,[double]30))
        $reduced = $inputCopy.ReduceDomain($tiny.Region)
        try { M 'RunImageThresholdTool' @($tool,$reduced) | Out-Null; ThresholdArea 9 } finally { $reduced.Dispose(); $inputCopy.Dispose(); $tiny.Dispose() }
        $mask = NewThresholdRoi '屏蔽区' 10 30 15 35 $true
        $tool.BindRoi($mask.RoiId)
        M 'RunStandaloneTool' @($tool,'Threshold local minus mask') | Out-Null; ThresholdArea 85
        $tool.UnbindRoi($mask.RoiId)
        $detect.IsEnabled = $false; ThresholdMustFail 'disabled ROI'; $detect.IsEnabled = $true
        $correctContext = $detect.ImageContextId; $detect.ImageContextId = 'other-image.Image'; ThresholdMustFail 'coordinate mismatch'; $detect.ImageContextId = $correctContext
        $outside = NewThresholdRoi '图像外检测区' 100 100 110 110 $false
        $tool.ReplaceRoiBindings([string[]]@($outside.RoiId)); ThresholdMustFail 'empty image-domain intersection'
        $tool.ReplaceRoiBindings([string[]]@($detect.RoiId))
        $tool.BindRoi('missing-threshold-roi')
        ThresholdMustFail 'missing binding'
        M 'LoadDockConfigurationDraft' @($tool) | Out-Null
        $missing = @((F 'dockRoiBindingRows') | Where-Object RoiId -eq 'missing-threshold-roi')[0]
        if ($null -eq $missing -or $null -ne $missing.Layer -or -not $missing.IsBound -or -not $missing.CanChangeBinding) { throw 'Missing ROI vanished from draft or cannot be removed.' }
        $draftError = M 'ValidateDockRoiSourceDraftS86' @($tool,$tool.Parameters.Clone())
        if ($draftError -notlike '*失效*') { throw 'Draft accepted missing ROI and could silently run full domain.' }
        M 'TryHandleModuleParameterShortcutS89' @([Windows.Input.Key]::D3,[Windows.Input.ModifierKeys]::Control) | Out-Null
        (F 'DockRoiSelectionScrollUI02').ScrollToEnd()
        Pump 50; Shot 'threshold-missing-roi' $editor
        $missing.IsBound = $false; $box.DataContext = $missing; $box.IsChecked = $false
        M 'DockRoiBindingCheckBox_Click' @($box,$null) | Out-Null
        M 'TryHandleModuleParameterShortcutS89' @([Windows.Input.Key]::Enter,[Windows.Input.ModifierKeys]::Control) | Out-Null
        if ($tool.IsRoiBound('missing-threshold-roi') -or -not $tool.IsRoiBound($detect.RoiId)) { throw 'Explicit ROI repair did not apply the selected bindings.' }
        M 'RunStandaloneTool' @($tool,'Threshold repaired ROI') | Out-Null; ThresholdArea 121
        $child = M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageThreshold'),'继承区域阈值',$true,$null)
        $flow.Add($child); $child.Parameters.ImageThresholdMinGray = 100; $child.Parameters.ImageThresholdMaxGray = 200
        $child.SetInputBinding('Image',$flow[0].ToolId,'Image'); $child.SetInputBinding('ROI',$tool.ToolId,'Region'); $child.Parameters.RoiSourceMode = 'Inherited'
        M 'RunStandaloneTool' @($child,'Threshold inherited Region') | Out-Null; ThresholdArea 121 $child
        $child.BindRoi($mask.RoiId)
        M 'RunStandaloneTool' @($child,'Threshold inherited minus mask') | Out-Null; ThresholdArea 85 $child
        $tool.ClearRuntimeOutputs(); ThresholdMustFail 'missing inherited upstream Region' $child
        M 'RunStandaloneTool' @($tool,'Restore local Region') | Out-Null; ThresholdArea 121
        $child.UnbindRoi($mask.RoiId)
        M 'RunStandaloneTool' @($child,'Restore inherited Region') | Out-Null; ThresholdArea 121 $child
        $savedPath = Join-Path $temp 'threshold-roi-user.json'
        $service.SaveRecipe($savedPath,(M 'CaptureRecipe'))
        $toolId = $tool.ToolId; $sourceId = $flow[0].ToolId
        M 'CloseCentralModuleConfigurationS77' | Out-Null
        M 'LoadRecipeFromPath' @($savedPath) | Out-Null
        $restored = @((F 'flowTools') | Where-Object ToolId -eq $toolId)[0]
        $restoredSource = @((F 'flowTools') | Where-Object ToolId -eq $sourceId)[0]
        if ($null -eq $restored -or -not $restored.IsRoiBound($detect.RoiId) -or $restored.LastOutputUtc -ne $null) { throw 'Saved threshold binding or no-autoload output state failed.' }
        M 'RunStandaloneTool' @($restoredSource,'Reload source explicitly') | Out-Null
        M 'RunStandaloneTool' @($restored,'Reload ROI explicitly') | Out-Null; ThresholdArea 121 $restored
        $restoredSource.ClearRuntimeOutputs(); ThresholdMustFail 'missing image after valid ROI result' $restored
        Write-Output 'UI02_BLOCK01_THRESHOLD_ROI=PASS; CommonRoiListVisible=True; DrawingKeepsDraftAndLayout=True; FullArea1664=True; DraftLocal121NotApplied=True; AppliedLocal121=True; DomainIntersection9=True; Mask85=True; RealRegionDownstream121=True; DisabledMissingCrossContextEmptyRejected=True; MissingDraftNotDropped=True; ExplicitRepair=True; Inherited121Mask85=True; MissingUpstreamNoOldValue=True; SaveReload121=True; NoMouse=True; NoAutoRun=True'
    }
    if ($Phase -eq 'block10') {
        M 'OpenHelpCenterS78' @($null) | Out-Null; Pump 100
        $help = F 'helpCenterWindowS78'; $helpType = $help.GetType()
        function HF([string]$name) { Write-Output -NoEnumerate ($helpType.GetField($name,$all).GetValue($help)) }
        if ($help.ShowActivated -or $help.IsActive -or $help.Left -gt -10000) { throw 'Help diagnostics activated the foreground.' }
        $doc = HF 'HelpDocument'
        $text = [Windows.Documents.TextRange]::new($doc.ContentStart,$doc.ContentEnd).Text
        if ($text -notlike '*Ctrl+Enter*' -or $text.Length -gt 1100) { throw 'Quick start is not concise or misses actual shortcuts.' }
        Shot 'help-quick-start' $help
        $thresholdKind = [Enum]::Parse($enumType,'ImageThreshold')
        M 'OpenHelpCenterS78' @($thresholdKind) | Out-Null
        if ((F 'helpCenterWindowS78') -ne $help -or (HF 'HelpTopicList').SelectedItem.ToolKind -ne $thresholdKind) { throw 'Current module help duplicated windows or selected wrong topic.' }
        (HF 'HelpScopeUI10').SelectedIndex = 2
        foreach ($topic in (HF 'visibleTopics')) { if ($null -eq $topic.ToolKind) { throw 'Module scope contains operation guides.' } }
        (HF 'HelpSearchTextBox').Text = 'ImageThreshold Region'
        if ((HF 'visibleTopics').Count -ne 1 -or (HF 'HelpTopicList').SelectedItem.ToolKind -ne $thresholdKind) { throw 'Actual port multiword help search failed.' }
        (HF 'HelpSearchTextBox').Clear()
        if ((HF 'HelpTopicList').SelectedItem.ToolKind -ne $thresholdKind) { throw 'Clearing help search lost visible current module.' }
        (HF 'HelpSearchTextBox').Text = 'no-matching-help-ui10'
        $text = [Windows.Documents.TextRange]::new($doc.ContentStart,$doc.ContentEnd).Text
        if ((HF 'visibleTopics').Count -ne 0 -or $text -notlike '*没有匹配*' -or $text -like '*灰度阈值*') { throw 'Empty search reused stale help content.' }
        (HF 'HelpSearchTextBox').Clear(); (HF 'HelpScopeUI10').SelectedIndex = 1
        foreach ($topic in (HF 'visibleTopics')) { if ($null -ne $topic.ToolKind) { throw 'Operation scope contains module entries.' } }
        $help.SelectToolTopicUI10($null); Pump 50
        Shot 'help-search-directory' $help
        $help.Close(); if ((F 'helpCenterWindowS78') -ne $null) { throw 'Closed help retained the window instance.' }
        $main.GetField('isRecipeDirty',$all).SetValue($window,$false)
        M 'LoadExampleProgramS78' @('project-object-production') | Out-Null; Pump 100
        $guide = F 'learningGuideWindowS146'
        if ($null -eq $guide -or $guide.ShowActivated -or $guide.IsActive -or $guide.Left -gt -10000 -or (F 'learningStepDetailsS148').IsExpanded) { throw 'Learning guide did not remain concise and nonactivated.' }
        $stepBefore = (F 'learningStepTitleS146').Text
        $projectIds = @((F 'flowTools') | ForEach-Object ToolId) -join ','
        M 'MoveLearningStepS146' @([int]1) | Out-Null
        if ((F 'learningStepTitleS146').Text -eq $stepBefore -or (@((F 'flowTools') | ForEach-Object ToolId) -join ',') -ne $projectIds) { throw 'Learning navigation did not change step or changed project instances.' }
        foreach ($node in (F 'flowTools')) { if ($node.LastOutputUtc -ne $null) { throw 'Reading learning instructions executed algorithms.' } }
        Shot 'learning-current-step' $guide
        $guide.Close()
        Write-Output 'UI_BLOCK10_HELP_CORE=PASS; OriginalCatalogAdd=True; OriginalImageBinding=True; RealThresholdDraft1664=True; ApplySave=True; CompactSixStepHelp=True; ActualPortSearch=True; SelectedTopicPreserved=True; EmptySearchNoStaleContent=True; SingleHelpWindow=True; LearningNavigationNoRun=True; NoForegroundActivation=True; NoMouse=True'
    }
    if ($Phase -notin @('block04','block05','block06','block07','block08','block09','block10','ui02block01','ui02block02','ui02block03','ui02block04','ui02block05','ui02block06','ui02block07','ui02block08','ui02block09')) { Write-Output 'UI_BASELINE_STARTUP=PASS; Blank=True; ExplicitGraphOnly=True; NoFlowExecution=True; NoMouse=True; NoDeviceConnection=True' }
} finally {
    if ($null -ne $window) {
        if ((F 'flowStepActivityWindowS71') -ne $null) { M 'CloseFlowStepActivityButton_Click' @($null,$null) | Out-Null }
        if ((F 'helpCenterWindowS78') -ne $null) { (F 'helpCenterWindowS78').Close() }
        if ((F 'learningGuideWindowS146') -ne $null) { (F 'learningGuideWindowS146').Close() }
        $main.GetField('closingModuleParameterWindowS89', $all).SetValue($window, $true)
        $main.GetField('dockDraftDirty', $all).SetValue($window, $false)
        $main.GetField('recipeCloseConfirmed', $all).SetValue($window, $true)
        $window.Close()
    }
    $app.Shutdown()
    if ($logger -is [IDisposable]) { $logger.Dispose() }
    # Preserve this unique isolated state directory for failure diagnostics.
    Write-Output ('ISOLATED_STATE=' + $temp)
}
