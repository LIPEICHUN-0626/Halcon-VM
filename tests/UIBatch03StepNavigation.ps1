# Current step owner navigation, real failure -> explicit source repair -> HALCON output.
$flow=F 'flowTools';$source=$flow[0];$threshold=$flow[1]
$source.Parameters.LocalImagePath=Join-Path $temp 'missing-ui07.png'
$sourceFailed=$false;$inputFailed=$false
try{M 'RunStandaloneToolCoreUI02' @($source,'UI07 missing image',$false)|Out-Null}catch{$sourceFailed=$true}
try{M 'RunStandaloneToolCoreUI02' @($threshold,'UI07 missing input',$false)|Out-Null}catch{$inputFailed=$true}
if(-not $sourceFailed -or -not $inputFailed){throw 'Missing source/input unexpectedly succeeded.'}
if([string]::IsNullOrWhiteSpace($source.ErrorMessage) -or [string]::IsNullOrWhiteSpace($threshold.ErrorMessage)){throw 'Real missing source/input did not expose an error.'}
M 'OpenFlowStepActivityS71' @($source)|Out-Null
$viewer=F 'flowStepActivityWindowS71'
(F 'FlowStepFollowS71').IsChecked=$false
if($viewer.IsActive -or $viewer.ShowActivated -or (F 'FlowStepLogTextBox').Text -notlike '*当前异常*'){throw 'Step view activation or genuine error presentation failed.'}
M 'OpenCentralModuleConfigurationS77' @($threshold,$false)|Out-Null
(F 'DockImageThresholdMinTextBox').Text='160'
if(-not (F 'dockDraftDirty')){throw 'Expected a real unapplied draft.'}
$definition=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
$imageKey=M 'GetImageContextSelectionKeyS112';$zoom=(F 'FlowGraphScaleTransform').ScaleX
$version=$source.RuntimeOutputVersion
M 'FlowStepNavigateUI07_Click' @((F 'FlowStepLocateUI07'),$null)|Out-Null;Pump 70
if((F 'FlowToolList').SelectedItem -ne $source -or (F 'dockEditingTool') -ne $threshold -or -not (F 'dockDraftDirty') -or (F 'DockImageThresholdMinTextBox').Text -ne '160' -or $definition -ne $service.GetRecipeFingerprint((M 'CaptureRecipe')) -or (M 'GetImageContextSelectionKeyS112') -ne $imageKey -or (F 'FlowGraphScaleTransform').ScaleX -ne $zoom -or $source.RuntimeOutputVersion -ne $version){throw 'Read-only locate changed draft / image / definition / execution.'}
Shot 'step-locate-keeps-draft' $viewer
M 'DockRevertButton_Click' @($null,$null)|Out-Null
M 'FlowStepNavigateUI07_Click' @((F 'FlowStepParametersUI07'),$null)|Out-Null
if((F 'dockEditingTool') -ne $source -or -not (F 'centralModuleConfigurationOpenS77') -or $source.RuntimeOutputVersion -ne $version){throw 'Explicit original parameter navigation failed or executed.'}
# A changed definition must not be silently accepted by following runtime text.
$source.InstanceName='已改名图像源'
M 'RefreshFlowStepActivityS71'|Out-Null
M 'FlowStepNavigateUI07_Click' @((F 'FlowStepLocateUI07'),$null)|Out-Null
if((F 'FlowStepNavigationStatusUI07').Text -notlike '*定义已改变*'){throw 'Changed definition retained old navigation authority.'}
M 'RefreshFlowStepActivityButton_Click' @($null,$null)|Out-Null
M 'FlowStepNavigateUI07_Click' @((F 'FlowStepLocateUI07'),$null)|Out-Null
if((F 'FlowStepNavigationStatusUI07').Text -notlike '*已选中*'){throw 'Explicit refreshed definition did not navigate.'}
# Busy/history states reject navigation without mutating selection.
foreach($field in @('isFlowExecutionActive','historicalImageMode')){
    $main.GetField($field,$all).SetValue($window,$true)
    try{M 'FlowStepNavigateUI07_Click' @((F 'FlowStepParametersUI07'),$null)|Out-Null
        if((F 'FlowStepNavigationStatusUI07').Text -notlike '*暂不切换*'){throw ('Navigation ignored '+$field)}}
    finally{$main.GetField($field,$all).SetValue($window,$false)}
}
# Open the failed consumer, navigate its exact input source using the original S96 action.
M 'OpenFlowStepActivityS71' @($threshold)|Out-Null
(F 'FlowStepFollowS71').IsChecked=$false
M 'FlowStepNavigateUI07_Click' @((F 'FlowStepParametersUI07'),$null)|Out-Null
M 'SelectCentralConfigurationTaskS77' @((F 'DockIoTaskTab'),'Io')|Out-Null
$row=@((F 'dockInputPortRows')|Where-Object PortName -eq 'Image')[0]
$button=New-Object Windows.Controls.Button;$button.DataContext=$row
M 'OpenInputProducerS96_Click' @($button,$null)|Out-Null
if((F 'dockEditingTool') -ne $source -or (F 'inputRepairVisitsS96').Count -ne 1){throw 'Current input source did not open / create exact return visit.'}
$visits=(F 'inputRepairVisitsS96').Count
M 'OpenInputProducerS96_Click' @($button,$null)|Out-Null
if((F 'inputRepairVisitsS96').Count -ne $visits -or (F 'dockEditingTool') -ne $source){throw 'Old consumer row was accepted in another editor.'}
$path=Join-Path $temp 'repaired-ui07.png'
[HalconDotNet.HObject]$zero=$null;[HalconDotNet.HObject]$image=$null
try{[HalconDotNet.HOperatorSet]::GenImageConst([ref]$zero,'byte',32,24);[HalconDotNet.HOperatorSet]::ScaleImage($zero,[ref]$image,1,200);[HalconDotNet.HOperatorSet]::WriteImage($image,'png',0,$path)}finally{if($image){$image.Dispose()};if($zero){$zero.Dispose()}}
(F 'DockLocalImagePathTextBox').Text=$path
if(-not (M 'ApplyDockConfigurationDraft' @($true))){throw 'Explicit source path apply failed.'}
if($null -ne $source.LastOutputUtc){throw 'Source path apply auto-ran the source.'}
M 'ReturnInputConsumerS96_Click' @($null,$null)|Out-Null
if((F 'dockEditingTool') -ne $threshold -or (F 'DockConfigurationTabs').SelectedItem -ne (F 'DockIoTaskTab')){throw 'Return did not restore consumer input task.'}
M 'RunStandaloneToolCoreUI02' @($source,'UI07 explicit repaired source',$false)|Out-Null
M 'RunStandaloneToolCoreUI02' @($threshold,'UI07 explicit repaired threshold',$false)|Out-Null
[double]$area=0
if(-not $threshold.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 768 -or $threshold.ResultCode -ne 'OK' -or -not [string]::IsNullOrWhiteSpace($threshold.ErrorMessage)){throw 'Explicit real HALCON repair did not clear failure / produce area 768.'}
M 'RefreshFlowStepActivityButton_Click' @($null,$null)|Out-Null
if((F 'FlowStepLogTextBox').Text -like '*[[]当前异常[]]*'){throw 'Repaired current error was retained.'}
Shot 'step-repaired' $viewer
# Definition/session/instance mismatch never resolves by the same ID or name.
$session=F 'projectSessionIdS104'
$main.GetField('projectSessionIdS104',$all).SetValue($window,'different-ui07-session')
try{M 'FlowStepNavigateUI07_Click' @((F 'FlowStepLocateUI07'),$null)|Out-Null
    if((F 'FlowStepNavigationStatusUI07').Text -notlike '*查看窗口已改变*'){throw 'Old session navigated.'}}
finally{$main.GetField('projectSessionIdS104',$all).SetValue($window,$session)}
$saved=Join-Path $temp 'navigation-ui07.json';$service.SaveRecipe($saved,(M 'CaptureRecipe'))
$old=$threshold
M 'CloseCentralModuleConfigurationS77'|Out-Null
M 'ApplyRecipe' @($service.LoadRecipe($saved))|Out-Null
M 'RefreshFlowStepActivityS71'|Out-Null
M 'FlowStepNavigateUI07_Click' @((F 'FlowStepParametersUI07'),$null)|Out-Null
if((F 'FlowStepLocateUI07').IsEnabled -or (F 'centralModuleConfigurationOpenS77') -or (F 'flowTools')[1] -eq $old -or (F 'flowTools')[1].ToolId -ne $old.ToolId){throw 'Recipe reload reused stale navigation / failed identity roundtrip.'}
M 'CloseFlowStepActivityButton_Click' @($null,$null)|Out-Null
Write-Output 'UI03_BLOCK07=PASS; RealMissingInput=True; ReadOnlyKeepsDraftImageZoom=True; ExplicitParameters=True; DefinitionRefresh=True; BusyHistorySessionRejected=True; StaleInputRowRejected=True; SourceRepairReturn=True; HALCONArea768=True; SavedSameIdNewInstanceRejected=True; NoMouse=True; NoDevices=True'
