# Original ROI selection / numeric editor, isolated windows; no pointer capture or desktop input.
function RunRoi04($tool) { M 'RunStandaloneToolCoreUI02' @($tool,'UI03 ROI explicit test',$false) | Out-Null }
function OpenRoi04($tool) {
    M 'OpenCentralModuleConfigurationS77' @($tool,$false) | Out-Null
    M 'SelectCentralConfigurationTaskS77' @((F 'DockRoiTaskTab'),'Roi') | Out-Null; Pump 60
    $editor=F 'moduleParameterWindowS89'
    if($editor.ShowActivated -or $editor.IsActive -or $window.IsActive -or $editor.Left -gt -10000){throw 'ROI test activated a foreground window.'}
}
function CloseRoi04 { M 'CloseCentralModuleConfigurationS77' | Out-Null }
function LocateRoi04($row) {
    $button=[Windows.Controls.Button]::new();$button.DataContext=$row
    M 'DockLocateRoiUI03_Click' @($button,$null)|Out-Null
}
function AreaRoi04($tool,[double]$expected) {
    [double]$area=0
    if(-not $tool.TryGetNumericOutput('Area',[ref]$area) -or $area -ne $expected){throw ('ROI area expected '+$expected+' actual '+$area+' '+$tool.ErrorMessage)}
}
function ExpectedCircle04([double]$radius) {
    [HalconDotNet.HObject]$circle=$null;[HalconDotNet.HObject]$cut=$null;[HalconDotNet.HObject]$effective=$null
    [HalconDotNet.HTuple]$a=$null;[HalconDotNet.HTuple]$r=$null;[HalconDotNet.HTuple]$c=$null
    try {
        [HalconDotNet.HOperatorSet]::GenCircle([ref]$circle,20,20,$radius)
        [HalconDotNet.HOperatorSet]::GenRectangle1([ref]$cut,19,19,21,21)
        [HalconDotNet.HOperatorSet]::Difference($circle,$cut,[ref]$effective)
        [HalconDotNet.HOperatorSet]::AreaCenter($effective,[ref]$a,[ref]$r,[ref]$c)
        return $a.D
    }finally{foreach($obj in @($a,$r,$c,$circle,$cut,$effective)){if($null -ne $obj){$obj.Dispose()}}}
}
$largeArea=ExpectedCircle04 5;$smallArea=ExpectedCircle04 3
Write-Output ('HALCON_CIRCLE_MASK_EXPECTED='+$largeArea+','+$smallArea)
$main.GetField('isRecipeDirty',$all).SetValue($window,$false)
M 'LoadExampleProgramS78' @('color-coverage')|Out-Null
$flow=F 'flowTools';$source=$flow[0];$color=$flow[1]
$path=Join-Path $temp 'roi-red.png'
[HalconDotNet.HObject]$zero=$null;[HalconDotNet.HObject]$red=$null;[HalconDotNet.HObject]$rgb=$null
try {
    [HalconDotNet.HOperatorSet]::GenImageConst([ref]$zero,'byte',64,64)
    [HalconDotNet.HOperatorSet]::ScaleImage($zero,[ref]$red,1,255)
    [HalconDotNet.HOperatorSet]::Compose3($red,$zero,$zero,[ref]$rgb)
    [HalconDotNet.HOperatorSet]::WriteImage($rgb,'png',0,$path)
}finally{foreach($obj in @($rgb,$red,$zero)){if($obj){$obj.Dispose()}}}
$source.Parameters.LocalImagePath=$path;RunRoi04 $source
$layers=F 'roiLayers';$roiType=$assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
$detect=[Activator]::CreateInstance($assembly.GetType('HalconWinFormsDemo.Models.VmRoiLayer',$true))
$detect.Name='检测圆';$detect.Geometry=$roiType.GetMethod('CreateCircle').Invoke($null,[object[]]@(20.0,20.0,5.0))
$detect.ImageContextId=M 'ResolveTargetImageContextIdS86' @($color);$layers.Add($detect);$color.BindRoi($detect.RoiId)
$mask=[Activator]::CreateInstance($detect.GetType());$mask.Name='中心屏蔽';$mask.IsMask=$true
$mask.Geometry=$roiType.GetMethod('CreateRectangle').Invoke($null,[object[]]@(19.0,19.0,21.0,21.0));$mask.ImageContextId=$detect.ImageContextId
$layers.Add($mask);$color.BindRoi($mask.RoiId)
RunRoi04 $color;AreaRoi04 $color $largeArea
$child=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ColorInspectionHsv'),'继承检测区',$true,$null)
$flow.Add($child);$child.SetInputBinding('Image',$source.ToolId,'Image');$child.SetInputBinding('ROI',$color.ToolId,'Region');$child.Parameters.RoiSourceMode='Inherited'
RunRoi04 $child;AreaRoi04 $child $largeArea
OpenRoi04 $color
$row=@((F 'dockRoiBindingRows')|Where-Object RoiId -eq $detect.RoiId)[0]
$maskRow=@((F 'dockRoiBindingRows')|Where-Object RoiId -eq $mask.RoiId)[0]
(F 'FlowToolList').SelectedItem=$source
LocateRoi04 $row
if((F 'RoiLayerList').SelectedItem -ne $detect -or (M 'GetImageTaskToolS89') -ne $color -or (F 'dockDraftDirty')){throw 'Locate changed editor identity or parameter draft.'}
if((F 'DockRoiRoleSummaryText').Text -notlike ($color.InstanceName+'*')){throw 'Missing module ownership.'}
Shot 'roi-selected-circle' (F 'moduleParameterWindowS89')
(F 'RoiCircleDiameterTextBox').Text='6'
if(-not (F 'roiGeometryDraftDirty')){throw 'Original numeric edit did not create geometry draft.'}
LocateRoi04 $maskRow
if((F 'RoiLayerList').SelectedItem -ne $detect -or (F 'DockRoiSourceStatusText').Text -notlike '*应用或取消*'){throw 'Locate discarded numeric draft.'}
M 'CancelRoiGeometryDraftButton_Click' @($null,$null)|Out-Null
if($detect.Geometry.Radius -ne 5){throw 'Numeric cancel did not restore original circle.'}
(F 'RoiCircleDiameterTextBox').Text='6'
M 'ApplyRoiGeometryDraftButton_Click' @($null,$null)|Out-Null
if($detect.Geometry.Radius -ne 3 -or $null -ne $color.LastOutputUtc -or $null -ne $child.LastOutputUtc){throw 'Geometry apply did not invalidate dependent output.'}
CloseRoi04;RunRoi04 $color;AreaRoi04 $color $smallArea
RunRoi04 $child;AreaRoi04 $child $smallArea
OpenRoi04 $color
$row=@((F 'dockRoiBindingRows')|Where-Object RoiId -eq $detect.RoiId)[0]
$detect.IsLocked=$true;LocateRoi04 $row
if((F 'DockRoiSourceStatusText').Text -notlike '*只读*' -or (F 'ReloadRoiGeometryDraftButton').IsEnabled){throw 'Locked ROI not read-only.'}
$detect.IsLocked=$false;$detect.IsVisible=$false;LocateRoi04 $row
if((F 'DockRoiSourceStatusText').Text -notlike '*隐藏*' -or $detect.IsVisible){throw 'Hidden layer silently modified.'}
$detect.IsVisible=$true;CloseRoi04
# ROI binding draft stays separate from selecting a layer; a different input draft cannot route to old image.
OpenRoi04 $color
$row=@((F 'dockRoiBindingRows')|Where-Object RoiId -eq $detect.RoiId)[0]
$row.IsBound=$false;$bindingBox=[Windows.Controls.CheckBox]::new();$bindingBox.DataContext=$row;$bindingBox.IsChecked=$false
M 'DockRoiBindingCheckBox_Click' @($bindingBox,$null)|Out-Null;LocateRoi04 $row
if(-not (F 'dockDraftDirty') -or $row.IsBound -or -not $color.IsRoiBound($detect.RoiId)){throw 'Locate applied or discarded binding draft.'}
$input=@((F 'dockInputPortRows')|Where-Object PortName -eq 'Image')[0]
$input.RestoreSourceKeyUI06('missing-input-ui04');LocateRoi04 $row
if((F 'DockRoiSourceStatusText').Text -notlike '*输入图像来源尚未应用*'){throw 'Unapplied input silently routed to old image.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null;CloseRoi04
# Inject malformed configuration, then explicitly load that definition (not an old retained draft).
$color.BindRoi('missing-ui04');M 'LoadDockConfigurationDraft' @($color)|Out-Null;OpenRoi04 $color
$missing=@((F 'dockRoiBindingRows')|Where-Object RoiId -eq 'missing-ui04')[0]
Write-Output ('MISSING_DIAGNOSTIC=boundIds:'+($color.BoundRoiIds -join ',')+' rows:'+(((F 'dockRoiBindingRows')|ForEach-Object { $_.RoiId+':'+$_.IsBound+':'+$_.CanLocate }) -join ',')+' editor:'+(F 'dockEditingTool').ToolId)
if($null -eq $missing -or -not $missing.IsBound -or $missing.CanLocate -or (F 'dockRoiBindingRows')[0] -ne $missing){throw 'Missing binding not retained / first / correctly locatable.'}
if(M 'ApplyDockConfigurationDraft' @($true)){throw 'Apply silently dropped missing binding.'}
Shot 'roi-missing-repair' (F 'moduleParameterWindowS89')
$missing.IsBound=$false;$box=[Windows.Controls.CheckBox]::new();$box.DataContext=$missing;$box.IsChecked=$false
M 'DockRoiBindingCheckBox_Click' @($box,$null)|Out-Null
if($missing.CanChangeBinding -or -not $color.IsRoiBound('missing-ui04')){throw 'Unbind was applied early or missing row can be rebound.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null
if(-not @((F 'dockRoiBindingRows')|Where-Object RoiId -eq 'missing-ui04')[0].IsBound){throw 'Cancel lost missing binding.'}
$missing=@((F 'dockRoiBindingRows')|Where-Object RoiId -eq 'missing-ui04')[0];$missing.IsBound=$false;$box.DataContext=$missing
M 'DockRoiBindingCheckBox_Click' @($box,$null)|Out-Null
if(-not (M 'ApplyDockConfigurationDraft' @($true)) -or $color.IsRoiBound('missing-ui04')){throw 'Explicit repair did not apply.'}
CloseRoi04;RunRoi04 $color;AreaRoi04 $color $smallArea
$saved=Join-Path $temp 'roi04.json';$service.SaveRecipe($saved,(M 'CaptureRecipe'))
M 'ApplyRecipe' @($service.LoadRecipe($saved))|Out-Null
$flow=F 'flowTools';$source=$flow[0];$color=$flow[1];$child=$flow[4]
RunRoi04 $source;RunRoi04 $color;AreaRoi04 $color $smallArea
RunRoi04 $child;AreaRoi04 $child $smallArea
OpenRoi04 $color;LocateRoi04 $row
if((F 'DockRoiSourceStatusText').Text -notlike '*已变化*'){throw 'Stale row crossed recipe reload.'}
CloseRoi04
Write-Output ('UI03_BLOCK04_ROI=PASS; CircleMask='+$largeArea+'; NumericCancelRadius5; ApplyRadius3Area='+$smallArea+'; MissingBindingExplicitRepair; SaveReload; LockedHiddenStaleGuards; NoMouse')
