# Real catalog recovery, favorite action and original insertion; no physical mouse/keyboard or algorithm run.
function ClickCatalog05([string]$name){$button=F $name;if(-not $button.IsVisible -or -not $button.IsEnabled){throw ('Unavailable catalog action: '+$name)};$button.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));Pump 40}
function CatalogVisuals05($root){$stack=[Collections.Generic.Stack[Windows.DependencyObject]]::new();$stack.Push($root);while($stack.Count){$v=$stack.Pop();Write-Output -NoEnumerate $v;for($i=0;$i -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($v);$i++){$stack.Push([Windows.Media.VisualTreeHelper]::GetChild($v,$i))}}}
function CatalogQuery05([string]$query){(F 'ToolSearchTextBox').Text=$query;Pump 50}
$flow=F 'flowTools';$beforeIds=@($flow|ForEach-Object ToolId)-join ','
$list=F 'ToolCatalogList';(F 'favoriteToolKinds').Clear();(F 'recentToolKinds').Clear();M 'RefreshToolCatalogView'|Out-Null
ClickCatalog05 'ToolCatalogFavoriteButton'
if((F 'ToolCatalogEmptyTitleUI05').Text -ne '还没有收藏' -or -not (F 'ToolCatalogEmptyBrowseUI05').IsVisible -or (F 'ToolCatalogEmptyClearUI05').IsVisible){throw 'Empty favorite list has no correct recovery.'}
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
    $window.Width=$size[0];$window.Height=$size[1];Pump 50
    $panel=F 'ToolCatalogEmptyStateUI05';$button=F 'ToolCatalogEmptyBrowseUI05'
    $rect=$button.TransformToAncestor($panel).TransformBounds([Windows.Rect]::new(0,0,$button.ActualWidth,$button.ActualHeight))
    if($rect.Left -lt -1 -or $rect.Right -gt $panel.ActualWidth+1 -or $rect.Bottom -gt $panel.ActualHeight+1){throw 'Empty-state recovery button clips in catalog.'}
    Shot ('empty-favorites-'+$size[0]+'x'+$size[1])
}
CatalogQuery05 'ui05-no-matching-module'
if($list.Items.Count -or (F 'ToolCatalogEmptyTitleUI05').Text -ne '未找到模块' -or -not (F 'ToolCatalogEmptyClearUI05').IsVisible -or (F 'ToolCatalogStatusText').Text -ne '搜索全部工具 · 0 项'){throw 'Global search empty state is ambiguous.'}
foreach($name in @('ToolCatalogAllButton','ToolCatalogCommonButton','ToolCatalogFavoriteButton','ToolCatalogRecentButton')){if((F $name).Background.ToString() -eq '#FF0F766E'){throw 'Global search falsely highlights a current filter.'}}
Shot 'no-match-recovery'
ClickCatalog05 'ToolCatalogEmptyClearUI05'
if((F 'toolCatalogMode') -ne 'Favorite' -or (F 'ToolCatalogEmptyTitleUI05').Text -ne '还没有收藏'){throw 'Clear-search action abandoned the original favorite mode.'}
CatalogQuery05 'NCC';$list.SelectedItem=$list.Items[0];Pump 40;$item=$list.SelectedItem
$star=@(CatalogVisuals05 $list|Where-Object {$_ -is [Windows.Controls.Button] -and [object]::ReferenceEquals($_.Tag,$item.PSObject.BaseObject)})[0]
if($null -eq $star -or -not $star.IsVisible -or -not $star.IsHitTestVisible){throw 'Selected catalog row still has no real visible favorite star.'}
$mouse=[Windows.Input.MouseButtonEventArgs]::new([Windows.Input.Mouse]::PrimaryDevice,0,[Windows.Input.MouseButton]::Left)
$mouse.RoutedEvent=[Windows.Controls.Control]::MouseDoubleClickEvent;$mouse.Source=$star
if(-not (M 'IsToolCatalogActionUI05' @($star)) -or $mouse.OriginalSource -ne $star){throw 'Favorite action event fixture does not identify its actual source.'}
M 'ToolCatalogList_MouseDoubleClick' @($list,$mouse)|Out-Null
$main.GetField('toolCatalogDragItem',$all).SetValue($window,$item.PSObject.BaseObject)
M 'ToolCatalogList_PreviewMouseLeftButtonDown' @($list,$mouse)|Out-Null
$key=[Windows.Input.KeyEventArgs]::new([Windows.Input.Keyboard]::PrimaryDevice,[Windows.PresentationSource]::FromVisual($window),0,[Windows.Input.Key]::Enter)
$key.RoutedEvent=[Windows.Input.Keyboard]::PreviewKeyDownEvent;$key.Source=$star
M 'ToolCatalogList_PreviewKeyDown' @($list,$key)|Out-Null
if($flow.Count -ne 4 -or $null -ne (F 'toolCatalogDragItem') -or $key.Handled){throw 'Favorite action double-click, press or Enter was consumed as module insertion/drag.'}
$star.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));Pump 40
if(-not (F 'favoriteToolKinds').Contains($item.Kind) -or $flow.Count -ne 4){throw 'Original star action did not favorite, or added a module.'}
ClickCatalog05 'ClearToolSearchUI07'
if((F 'toolCatalogMode') -ne 'Favorite' -or $list.Items.Count -ne 1 -or $list.Items[0].Kind.ToString() -ne 'NccTemplateMatch'){throw 'Favorite star did not feed the original favorite filter.'}
Shot 'favorite-real-star'
CatalogQuery05 'ui05-no-matching-module';ClickCatalog05 'ToolCatalogEmptyBrowseUI05'
if((F 'toolCatalogMode') -ne 'All' -or (F 'ToolSearchTextBox').Text -ne '' -or $list.Items.Count -ne (F 'toolCatalog').Count){throw 'Browse-categories action did not explicitly leave search for the complete real catalog.'}
# Choosing a filter is an intentional exit from global search, not a misleading highlighted button.
CatalogQuery05 'NCC';ClickCatalog05 'ToolCatalogRecentButton'
if((F 'ToolSearchTextBox').Text -ne '' -or $list.Items.Count -or (F 'ToolCatalogEmptyTitleUI05').Text -ne '暂无最近使用'){throw 'Choosing Recent kept an unrelated global search active.'}
CatalogQuery05 'NCC';$list.SelectedItem=$list.Items[0];M 'ToolCatalogList_MouseDoubleClick' @($null,$null)|Out-Null;Pump 50
$added=$flow[$flow.Count-1]
if($flow.Count -ne 5 -or $added.Kind.ToString() -ne 'NccTemplateMatch' -or (F 'centralModuleConfigurationOpenS77')){throw 'Original tool double-click failed to add exactly one real module.'}
ClickCatalog05 'ClearToolSearchUI07'
if((F 'toolCatalogMode') -ne 'Recent' -or @($list.Items|Where-Object Kind -eq $added.Kind).Count -ne 1){throw 'Original add did not populate Recent.'}
ClickCatalog05 'ToolCatalogSnippetButton';CatalogQuery05 'ui05-no-matching-snippet';M 'RefreshToolCatalogView'|Out-Null;Pump 30
if((F 'ToolCatalogEmptyTitleUI05').Text -ne '未找到片段' -or (F 'ToolCatalogEmptyBrowseUI05').Content -ne '返回工具' -or (F 'ToolCatalogStatusText').Text -ne '搜索片段 · 0 项'){throw 'Global refresh replaced snippet empty/search presentation.'}
ClickCatalog05 'ToolCatalogEmptyClearUI05'
if(-not (F 'toolboxSnippetModeS71') -or (F 'ToolSearchTextBox').Text -ne ''){throw 'Clearing snippet search changed modes.'}
CatalogQuery05 'ui05-no-matching-snippet';ClickCatalog05 'ToolCatalogEmptyBrowseUI05'
if((F 'toolboxSnippetModeS71') -or (F 'toolCatalogMode') -ne 'Recent'){throw 'Snippet recovery lost the previous actual filter.'}
# Contextual source tasks cannot be dismissed by generic recovery, and do not change real bindings.
$target=$flow[1];$oldBinding=$target.GetInputBinding('Image');$target.RemoveInputBinding('Image')
$definition=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
if(-not (M 'EnterInputSourceContextS68' @($target.ToolId,'Image'))){throw 'Missing-input context fixture was not accepted by original guard.'};CatalogQuery05 'ui05-no-matching-module'
if((F 'ToolCatalogEmptyBrowseUI05').IsVisible){throw 'Generic browse leaks out of input source context.'}
ClickCatalog05 'ToolCatalogEmptyClearUI05'
if((F 'inputSourceTargetToolIdS68') -ne $target.ToolId -or (F 'toolboxSnippetModeS71')){throw 'Clear search lost the active source task.'}
M 'ExitInputSourceContextS68' @($false)|Out-Null
M 'EnterToolRecommendationContextS67' @($flow[0].ToolId,'Image')|Out-Null;CatalogQuery05 'ui05-no-matching-module'
if((F 'ToolCatalogEmptyBrowseUI05').IsVisible){throw 'Generic browse leaks out of recommendation context.'}
ClickCatalog05 'ToolCatalogEmptyClearUI05'
if((F 'recommendationSourceToolIdS67') -ne $flow[0].ToolId){throw 'Clear search lost original port recommendation.'}
M 'ExitToolRecommendationContextS67' @($false)|Out-Null
if($service.GetRecipeFingerprint((M 'CaptureRecipe')) -ne $definition){throw 'Read-only context recovery mutated applied bindings or recipe.'}
if($null -ne $oldBinding){$target.SetInputBinding('Image',$oldBinding.SourceToolId,$oldBinding.SourcePortName)}
$savedPath=Join-Path $temp 'catalog05.json';$service.SaveRecipe($savedPath,(M 'CaptureRecipe'));$saved=$service.LoadRecipe($savedPath);$savedFlow=@($saved.Flows|Where-Object FlowId -eq $saved.ActiveFlowId)[0]
if($savedFlow.Tools.Count -ne 5 -or @($savedFlow.Tools|Where-Object ToolId -eq $added.ToolId).Count -ne 1 -or (@($flow|Select-Object -First 4|ForEach-Object ToolId)-join ',') -ne $beforeIds -or @($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count){throw 'Catalog recovery/add/save changed original identity or ran algorithms.'}
if($window.IsActive -or $window.ShowActivated -or $window.Left -gt -10000){throw 'Catalog test activated foreground.'}
Write-Output 'UI05_CATALOG_RECOVERY=PASS; ThreeSizes; ActionableEmptyStates; ExplicitGlobalSearch; ClearPreservesMode; FilterExitsSearch; RealFavoriteStar; StarDoesNotDragOrAdd; RealNccAddAndSchema3Save; SnippetRecovery; SourceAndRecommendationKept; NoAutoRun; NoMouse'
