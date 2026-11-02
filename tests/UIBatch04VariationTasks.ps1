# Real original variation project; isolated images and explicit local training, no desktop input.
function RunVariation04($tool){
    try{M 'RunStandaloneToolCoreUI02' @($tool,'UI04 explicit local module',$false)|Out-Null}
    catch{throw ('Stage '+$script:variationStage+' / '+$tool.InstanceName+': '+$_.Exception.Message)}
}
function NumberVariation04($tool,[string]$port){[double]$v=0;if(-not $tool.TryGetNumericOutput($port,[ref]$v)){throw ('Missing '+$port+': '+$tool.ErrorMessage)};return $v}
function WaitVariation04 {
    $deadline=[DateTime]::UtcNow.AddSeconds(25)
    while($null -ne (F 'variationTrainingCancellationS130') -and [DateTime]::UtcNow -lt $deadline){Pump 40}
    if($null -ne (F 'variationTrainingCancellationS130')){throw 'Training did not finish.'}
}
function ApplyVariation04 {if(-not (M 'ApplyDockConfigurationDraft' @($true))){throw ('Apply: '+(F 'DockValidationText').Text)}}
function FrameVariation04([int]$gray,[bool]$defect=$false,[int]$width=64){
    $path=Join-Path $temp ('variation-'+$gray+'-'+$defect+'-'+$width+'.png')
    [HalconDotNet.HObject]$z=$null;[HalconDotNet.HObject]$im=$null;[HalconDotNet.HObject]$reg=$null;[HalconDotNet.HObject]$painted=$null
    try{
        [HalconDotNet.HOperatorSet]::GenImageConst([ref]$z,'byte',$width,48)
        [HalconDotNet.HOperatorSet]::ScaleImage($z,[ref]$im,1,$gray)
        if($defect){[HalconDotNet.HOperatorSet]::GenRectangle1([ref]$reg,10,12,17,19);[HalconDotNet.HOperatorSet]::PaintRegion($reg,$im,[ref]$painted,200,'fill');[HalconDotNet.HOperatorSet]::WriteImage($painted,'png',0,$path)}
        else{[HalconDotNet.HOperatorSet]::WriteImage($im,'png',0,$path)}
    }finally{foreach($o in @($painted,$reg,$im,$z)){if($o){$o.Dispose()}}}
    $source.Parameters.LocalImagePath=$path;RunVariation04 $source
}
$main.GetField('isRecipeDirty',$all).SetValue($window,$false)
M 'LoadExampleProgramS78' @('variation-surface')|Out-Null;Pump 70
$flow=F 'flowTools';$source=$flow[0];$variation=$flow[1];$contour=$flow[2];$judge=$flow[3]
if($flow.Count -ne 4 -or @($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count){throw 'Original example auto-executed.'}
(F 'AutoSendMatchResultCheckBox').IsChecked=$false
M 'OpenCentralModuleConfigurationS77' @($variation,$false)|Out-Null;Pump 50
$editor=F 'moduleParameterWindowS89'
if($editor.IsActive -or $editor.ShowActivated -or $window.IsActive -or $editor.Left -gt -10000){throw 'Foreground activation.'}
if((F 'DockVariationCaptureS130').IsEnabled -or (F 'DockVariationTrainS130').IsEnabled -or (F 'DockVariationAddFilesS130').IsEnabled -or (F 'DockVariationManageUI04').IsExpanded){throw 'Missing input enabled actions or management starts expanded.'}
(F 'DockVariationSamplesTaskUI04').IsExpanded=$true
Shot 'variation-no-input' $editor
foreach($gray in @(99,100,101)){
    FrameVariation04 $gray
    M 'RefreshVariationTaskS130'|Out-Null
    if(-not (F 'DockVariationCaptureS130').IsEnabled){throw 'Real input capture unavailable.'}
    M 'DockVariationCaptureS130_Click' @($null,$null)|Out-Null
}
if((F 'variationSamplesS130').Count -ne 3 -or -not (F 'DockVariationTrainS130').IsEnabled -or $variation.Parameters.VariationInspectionS130.HasModelVersion()){throw 'Capture did not stay in draft.'}
(F 'DockVariationAlignedS130').IsChecked=$true
if(-not (F 'DockVariationAddFilesS130').IsEnabled){throw 'Explicit external confirmation not reflected.'}
(F 'DockVariationAlignedS130').IsChecked=$false
M 'DockVariationTrainS130_Click' @($null,$null)|Out-Null;WaitVariation04
if(-not (F 'variationDraftS130').HasModelVersion()){throw ('Real training failed: '+(F 'DockVariationTrainingStatusS130').Text)}
if((F 'DockVariationModelStateS130').Text -notlike '*待应用*' -or $variation.Parameters.VariationInspectionS130.HasModelVersion()){throw 'Learned model silently applied / wrong status.'}
FrameVariation04 100 $true
M 'DockTrialRunButton_Click' @($null,$null)|Out-Null
if((NumberVariation04 $variation 'Area') -ne 64 -or $variation.Parameters.VariationInspectionS130.HasModelVersion() -or $null -ne $judge.LastOutputUtc -or (F 'DockVariationModelStateS130').Text -notlike '*待应用*'){throw 'F5 draft model did not detect64 / applied / ran downstream / misrepresented model status.'}
Shot 'variation-trained-draft' $editor
(F 'DockVariationAbsoluteS130').Text='110'
M 'DockTrialRunButton_Click' @($null,$null)|Out-Null
if((NumberVariation04 $variation 'Area') -ne 0){throw 'Changed draft tolerance did not re-prepare actual model.'}
(F 'DockVariationAbsoluteS130').Text='15'
ApplyVariation04
if((F 'DockVariationModelStateS130').Text -notlike '*已应用模型*'){throw 'Applied model status missing.'}
$hash=$variation.Parameters.VariationInspectionS130.ModelSha256
$script:variationStage='applied-first-run'
Write-Output ('VARIATION_IDENTITY=Saved:'+ $variation.Parameters.VariationInspectionS130.DatasetContext+'; Current:'+(M 'ResolveTargetImageContextIdS86' @($variation))+'; SignatureMatch:'+($variation.Parameters.VariationInspectionS130.DatasetSourceSignature -eq (M 'VariationSourceSignatureS130' @($variation))))
RunVariation04 $variation;RunVariation04 $contour;RunVariation04 $judge
if((NumberVariation04 $variation 'Area') -ne 64 -or (NumberVariation04 $variation 'SampleCount') -ne 3 -or $judge.ResultCode -ne 'NG' -or $contour.ResultCode -ne 'OK'){throw 'Actual defect downstream chain failed.'}
FrameVariation04 100;RunVariation04 $variation;RunVariation04 $judge
if((NumberVariation04 $variation 'Area') -ne 0 -or $judge.ResultCode -ne 'OK'){throw 'Good image not zero / judge not OK.'}
# Actual learning cancellation retains the same formal and draft model.
$script:variationStage='cancel-learning'
M 'DockVariationTrainS130_Click' @($null,$null)|Out-Null
if($null -eq (F 'variationTrainingCancellationS130')){throw 'Cancellation test needs an active training task.'}
M 'DockVariationCancelS130_Click' @($null,$null)|Out-Null;WaitVariation04
if($variation.Parameters.VariationInspectionS130.ModelSha256 -ne $hash -or (F 'variationDraftS130').ModelSha256 -ne $hash -or (F 'DockVariationTrainingStatusS130').Text -notlike '*已取消*'){throw ('Cancellation changed model or left stale progress: '+(F 'DockVariationTrainingStatusS130').Text)}
# Clearing only removes draft samples; cancel restores formal resources.
M 'DockVariationClearS130_Click' @($null,$null)|Out-Null
if((F 'variationDraftS130').HasModelVersion() -or $variation.Parameters.VariationInspectionS130.ModelSha256 -ne $hash -or (F 'DockVariationTrainS130').IsEnabled){throw 'Clear changed formal model.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null
if((F 'variationSamplesS130').Count -ne 3 -or -not (F 'variationDraftS130').HasModelVersion()){throw 'Cancel did not restore model.'}
FrameVariation04 100 $true
RunVariation04 $variation;RunVariation04 $judge
# Wrong dimensions fail, clear old output and invalidate downstream.
$script:variationStage='wrong-dimensions'
FrameVariation04 100 $false 65
$failed=$false;try{RunVariation04 $variation}catch{$failed=$true}
[object]$old=$null
if(-not $failed -or ($variation.TryGetOutputValue('Area',[ref]$old) -and $null -ne $old) -or $null -ne $judge.LastOutputUtc){throw 'Wrong dimensions retained old area/downstream.'}
FrameVariation04 100 $true;RunVariation04 $variation;RunVariation04 $judge
M 'CloseCentralModuleConfigurationS77'|Out-Null
$saved=Join-Path $temp 'variation04.json';$service.SaveRecipe($saved,(M 'CaptureRecipe'));M 'ApplyRecipe' @($service.LoadRecipe($saved))|Out-Null
$flow=F 'flowTools';$source=$flow[0];$variation=$flow[1];$judge=$flow[3]
$script:variationStage='saved-reload'
Write-Output ('VARIATION_RELOAD_IDENTITY=Saved:'+ $variation.Parameters.VariationInspectionS130.DatasetContext+'; Current:'+(M 'ResolveTargetImageContextIdS86' @($variation))+'; SignatureMatch:'+($variation.Parameters.VariationInspectionS130.DatasetSourceSignature -eq (M 'VariationSourceSignatureS130' @($variation))))
RunVariation04 $source;RunVariation04 $variation;RunVariation04 $judge
if((NumberVariation04 $variation 'Area') -ne 64 -or $judge.ResultCode -ne 'NG' -or $variation.Parameters.VariationInspectionS130.ModelSha256 -ne $hash){throw 'Saved managed model did not reload / run.'}
# A self-generated old compact .vam must reject re-preparation with useful guidance.
$p=$variation.Parameters.VariationInspectionS130;$originalPath=$p.ModelPath;$originalHash=$p.ModelSha256
$legacy=Join-Path $temp 'legacy-cleared.vam';[HalconDotNet.HTuple]$model=$null
try{[HalconDotNet.HOperatorSet]::ReadVariationModel($originalPath,[ref]$model);[HalconDotNet.HOperatorSet]::ClearTrainDataVariationModel($model);[HalconDotNet.HOperatorSet]::WriteVariationModel($model,$legacy)}
finally{if($model){[HalconDotNet.HOperatorSet]::ClearVariationModel($model);$model.Dispose()}}
$p.ModelPath=$legacy;$p.ModelSha256=[HalconWinFormsDemo.Models.VmVariationModelS130]::HashFile($legacy)
$rejected=$false;try{RunVariation04 $variation}catch{$rejected=$_.Exception.Message -like '*重新学习*'}
[object]$old=$null
if(-not $rejected -or ($variation.TryGetOutputValue('Area',[ref]$old) -and $null -ne $old)){throw 'Old compact model silently ran or lacked recovery guidance.'}
$p.ModelPath=$originalPath;$p.ModelSha256=$originalHash;RunVariation04 $variation
if((NumberVariation04 $variation 'Area') -ne 64){throw 'Old model rejection damaged current resources.'}
$foreign=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageSource'),'另一图像源',$true,$null)
$foreign.Parameters=$source.Parameters.Clone();$flow.Insert(0,$foreign)
RunVariation04 $foreign;$variation.SetInputBinding('Image',$foreign.ToolId,'Image')
$rejected=$false;try{RunVariation04 $variation}catch{$rejected=$_.Exception.Message -like '*来源*'}
if(-not $rejected -or ($variation.TryGetOutputValue('Area',[ref]$old) -and $null -ne $old)){throw 'Foreign source consumed the old model.'}
$variation.SetInputBinding('Image',$source.ToolId,'Image');RunVariation04 $variation
if((NumberVariation04 $variation 'Area') -ne 64){throw 'Explicit original source restore failed.'}
Write-Output 'UI04_BLOCK01_VARIATION=PASS; OriginalFourTools; ActualThreeSamples; RealHalconTraining; F5DraftArea64NoApply; Tolerance110Area0; Good0; DownstreamNGandOK; CancelKeepsModel; ClearDraftRevert; WrongSizeClears; SaveReload; LegacyCompactRejected; ForeignSourceRejected; NoMouseNoDevices'
