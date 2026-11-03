# Compact result workspace and existing flow management placement; no physical input.
function RefreshCompact10 {
    M 'RefreshInspector'|Out-Null
    M 'RefreshUiState'|Out-Null
    Pump 70
}
function BoundsCompact10($element,$root) {
    $element.TransformToAncestor($root).TransformBounds([Windows.Rect]::new(0,0,$element.ActualWidth,$element.ActualHeight))
}
function AssertResultVisibleCompact10([string]$name) {
    $element=F $name;$tabs=F 'RightTabs'
    if(-not $element.IsVisible){throw ('Compact result lost visible content: '+$name)}
    $rect=BoundsCompact10 $element $tabs
    if($rect.Top -lt -1 -or $rect.Left -lt -1 -or $rect.Bottom -gt $tabs.ActualHeight+1 -or $rect.Right -gt $tabs.ActualWidth+1){
        throw ('Compact result clips '+$name+'; bounds='+$rect+'; viewport='+$tabs.ActualWidth+'x'+$tabs.ActualHeight)
    }
}
function AssertAutoCompact10([string]$stage) {
    if(-not (F 'RightConfigurationRow').Height.IsAuto -or (F 'RightConfigurationRow').MaxHeight -gt 240.5 -or -not (F 'ImageWorkspaceRow').Height.IsStar){throw ('Default compact layout missing: '+$stage)}
    if((F 'RightConfigurationRow').ActualHeight -gt 240.5 -or (F 'ImageWorkspaceRow').ActualHeight -le (F 'RightConfigurationRow').ActualHeight){throw ('Image/result hierarchy failed: '+$stage)}
}
$flow=F 'flowTools';$source=$flow[0];$threshold=$flow[1]
$ids=@($flow|ForEach-Object ToolId)-join ','
(F 'FlowToolList').SelectedItem=$threshold;RefreshCompact10
$definition=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
    $window.Width=$size[0];$window.Height=$size[1];Pump 70
    AssertAutoCompact10 'pending'
    $resultHeight=(F 'RightConfigurationRow').ActualHeight
    $workspaceHeight=(F 'MainWorkspaceGrid').ActualHeight
    $oldResultHeight=[Math]::Max(180,($workspaceHeight-5)*2/7)
    $oldImageHeight=$workspaceHeight-5-$oldResultHeight
    if($resultHeight -ge 180 -or (F 'ImageWorkspaceRow').ActualHeight -lt $oldImageHeight+15){throw 'Pending result still reserves a large empty block instead of returning room to the image.'}
    AssertResultVisibleCompact10 'FlowRunStatusText'
    AssertResultVisibleCompact10 'SelectedResultToolText'
    AssertResultVisibleCompact10 'WorkspaceRunDetailsExpander'
    $manage=F 'ProjectFlowManageUI10'
    if($manage.IsVisible -or (F 'FlowHeadingCommandsUI06').IsAncestorOf($manage) -or -not (F 'FlowMoreManagementRowUI10').IsAncestorOf($manage)){throw 'Flow management still occupies the default flow heading.'}
    if(-not (F 'ProjectFlowPickerS104').IsVisible -or -not (F 'FlowMoreToggleButton').IsVisible){throw 'Current flow selection or operation entry disappeared.'}
    Write-Output ('UI05_COMPACT_LAYOUT='+$size[0]+'x'+$size[1]+'; Result='+$resultHeight+'; Image='+(F 'ImageWorkspaceRow').ActualHeight+'; ImageGain='+((F 'ImageWorkspaceRow').ActualHeight-$oldImageHeight))
    Shot ('compact-pending-'+$size[0]+'x'+$size[1])
}
$window.Width=1366;$window.Height=768
(F 'FlowMoreToggleButton').IsChecked=$true;Pump 50
$manage=F 'ProjectFlowManageUI10'
if(-not $manage.IsVisible -or $manage.Content -ne '方案流程 ▾' -or $null -eq $manage.ContextMenu){throw 'Original project flow management was not discoverable under flow operations.'}
$items=@($manage.ContextMenu.Items|Where-Object {$_ -is [Windows.Controls.MenuItem]})
if($items.Count -ne 5 -or (@($items|Where-Object Tag|ForEach-Object Tag)-join ',') -ne 'New,Rename,Copy,Delete' -or $items[4].Header -notlike '*恢复*'){throw 'Project flow management lost original commands or dispatch tags.'}
foreach($handler in @('OpenProjectFlowMenuS104_Click','EditProjectFlowS104_Click','RestoreDeletedProjectFlowS104_Click')){
    if($null -eq $main.GetMethod($handler,$private)){throw ('Original project flow handler missing: '+$handler)}
}
# Check the existing containing-panel dismissal handler without opening a native menu:
# its placement target must remain visible until the user chooses a menu command.
$menuBubble=[Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent,$manage)
M 'FlowMoreCommand_ClickUI10' @((F 'FlowMorePanelUI10'),$menuBubble)|Out-Null
if(-not (F 'FlowMoreToggleButton').IsChecked -or -not $manage.IsVisible){throw 'Flow management immediately hid the original native-menu placement target.'}
$manageBounds=BoundsCompact10 $manage (F 'FlowMorePanelUI10')
if($manageBounds.Left -lt 0 -or $manageBounds.Right -gt (F 'FlowMorePanelUI10').ActualWidth+1){throw 'Flow management command clips in the existing bounded panel.'}
$locatorBounds=BoundsCompact10 (F 'FlowModuleNavigatorUI05') (F 'FlowMorePanelUI10')
$queueBounds=BoundsCompact10 (F 'ImageIndexText') (F 'FlowMorePanelUI10')
if($locatorBounds.Right -gt $queueBounds.Left-1 -or $queueBounds.Right -gt (F 'FlowMorePanelUI10').ActualWidth-1){throw 'Module locator overlaps the queue summary in flow operations.'}
Shot 'compact-flow-operations'
(F 'FlowMoreToggleButton').IsChecked=$false
if($service.GetRecipeFingerprint((M 'CaptureRecipe')) -ne $definition -or @($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count){throw 'Presentation navigation mutated or ran the original flow.'}
# Expansion is an explicit request for detail space; closing returns the compact default.
(F 'WorkspaceRunDetailsExpander').IsExpanded=$true;Pump 60
if(-not (F 'RightConfigurationRow').Height.IsStar -or (F 'RightConfigurationRow').MinHeight -lt 180 -or -not [double]::IsPositiveInfinity((F 'RightConfigurationRow').MaxHeight)){throw 'Expanded flow detail remains confined to the compact summary.'}
Shot 'compact-details-expanded'
(F 'WorkspaceRunDetailsExpander').IsExpanded=$false;Pump 60
AssertAutoCompact10 'details collapsed'
M 'RightImageRoiMenuItem_Click' @($null,$null)|Out-Null;Pump 60
if((F 'RightTabs').SelectedItem -ne (F 'RoiTab') -or -not (F 'RightConfigurationRow').Height.IsStar){throw 'Original ROI workspace did not recover its normal space.'}
M 'RightResultTaskButton_Click' @($null,$null)|Out-Null;Pump 60
AssertAutoCompact10 'return from ROI'
# The movable parameter editor must not change the main split, apply the draft, or run a tool.
$imageLength=(F 'ImageWorkspaceRow').Height;$resultLength=(F 'RightConfigurationRow').Height
$catalogWidth=(F 'ToolCatalogColumn').Width;$zoom=(F 'FlowGraphScaleTransform').ScaleX
M 'OpenCentralModuleConfigurationS77' @($threshold,$false)|Out-Null;Pump 60
$applied=$threshold.Parameters.ImageThresholdMinGray
(F 'DockImageThresholdMinTextBox').Text=([double]$applied+1).ToString([Globalization.CultureInfo]::InvariantCulture)
RefreshCompact10
(F 'FlowMoreToggleButton').IsChecked=$true;Pump 30
(F 'FlowMoreToggleButton').IsChecked=$false;Pump 30
if(-not (F 'dockDraftDirty') -or $threshold.Parameters.ImageThresholdMinGray -ne $applied -or (F 'ImageWorkspaceRow').Height -ne $imageLength -or (F 'RightConfigurationRow').Height -ne $resultLength -or (F 'ToolCatalogColumn').Width -ne $catalogWidth -or (F 'FlowGraphScaleTransform').ScaleX -ne $zoom){throw 'Parameter draft or flow operations changed the main layout/applied definition.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null
M 'CloseCentralModuleConfigurationS77'|Out-Null
# Reuse the real tiny HALCON image/Area3072, original details route, F5 draft/cancel,
# disabled-source and historical qualification checks; this is not a mock summary.
. (Join-Path $PSScriptRoot 'UIBatch04ImageResults.ps1')
M 'RunStandaloneToolCoreUI02' @($source,'UI05 compact source recovery',$false)|Out-Null
M 'RunStandaloneToolCoreUI02' @($threshold,'UI05 compact result recovery',$false)|Out-Null
(F 'FlowToolList').SelectedItem=$threshold;RefreshCompact10
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
    $window.Width=$size[0];$window.Height=$size[1];Pump 60
    AssertAutoCompact10 'actual output'
    foreach($name in @('SelectedResultToolText','SelectedResultOutputText','SelectedResultOriginTextS88','WorkspaceRunDetailsExpander')){AssertResultVisibleCompact10 $name}
    Shot ('compact-result-'+$size[0]+'x'+$size[1])
}
$goodPath=$source.Parameters.LocalImagePath
$source.Parameters.LocalImagePath=Join-Path $temp 'compact-missing-source.png'
$sourceFailed=$false;$thresholdFailed=$false
try {M 'RunStandaloneToolCoreUI02' @($source,'UI05 compact missing source',$false)|Out-Null} catch {$sourceFailed=$true}
try {M 'RunStandaloneToolCoreUI02' @($threshold,'UI05 compact missing input',$false)|Out-Null} catch {$thresholdFailed=$true}
RefreshCompact10
if(-not $sourceFailed -or -not $thresholdFailed -or [string]::IsNullOrWhiteSpace($threshold.ErrorMessage) -or (F 'SelectedResultStatusText').Text -ne '异常'){throw 'Real missing input did not surface in the compact result.'}
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
    $window.Width=$size[0];$window.Height=$size[1];Pump 60
    AssertAutoCompact10 'actual error'
    AssertResultVisibleCompact10 'SelectedResultErrorText'
    AssertResultVisibleCompact10 'WorkspaceRunDetailsExpander'
    Shot ('compact-error-'+$size[0]+'x'+$size[1])
}
$source.Parameters.LocalImagePath=$goodPath
M 'RunStandaloneToolCoreUI02' @($source,'UI05 compact final source',$false)|Out-Null
M 'RunStandaloneToolCoreUI02' @($threshold,'UI05 compact final result',$false)|Out-Null
RefreshCompact10
[double]$area=0
if(-not $threshold.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 3072){throw 'Original typed result failed to recover after the missing-input check.'}
# Raise the original Thumb events to exercise GridSplitter's resize path, without
# synthesizing mouse/keyboard input or acquiring foreground focus.
$window.Width=1366;$window.Height=768;Pump 60
$splitter=F 'RightWorkspaceSplitter'
function StartSplitCompact10 {
    $args10=[Windows.Controls.Primitives.DragStartedEventArgs]::new(0,0)
    $args10.RoutedEvent=[Windows.Controls.Primitives.Thumb]::DragStartedEvent
    $splitter.RaiseEvent($args10);Pump 30
}
function MoveSplitCompact10([double]$vertical) {
    $args10=[Windows.Controls.Primitives.DragDeltaEventArgs]::new(0,$vertical)
    $args10.RoutedEvent=[Windows.Controls.Primitives.Thumb]::DragDeltaEvent
    $splitter.RaiseEvent($args10);Pump 40
}
function EscapeSplitCompact10 {
    # Exercise both the application's routed preview hook and GridSplitter's real
    # OnKeyDown cancellation. A synthetic DragCompleted(Canceled) alone would miss
    # the framework path which restores lengths without sending that completion.
    $presentation=[Windows.PresentationSource]::FromVisual($splitter)
    $key10=[Windows.Input.KeyEventArgs]::new([Windows.Input.Keyboard]::PrimaryDevice,$presentation,0,[Windows.Input.Key]::Escape)
    $key10.RoutedEvent=[Windows.Input.Keyboard]::PreviewKeyDownEvent
    $splitter.RaiseEvent($key10)
    $key10.RoutedEvent=[Windows.Input.Keyboard]::KeyDownEvent
    $onKey10=[Windows.Controls.GridSplitter].GetMethod('OnKeyDown',$private)
    if($null -eq $onKey10){throw 'Original GridSplitter keyboard cancellation method unavailable.'}
    $onKey10.Invoke($splitter,[object[]]@($key10.PSObject.BaseObject))|Out-Null
    Pump 60
    $resizeData10=[Windows.Controls.GridSplitter].GetField('_resizeData',$private)
    if($null -eq $resizeData10 -or $null -ne $resizeData10.GetValue($splitter)){throw 'Original GridSplitter Escape did not clear its active resize state.'}
}
function AssertAutoRestoredCompact10([string]$stage) {
    AssertAutoCompact10 $stage
    if((F 'rightWorkspaceUserSizedUI10') -or (F 'rightWorkspaceResizingUI10') -or (F 'RightConfigurationRow').MaxHeight -ne 240){throw ('Automatic result sizing was lost after '+$stage)}
}
$autoImageLength=(F 'ImageWorkspaceRow').Height;$autoResultLength=(F 'RightConfigurationRow').Height
StartSplitCompact10
$noMovement10=[Windows.Controls.Primitives.DragCompletedEventArgs]::new(0,0,$false)
$noMovement10.RoutedEvent=[Windows.Controls.Primitives.Thumb]::DragCompletedEvent
$splitter.RaiseEvent($noMovement10);Pump 50
AssertAutoRestoredCompact10 'click without movement'
if((F 'ImageWorkspaceRow').Height -ne $autoImageLength -or (F 'RightConfigurationRow').Height -ne $autoResultLength){throw 'No-movement split changed the original row lengths.'}
StartSplitCompact10
MoveSplitCompact10 -24
EscapeSplitCompact10
AssertAutoRestoredCompact10 'Escape after compact drag'
if((F 'ImageWorkspaceRow').Height -ne $autoImageLength -or (F 'RightConfigurationRow').Height -ne $autoResultLength){throw 'Compact Escape failed to restore both original row lengths.'}
# Route a focus-loss event on the same off-screen control; no real focus transfer.
StartSplitCompact10
MoveSplitCompact10 -16
$focusLost10=[Windows.Input.KeyboardFocusChangedEventArgs]::new([Windows.Input.Keyboard]::PrimaryDevice,0,$splitter,(F 'ProjectFlowPickerS104'))
$focusLost10.RoutedEvent=[Windows.Input.Keyboard]::LostKeyboardFocusEvent
$splitter.RaiseEvent($focusLost10);Pump 60
AssertAutoRestoredCompact10 'splitter focus loss'
$focusResizeData10=[Windows.Controls.GridSplitter].GetField('_resizeData',$private)
if($null -eq $focusResizeData10 -or $null -ne $focusResizeData10.GetValue($splitter) -or (F 'ImageWorkspaceRow').Height -ne $autoImageLength -or (F 'RightConfigurationRow').Height -ne $autoResultLength){throw 'Original GridSplitter focus cancellation retained resize state or changed original lengths.'}
# A detail workspace starts with two star rows. Exercise its actual splitter move
# before Escape, then require the original star lengths and automatic mode back.
(F 'WorkspaceRunDetailsExpander').IsExpanded=$true;Pump 60
$detailImageLength=(F 'ImageWorkspaceRow').Height;$detailResultLength=(F 'RightConfigurationRow').Height
if(-not $detailImageLength.IsStar -or -not $detailResultLength.IsStar){throw 'Detail resize fixture did not start with two star rows.'}
$detailImageHeight=(F 'ImageWorkspaceRow').ActualHeight;$detailResultHeight=(F 'RightConfigurationRow').ActualHeight
StartSplitCompact10
MoveSplitCompact10 -24
if((F 'ImageWorkspaceRow').ActualHeight -ge $detailImageHeight-5 -or (F 'RightConfigurationRow').ActualHeight -le $detailResultHeight+5){throw 'Original GridSplitter two-star detail resize did not change the actual heights.'}
Write-Output ('UI05_SPLITTER_DETAIL=Image '+$detailImageHeight+'->'+(F 'ImageWorkspaceRow').ActualHeight+'; Result '+$detailResultHeight+'->'+(F 'RightConfigurationRow').ActualHeight)
EscapeSplitCompact10
if((F 'rightWorkspaceUserSizedUI10') -or (F 'rightWorkspaceResizingUI10') -or (F 'ImageWorkspaceRow').Height -ne $detailImageLength -or (F 'RightConfigurationRow').Height -ne $detailResultLength -or -not [double]::IsPositiveInfinity((F 'RightConfigurationRow').MaxHeight)){throw 'Detail Escape failed to restore original two-star layout and automatic mode.'}
(F 'WorkspaceRunDetailsExpander').IsExpanded=$false;Pump 60
AssertAutoRestoredCompact10 'details collapsed after Escape'
$beforeImage=(F 'ImageWorkspaceRow').ActualHeight;$beforeResult=(F 'RightConfigurationRow').ActualHeight
$start=[Windows.Controls.Primitives.DragStartedEventArgs]::new(0,0)
$start.RoutedEvent=[Windows.Controls.Primitives.Thumb]::DragStartedEvent
$splitter.RaiseEvent($start);Pump 30
if(-not (F 'rightWorkspaceUserSizedUI10') -or -not [double]::IsPositiveInfinity((F 'RightConfigurationRow').MaxHeight)){throw 'User resizing did not release the automatic summary height limit.'}
$delta=[Windows.Controls.Primitives.DragDeltaEventArgs]::new(0,-28)
$delta.RoutedEvent=[Windows.Controls.Primitives.Thumb]::DragDeltaEvent
$splitter.RaiseEvent($delta);Pump 40
$complete=[Windows.Controls.Primitives.DragCompletedEventArgs]::new(0,-28,$false)
$complete.RoutedEvent=[Windows.Controls.Primitives.Thumb]::DragCompletedEvent
$splitter.RaiseEvent($complete);Pump 40
if((F 'ImageWorkspaceRow').ActualHeight -ge $beforeImage-5 -or (F 'RightConfigurationRow').ActualHeight -le $beforeResult+5){throw 'Original GridSplitter event path did not move space from image to results.'}
Write-Output ('UI05_SPLITTER_RESULT=Image '+$beforeImage+'->'+(F 'ImageWorkspaceRow').ActualHeight+'; Result '+$beforeResult+'->'+(F 'RightConfigurationRow').ActualHeight)
(F 'ImageWorkspaceRow').Height=[Windows.GridLength]::new(380)
(F 'RightConfigurationRow').Height=[Windows.GridLength]::new(210)
M 'RightImageRoiMenuItem_Click' @($null,$null)|Out-Null
M 'RightResultTaskButton_Click' @($null,$null)|Out-Null
(F 'WorkspaceRunDetailsExpander').IsExpanded=$true;Pump 40
(F 'WorkspaceRunDetailsExpander').IsExpanded=$false;Pump 40
M 'SetProductionRunMode' @($true)|Out-Null;Pump 40
M 'SetProductionRunMode' @($false)|Out-Null;RefreshCompact10
if((F 'ImageWorkspaceRow').Height.Value -ne 380 -or (F 'RightConfigurationRow').Height.Value -ne 210 -or -not (F 'DeveloperToolbarPanel').IsVisible){throw 'Navigation, details or mode return overwrote the user-sized split.'}
$save=Join-Path $temp 'compact-workspace10.json'
$service.SaveRecipe($save,(M 'CaptureRecipe'));$loaded=$service.LoadRecipe($save)
$savedFlow=@($loaded.Flows|Where-Object FlowId -eq $loaded.ActiveFlowId)[0]
if((@($savedFlow.Tools|ForEach-Object ToolId)-join ',') -ne $ids -or (@($flow|ForEach-Object ToolId)-join ',') -ne $ids){throw 'Compact workspace changed saved module identities.'}
if($window.IsActive -or $window.ShowActivated -or (F 'isFlowExecutionActive')){throw 'Compact verification activated the desktop or left execution running.'}
Shot 'compact-user-split-preserved'
Write-Output 'UI05_WORKSPACE_COMPACT=PASS; ThreeSizes; PendingImageSpaceRecovered; FullOutputAndActualErrorVisible; DetailAndWorkspaceExpansion; ProjectFlowMenuPreserved; DraftNoSqueeze; TypedArea3072; F5NotApplied; MissingInputRecovery; SplitNoMovementRestored; FrameworkEscapeRestored; FrameworkFocusLossRestored; TwoStarDetailDragAndEscape; UserSplitPreserved; SaveIdentity; NoMouse'
