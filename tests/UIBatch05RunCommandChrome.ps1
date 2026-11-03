# Real original run commands and contextual shortcut presentation; no physical input or device traffic.
function ClickRun05([string]$name){$b=F $name;if((F 'FlowMoreContentUI06').IsAncestorOf($b) -and -not $b.IsVisible){(F 'FlowMoreToggleButton').IsChecked=$true;Pump 30};if(-not $b.IsEnabled){throw ('Disabled run action: '+$name)};$b.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));Pump 40}
function IdleRun05{
    $deadline=[DateTime]::UtcNow.AddSeconds(20)
    while((F 'isFlowExecutionActive') -and [DateTime]::UtcNow -lt $deadline){Pump 40}
    if(F 'isFlowExecutionActive'){M 'StopRunButton_Click' @($null,$null)|Out-Null;throw 'Original run did not finish within bounded wait.'}
}
function CheckBarRun05([bool]$paused){
    $bar=F 'FlowHeadingCommandsUI06';$previousRight=-1
    $elements=if($paused){@((F 'DebugContinueButton'),(F 'FlowMoreToggleButton'))}else{@((F 'FlowMoreToggleButton'))}
    foreach($element in $elements){
        $rect=$element.TransformToAncestor($bar).TransformBounds([Windows.Rect]::new(0,0,$element.ActualWidth,$element.ActualHeight))
        if(-not $element.IsVisible -or $rect.Left -lt $previousRight-1 -or $rect.Right -gt $bar.ActualWidth+1){throw 'Contextual run toolbar clips or overlaps an actual action.'}
        $previousRight=$rect.Right
    }
    if((F 'DebugContinueButton').IsVisible -ne $paused -or (F 'FlowSecondaryHeaderUI06').IsVisible -or (F 'FlowDebugStateText').IsVisible){throw 'Lean heading lost paused continuation or exposed secondary controls.'}
}
$flow=F 'flowTools';$source=$flow[0];$threshold=$flow[1];$objects=$flow[2]
$ids=@($flow|ForEach-Object ToolId)-join ','
(F 'FlowToolList').SelectedItem=$threshold;M 'RefreshUiState'|Out-Null
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
    $window.Width=$size[0];$window.Height=$size[1];Pump 60;CheckBarRun05 $false
    if((F 'FlowRunFullButtonS89').IsVisible -or -not (F 'RunOnceButton').IsVisible -or -not (F 'StopRunButton').IsVisible){throw 'Duplicate full run remains permanent or main run/stop disappeared.'}
    Shot ('run-actions-'+$size[0]+'x'+$size[1])
}
$objects.IsBreakpoint=$true;M 'RefreshUiState'|Out-Null
if((F 'FlowRunShortcutHintS89').Text -notlike '*F5*断点 1*' -or -not (F 'FlowMoreContentUI06').IsAncestorOf((F 'FlowRunShortcutHintS89'))){throw 'Advanced F5 scope detail was lost.'}
$objects.IsBreakpoint=$false
(F 'FlowMoreToggleButton').IsChecked=$true;Pump 40
if(-not (F 'FlowRunFullButtonS89').IsVisible -or -not (F 'FlowMorePanelUI10').IsAncestorOf((F 'FlowRunFullButtonS89'))){throw 'Original full command was not retained in More.'}
(F 'FlowMoreToggleButton').IsChecked=$false
$path=Join-Path $temp 'run05.png';[HalconDotNet.HObject]$zero=$null;[HalconDotNet.HObject]$im=$null
try{[HalconDotNet.HOperatorSet]::GenImageConst([ref]$zero,'byte',64,48);[HalconDotNet.HOperatorSet]::ScaleImage($zero,[ref]$im,1,200);[HalconDotNet.HOperatorSet]::WriteImage($im,'png',0,$path)}finally{if($im){$im.Dispose()};if($zero){$zero.Dispose()}}
$source.Parameters.ImageSourceModeS114='File';$source.Parameters.LocalImagePath=$path
$threshold.Parameters.ImageThresholdMinGray=128;$threshold.Parameters.ImageThresholdMaxGray=255
(F 'AutoSendMatchResultCheckBox').IsChecked=$false;(F 'StopOnNgCheckBox').IsChecked=$false
M 'RunStandaloneToolCoreUI02' @($source,'UI05 source',$false)|Out-Null
$sourceVersion=$source.RuntimeOutputVersion
ClickRun05 'FlowRunSelectedButtonS89';IdleRun05
[double]$area=0
if(-not $threshold.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 3072 -or $source.RuntimeOutputVersion -ne $sourceVersion -or $null -ne $objects.LastOutputUtc){throw 'Selected command ran upstream/downstream or did not use applied threshold.'}
M 'OpenCentralModuleConfigurationS77' @($threshold,$false)|Out-Null
(F 'DockImageThresholdMinTextBox').Text='201';M 'RefreshUiState'|Out-Null
if((F 'FlowRunShortcutHintS89').Text -notlike '*试运行草稿*'){throw 'Open editor does not identify draft shortcut scope.'}
M 'TryHandleRunShortcutS89' @([Windows.Input.Key]::F5,[Windows.Input.ModifierKeys]::None)|Out-Null;Pump 80
if(-not $threshold.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 0 -or $threshold.Parameters.ImageThresholdMinGray -ne 128 -or -not (F 'dockDraftDirty') -or $source.RuntimeOutputVersion -ne $sourceVersion -or $null -ne $objects.LastOutputUtc){throw 'F5 draft shortcut applied changes or ran a different range.'}
# Presentation-only root pause with an open draft must not promise that F5 can trial it.
try{
    $main.GetField('isFlowExecutionActive',$all).SetValue($window,$true);$main.GetField('isPauseRequested',$all).SetValue($window,$true)
    M 'RefreshUiState'|Out-Null
    if((F 'FlowRunShortcutHintS89').Text -notlike '*F5不试草稿*'){throw 'Paused root flow wrongly advertises a draft F5 run.'}
}finally{$main.GetField('isFlowExecutionActive',$all).SetValue($window,$false);$main.GetField('isPauseRequested',$all).SetValue($window,$false)}
M 'DockRevertButton_Click' @($null,$null)|Out-Null;M 'CloseCentralModuleConfigurationS77'|Out-Null
M 'TryHandleRunShortcutS89' @([Windows.Input.Key]::F5,[Windows.Input.ModifierKeys]::None)|Out-Null;IdleRun05
if($source.RuntimeOutputVersion -le $sourceVersion -or $null -eq $objects.LastOutputUtc){throw 'Closed-editor F5 failed to run the original full current flow.'}
$sourceVersion=$source.RuntimeOutputVersion
# Explicit apply changes the formal definition, not the last trial's range or execution state.
M 'OpenCentralModuleConfigurationS77' @($threshold,$false)|Out-Null;(F 'DockImageThresholdMinTextBox').Text='201'
M 'ApplyDockConfigurationDraft' @($true)|Out-Null
if($threshold.Parameters.ImageThresholdMinGray -ne 201 -or (F 'dockDraftDirty') -or $source.RuntimeOutputVersion -ne $sourceVersion){throw 'Explicit apply did not commit only the definition.'}
M 'CloseCentralModuleConfigurationS77'|Out-Null
ClickRun05 'FlowRunSelectedButtonS89';IdleRun05
if(-not $threshold.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 0 -or $source.RuntimeOutputVersion -ne $sourceVersion -or $null -ne $objects.LastOutputUtc){throw 'Selected run after apply used old definition or ran upstream/downstream.'}
M 'OpenCentralModuleConfigurationS77' @($threshold,$false)|Out-Null;(F 'DockImageThresholdMinTextBox').Text='128'
M 'ApplyDockConfigurationDraft' @($true)|Out-Null;M 'CloseCentralModuleConfigurationS77'|Out-Null
ClickRun05 'RunOnceButton';IdleRun05
if(-not $threshold.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 3072 -or $source.RuntimeOutputVersion -le $sourceVersion -or $null -eq $objects.LastOutputUtc){throw 'Original top full-run action lost current-flow scope after apply.'}
$sourceVersion=$source.RuntimeOutputVersion
# A genuine original single step pauses before the next module; the moved button continues that same run.
(F 'FlowToolList').SelectedItem=$threshold;M 'DebugStepButton_Click' @($null,$null)|Out-Null
$deadline=[DateTime]::UtcNow.AddSeconds(15)
while(-not (F 'isPauseRequested') -and [DateTime]::UtcNow -lt $deadline){Pump 40}
if(-not (F 'isFlowExecutionActive') -or -not (F 'isPauseRequested')){throw 'Original single step did not establish a real paused execution.'}
$thresholdVersion=$threshold.RuntimeOutputVersion
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){$window.Width=$size[0];$window.Height=$size[1];M 'RefreshUiState'|Out-Null;Pump 40;CheckBarRun05 $true;Shot ('paused-actions-'+$size[0]+'x'+$size[1])}
ClickRun05 'DebugContinueButton';IdleRun05
if($source.RuntimeOutputVersion -ne $sourceVersion -or $threshold.RuntimeOutputVersion -ne $thresholdVersion -or $null -eq $objects.LastOutputUtc -or (F 'DebugContinueButton').IsVisible){throw 'Continue restarted upstream, retained pause or lost downstream output.'}
# Stop the same actual paused execution with the original Shift+F5 route.
M 'DebugStepButton_Click' @($null,$null)|Out-Null
$deadline=[DateTime]::UtcNow.AddSeconds(15);while(-not (F 'isPauseRequested') -and [DateTime]::UtcNow -lt $deadline){Pump 40}
if(-not (F 'isPauseRequested')){throw 'Second real pause unavailable for stop test.'}
M 'TryHandleRunShortcutS89' @([Windows.Input.Key]::F5,[Windows.Input.ModifierKeys]::Shift)|Out-Null;IdleRun05
if((F 'FlowRunResultText').Text -ne 'STOP'){throw 'Shift+F5 failed to stop the same paused execution.'}
$saved=Join-Path $temp 'run05.json';$service.SaveRecipe($saved,(M 'CaptureRecipe'));$loaded=$service.LoadRecipe($saved)
$savedFlow=@($loaded.Flows|Where-Object FlowId -eq $loaded.ActiveFlowId)[0]
if((@($savedFlow.Tools|ForEach-Object ToolId)-join ',') -ne $ids){throw 'Original schema3 save did not preserve current module identities.'}
if($window.IsActive -or $window.ShowActivated -or $window.Left -gt -10000){throw 'Run UI test activated foreground.'}
Write-Output 'UI05_RUN_COMMANDS=PASS; ThreeSizesNormalAndPaused; NoPermanentFullDuplicate; BreakpointF5Visible; OriginalSelectedArea3072; OriginalDraftF5Area0NotApplied; CancelAndExplicitApply; ClosedEditorF5AndTopFull; RealSingleStepAndMovedContinue; ShiftF5Stop; SaveIdentity; NoMouse'
