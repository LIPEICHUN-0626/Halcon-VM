# Explicit applied input images, genuine HALCON difference and downstream; no desktop input.
function RunInput05($tool) { M 'RunStandaloneToolCoreUI02' @($tool,'UI05 input test',$false)|Out-Null }
function OpenInput05($tool) {
    M 'OpenCentralModuleConfigurationS77' @($tool,$false)|Out-Null
    M 'SelectCentralConfigurationTaskS77' @((F 'DockIoTaskTab'),'Io')|Out-Null;Pump 60
    $editor=F 'moduleParameterWindowS89'
    if($editor.IsActive -or $editor.ShowActivated -or $window.IsActive -or $editor.Left -gt -10000){throw 'Input test activated desktop.'}
}
function ViewInput05($row) {
    $button=[Windows.Controls.Button]::new();$button.DataContext=$row
    M 'DockViewInputImageUI05_Click' @($button,$null)|Out-Null
}
function InputRow05([string]$port) { (F 'dockInputPortRows')|Where-Object PortName -eq $port }
function ChooseInput05([string]$port,$producer) {
    $row=InputRow05 $port
    $choice=@($row.SourceOptions|Where-Object {if($null -eq $producer){$_.IsDefault}else{$_.SourceToolId -eq $producer.ToolId -and $_.SourcePortName -eq 'Image'}})[0]
    $combo=[Windows.Controls.ComboBox]::new();$combo.DataContext=$row;$combo.ItemsSource=$row.SourceOptions;$combo.SelectedItem=$choice
    M 'DockInputSourceComboBox_SelectionChanged' @($combo,$null)|Out-Null
}
function ApplyInput05 {if(-not (M 'ApplyDockConfigurationDraft' @($true))){throw ('Input apply failed: '+(F 'DockValidationText').Text)}}
function PixelInput05($snapshot,[double]$expected) {
    if($null -eq $snapshot){throw 'No snapshot.'}
    $im=$snapshot.CreateImageCopy();$value=$null
    try {$value=$im.GetGrayval(10,10);if($value.D -ne $expected){throw ('Wrong input pixel '+$value.D+' expected '+$expected)}}finally{if($value){$value.Dispose()};$im.Dispose()}
}
function OutputInput05($tool,[double]$expected) {
    [object]$value=$null;if(-not $tool.TryGetOutputValue('Image',[ref]$value)){throw 'No image output.'};PixelInput05 $value $expected
}
function WriteInputFixture05([string]$name,[int]$level,[int]$width=64) {
    $path=Join-Path $temp $name
    [HalconDotNet.HObject]$zero=$null;[HalconDotNet.HObject]$im=$null
    try{[HalconDotNet.HOperatorSet]::GenImageConst([ref]$zero,'byte',$width,48);[HalconDotNet.HOperatorSet]::ScaleImage($zero,[ref]$im,1,$level);[HalconDotNet.HOperatorSet]::WriteImage($im,'png',0,$path)}finally{if($im){$im.Dispose()};if($zero){$zero.Dispose()}}
    return $path
}
$flow=F 'flowTools';$a=$flow[0];$threshold=$flow[1]
$b=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageSource'),'图像 B · 40',$true,$null);$flow.Insert(1,$b)
$c=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageSource'),'图像 C · 80',$true,$null);$flow.Insert(2,$c)
$diff=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageDifference'),'双图差分',$true,$null);$flow.Insert(3,$diff)
$diff.SetInputBinding('ImageA',$a.ToolId,'Image');$diff.SetInputBinding('ImageB',$b.ToolId,'Image')
$threshold.SetInputBinding('Image',$diff.ToolId,'Image');$threshold.Parameters.ImageThresholdMinGray=128;$threshold.Parameters.ImageThresholdMaxGray=255
$a.Parameters.LocalImagePath=WriteInputFixture05 'input-a.png' 200
$b.Parameters.LocalImagePath=WriteInputFixture05 'input-b.png' 40
$c.Parameters.LocalImagePath=WriteInputFixture05 'input-c.png' 80
foreach($t in @($a,$b,$c,$diff,$threshold)){RunInput05 $t}
OutputInput05 $diff 160
[double]$area=0;if(-not $threshold.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 3072){throw 'Actual difference downstream failed.'}
OpenInput05 $diff
$rowA=InputRow05 'ImageA';$rowB=InputRow05 'ImageB';$sourceVersion=$b.RuntimeOutputVersion
ViewInput05 $rowB;PixelInput05 (M 'ResolveImageContext').Snapshot 40
if((M 'GetImageTaskToolS89') -ne $diff -or (F 'dockDraftDirty') -or $b.RuntimeOutputVersion -ne $sourceVersion){throw 'View applied or executed / switched editor.'}
(F 'FlowToolList').SelectedItem=$a
ViewInput05 $rowA;PixelInput05 (M 'ResolveImageContext').Snapshot 200
M 'SetDualImageMode' @($true)|Out-Null;M 'RefreshDualImageInputOptions' @($diff)|Out-Null
(F 'DualInputPortComboBox').SelectedValue='ImageB'
PixelInput05 (M 'ResolveDualInputImageContext').Snapshot 40
Shot 'input-two-sources' (F 'moduleParameterWindowS89')
ChooseInput05 'ImageB' $c
if(-not (InputRow05 'ImageB').HasSourceDraftUI05 -or $diff.GetInputBinding('ImageB').SourceToolId -ne $b.ToolId){throw 'Selection did not remain marked draft.'}
$key=(F 'ImageContextComboBox').SelectedValue;ViewInput05 (InputRow05 'ImageB')
if((F 'ImageContextComboBox').SelectedValue -ne $key -or (F 'DockInputSummaryText').Text -notlike '*尚未应用*'){throw 'Draft source shown as applied.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null
if((InputRow05 'ImageB').HasSourceDraftUI05){throw 'Cancel retained draft source marker.'}
ChooseInput05 'ImageB' $c;M 'DockTrialRunButton_Click' @($null,$null)|Out-Null;OutputInput05 $diff 120
if($diff.GetInputBinding('ImageB').SourceToolId -ne $b.ToolId){throw 'F5 applied input binding.'}
ApplyInput05
if($null -ne $diff.LastOutputUtc -or $null -ne $threshold.LastOutputUtc){throw 'Apply retained old downstream output.'}
RunInput05 $diff;RunInput05 $threshold;OutputInput05 $diff 120
if(-not $threshold.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 0){throw 'New input did not reach actual threshold.'}
ViewInput05 (InputRow05 'ImageB');PixelInput05 (M 'ResolveImageContext').Snapshot 80
ChooseInput05 'ImageA' $null
if(M 'ApplyDockConfigurationDraft' @($true)){throw 'Required missing input was accepted.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null
# Inject an incomplete definition as can occur while building/loading a flow; do not bypass the UI guard in production.
$diff.RemoveInputBinding('ImageA');M 'LoadDockConfigurationDraft' @($diff)|Out-Null
M 'SetImageContextSelection' @('ModuleInput')|Out-Null
if((M 'ResolveImageContext').HasImage){throw 'Missing first input borrowed B or global image.'}
ViewInput05 (InputRow05 'ImageA');if((M 'ResolveImageContext').HasImage){throw 'Explicit missing input borrowed image.'}
$diff.SetInputBinding('ImageA',$a.ToolId,'Image');M 'LoadDockConfigurationDraft' @($diff)|Out-Null
ViewInput05 (InputRow05 'ImageB');$c.IsEnabled=$false
if((M 'ResolveImageContext').HasImage -or (M 'ResolveDualInputImageContext').HasImage){throw 'Disabled applied source displayed an old frame.'}
$c.IsEnabled=$true
$c.Parameters.LocalImagePath=WriteInputFixture05 'input-small.png' 80 32;RunInput05 $c
$failed=$false;try{RunInput05 $diff}catch{$failed=$true}
if(-not $failed -or $null -ne $diff.LastOutputUtc -or $null -ne $threshold.LastOutputUtc){throw 'Size mismatch reused previous result.'}
$c.Parameters.LocalImagePath=Join-Path $temp 'input-c.png';RunInput05 $c;RunInput05 $diff;RunInput05 $threshold
$stale=InputRow05 'ImageB';M 'CloseCentralModuleConfigurationS77'|Out-Null
$saved=Join-Path $temp 'input05.json';$service.SaveRecipe($saved,(M 'CaptureRecipe'));M 'ApplyRecipe' @($service.LoadRecipe($saved))|Out-Null
$flow=F 'flowTools';$a=$flow[0];$b=$flow[1];$c=$flow[2];$diff=$flow[3];$threshold=$flow[4]
foreach($t in @($a,$b,$c,$diff,$threshold)){RunInput05 $t};OutputInput05 $diff 120
OpenInput05 $diff;ViewInput05 (InputRow05 'ImageA');$key=(F 'ImageContextComboBox').SelectedValue;ViewInput05 $stale
if((F 'ImageContextComboBox').SelectedValue -ne $key){throw 'Old row crossed recipe reload.'}
Shot 'input-saved' (F 'moduleParameterWindowS89');M 'CloseCentralModuleConfigurationS77'|Out-Null
OpenInput05 $threshold;ViewInput05 (InputRow05 'Image');PixelInput05 (M 'ResolveImageContext').Snapshot 120
if((M 'GetImageTaskToolS89') -ne $threshold){throw 'Processed output view switched consumer.'}
M 'CloseCentralModuleConfigurationS77'|Out-Null
Write-Output 'UI03_BLOCK05_INPUT=PASS; ActualPixels200And40; Difference160To120; Threshold3072To0; DraftCancelF5Apply; MissingFirstInputNoFallback; DisabledNoOldFrame; SizeMismatchClears; SaveReload; NoMouse'
