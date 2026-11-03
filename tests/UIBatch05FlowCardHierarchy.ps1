# Flow visual hierarchy and actual output/failure states; no physical input or external communication.
function CardVisuals05($root){$s=[Collections.Generic.Stack[Windows.DependencyObject]]::new();$s.Push($root);while($s.Count){$v=$s.Pop();Write-Output -NoEnumerate $v;for($i=0;$i -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($v);$i++){$s.Push([Windows.Media.VisualTreeHelper]::GetChild($v,$i))}}}
function CardPart05($tool,[string]$name){$root=if($name -like 'List*' -or $name -eq 'ToolCard'){F 'FlowToolList'}else{F 'FlowGraphNodeItemsControl'};Write-Output -NoEnumerate (@(CardVisuals05 $root|Where-Object {$_.Name -eq $name -and [object]::ReferenceEquals($_.DataContext,$tool.PSObject.BaseObject)})[0])}
function CardPositions05{(@($flow|ForEach-Object {$_.ToolId+'='+$_.GraphX+','+$_.GraphY})-join ';')}
function IdleCard05{$end=[DateTime]::UtcNow.AddSeconds(20);while((F 'isFlowExecutionActive') -and [DateTime]::UtcNow -lt $end){Pump 40};if(F 'isFlowExecutionActive'){M 'StopRunButton_Click' @($null,$null)|Out-Null;throw 'Actual card fixture run did not complete.'}}
$flow=F 'flowTools';$source=$flow[0];$threshold=$flow[1];$objects=$flow[2];$judge=$flow[3]
$ids=@($flow|ForEach-Object ToolId)-join ','
M 'EnableManualFlowLayout'|Out-Null;$threshold.GraphX+=12;$threshold.GraphY+=18
(F 'FlowToolList').SelectedItem=$threshold;M 'RefreshUiState'|Out-Null;Pump 60
$positions=CardPositions05;$definition=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
    $window.Width=$size[0];$window.Height=$size[1];Pump 50
    foreach($tool in @($source,$objects)){
        $card=CardPart05 $tool 'GraphToolCard';$accent=CardPart05 $tool 'GraphRoleAccentUI05'
        if($card.BorderThickness.Left -ne 1 -or -not $accent.IsVisible -or $accent.IsHitTestVisible -or $accent.Width -ne 3){throw 'Unselected dependencies still use dominant frames or obstruct input.'}
    }
    $selected=CardPart05 $threshold 'GraphToolCard'
    if($selected.BorderThickness.Left -ne 2 -or $selected.BorderBrush.ToString() -ne '#FF2563EB'){throw 'Actual selection is not primary.'}
    foreach($name in @('S75InputAnchor','S75OutputAnchor')){$anchor=CardPart05 $threshold $name;if($anchor.Width -ne 18 -or $anchor.Height -ne 16){throw 'Visual cleanup enlarged the connection hit region.'}}
    if((CardPositions05) -ne $positions -or @($flow|Where-Object {$_.GraphHeight -ne 52}).Count){throw 'Styling or resizing moved/expanded fixed cards.'}
    Shot ('card-hierarchy-'+$size[0]+'x'+$size[1])
}
# Explicit presentation fixture: independent fault/NG, selection, pointer and breakpoint indicators.
$source.ResultCode='OK';$source.RunStatus='完成'
$objects.ResultCode='NG';$objects.RunStatus='完成';$objects.ErrorMessage=''
$threshold.ResultCode='NG';$threshold.RunStatus='失败';$threshold.ErrorMessage='UI05 fixture failure with full diagnostic'
$threshold.IsBreakpoint=$true;$threshold.IsExecutionPointer=$true;Pump 50
if((CardPart05 $source 'GraphStatusBadgeUI05').Background.ToString() -ne '#FFDCFCE7' -or (CardPart05 $source 'GraphToolCard').BorderThickness.Left -ne 1){throw 'OK status is not quiet and local to its badge.'}
if((CardPart05 $objects 'GraphStatusTextUI05').Text -ne 'NG' -or (CardPart05 $threshold 'GraphStatusTextUI05').Text -ne '异常' -or $threshold.ResultCode -ne 'NG'){throw 'Presentation confused business NG with failure or rewrote formal result code.'}
if((CardPart05 $threshold 'GraphToolCard').BorderBrush.ToString() -ne '#FF2563EB' -or -not (CardPart05 $threshold 'GraphPointerMarkUI05').IsVisible -or -not (CardPart05 $threshold 'GraphBreakpointDotUI05').IsVisible -or (CardPart05 $threshold 'GraphStatusBadgeUI05').ToolTip -notlike '*full diagnostic*'){throw ('Card state mismatch: Border='+(CardPart05 $threshold 'GraphToolCard').BorderBrush+'; Pointer='+(CardPart05 $threshold 'GraphPointerMarkUI05').IsVisible+'; Breakpoint='+(CardPart05 $threshold 'GraphBreakpointDotUI05').IsVisible+'; Tooltip='+(CardPart05 $threshold 'GraphStatusBadgeUI05').ToolTip)}
$threshold.IsFlowDetailExpanded=$true;Pump 50
if((CardPart05 $threshold 'CompactNodeSummary').IsVisible -or -not (CardPart05 $threshold 'GraphBreakpointDotUI05').IsVisible){throw 'Expanded ports hid the only breakpoint indicator.'}
$threshold.IsFlowDetailExpanded=$false
$objects.IsTaskLensCompactS76=$true;$objects.RunStatus='失败';Pump 50
if(-not (CardPart05 $objects 'GraphCompactStatusBadgeUI05').IsVisible -or (CardPart05 $objects 'GraphCompactStatusTextUI05').Text -ne '异常'){throw 'Compact status style masked failure.'}
$objects.IsTaskLensCompactS76=$false;Shot 'independent-card-state-fixture'
M 'SetFlowEditorView' @($false)|Out-Null;Pump 50
if((CardPart05 $threshold 'ListStatusTextUI05').Text -ne '异常' -or (CardPart05 $threshold 'ToolCard').BorderBrush.ToString() -ne '#FF2563EB'){throw 'Original list view lost selection or failure status.'}
M 'SetFlowEditorView' @($true)|Out-Null
foreach($tool in $flow){$tool.ResultCode='--';$tool.RunStatus='未运行';$tool.ErrorMessage='';$tool.IsBreakpoint=$false;$tool.IsExecutionPointer=$false;$tool.IsFlowDetailExpanded=$false;$tool.IsTaskLensCompactS76=$false}
M 'RefreshUiState'|Out-Null;Pump 40
if((CardPositions05) -ne $positions -or $service.GetRecipeFingerprint((M 'CaptureRecipe')) -ne $definition -or @($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count){throw 'Pure visual state navigation changed definition/positions or executed tools.'}
# Actual HALCON values and an ordinary business NG, rather than fixture text as algorithm proof.
$path=Join-Path $temp 'cards05.png';[HalconDotNet.HObject]$im=$null
try{[HalconDotNet.HOperatorSet]::GenImageConst([ref]$im,'byte',64,48);[HalconDotNet.HOperatorSet]::WriteImage($im,'png',0,$path)}finally{if($im){$im.Dispose()}}
$source.Parameters.ImageSourceModeS114='File';$source.Parameters.LocalImagePath=$path
$threshold.Parameters.ImageThresholdMinGray=0;$threshold.Parameters.ImageThresholdMaxGray=255
$judge.NumericOperator='BetweenInclusive';$judge.NumericLowerLimit=2;$judge.NumericUpperLimit=2
(F 'AutoSendMatchResultCheckBox').IsChecked=$false;(F 'StopOnNgCheckBox').IsChecked=$false
foreach($tool in $flow){M 'RunStandaloneToolCoreUI02' @($tool,'UI05 actual card output',$false)|Out-Null};Pump 50
[double]$area=0
if(-not $threshold.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 3072 -or $judge.ResultCode -ne 'NG' -or $judge.FlowHasErrorUI05 -or (CardPart05 $judge 'GraphStatusTextUI05').Text -ne 'NG'){throw 'Actual values/business NG do not match their card badges.'}
# The original inline execution boundary throws without a dialog and publishes real failure state.
$source.Parameters.LocalImagePath=Join-Path $temp 'missing-card-source.png';$failed=$false
try{M 'RunStandaloneToolCoreUI02' @($source,'UI05 missing source',$false)|Out-Null}catch{$failed=$true}
M 'RefreshUiState'|Out-Null;(F 'FlowToolList').SelectedItem=$source;Pump 50
if((F 'FlowRunSelectedButtonS89').Content -notlike ('*#'+$source.SequenceText)){throw 'Selected-run caption retained the previous module after direct selection.'}
if(-not $failed -or -not $source.FlowHasErrorUI05 -or [string]::IsNullOrWhiteSpace($source.ErrorMessage) -or (CardPart05 $source 'GraphStatusTextUI05').Text -ne '异常' -or (CardPart05 $source 'GraphToolCard').BorderBrush.ToString() -ne '#FF2563EB'){throw 'Real selected failure was masked by selection.'}
Shot 'actual-selected-failure'
$source.Parameters.LocalImagePath=$path;M 'RunStandaloneToolCoreUI02' @($source,'UI05 source repair',$false)|Out-Null
(F 'FlowToolList').SelectedItem=$threshold;M 'DebugStepButton_Click' @($null,$null)|Out-Null
$end=[DateTime]::UtcNow.AddSeconds(15);while(-not (F 'isPauseRequested') -and [DateTime]::UtcNow -lt $end){Pump 40}
if(-not (F 'isPauseRequested') -or -not $objects.IsExecutionPointer -or -not (CardPart05 $objects 'GraphPointerMarkUI05').IsVisible -or (CardPart05 $objects 'GraphToolCard').BorderBrush.ToString() -ne '#FFF59E0B'){throw 'Real paused next-execution card is not distinguishable.'}
Shot 'actual-paused-pointer';M 'StopRunButton_Click' @($null,$null)|Out-Null;IdleCard05
if((CardPositions05) -ne $positions -or (@($flow|ForEach-Object ToolId)-join ',') -ne $ids){throw 'Actual execution visualization moved nodes or reordered workflow.'}
$savedPath=Join-Path $temp 'cards05.json';$service.SaveRecipe($savedPath,(M 'CaptureRecipe'));$loaded=$service.LoadRecipe($savedPath);$savedFlow=@($loaded.Flows|Where-Object FlowId -eq $loaded.ActiveFlowId)[0]
foreach($tool in $flow){$saved=@($savedFlow.Tools|Where-Object ToolId -eq $tool.ToolId)[0];if($saved.GraphX -ne $tool.GraphX -or $saved.GraphY -ne $tool.GraphY){throw 'Manual positions changed during original schema save.'}}
if((@($savedFlow.Tools|ForEach-Object ToolId)-join ',') -ne $ids -or $window.IsActive -or $window.ShowActivated){throw 'Saved workflow order or non-activated UI boundary failed.'}
Write-Output 'UI05_FLOW_CARD_HIERARCHY=PASS; ThreeSizes; QuietDependencies; SelectionPriority; IndependentStateIndicators; CompactAndList; ActualArea3072AndBusinessNG; RealSelectedFailure; RealPausedPointer; ManualPositionsAndOrderSaved; SmallAnchors; NoMouse'
