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
    $directory = Join-Path $ProjectRoot 'artifacts\ui-batch-01'
    [IO.Directory]::CreateDirectory($directory) | Out-Null
    $path = Join-Path $directory ($Phase + '-' + $name + '.png')
    $surface.UpdateLayout()
    $render = [Windows.Media.Imaging.RenderTargetBitmap]::new([int][Math]::Ceiling($surface.ActualWidth), [int][Math]::Ceiling($surface.ActualHeight), 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
    $render.Render($surface)
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
    if ($Phase -like 'block04*' -or $Phase -eq 'block05' -or $Phase -eq 'block06' -or $Phase -eq 'block07' -or $Phase -eq 'block10') {
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
        if ($Phase -eq 'block04' -or $Phase -eq 'block05' -or $Phase -eq 'block06' -or $Phase -eq 'block07' -or $Phase -eq 'block10') {
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
        if ($Phase -eq 'block04' -or $Phase -eq 'block06' -or $Phase -eq 'block10') {
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
    if ($Phase -notin @('block04','block05','block06','block07','block08','block09','block10')) { Write-Output 'UI_BASELINE_STARTUP=PASS; Blank=True; ExplicitGraphOnly=True; NoFlowExecution=True; NoMouse=True; NoDeviceConnection=True' }
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
