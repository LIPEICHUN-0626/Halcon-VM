# Original color example; self-owned RGB fixture, isolated WPF, no desktop input.
function RunColor03($tool) { M 'RunStandaloneToolCoreUI02' @($tool,'UI03 explicit color test',$false) | Out-Null }
function OpenColor03($tool) {
    M 'OpenCentralModuleConfigurationS77' @($tool,$false) | Out-Null
    M 'SelectCentralConfigurationTaskS77' @((F 'DockParametersTaskTab'),'Parameters') | Out-Null; Pump 50
    $editor=F 'moduleParameterWindowS89'
    if($editor.ShowActivated -or $editor.IsActive -or $window.IsActive -or $editor.Left -gt -10000){throw 'Color test activated a foreground window.'}
}
function CloseColor03 { M 'CloseCentralModuleConfigurationS77' | Out-Null }
function ApplyColor03 { if(-not (M 'ApplyDockConfigurationDraft' @($true))){throw ('Color apply failed: '+(F 'DockValidationText').Text)} }
function ValueColor03($tool,[string]$port) {
    [object]$value=$null
    if(-not $tool.TryGetOutputValue($port,[ref]$value) -or $null -eq $value){throw ('Missing '+$port+': '+$tool.ErrorMessage)}
    Write-Output -NoEnumerate $value
}
function AreaColor03([double]$area,[double]$count) {
    if([Math]::Abs([double](ValueColor03 $color 'Area')-$area) -gt 1e-6 -or
       [Math]::Abs([double](ValueColor03 $color 'SampleCount')-$count) -gt 1e-6 -or
       [Math]::Abs([double](ValueColor03 $color 'AreaRatio')-100*$area/$count) -gt 1e-6){throw 'Color area / effective denominator / percentage differs.'}
}
function PickColor03([single]$x,[single]$y) {
    M 'DockColorPickS129_Click' @($null,$null) | Out-Null
    if((F 'colorPickingToolS129') -ne $color){throw ('Pick did not start: '+(F 'DockColorSampleSummaryS129').Text)}
    M 'ColorMouseDownS129' @([Drawing.PointF]::new($x,$y),$false) | Out-Null
}
function FullColor03 {
    $full=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmFlowRunRequestMode',$true),'Full')
    $manual=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmRunOriginS135',$true),'ManualFull')
    $task=M 'RunInspectionRangeAsync' @($full,'UI03 color original full chain',$false,$true,$manual)
    $deadline=[DateTime]::UtcNow.AddSeconds(20)
    while(-not $task.IsCompleted -and [DateTime]::UtcNow -lt $deadline){Pump 50}
    if(-not $task.IsCompleted){throw 'Color chain timeout.'}
    $result=$task.GetAwaiter().GetResult()
    if($result.ExecutionError -or $result.ResultCode -ne 'OK'){throw ('Color full chain failed: '+$result.StopReason)}
    AreaColor03 604 8000
    if((ValueColor03 $feature 'Area') -ne 600 -or -not (ValueColor03 $judge 'Passed')){throw 'Region / original coverage branches failed.'}
}
$window.Width=1366;$window.Height=768
$main.GetField('isRecipeDirty',$all).SetValue($window,$false)
M 'LoadExampleProgramS78' @('color-coverage') | Out-Null; Pump 70
$flow=F 'flowTools';$source=$flow[0];$color=$flow[1];$feature=$flow[2];$judge=$flow[3]
if($flow.Count -ne 4 -or @($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count -ne 0){throw 'Color example executed on load.'}
OpenColor03 $color
if((F 'DockColorPickButtonUI03').IsEnabled){throw 'Pick enabled without a current image.'}
Shot 'color-missing-input' (F 'moduleParameterWindowS89');CloseColor03
$path=Join-Path $temp 'ui03-color-source.png'
[HalconDotNet.HObject]$base=$null;[HalconDotNet.HObject]$big=$null;[HalconDotNet.HObject]$small=$null;[HalconDotNet.HObject]$redRegion=$null
[HalconDotNet.HObject]$red=$null;[HalconDotNet.HObject]$green=$null;[HalconDotNet.HObject]$rgb=$null
try {
    [HalconDotNet.HOperatorSet]::GenImageConst([ref]$base,'byte',100,80)
    [HalconDotNet.HOperatorSet]::GenRectangle1([ref]$big,10,10,29,39)
    [HalconDotNet.HOperatorSet]::GenRectangle1([ref]$small,50,50,51,51)
    [HalconDotNet.HOperatorSet]::Union2($big,$small,[ref]$redRegion)
    [HalconDotNet.HOperatorSet]::PaintRegion($redRegion,$base,[ref]$red,255,'fill')
    [HalconDotNet.HOperatorSet]::ScaleImage($red,[ref]$green,-1,255)
    [HalconDotNet.HOperatorSet]::Compose3($red,$green,$base,[ref]$rgb)
    [HalconDotNet.HOperatorSet]::WriteImage($rgb,'png',0,$path)
}finally{foreach($obj in @($rgb,$green,$red,$redRegion,$small,$big,$base)){if($obj){$obj.Dispose()}}}
OpenColor03 $source
$lease=M 'CaptureEngineeringEditLeaseS137' @('Color fixture file selection')
M 'SelectSourceFileDraftUI03' @($path,$lease) | Out-Null;ApplyColor03;CloseColor03;RunColor03 $source
RunColor03 $color;AreaColor03 604 8000
RunColor03 $feature;RunColor03 $judge
if((ValueColor03 $feature 'Area') -ne 600 -or -not (ValueColor03 $judge 'Passed')){throw 'Actual filter / coverage judge failed.'}
OpenColor03 $color
if(-not (F 'DockColorPickButtonUI03').IsEnabled -or (F 'dockDraftDirty')){throw 'Valid RGB not ready or opening marked dirty.'}
foreach($name in @('DockColorHueS129','DockColorToleranceS129','DockColorSatMinS129','DockColorValueMinS129')){
    $field=F $name;if(-not $field.IsVisible -or $field.ActualWidth -gt 140){throw ('Color common field is hidden / stretched: '+$name)}
}
$oldParameters=$color.Parameters;$sourceVersion=$source.RuntimeOutputVersion
PickColor03 70 60
$hue=[double]::Parse((F 'DockColorHueS129').Text,[Globalization.CultureInfo]::InvariantCulture)
if([Math]::Abs($hue-120) -gt 0.01 -or $color.Parameters.ColorInspectionS129.HueDegrees -ne 0 -or -not (F 'dockDraftDirty') -or $source.RuntimeOutputVersion -ne $sourceVersion){throw ('Real green sample did not stay draft: '+$hue)}
if((F 'DockColorChipS129').Background.Color.G -ne 255 -or (F 'DockColorSampleSummaryS129').Text.Length -gt 40){throw 'Sample chip / concise summary failed.'}
Shot 'color-sampled-draft' (F 'moduleParameterWindowS89')
M 'DockTrialRunButton_Click' @($null,$null) | Out-Null;AreaColor03 7396 8000
if(-not [object]::ReferenceEquals($oldParameters,$color.Parameters) -or $color.RuntimeResultOrigin.ToString() -ne 'DraftTrial'){throw 'Color F5 applied draft or lost provenance.'}
ApplyColor03
if($null -ne $color.LastOutputUtc -or $null -ne $judge.LastOutputUtc){throw 'Apply retained stale current / downstream output.'}
CloseColor03;RunColor03 $color;AreaColor03 7396 8000
OpenColor03 $color;PickColor03 20 20
if([Math]::Abs([double](F 'DockColorHueS129').Text) -gt 0.01){throw 'Real red sampling failed.'}
(F 'DockColorHueS129').Text='240'
if((F 'DockColorChipS129').Background.Color.A -ne 0){throw 'Manual hue retained misleading old sample chip.'}
M 'DockTrialRunButton_Click' @($null,$null) | Out-Null;AreaColor03 0 8000
if((ValueColor03 $color 'HasMatch') -or (ValueColor03 $color 'RegionCount') -ne 0 -or $color.ResultCode -ne 'OK'){throw 'No-match must be actual zero, not failure / old region.'}
PickColor03 20 20;ApplyColor03;CloseColor03;FullColor03
OpenColor03 $color
$imageRow=@((F 'dockInputPortRows')|Where-Object PortName -eq 'Image')[0]
$imageRow.SelectedSourceKey=@($imageRow.SourceOptions|Where-Object IsDefault)[0].Key
M 'DockDraftValue_Changed' @($null,$null) | Out-Null
if((F 'DockColorPickButtonUI03').IsEnabled){throw 'Unapplied input draft allowed picking old input.'}
M 'DockRevertButton_Click' @($null,$null) | Out-Null
if(-not (F 'DockColorPickButtonUI03').IsEnabled){throw 'Input cancel did not restore ready state.'}
CloseColor03
$saved=Join-Path $temp 'ui03-color-recipe.json';$service.SaveRecipe($saved,(M 'CaptureRecipe'));$loaded=$service.LoadRecipe($saved)
M 'ApplyRecipe' @($loaded) | Out-Null
$flow=F 'flowTools';$source=$flow[0];$color=$flow[1];$feature=$flow[2];$judge=$flow[3]
if(@($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count -ne 0 -or $color.GetInputBinding('Image').SourceToolId -ne $source.ToolId -or $judge.GetInputBinding('Value').SourceToolId -ne $color.ToolId){throw 'Save/reopen lost binding or restored stale runtime values.'}
FullColor03
OpenColor03 $color;M 'SelectCentralConfigurationTaskS77' @((F 'DockResultTaskTab'),'Result') | Out-Null
Shot 'color-results' (F 'moduleParameterWindowS89');CloseColor03
$source.Parameters.LocalImagePath=Join-Path $temp 'missing-color.png'
$failed=$false;try{RunColor03 $source}catch{$failed=$true}
[object]$stale=$null
if(-not $failed -or ($color.TryGetOutputValue('AreaRatio',[ref]$stale) -and $null -ne $stale) -or ($judge.TryGetOutputValue('Passed',[ref]$stale) -and $null -ne $stale)){throw 'Missing source reused old color / judgment.'}
$source.Parameters.LocalImagePath=$path;RunColor03 $source;RunColor03 $color

# Actual local ROI draft and mask use the original parameter binding UI.
$layers=F 'roiLayers';$roiType=$assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
function NewColorRoi03([string]$name,[double]$r1,[double]$c1,[double]$r2,[double]$c2,[bool]$mask) {
    $layer=[Activator]::CreateInstance($assembly.GetType('HalconWinFormsDemo.Models.VmRoiLayer',$true))
    $layer.Name=$name;$layer.IsMask=$mask;$layer.IsEnabled=$true
    $layer.Geometry=$roiType.GetMethod('CreateRectangle').Invoke($null,[object[]]@($r1,$c1,$r2,$c2))
    $layer.ImageContextId=M 'ResolveTargetImageContextIdS86' @($color);$layers.Add($layer)
    Write-Output -NoEnumerate $layer
}
$detect=NewColorRoi03 'Color test detection' 10 10 29 49 $false
$mask=NewColorRoi03 'Color test mask' 10 10 19 19 $true
OpenColor03 $color
foreach($layer in @($detect,$mask)){
    $row=@((F 'dockRoiBindingRows')|Where-Object RoiId -eq $layer.RoiId)[0];$row.IsBound=$true
    $box=[Windows.Controls.CheckBox]::new();$box.DataContext=$row;$box.IsChecked=$true
    M 'DockRoiBindingCheckBox_Click' @($box,$null) | Out-Null
}
if($color.IsRoiBound($detect.RoiId)){throw 'ROI selection applied without explicit apply.'}
M 'DockTrialRunButton_Click' @($null,$null) | Out-Null;AreaColor03 500 700
ApplyColor03;CloseColor03;RunColor03 $color;AreaColor03 500 700
RunColor03 $feature;RunColor03 $judge
if((ValueColor03 $feature 'Area') -ne 500 -or -not (ValueColor03 $judge 'Passed')){throw 'ROI-derived real region / coverage downstream failed.'}
OpenColor03 $color;PickColor03 15 15
if((F 'colorPickingToolS129') -ne $color -or (F 'dockDraftDirty') -or (F 'DockColorSampleSummaryS129').Text -notlike '*有效域*'){throw 'Masked sample wrote hue instead of rejecting inline.'}
M 'DockColorCancelPickS129_Click' @($null,$null) | Out-Null;CloseColor03
function RejectColor03([string]$reason) {
    $rejected=$false;try{RunColor03 $color}catch{$rejected=$true}
    [object]$old=$null
    if(-not $rejected -or ($color.TryGetOutputValue('AreaRatio',[ref]$old) -and $null -ne $old)){throw ('Invalid ROI reused result: '+$reason)}
    OpenColor03 $color
    if((F 'DockColorPickButtonUI03').IsEnabled){throw ('Invalid ROI still allows pick: '+$reason)}
    CloseColor03
}
$color.BindRoi('missing-color-roi');RejectColor03 'missing binding';$color.UnbindRoi('missing-color-roi')
$context=$detect.ImageContextId;$detect.ImageContextId='other.Image';RejectColor03 'foreign image';$detect.ImageContextId=$context
$color.UnbindRoi($mask.RoiId);$detect.IsEnabled=$false;RejectColor03 'all disabled';$detect.IsEnabled=$true
$color.BindRoi($mask.RoiId);RunColor03 $color;AreaColor03 500 700
$roiSave=Join-Path $temp 'ui03-color-roi.json';$service.SaveRecipe($roiSave,(M 'CaptureRecipe'));M 'ApplyRecipe' @($service.LoadRecipe($roiSave)) | Out-Null
$flow=F 'flowTools';$source=$flow[0];$color=$flow[1];$feature=$flow[2];$judge=$flow[3]
RunColor03 $source;RunColor03 $color;AreaColor03 500 700

# Algorithm contract checks use real HALCON images, without claiming all file
# acquisition paths or all geometry modes. They do not replace the UI chain above.
$color.ReplaceRoiBindings([string[]]@())
$input=M 'CreateToolInputImage' @($color)
try {
    foreach($spec in @(@('uint2',257.0),@('real',(1.0/255)))){
        $typed=$input.ConvertImageType([string]$spec[0]);$scaled=$null
        try {
            $scaled=$typed.ScaleImage([double]$spec[1],0)
            M 'RunColorInspectionS129' @($color,$scaled) | Out-Null;AreaColor03 604 8000
            Write-Output ('COLOR_ACTUAL_TYPE='+$spec[0]+'; Area604; Denominator8000')
        }finally{if($scaled){$scaled.Dispose()};$typed.Dispose()}
    }
    $tiny=$roiType.GetMethod('CreateRectangle').Invoke($null,[object[]]@(12.0,22.0,14.0,24.0))
    $reduced=$input.ReduceDomain($tiny.Region)
    try{M 'RunColorInspectionS129' @($color,$reduced) | Out-Null;AreaColor03 9 9}finally{$reduced.Dispose();$tiny.Dispose()}
    # real[0,255] is not silently rescaled; previous output is discarded.
    $invalid=$input.ConvertImageType('real');$rejected=$false
    try{try{M 'RunColorInspectionS129' @($color,$invalid) | Out-Null}catch{$rejected=$true}}
    finally{$invalid.Dispose()}
    [object]$old=$null
    if(-not $rejected -or ($color.TryGetOutputValue('AreaRatio',[ref]$old) -and $null -ne $old)){throw 'Invalid real amplitude accepted or retained output.'}
    # Both wrap-around threshold branches include pure red.
    foreach($h in @(359.0,1.0)){
        $color.Parameters.ColorInspectionS129.HueDegrees=$h
        M 'RunColorInspectionS129' @($color,$input) | Out-Null;AreaColor03 604 8000
    }
    $color.Parameters.ColorInspectionS129.HueDegrees=0
}finally{$input.Dispose()}
RunColor03 $color
OpenColor03 $color;PickColor03 20 20
RunColor03 $source;M 'RefreshColorPickStateS129' | Out-Null
if((F 'DockColorChipS129').Background.Color.A -ne 0){throw 'New input frame retained old sample chip.'}
M 'DockRevertButton_Click' @($null,$null) | Out-Null;CloseColor03
# Real RGB pixel near 360 degrees must round back to 0, never propose invalid 360.
# Inject a genuine in-memory image snapshot to test sampling; not a camera/file claim.
$zero=[HalconDotNet.HImage]::new();$zero.GenImageConst('real',100,80)
$one=$zero.ScaleImage(1,1);$nearZero=$zero.ScaleImage(1,0.000001);$nearRed=$one.Compose3($zero,$nearZero)
try {
    $snapshotType=$assembly.GetType('HalconWinFormsDemo.Models.VmImageSnapshot',$true)
    $snapshot=$snapshotType.GetMethod('Create').Invoke($null,[object[]]@($nearRed,$source.ToolId,$source.InstanceName,'Image','','seam-fixture'))
    $source.SetOutputValue('Image',$snapshot)
    OpenColor03 $color;PickColor03 20 20
    if((F 'DockColorHueS129').Text -ne '0' -or (F 'DockColorSampleSummaryS129').Text -notlike '*H 0°*'){throw ('Picked hue / summary rounded outside [0,360): '+(F 'DockColorHueS129').Text)}
    M 'DockRevertButton_Click' @($null,$null) | Out-Null;CloseColor03
}finally{$nearRed.Dispose();$nearZero.Dispose();$one.Dispose();$zero.Dispose()}
RunColor03 $source
# Actual inherited Region from the original HSV result, same image coordinate chain.
RunColor03 $color
$child=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ColorInspectionHsv'),'Inherited color domain',$true,$null)
$flow.Add($child);$child.SetInputBinding('Image',$source.ToolId,'Image');$child.SetInputBinding('ROI',$color.ToolId,'Region');$child.Parameters.RoiSourceMode='Inherited'
RunColor03 $child
if((ValueColor03 $child 'Area') -ne 604 -or (ValueColor03 $child 'SampleCount') -ne 604 -or (ValueColor03 $child 'AreaRatio') -ne 100){throw 'Inherited color region denominator is not the actual effective domain.'}
$color.ClearRuntimeOutputs();$rejected=$false;try{RunColor03 $child}catch{$rejected=$true}
[object]$old=$null
if(-not $rejected -or ($child.TryGetOutputValue('AreaRatio',[ref]$old) -and $null -ne $old)){throw 'Missing inherited source reused old result.'}
# A grayscale source is usable elsewhere, but not a ready color-picking input.
[HalconDotNet.HObject]$gray=$null;$grayPath=Join-Path $temp 'ui03-gray.png'
try{[HalconDotNet.HOperatorSet]::GenImageConst([ref]$gray,'byte',100,80);[HalconDotNet.HOperatorSet]::WriteImage($gray,'png',0,$grayPath)}finally{if($gray){$gray.Dispose()}}
$source.Parameters.LocalImagePath=$grayPath;RunColor03 $source
OpenColor03 $color
if((F 'DockColorPickButtonUI03').IsEnabled -or (F 'DockColorSampleSummaryS129').Text -notlike '*灰度*'){throw 'Grayscale input was presented as ready for RGB sampling.'}
M 'DockTrialRunButton_Click' @($null,$null) | Out-Null
if($color.TryGetOutputValue('AreaRatio',[ref]$old) -and $null -ne $old){throw 'Grayscale trial retained old color output.'}
CloseColor03
Write-Output 'UI03_COLOR_CONTRACTS=PASS; RealUint2AndNormalizedReal=True; ReducedDomain9=True; RealOutOfRangeRejected=True; HueWrapBothSides=True; ReplacedFrameClearsChip=True; Inherited604of604=True; MissingInheritedSourceClears=True; GrayscaleNotReady=True'
Write-Output 'UI03_BLOCK03_COLOR=PASS; RealRGB604of8000=True; ActualFilteredRegion600=True; RealGreenPickDraftAndF5=True; ApplyClearsDownstream=True; ZeroMatch=True; InputCancel=True; SaveReopenRerun=True; LocalMask500of700=True; MaskedSampleRejected=True; MissingDisabledForeignRoiRejected=True; MissingSourceClears=True; NoMouse=True; NoExternalSend=True'
