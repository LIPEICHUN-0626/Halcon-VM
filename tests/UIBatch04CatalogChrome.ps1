# Catalog-only UI and real add/save actions; no algorithm or physical input.
function CatalogBounds07($element,$root){$element.TransformToAncestor($root).TransformBounds([Windows.Rect]::new(0,0,$element.ActualWidth,$element.ActualHeight))}
function ClickCatalog07([string]$name){(F $name).RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));Pump 60}
$list=F 'ToolCatalogList';$flow=F 'flowTools';$originalIds=@($flow|ForEach-Object ToolId)-join ','
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
    $window.Width=$size[0];$window.Height=$size[1];Pump 70
    $groups=@(CategoryExpanders)
    if($groups.Count -ne 10){throw 'Original ten categories missing.'}
    foreach($group in $groups){$r=CatalogBounds07 $group $list;if($r.Top -lt 0 -or $r.Top+28 -gt $list.ActualHeight+1){throw 'Category no longer discoverable on first screen.'}}
    $modes=F 'ToolCatalogModePanel'
    Write-Output ('CATALOG_HEADER='+ (F 'ToolCatalogTitleUI07').Visibility+'; Width='+(F 'ToolCatalogTitleUI07').ActualWidth+'; Height='+(F 'ToolCatalogTitleUI07').ActualHeight+'; Text='+(F 'ToolCatalogTitleUI07').Text)
    Write-Output ('CATALOG_TITLE_BOUNDS='+(CatalogBounds07 (F 'ToolCatalogTitleUI07') (F 'ToolCatalogHeaderUI07'))+'; Header='+(F 'ToolCatalogHeaderUI07').RenderSize+'; Color='+(F 'ToolCatalogTitleUI07').Foreground+'; Opacity='+(F 'ToolCatalogTitleUI07').Opacity)
    if(-not (F 'ToolCatalogTitleUI07').IsVisible -or (F 'ToolCatalogTitleUI07').ActualWidth -lt 70){throw 'Catalog title missing.'}
    $header=F 'ToolCatalogHeaderUI07';$titleBounds=CatalogBounds07 (F 'ToolCatalogTitleUI07') $header;$snippetBounds=CatalogBounds07 (F 'ToolCatalogSnippetButton') $header
    if($titleBounds.Left -lt 0 -or $titleBounds.Right -gt $snippetBounds.Left+1 -or $titleBounds.Bottom -gt $header.ActualHeight+1){throw 'Header title clipped or overlapped snippet.'}
    if($modes.ColumnDefinitions.Count -ne 4){throw 'Catalog mode row still crowded with snippet action.'}
    foreach($name in @('ToolCatalogAllButton','ToolCatalogCommonButton','ToolCatalogFavoriteButton','ToolCatalogRecentButton')){
        $r=CatalogBounds07 (F $name) $modes
        if($r.Left -lt 0 -or $r.Right -gt $modes.ActualWidth+1 -or $r.Width -lt 45){throw ('Clipped catalog mode '+$name+'; Bounds='+$r+'; PanelWidth='+$modes.ActualWidth)}
    }
    if(-not (F 'ToolCatalogSnippetButton').IsVisible -or (F 'ToolCatalogCapabilityText').IsVisible -or (F 'ClearToolSearchUI07').IsVisible){throw 'Header/search empty state invalid.'}
    Shot ('catalog-'+$size[0]+'x'+$size[1])
}
$window.Width=1366;$window.Height=768
$expanded=@(CategoryExpanders|Where-Object {-not $_.IsExpanded})[0];$category=$expanded.DataContext.Name;$expanded.IsExpanded=$true;Pump 30
(F 'ToolSearchTextBox').Text='NCC';Pump 60
if($list.Items.Count -ne 1 -or -not (F 'ClearToolSearchUI07').IsVisible -or (F 'ToolCatalogStatusText').Text -ne '搜索结果 · 1 项'){throw 'Actual search / short status / clear entry failed.'}
Shot 'catalog-search'
ClickCatalog07 'ClearToolSearchUI07'
if((F 'ToolSearchTextBox').Text -ne '' -or (F 'ClearToolSearchUI07').IsVisible -or -not (@(CategoryExpanders)|Where-Object {$_.DataContext.Name -eq $category}).IsExpanded){throw 'Clear did not restore original category expansion.'}
(F 'ToolSearchTextBox').Text='NCC';Pump 40;$list.SelectedItem=$list.Items[0]
M 'ToolCatalogList_MouseDoubleClick' @($null,$null)|Out-Null;Pump 50
$added=$flow[$flow.Count-1]
if($flow.Count -ne 5 -or $added.Kind.ToString() -ne 'NccTemplateMatch' -or (F 'centralModuleConfigurationOpenS77') -or @($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count){throw 'Original add did not add exactly one real module without running/opening editor.'}
M 'ToolCatalogToggleFavoriteMenuItem_Click' @($null,$null)|Out-Null
ClickCatalog07 'ClearToolSearchUI07';ClickCatalog07 'ToolCatalogFavoriteButton'
if(@($list.Items|Where-Object Kind -eq $added.Kind).Count -ne 1){throw 'Original favorite filtering lost the added tool.'}
(F 'ToolSearchTextBox').Text='no-such-tool-unique-fixture';Pump 40
if($list.Items.Count -ne 0 -or -not (F 'ToolCatalogEmptyText').IsVisible){throw 'No-result search not explained.'}
ClickCatalog07 'ClearToolSearchUI07'
if((F 'toolCatalogMode') -ne 'Favorite' -or @($list.Items|Where-Object Kind -eq $added.Kind).Count -ne 1){throw 'Search clear lost favorite mode.'}
ClickCatalog07 'ToolCatalogRecentButton'
if(@($list.Items|Where-Object Kind -eq $added.Kind).Count -ne 1){throw 'Recent did not include original add.'}
ClickCatalog07 'ToolCatalogSnippetButton'
if(-not (F 'toolboxSnippetModeS71') -or (F 'ToolCatalogSnippetButton').Content -ne '返回' -or (F 'ToolCatalogStatusText').Text.Length -gt 30){throw 'Moved snippet entry lost mode/return or kept verbose status.'}
ClickCatalog07 'ToolCatalogSnippetButton'
if((F 'toolboxSnippetModeS71') -or (F 'toolCatalogMode') -ne 'Recent'){throw 'Snippet return did not preserve the original catalog filter.'}
ClickCatalog07 'ToolCatalogCommonButton';ClickCatalog07 'ToolCatalogAllButton'
$list.SelectedItem=@((F 'toolCatalog')|Where-Object Kind -eq $added.Kind)[0]
M 'ToolCatalogShowDescriptionMenuItem_Click' @($null,$null)|Out-Null;Pump 40
if(-not (F 'ToolCapabilityExpander').IsExpanded -or (F 'ToolCapabilityInputText').Text -notlike '*Image*'){throw 'Real module explanation / input preview lost.'}
Shot 'catalog-details'
(F 'ToolCapabilityExpander').IsExpanded=$false
$path=Join-Path $temp 'catalog-ui07.json';$service.SaveRecipe($path,(M 'CaptureRecipe'));$saved=$service.LoadRecipe($path)
$savedFlow=@($saved.Flows|Where-Object FlowId -eq $saved.ActiveFlowId)[0]
if(@($savedFlow.Tools|Where-Object ToolId -eq $added.ToolId).Count -ne 1 -or $savedFlow.Tools.Count -ne 5){throw 'Real added module did not survive original schema save.'}
if((@($flow|Select-Object -First 4|ForEach-Object ToolId)-join ',') -ne $originalIds -or $window.IsActive){throw 'Catalog navigation changed original workflow identity / foreground.'}
Write-Output 'UI04_CATALOG_CHROME=PASS; ThreeSizes; TenCategories; FourModes; ClearRestoresCategoryAndFavorite; GenuineNccAddAndSave; Recent; SnippetReturn; RealPortPreview; NoAlgorithm; NoMouse'
