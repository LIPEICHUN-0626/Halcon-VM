# Original world-measurement example, self-owned test scale (not field accuracy).
# Dot-sourced by the isolated STA WPF harness; never operates desktop input.
function RunWorld03($tool) { M 'RunStandaloneToolCoreUI02' @($tool,'UI03 explicit world measurement',$false) | Out-Null }
function OpenWorld03($tool) {
    M 'OpenCentralModuleConfigurationS77' @($tool,$false) | Out-Null
    M 'SelectCentralConfigurationTaskS77' @((F 'DockParametersTaskTab'),'Parameters') | Out-Null; Pump 50
    $editor=F 'moduleParameterWindowS89'
    if($editor.ShowActivated -or $editor.IsActive -or $editor.Left -gt -10000){throw 'World parameter window isolation failed.'}
    $caption=if($tool.Kind.ToString() -eq 'ImageSource'){'▶ 读取一帧'}else{'▶ 试运行当前模块'}
    if((F 'DockTrialRunButton').Content -ne $caption){throw ('Run action inherited another module label: '+(F 'DockTrialRunButton').Content)}
}
function CloseWorld03 { M 'CloseCentralModuleConfigurationS77' | Out-Null }
function ApplyWorld03 { if(-not (M 'ApplyDockConfigurationDraft' @($true))){throw 'World explicit apply failed.'} }
function ValueWorld03($tool,[string]$port) {
    [object]$value=$null
    if(-not $tool.TryGetOutputValue($port,[ref]$value) -or $null -eq $value){throw ('Missing '+$port+': '+$tool.ErrorMessage)}
    Write-Output -NoEnumerate $value
}
function AssertWorld03 {
    if([Math]::Abs([double](ValueWorld03 $distance 'WorldDistance')-18) -gt 1e-6 -or
       [Math]::Abs([double](ValueWorld03 $distance 'DeltaY')) -gt 1e-6 -or
       (ValueWorld03 $distance 'Unit') -ne 'mm' -or -not (ValueWorld03 $judge 'Passed')){throw 'Expected actual 18 mm test distance and judge.'}
    if([Math]::Abs((ValueWorld03 $line 'MainLine').StartRow-149.5) -gt 0.3 -or (ValueWorld03 $line 'ValidCount') -ne 20){throw 'Actual edge point evidence missing.'}
    M 'RequireAppliedPlanarOutputS148' @($calibration) | Out-Null
    Write-Output ('UI03_WORLD_ACTUAL=Distance:'+(ValueWorld03 $distance 'WorldDistance')+' mm; RMS:'+(ValueWorld03 $calibration 'RmsResidual')+'; EdgePoints:'+(ValueWorld03 $line 'ValidCount'))
}
function FullWorld03 {
    $full=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmFlowRunRequestMode',$true),'Full')
    $manual=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmRunOriginS135',$true),'ManualFull')
    $task=M 'RunInspectionRangeAsync' @($full,'UI03 explicit world full chain',$false,$true,$manual)
    $deadline=[DateTime]::UtcNow.AddSeconds(20)
    while(-not $task.IsCompleted -and [DateTime]::UtcNow -lt $deadline){Pump 50}
    if(-not $task.IsCompleted){throw 'World chain timeout.'}
    $result=$task.GetAwaiter().GetResult()
    if($result.ExecutionError -or $result.ResultCode -ne 'OK'){throw ('World full chain failed: '+$result.StopReason)}
    AssertWorld03
}
function NewPointWorld03([double]$r,[double]$c,[string]$context) {
    $layer=[Activator]::CreateInstance($assembly.GetType('HalconWinFormsDemo.Models.VmRoiLayer',$true))
    $layer.Geometry=$assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true).GetMethod('CreatePoint').Invoke($null,[object[]]@($r,$c))
    $layer.Name='Explicit calibration test point';$layer.ImageContextId=$context;$layer.IsEnabled=$true
    (F 'roiLayers').Add($layer)
    Write-Output -NoEnumerate $layer
}
$window.Width=1366;$window.Height=768
$main.GetField('isRecipeDirty',$all).SetValue($window,$false)
M 'LoadExampleProgramS78' @('project-world-distance') | Out-Null; Pump 100
$flow=F 'flowTools';$source=$flow[0];$line=$flow[1];$fitTool=$flow[2];$calibration=$flow[3];$distance=$flow[4];$judge=$flow[5]
if($flow.Count -ne 6 -or $calibration.Parameters.PlanarCalibrationSamplesS124.Count -ne 0 -or @($flow | Where-Object {$null -ne $_.LastOutputUtc}).Count -ne 0){throw 'Example fabricated calibration or executed.'}
$path=Join-Path $temp 'ui03-world-source.png'
[HalconDotNet.HObject]$base=$null;[HalconDotNet.HObject]$region=$null;[HalconDotNet.HObject]$image=$null
try{
    [HalconDotNet.HOperatorSet]::GenImageConst([ref]$base,'byte',320,240)
    [HalconDotNet.HOperatorSet]::GenRectangle1([ref]$region,150,40,210,280)
    [HalconDotNet.HOperatorSet]::PaintRegion($region,$base,[ref]$image,255,'fill')
    [HalconDotNet.HOperatorSet]::WriteImage($image,'png',0,$path)
}finally{foreach($obj in @($image,$region,$base)){if($obj){$obj.Dispose()}}}
OpenWorld03 $source
$lease=M 'CaptureEngineeringEditLeaseS137' @('World test source selection')
M 'SelectSourceFileDraftUI03' @($path,$lease) | Out-Null;ApplyWorld03;CloseWorld03;RunWorld03 $source
OpenWorld03 $line
foreach($entry in @{DockEdgeLineStartRowS143='145';DockEdgeLineStartColumnS143='70';DockEdgeLineEndRowS143='145';DockEdgeLineEndColumnS143='250';DockEdgeLineCountS143='20';DockEdgeLineHalfWidthS143='12'}.GetEnumerator()){(F $entry.Key).Text=$entry.Value}
ApplyWorld03;CloseWorld03;RunWorld03 $line;RunWorld03 $fitTool
OpenWorld03 $calibration
if((F 'planarDraftFitS124') -ne $null -or (F 'DockPlanarUnitS124').Text -ne ''){throw 'Opening calibration fitted or fabricated a unit.'}
Shot 'calibration-empty' (F 'moduleParameterWindowS89')
(F 'DockPlanarUnitS124').Text='mm'
$context=M 'ResolveTargetImageContextIdS86' @($calibration)
foreach($point in @(@(20,20),@(20,280),@(210,280),@(210,20))){
    $layer=NewPointWorld03 $point[0] $point[1] $context
    (F 'RoiLayerList').SelectedItem=$layer
    (F 'DockPlanarWorldXS124').Text=([double]$point[1]*0.1).ToString('R',[Globalization.CultureInfo]::InvariantCulture)
    (F 'DockPlanarWorldYS124').Text=([double]$point[0]*0.1).ToString('R',[Globalization.CultureInfo]::InvariantCulture)
    M 'DockPlanarAddPointS124_Click' @($null,$null) | Out-Null
}
if((F 'planarDraftSamplesS124').Count -ne 4 -or $calibration.Parameters.PlanarCalibrationSamplesS124.Count -ne 0 -or (F 'planarDraftFitS124') -ne $null){throw 'Adding samples fitted/applied implicitly or failed.'}
$bad=NewPointWorld03 25 30 'Other.Image'
(F 'RoiLayerList').SelectedItem=$bad
M 'DockPlanarAddPointS124_Click' @($null,$null) | Out-Null
if((F 'planarDraftSamplesS124').Count -ne 4 -or (F 'DockPlanarFitStatusS124').Text -notlike '*同一图像链*'){throw 'Foreign ROI accepted as calibration sample.'}
M 'DockPlanarFitUI03_Click' @($null,$null) | Out-Null
if((F 'planarDraftFitS124') -eq $null -or (F 'planarDraftFitS124').MaxResidual -gt 1e-9){throw ('Explicit real fit failed: '+(F 'DockPlanarFitStatusS124').Text)}
Shot 'calibration-residual-table' (F 'moduleParameterWindowS89')
if((F 'DockParameterPriorityBar').Visibility -ne [Windows.Visibility]::Collapsed){throw 'Calibration still shows a misleading zero-field banner.'}
$fitButton=F 'DockPlanarFitButtonUI03';$content=(F 'moduleParameterWindowS89').Content
$fitPosition=$fitButton.TransformToAncestor($content).Transform([Windows.Point]::new(0,0))
if($fitPosition.Y -lt 0 -or $fitPosition.Y+$fitButton.ActualHeight -gt $content.ActualHeight){throw 'Explicit fit action is clipped from the default parameter viewport.'}
(F 'DockPlanarSampleListS124').SelectedIndex=0
if((F 'DockPlanarWorldXS124').Text -ne '2' -or (F 'DockPlanarWorldYS124').Text -ne '2'){throw 'Selected row did not refill coordinate editor.'}
$before=(F 'planarDraftSamplesS124')[0].Clone()
(F 'DockPlanarWorldXS124').Text='2.5';M 'DockPlanarUpdateSampleUI03_Click' @($null,$null) | Out-Null
$updated=(F 'planarDraftSamplesS124')[0]
if($updated.WorldX -ne 2.5 -or $updated.PixelRow -ne $before.PixelRow -or $updated.PixelColumn -ne $before.PixelColumn -or $updated.SourceRoiId -ne $before.SourceRoiId -or (F 'planarDraftFitS124') -ne $null){throw 'Updating X/Y changed pixel identity or retained stale fit.'}
if(@((F 'DockPlanarSampleListS124').Items | Where-Object {$_.Residual -ne '—'}).Count -ne 0){throw 'Edited samples still display old residuals.'}
(F 'DockPlanarWorldXS124').Text='2';M 'DockPlanarUpdateSampleUI03_Click' @($null,$null) | Out-Null
M 'DockPlanarFitUI03_Click' @($null,$null) | Out-Null
M 'DockTrialRunButton_Click' @($null,$null) | Out-Null
if($calibration.ResultCode -ne 'OK' -or $calibration.Parameters.PlanarCalibrationSamplesS124.Count -ne 0){throw ('Calibration F5 failed or applied: '+$calibration.ErrorMessage)}
$rejected=$false;try{M 'RequireAppliedPlanarOutputS148' @($calibration) | Out-Null}catch{$rejected=$true}
if(-not $rejected){throw 'Calibration draft World2D output gained formal qualification.'}
ApplyWorld03
if($null -ne $calibration.LastOutputUtc){throw 'Calibration apply retained trial outputs.'}
RunWorld03 $calibration;CloseWorld03;RunWorld03 $distance;RunWorld03 $judge;AssertWorld03
FullWorld03
OpenWorld03 $calibration
if((F 'planarDraftFitS124') -ne $null){throw 'Opening applied calibration auto-computed preview.'}
M 'DockPlanarFitUI03_Click' @($null,$null) | Out-Null
$imageRow=@((F 'dockInputPortRows') | Where-Object PortName -eq 'Image')[0]
$savedInputKey=$imageRow.SelectedSourceKey
$imageRow.SelectedSourceKey=@($imageRow.SourceOptions | Where-Object IsDefault)[0].Key
M 'DockDraftValue_Changed' @($null,$null) | Out-Null
if((F 'planarDraftFitS124') -ne $null){throw 'Input draft change retained old fit preview.'}
M 'DockPlanarFitUI03_Click' @($null,$null) | Out-Null
if((F 'planarDraftFitS124') -ne $null -or $calibration.GetInputBinding('Image').SourceToolId -ne $source.ToolId){throw 'Changed input preview fitted old source or committed input draft.'}
M 'DockRevertButton_Click' @($null,$null) | Out-Null
if(@((F 'dockInputPortRows') | Where-Object PortName -eq 'Image')[0].SelectedSourceKey -ne $savedInputKey){throw 'Cancelling calibration input did not restore source.'}
(F 'DockPlanarUnitS124').Text=''
if(M 'ApplyDockConfigurationDraft' @($true)){throw 'Empty unit was applied or normalized to mm.'}
if($calibration.Parameters.PlanarCalibrationUnitS124 -ne 'mm'){throw 'Rejected unit edit changed applied configuration.'}
M 'DockRevertButton_Click' @($null,$null) | Out-Null
for($index=0;$index -lt 4;$index++){
    (F 'DockPlanarSampleListS124').SelectedIndex=$index
    (F 'DockPlanarWorldXS124').Text=($index*10).ToString();(F 'DockPlanarWorldYS124').Text='0'
    M 'DockPlanarUpdateSampleUI03_Click' @($null,$null) | Out-Null
}
M 'DockPlanarFitUI03_Click' @($null,$null) | Out-Null
if((F 'planarDraftFitS124') -ne $null -or (F 'DockPlanarFitStatusS124').Text -notlike '*共线*'){throw 'Degenerate world samples fitted successfully.'}
if(M 'ApplyDockConfigurationDraft' @($true)){throw 'Degenerate world samples applied.'}
M 'DockRevertButton_Click' @($null,$null) | Out-Null;CloseWorld03
$file=Join-Path $temp 'ui03-world-project.json';$service.SaveRecipe($file,(M 'CaptureRecipe'))
$loaded=$service.LoadRecipe($file);M 'ApplyRecipe' @($loaded) | Out-Null
$flow=F 'flowTools';$source=$flow[0];$line=$flow[1];$fitTool=$flow[2];$calibration=$flow[3];$distance=$flow[4];$judge=$flow[5]
if(@($flow | Where-Object {$null -ne $_.LastOutputUtc}).Count -ne 0 -or $calibration.Parameters.PlanarCalibrationSamplesS124.Count -ne 4 -or $calibration.Parameters.PlanarCalibrationUnitS124 -ne 'mm'){throw 'Save/reopen lost calibration or restored old results.'}
FullWorld03
OpenWorld03 $distance
M 'SelectCentralConfigurationTaskS77' @((F 'DockResultTaskTab'),'Result') | Out-Null
Shot 'world-distance-results' (F 'moduleParameterWindowS89');CloseWorld03
$compat=$calibration.Parameters.Clone()
# PowerShell converts $null assigned to a .NET string property into "";
# reflection is needed here to represent the actual missing legacy field.
$compat.GetType().GetProperty('PlanarCalibrationUnitS124').SetValue($compat,$null)
$compat.Normalize()
if($compat.PlanarCalibrationUnitS124 -ne 'mm'){throw 'Legacy missing unit default changed.'}
$compat.PlanarCalibrationUnitS124=' ';$compat.Normalize()
if($compat.PlanarCalibrationUnitS124 -ne '' -or [string]::IsNullOrWhiteSpace($compat.Validate($calibration.Kind))){throw 'Explicit blank unit accepted by model validation.'}
$source.Parameters.LocalImagePath=Join-Path $temp 'missing-image.png'
$failed=$false;try{RunWorld03 $source}catch{$failed=$true}
[object]$stale=$null
if(-not $failed -or ($distance.TryGetOutputValue('WorldDistance',[ref]$stale) -and $null -ne $stale)){throw 'Missing source preserved old world distance.'}
Write-Output 'UI03_BLOCK02_WORLD=PASS; SixToolOriginalProject=True; RealEdge20=True; ActualWorldDistance18mm=True; ExplicitFit=True; DraftWorldRejected=True; UpdateSelectedWorldOnly=True; EmptyUnitRejected=True; LegacyNullUnitCompatible=True; CollinearRejected=True; ForeignRoiRejected=True; SaveReopenRerun=True; MissingSourceClearsDistance=True; NoMouse=True; NoExternalSend=True'
