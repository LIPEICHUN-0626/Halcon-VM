# Reuse the genuine multi-input/draft/save chain; then exercise its original view controls.
function RefreshView06 {M 'RefreshUiState'|Out-Null;Pump 70}
function BoundsView06($element,$root){$element.TransformToAncestor($root).TransformBounds([Windows.Rect]::new(0,0,$element.ActualWidth,$element.ActualHeight))}
function ClickView06([string]$name){(F $name).RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));Pump 60}
function MenuView06([string]$name){(F $name).RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.MenuItem]::ClickEvent));Pump 60}
function PartView06($view){@('row1','column1','row2','column2')|ForEach-Object {$view.GetType().GetField($_,$private).GetValue($view)}}
M 'SetDualImageMode' @($true)|Out-Null;RefreshView06
foreach($name in @('DualLeftFitMenuUI06','DualLeftOriginalMenuUI06','DualRightFitMenuUI06','DualRightOriginalMenuUI06')){if((F $name).IsEnabled){throw 'Empty view exposes a no-op scale action.'}}
M 'SetDualImageMode' @($false)|Out-Null
. (Join-Path $PSScriptRoot 'UIBatch03InputImages.ps1')
(F 'FlowToolList').SelectedItem=$diff;OpenInput05 $diff;M 'SetDualImageMode' @($true)|Out-Null
(F 'DualInputPortComboBox').SelectedValue='Source:'+$b.ToolId
M 'SetImageContextSelection' @(('Source:'+$c.ToolId))|Out-Null
# The internal selection helper suppresses UI events; explicitly route the original selection event, as a user selection does.
M 'ImageContextComboBox_SelectionChanged' @((F 'ImageContextComboBox'),$null)|Out-Null;RefreshView06
PixelInput05 (M 'ResolveDualInputImageContext').Snapshot 40;PixelInput05 (M 'ResolveImageContext').Snapshot 80
$versions=@($flow|ForEach-Object RuntimeOutputVersion)-join ','
$definition=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
$rightKey=(F 'ImageContextComboBox').SelectedValue;$leftKey=(F 'DualInputPortComboBox').SelectedValue
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
    $window.Width=$size[0];$window.Height=$size[1];RefreshView06
    if((F 'ImageSourceBarUI04').Parent -ne (F 'DualRightHeaderContentUI06') -or (F 'ImageFitButton').IsVisible -or (F 'ImageOneToOneButton').IsVisible -or (F 'ImagePixelStatusText').IsVisible){throw 'Dual sources are not local or duplicate global view actions remain.'}
    foreach($pair in @(@('DualInputPortComboBox','DualLeftImagePanel'),@('ImageContextComboBox','DualRightImagePanel'),@('DualLeftViewMenuUI06','DualLeftImagePanel'),@('DualRightViewMenuUI06','DualRightImagePanel'),@('DualLeftMaximizeButton','DualLeftImagePanel'),@('DualRightMaximizeButton','DualRightImagePanel'))){
        $el=F $pair[0];$root=F $pair[1];$bounds=BoundsView06 $el $root
        if(-not $el.IsVisible -or $bounds.Left -lt -1 -or $bounds.Right -gt $root.ActualWidth+1 -or $bounds.Width -lt 30){throw ('Dual view element clipped: '+$pair[0])}
    }
    if((F 'DualInputPortComboBox').ActualWidth -lt 150 -or (F 'ImageContextComboBox').ActualWidth -lt 150 -or (F 'DualLeftHeaderUI06').ActualHeight -gt 62 -or (F 'DualRightHeaderBorder').ActualHeight -gt 62){throw 'Dual header is narrow or still contains repeated detail rows.'}
    if((F 'ImageContextPickerDetailUI06').Text -notlike ('*'+$c.InstanceName+'*') -or (F 'DualLeftSourceText').Text -notlike ('*'+$b.InstanceName+'*')){throw 'Actual selected source identity missing from full hover.'}
    Shot ('dual-local-sources-'+$size[0]+'x'+$size[1])
}
$window.Width=1366;$window.Height=768;RefreshView06
(F 'LinkDualImageViewsCheckBox').IsChecked=$false;RefreshView06
$rightPart=PartView06 (F 'viewport')
MenuView06 'DualLeftOriginalMenuUI06'
if((F 'comparisonViewport').ZoomText -notlike '*100%*' -or ((PartView06 (F 'viewport'))-join ',') -ne ($rightPart-join ',')){
    Write-Output ('LEFT_ZOOM='+(F 'comparisonViewport').ZoomText+'; LEFT_PART='+((PartView06 (F 'comparisonViewport'))-join ',')+'; RIGHT_BEFORE='+($rightPart-join ',')+'; RIGHT_AFTER='+((PartView06 (F 'viewport'))-join ',')+'; LINK='+ (M 'AreDualImageViewsLinked'))
    throw 'Left original-size command did not produce 1:1 or changed the independent right view.'
}
MenuView06 'DualLeftFitMenuUI06'
MenuView06 'DualRightOriginalMenuUI06'
if((F 'viewport').ZoomText -notlike '*100%*'){throw 'Right original-size menu is not wired to the existing viewport.'}
(F 'LinkDualImageViewsCheckBox').IsChecked=$true;MenuView06 'DualRightFitMenuUI06'
$lp=PartView06 (F 'comparisonViewport');$rp=PartView06 (F 'viewport')
for($i=0;$i -lt 4;$i++){if([Math]::Abs($lp[$i]-$rp[$i]) -gt 0.00001){throw 'Original linked view no longer synchronizes matching image sizes.'}}
foreach($name in @('DualLeftMaximizeButton','DualRightMaximizeButton')){
    ClickView06 $name
    if((F $name).Content -ne '恢复'){throw 'Maximize action did not expose restore.'}
    ClickView06 $name
    if(-not (F 'DualLeftImagePanel').IsVisible -or -not (F 'DualRightImagePanel').IsVisible){throw 'Restore lost a pane.'}
}
(F 'DockImageDifferenceMultiplierTextBox').Text='2'
if(-not (F 'dockDraftDirty')){throw 'Draft fixture unavailable.'}
M 'SetDualImageMode' @($false)|Out-Null;RefreshView06
if((F 'ImageSourceBarUI04').Parent -ne (F 'ImageWorkspaceHeaderContentUI06') -or -not (F 'ImageFitButton').IsVisible -or -not (F 'ImageOneToOneButton').IsVisible -or -not (F 'dockDraftDirty')){throw 'Single view failed to restore source/actions or discarded draft.'}
M 'SetDualImageMode' @($true)|Out-Null;RefreshView06
if((F 'ImageContextComboBox').SelectedValue -ne $rightKey -or (F 'DualInputPortComboBox').SelectedValue -ne $leftKey){throw 'Switching layout silently changed explicit sources.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null;M 'CloseCentralModuleConfigurationS77'|Out-Null
# Read-only history UI fixture with genuine snapshot copies and the original return path; not archive-file navigation coverage.
(F 'historicalLiveViewport').CopyFrom((F 'viewport'));(F 'historicalLiveComparisonViewport').CopyFrom((F 'comparisonViewport'))
$main.GetField('historicalLiveDualImageMode',$all).SetValue($window,$true)
$main.GetField('historicalLiveLinkedViews',$all).SetValue($window,$true)
$main.GetField('historicalSourceImage',$all).SetValue($window,(M 'ResolveDualInputImageContext').Snapshot.CreateImageCopy())
$main.GetField('historicalResultImage',$all).SetValue($window,(M 'ResolveImageContext').Snapshot.CreateImageCopy())
$main.GetField('historicalImageMode',$all).SetValue($window,$true)
M 'UpdateDualImageViewLayout'|Out-Null;RefreshView06
if((F 'ImageContextComboBox').IsVisible -or (F 'DualInputPortComboBox').IsVisible -or (F 'ImageContextSourceText').Text -notlike '*只读*'){throw 'History displays editable live sources.'}
M 'ReturnLiveImageButton_Click' @($null,$null)|Out-Null;RefreshView06
if((F 'historicalImageMode') -or -not (F 'ImageContextComboBox').IsVisible -or -not (F 'DualInputPortComboBox').IsVisible -or (F 'ImageContextComboBox').SelectedValue -ne $rightKey){throw 'Original history return did not restore live source controls.'}
if((@($flow|ForEach-Object RuntimeOutputVersion)-join ',') -ne $versions -or $service.GetRecipeFingerprint((M 'CaptureRecipe')) -ne $definition){throw 'Display changes ran tools or changed recipe.'}
M 'SetDualImageMode' @($false)|Out-Null;RefreshView06;Shot 'single-source-restored'
if($window.IsActive -or $window.ShowActivated){throw 'View test activated desktop.'}
Write-Output 'UI05_DUAL_VIEWS=PASS; ThreeSizes; LocalSourceSelectors; BoundedTwoRowHeaders; OriginalMenusFit1to1; IndependentAndLinkedViews; MaximizeRestore; DraftKept; ExplicitSourcesKept; HistoryFixtureReturn; ActualMultiInputDraftAndSaveChain; NoMouse; NativeMenuPlacementNotTested'
