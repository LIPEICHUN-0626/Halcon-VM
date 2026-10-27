# Explicit bounded runs / conditional branches / debug rejection, isolated WPF.
function WaitScope06($task) {
    $deadline=[DateTime]::UtcNow.AddSeconds(20)
    while(-not $task.IsCompleted -and [DateTime]::UtcNow -lt $deadline){Pump 40}
    if(-not $task.IsCompleted){M 'StopRunButton_Click' @($null,$null)|Out-Null;throw 'Range task timeout.'}
    return $task.GetAwaiter().GetResult()
}
function RunScope06([string]$mode,$selected) {
    (F 'FlowToolList').SelectedItem=$selected
    $request=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmFlowRunRequestMode',$true),$mode)
    $origin=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmRunOriginS135',$true),'Debug')
    $outcome=WaitScope06 (M 'RunInspectionRangeAsync' @($request,'UI06 explicit range',$false,$true,$origin))
    if($outcome.ExecutionError){throw ('Range failed: '+$outcome.StopReason)}
    return $outcome
}
function IdleScope06 {
    $deadline=[DateTime]::UtcNow.AddSeconds(20)
    while((F 'isFlowExecutionActive') -and [DateTime]::UtcNow -lt $deadline){Pump 40}
    if(F 'isFlowExecutionActive'){throw 'Run handler did not finish.'}
}
$flow=F 'flowTools';$source=$flow[0];$threshold=$flow[1];$objects=$flow[2];$judge=$flow[3]
$judge.NumericLowerLimit=1;$judge.NumericUpperLimit=1;$judge.NumericOperator='BetweenInclusive'
$filter=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageFilter'),'通过才滤波',$true,$null);$flow.Add($filter)
$filter.SetInputBinding('Image',$source.ToolId,'Image');$filter.SetInputBinding('$RunCondition',$judge.ToolId,'Passed')
$tail=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageThreshold'),'条件下游',$true,$null);$flow.Add($tail);$tail.SetInputBinding('Image',$filter.ToolId,'Image')
(F 'StopOnNgCheckBox').IsChecked=$false
(F 'AutoSendMatchResultCheckBox').IsChecked=$false
$path=Join-Path $temp 'scope06.png'
[HalconDotNet.HObject]$zero=$null;[HalconDotNet.HObject]$im=$null
try{[HalconDotNet.HOperatorSet]::GenImageConst([ref]$zero,'byte',64,48);[HalconDotNet.HOperatorSet]::ScaleImage($zero,[ref]$im,1,200);[HalconDotNet.HOperatorSet]::WriteImage($im,'png',0,$path)}finally{if($im){$im.Dispose()};if($zero){$zero.Dispose()}}
$source.Parameters.LocalImagePath=$path
RunScope06 'ToHere' $threshold|Out-Null
[double]$area=0
if(-not $threshold.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 3072 -or $null -ne $objects.LastOutputUtc){throw 'ToHere did not stop exactly at selected tool.'}
$sourceVersion=$source.RuntimeOutputVersion;$thresholdVersion=$threshold.RuntimeOutputVersion
RunScope06 'FromHere' $objects|Out-Null
if($source.RuntimeOutputVersion -ne $sourceVersion -or $threshold.RuntimeOutputVersion -ne $thresholdVersion -or $filter.ResultCode -ne 'OK' -or $tail.ResultCode -ne 'OK'){throw 'FromHere reran upstream or failed true branch.'}
$judge.NumericLowerLimit=2;$judge.NumericUpperLimit=2
RunScope06 'FromHere' $judge|Out-Null
[object]$old=$null
if($judge.ResultCode -ne 'NG' -or $filter.ResultCode -ne 'SKIP' -or $tail.ResultCode -ne 'SKIP' -or ($filter.TryGetOutputValue('Image',[ref]$old) -and $null -ne $old) -or ($tail.TryGetOutputValue('Area',[ref]$old) -and $null -ne $old)){throw 'False branch used old output or SKIP did not propagate.'}
$judge.NumericLowerLimit=1;$judge.NumericUpperLimit=1
RunScope06 'Current' $judge|Out-Null
if($source.RuntimeOutputVersion -ne $sourceVersion -or $threshold.RuntimeOutputVersion -ne $thresholdVersion -or $null -ne $filter.LastOutputUtc){throw 'Current range ran unrelated steps / retained downstream.'}
RunScope06 'FromHere' $filter|Out-Null
(F 'FlowToolList').SelectedItem=$threshold;M 'RefreshUiState'|Out-Null
if((F 'FlowRunToHereButtonUI06').Content -notlike '*#02*' -or (F 'FlowRunFromHereButtonUI06').ToolTip -notlike '*不补跑上游*'){throw 'Visible range target / boundary missing.'}
(F 'FlowMoreToggleButton').IsChecked=$true;Shot 'range-actions'
# Missing selection and busy commands must not default to Full or stop a live mode.
(F 'FlowToolList').SelectedItem=$null;M 'DebugStepButton_Click' @($null,$null)|Out-Null;Pump 40
if((F 'isFlowExecutionActive') -or (F 'debugSingleStepRequested') -or $source.RuntimeOutputVersion -ne $sourceVersion){throw 'No-selection step started full flow.'}
(F 'FlowToolList').SelectedItem=$threshold
$main.GetField('isContinuousRunning',$all).SetValue($window,$true)
M 'DebugStepButton_Click' @($null,$null)|Out-Null
if(-not (F 'isContinuousRunning') -or (F 'debugSingleStepRequested')){throw 'Step implicitly stopped continuous mode or armed stale step.'}
$main.GetField('isContinuousRunning',$all).SetValue($window,$false)
# A known missing file blocks both commands before execution, and must leave no armed debug intent.
$source.Parameters.LocalImagePath=Join-Path $temp 'missing-scope06.png'
(F 'FlowToolList').SelectedItem=$source;M 'DebugStepButton_Click' @($null,$null)|Out-Null;Pump 50
if((F 'debugSingleStepRequested') -or (F 'debugPauseBeforeNextTool') -or (F 'isFlowExecutionActive')){throw 'Blocked step left armed debug state.'}
M 'RefreshFlowDebugMonitor'|Out-Null;(F 'DebugMonitorToolComboBox').SelectedItem=$threshold
M 'DebugRunToSelectedButton_Click' @($null,$null)|Out-Null;Pump 50
if(-not [string]::IsNullOrEmpty((F 'debugRunToToolId')) -or (F 'isFlowExecutionActive')){throw 'Blocked run-to left a target for the next run.'}
$source.Parameters.LocalImagePath=$path
RunScope06 'Full' $source|Out-Null
if((F 'isPauseRequested') -or $tail.ResultCode -ne 'OK'){throw 'Next unrelated full run inherited blocked debug intent.'}
# Real single step pauses before the next enabled tool; explicit stop ends that same task.
(F 'FlowToolList').SelectedItem=$threshold
$sourceVersion=$source.RuntimeOutputVersion;$thresholdVersion=$threshold.RuntimeOutputVersion
M 'DebugStepButton_Click' @($null,$null)|Out-Null
$deadline=[DateTime]::UtcNow.AddSeconds(12)
while(-not (F 'isPauseRequested') -and [DateTime]::UtcNow -lt $deadline){Pump 40}
if(-not (F 'isFlowExecutionActive') -or -not (F 'isPauseRequested') -or (F 'debugLastExecutedToolId') -ne $threshold.ToolId -or $threshold.RuntimeOutputVersion -le $thresholdVersion -or $source.RuntimeOutputVersion -ne $sourceVersion){throw 'Single step ran wrong range or did not pause.'}
M 'StopRunButton_Click' @($null,$null)|Out-Null;IdleScope06
if((F 'FlowRunResultText').Text -ne 'STOP'){throw 'Explicit stop did not end paused run.'}
$saved=Join-Path $temp 'scope06.json';$service.SaveRecipe($saved,(M 'CaptureRecipe'));M 'ApplyRecipe' @($service.LoadRecipe($saved))|Out-Null
$flow=F 'flowTools';$source=$flow[0];$tail=$flow[5]
(F 'FlowToolList').SelectedItem=$source
$full=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmFlowRunRequestMode',$true),'Full')
$debugOrigin=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmRunOriginS135',$true),'Debug')
if((F 'AutoSendMatchResultCheckBox').IsChecked){throw 'Isolated test must not enable automatic send.'}
# Exercise the real manual-start boundary after STOP; that boundary resets the explicit stop request.
WaitScope06 (M 'StartManualFlowRunAsync' @($full,'UI06 explicit restart after reload',$debugOrigin))|Out-Null
if($tail.ResultCode -ne 'OK'){throw 'Saved conditional chain failed after reload.'}
Write-Output 'UI03_BLOCK06_SCOPES=PASS; ToHere3072; FromHereNoUpstream; CurrentOnly; TrueFalseAndSkipClear; NoSelectionNoFull; BusyNoImplicitStop; BlockedDebugStateCleared; RealSingleStepPauseStop; SaveReload; NoMouse'
