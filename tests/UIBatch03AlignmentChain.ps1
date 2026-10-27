# Dot-sourced by UIBatch01WorkspaceRender.ps1 -Phase ui03block01.
# Real existing project, explicit UI actions, isolated local images; no desktop input.
function Run03($tool) { M 'RunStandaloneToolCoreUI02' @($tool,'UI03 explicit single module',$false) | Out-Null }
function Open03($tool) {
    M 'OpenCentralModuleConfigurationS77' @($tool,$false) | Out-Null
    M 'SelectCentralConfigurationTaskS77' @((F 'DockParametersTaskTab'),'Parameters') | Out-Null
    Pump 60
    $editor=F 'moduleParameterWindowS89'
    if($editor.ShowActivated -or $editor.IsActive -or $editor.Left -gt -10000){throw 'UI03 parameter window isolation failed.'}
}
function Close03 { M 'CloseCentralModuleConfigurationS77' | Out-Null }
function Apply03 { if(-not (M 'ApplyDockConfigurationDraft' @($true))){throw 'UI03 explicit apply failed.'} }
function Full03 {
    $full=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmFlowRunRequestMode',$true),'Full')
    $manual=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmRunOriginS135',$true),'ManualFull')
    $task=M 'RunInspectionRangeAsync' @($full,'UI03 explicit localization chain',$false,$true,$manual)
    $deadline=[DateTime]::UtcNow.AddSeconds(20)
    while(-not $task.IsCompleted -and [DateTime]::UtcNow -lt $deadline){Pump 50}
    if(-not $task.IsCompleted){throw 'UI03 full run timeout.'}
    $outcome=$task.GetAwaiter().GetResult()
    Write-Output ('UI03_CYCLE='+$outcome.ResultCode+'; Error='+$outcome.ExecutionError+'; Reason='+$outcome.StopReason)
    if($outcome.ExecutionError -or $outcome.ResultCode -ne 'OK'){throw 'UI03 full localization chain failed.'}
}
function Number03($tool,[string]$port) {
    [double]$n=0
    if(-not $tool.TryGetNumericOutput($port,[ref]$n)){throw ('Missing numeric output '+$port+': '+$tool.ErrorMessage)}
    return $n
}
function Output03($tool,[string]$port) {
    [object]$value=$null
    if(-not $tool.TryGetOutputValue($port,[ref]$value) -or $null -eq $value){throw ('Missing typed output '+$port+': '+$tool.ErrorMessage)}
    Write-Output -NoEnumerate $value
}
function AssertChain03 {
    if((Number03 $ncc 'SelectedScore') -lt 0.95){throw 'Actual NCC selected score is too low.'}
    M 'RequireRigidAppliedTemplateTargetS145' @($ncc) | Out-Null
    $aligned=Output03 $alignment 'Image'; $pose=Output03 $alignment 'AlignedPose'; $found=Output03 $line 'MainLine'
    if($aligned.IsDisposed -or $pose.CoordinateContextId -eq (Output03 $ncc 'SelectedPose').CoordinateContextId -or $found.CoordinateContextId -ne $pose.CoordinateContextId){throw 'Aligned image/line coordinate identity is not independent and consistent.'}
    $points=Output03 $line 'KeptPoints'
    if((Number03 $line 'Coverage') -lt 0.95 -or (Number03 $line 'ValidCount') -lt 18 -or -not (Output03 $judge 'Passed')){throw 'Actual aligned line/coverage judge failed.'}
    if([Math]::Abs($found.StartRow-149.5) -gt 0.3 -or [Math]::Abs($found.EndRow-149.5) -gt 0.3){throw 'Found geometry is not the real product edge (search baseline is 145).'}
    Write-Host ('UI03_ACTUAL=Score:'+(Number03 $ncc 'SelectedScore')+'; Coverage:'+(Number03 $line 'Coverage')+'; Points:'+(Number03 $line 'ValidCount')+'; EdgeRows:'+$found.StartRow+','+$found.EndRow)
    return $found
}
$window.Width=1366;$window.Height=768
$main.GetField('isRecipeDirty',$all).SetValue($window,$false)
M 'LoadExampleProgramS78' @('project-alignment-line') | Out-Null; Pump 100
$flow=F 'flowTools';$source=$flow[0];$ncc=$flow[1];$alignment=$flow[2];$line=$flow[3];$judge=$flow[4]
if($flow.Count -ne 5 -or @($flow | Where-Object {$null -ne $_.LastOutputUtc}).Count -ne 0 -or $ncc.Parameters.NccMatchingS131.HasModel -or $alignment.Parameters.PoseImageAlignmentS132.HasReference){throw 'Example loading fabricated model/reference or ran tools.'}
Open03 $alignment
if((F 'DockAlignmentCaptureS132').IsEnabled -or (F 'DockAlignmentCompareS132').IsEnabled){throw 'Missing outputs still offer capture/compare.'}
Shot 'alignment-prerequisites' (F 'moduleParameterWindowS89')
Close03

# Asymmetric marker and a separate straight product edge. They move together.
$path=Join-Path $temp 'ui03-current-frame.png'
[HalconDotNet.HObject]$base=$null;[HalconDotNet.HObject]$a=$null;[HalconDotNet.HObject]$b=$null
[HalconDotNet.HObject]$marker=$null;[HalconDotNet.HObject]$body=$null;[HalconDotNet.HObject]$regions=$null;[HalconDotNet.HObject]$image=$null
try {
    [HalconDotNet.HOperatorSet]::GenImageConst([ref]$base,'byte',320,240)
    [HalconDotNet.HOperatorSet]::GenRectangle1([ref]$a,40,45,75,54)
    [HalconDotNet.HOperatorSet]::GenRectangle1([ref]$b,66,45,75,85)
    [HalconDotNet.HOperatorSet]::Union2($a,$b,[ref]$marker)
    [HalconDotNet.HOperatorSet]::GenRectangle1([ref]$body,150,40,210,280)
    [HalconDotNet.HOperatorSet]::Union2($marker,$body,[ref]$regions)
    [HalconDotNet.HOperatorSet]::PaintRegion($regions,$base,[ref]$image,255,'fill')
    [HalconDotNet.HOperatorSet]::WriteImage($image,'png',0,$path)
} finally {foreach($obj in @($image,$regions,$body,$marker,$b,$a,$base)){if($obj){$obj.Dispose()}}}
Open03 $source
$lease=M 'CaptureEngineeringEditLeaseS137' @('UI03 explicit source choice')
M 'SelectSourceFileDraftUI03' @($path,$lease) | Out-Null
Apply03; Close03; Run03 $source
Open03 $ncc
M 'DockNccReadS131_Click' @($null,$null) | Out-Null
$main.GetField('nccTrainingBoxS131',$all).SetValue($window,[Windows.Rect]::new(35,30,60,60))
M 'DrawNccTrainingBoxS131' | Out-Null
M 'DockNccTrainS131_Click' @($null,$null) | Out-Null
for($i=0;$i -lt 300 -and (F 'nccTrainingCancellationS131') -ne $null;$i++){Pump 50}
if(-not (F 'nccDraftS131').HasModel -or $ncc.Parameters.NccMatchingS131.HasModel){throw 'NCC explicit training failed or bypassed draft.'}
M 'DockTrialRunButton_Click' @($null,$null) | Out-Null
$rejected=$false;try{M 'RequireRigidAppliedTemplateTargetS145' @($ncc) | Out-Null}catch{$rejected=$true}
if(-not $rejected){throw 'Draft NCC qualified as formal reference input.'}
Apply03; Close03; Run03 $ncc

Open03 $alignment
M 'RefreshAlignmentTaskS132' | Out-Null
if(-not (F 'DockAlignmentCaptureS132').IsEnabled){throw ('Formal pose cannot be captured: '+(F 'DockAlignmentStateS132').Text)}
M 'DockAlignmentCaptureS132_Click' @($null,$null) | Out-Null
if(-not (F 'alignmentDraftS132').HasReference -or $alignment.Parameters.PoseImageAlignmentS132.HasReference -or (F 'DockAlignmentReferenceS132').Text -notlike '*待应用*'){throw 'Reference was not a visible draft-only change.'}
M 'DockTrialRunButton_Click' @($null,$null) | Out-Null
if($alignment.Parameters.PoseImageAlignmentS132.HasReference -or $alignment.ResultCode -ne 'OK' -or (F 'DockAlignmentReferenceS132').Text -notlike '*待应用*'){throw ('Reference trial failed, applied or mislabeled: '+$alignment.ErrorMessage)}
Shot 'alignment-reference-draft' (F 'moduleParameterWindowS89')
Apply03
if($null -ne $alignment.LastOutputUtc){throw 'Apply retained old trial output.'}
Run03 $alignment
M 'RefreshAlignmentTaskS132' | Out-Null
if(-not (F 'DockAlignmentCompareS132').IsEnabled -or (F 'DockAlignmentReferenceS132').Text -notlike '*已应用*'){throw 'Applied alignment state/compare unavailable.'}
M 'DockAlignmentCompareS132_Click' @($null,$null) | Out-Null
if(-not (F 'dualImageMode')){throw 'Compare did not open existing dual view.'}
$savedVersion=$alignment.Parameters.PoseImageAlignmentS132.ReferenceVersion
(F 'DockAlignmentSizeModeS132').SelectedIndex=1
if((M 'ReadAlignmentChoicesUI03').OutputSizeMode -ne 'Input' -or (F 'DockAlignmentCompareS132').IsEnabled){throw 'Visible output range and preview identity diverged.'}
M 'DockRevertButton_Click' @($null,$null) | Out-Null
if((M 'ReadAlignmentChoicesUI03').OutputSizeMode -ne 'Reference' -or $alignment.Parameters.PoseImageAlignmentS132.ReferenceVersion -ne $savedVersion){throw 'Revert lost original reference.'}
(F 'DockAlignmentInterpolationS132').SelectedIndex=1
if((M 'ReadAlignmentChoicesUI03').Interpolation -ne 'nearest_neighbor' -or (F 'DockAlignmentCompareS132').IsEnabled){throw 'Interpolation choice retained the previous preview.'}
M 'DockRevertButton_Click' @($null,$null) | Out-Null
$inputRow=@((F 'dockInputPortRows') | Where-Object PortName -eq 'Pose')[0]
$inputRow.SelectedSourceKey=@($inputRow.SourceOptions | Where-Object IsDefault)[0].Key
M 'DockDraftValue_Changed' @($null,$null) | Out-Null
if((F 'DockAlignmentCaptureS132').IsEnabled -or (F 'DockAlignmentCompareS132').IsEnabled -or $alignment.GetInputBinding('Pose').SourceToolId -ne $ncc.ToolId){throw 'Input draft enabled capture/compare or committed subscription.'}
M 'DockRevertButton_Click' @($null,$null) | Out-Null
if(-not (F 'DockAlignmentCaptureS132').IsEnabled){throw ('Cancelling input draft did not restore capture state: '+(F 'DockAlignmentStateS132').Text+'; BindingsMatch='+(M 'AlignmentBindingsMatchDraftS132' @($alignment)))}
Shot 'alignment-applied' (F 'moduleParameterWindowS89')
Close03

Open03 $line
foreach($entry in @{DockEdgeLineStartRowS143='145';DockEdgeLineStartColumnS143='70';DockEdgeLineEndRowS143='145';DockEdgeLineEndColumnS143='250';DockEdgeLineCountS143='20';DockEdgeLineHalfWidthS143='12'}.GetEnumerator()){(F $entry.Key).Text=$entry.Value}
M 'DockTrialRunButton_Click' @($null,$null) | Out-Null
if($line.Parameters.ImageEdgeLineS143.StartRow -eq 145 -or $line.ResultCode -ne 'OK'){throw ('Real edge draft trial failed/applied: '+$line.ErrorMessage)}
Apply03; Run03 $line
M 'SelectCentralConfigurationTaskS77' @((F 'DockParametersTaskTab'),'Parameters') | Out-Null
Shot 'line-common' (F 'moduleParameterWindowS89')
Close03; Run03 $judge
$baseline=AssertChain03
Full03

# An explicitly supplied next frame moves the real target by (+12,+18).
[HalconDotNet.HObject]$original=$null;[HalconDotNet.HObject]$shifted=$null;[HalconDotNet.HTuple]$identity=$null;[HalconDotNet.HTuple]$matrix=$null
try{
    [HalconDotNet.HOperatorSet]::ReadImage([ref]$original,$path)
    [HalconDotNet.HOperatorSet]::HomMat2dIdentity([ref]$identity)
    [HalconDotNet.HOperatorSet]::HomMat2dTranslate($identity,12,18,[ref]$matrix)
    [HalconDotNet.HOperatorSet]::AffineTransImage($original,[ref]$shifted,$matrix,'constant','false')
    [HalconDotNet.HOperatorSet]::WriteImage($shifted,'png',0,$path)
}finally{foreach($obj in @($matrix,$identity,$shifted,$original)){if($obj){$obj.Dispose()}}}
Full03
$moved=AssertChain03
if([Math]::Abs((Number03 $alignment 'TranslationRow')+12) -gt 0.1 -or [Math]::Abs((Number03 $alignment 'TranslationColumn')+18) -gt 0.1 -or $alignment.Parameters.PoseImageAlignmentS132.ReferenceVersion -ne $savedVersion){throw 'Fixed reference drifted or actual translation is wrong.'}
Write-Output ('UI03_TRANSLATION=Row:'+(Number03 $alignment 'TranslationRow')+'; Column:'+(Number03 $alignment 'TranslationColumn'))
$file=Join-Path $temp 'ui03-alignment-project.json';$service.SaveRecipe($file,(M 'CaptureRecipe'))
$loaded=$service.LoadRecipe($file); M 'ApplyRecipe' @($loaded) | Out-Null
$flow=F 'flowTools';$source=$flow[0];$ncc=$flow[1];$alignment=$flow[2];$line=$flow[3];$judge=$flow[4]
if(@($flow | Where-Object {$null -ne $_.LastOutputUtc}).Count -ne 0 -or $alignment.Parameters.PoseImageAlignmentS132.ReferenceVersion -ne $savedVersion){throw 'Reopen restored runtime results or lost reference.'}
Full03; AssertChain03 | Out-Null

# A missing target must disable reference capture; old geometry cannot survive.
[HalconDotNet.HObject]$blank=$null
try{[HalconDotNet.HOperatorSet]::GenImageConst([ref]$blank,'byte',320,240);[HalconDotNet.HOperatorSet]::WriteImage($blank,'png',0,$path)}finally{if($blank){$blank.Dispose()}}
Run03 $source; Run03 $ncc
Open03 $alignment; M 'RefreshAlignmentTaskS132' | Out-Null
[object]$oldLine=$null
if((F 'DockAlignmentCaptureS132').IsEnabled -or ($line.TryGetOutputValue('MainLine',[ref]$oldLine) -and $null -ne $oldLine)){throw 'No target retained capture eligibility/old downstream line.'}
Shot 'alignment-target-missing' (F 'moduleParameterWindowS89')
Close03
Write-Output 'UI03_BLOCK01_ALIGNMENT=PASS; ExplicitOriginalProject=True; DraftNccRejected=True; ReferenceDraftApply=True; RealAlignedImageAndLine=True; ActualEdgeNotBaseline=True; ChoicesInvalidatePreview=True; InputDraftCancel=True; Shift12x18=True; FullCycle=True; SaveReopenRerun=True; MissingTargetClearsDownstream=True; NoMouse=True; NoExternalSend=True'
