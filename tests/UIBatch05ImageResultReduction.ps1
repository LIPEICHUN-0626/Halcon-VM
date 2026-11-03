# Image/result subtraction; only original controls and explicit tiny HALCON executions.
function RefreshImage05 { M 'RefreshInspector'|Out-Null; M 'RefreshUiState'|Out-Null; Pump 60 }
function BoundsImage05($element,$root){$element.TransformToAncestor($root).TransformBounds([Windows.Rect]::new(0,0,$element.ActualWidth,$element.ActualHeight))}
$flow=F 'flowTools';$source=$flow[0];$threshold=$flow[1]
# Settle the original lazy result-layer style registration before comparing presentation-only changes.
RefreshImage05
$definition=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
(F 'FlowToolList').SelectedItem=$null;RefreshImage05
if((F 'SelectedResultDetailsUI04').IsVisible -or (F 'SelectedResultOutputText').IsVisible -or (F 'SelectedResultStatusText').IsVisible -or (F 'SelectedResultOriginTextS88').IsVisible){throw 'Empty selection still exposes duplicate placeholder rows.'}
(F 'FlowToolList').SelectedItem=$threshold;RefreshImage05
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
    $window.Width=$size[0];$window.Height=$size[1];Pump 60
    if((F 'SelectedResultOutputText').IsVisible -or (F 'SelectedResultOriginTextS88').IsVisible -or -not (F 'SelectedResultStatusText').IsVisible){throw 'Pending module should have one run status, not repeated empty output/origin.'}
    if((F 'SelectedResultCardUI04').ActualHeight -gt 34){throw 'Pending result card is not a single compact row.'}
    if((F 'FlowRunSummaryBarUI05').ActualHeight -gt 30 -or (F 'ImageStatusBarUI05').ActualHeight -gt 28 -or (F 'FlowRunRangeText').IsVisible -or (F 'FlowRunResultText').IsVisible){throw 'Routine overview still occupies duplicate rows or shows an empty result badge.'}
    if(-not (F 'ImageDisplayPanelUI05').IsAncestorOf((F 'OverlayStatusText')) -or -not (F 'ImageDisplayPanelUI05').IsAncestorOf((F 'RoiOverlayLegendText'))){throw 'Layer counters were removed instead of placed by original display controls.'}
    $status=F 'ImageStatusBarUI05';$message=BoundsImage05 (F 'HeaderStatusText') $status;$pixel=BoundsImage05 (F 'ImagePixelStatusText') $status
    if($message.Width -lt 150 -or $message.Right -gt $pixel.Left+1 -or $pixel.Right -gt $status.ActualWidth+1){throw 'Single-row status overlaps or clips.'}
    Shot ('image-reduced-pending-'+$size[0]+'x'+$size[1])
}
$afterDefinition=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
if($definition -ne $afterDefinition){
    $index=0;while($index -lt [Math]::Min($definition.Length,$afterDefinition.Length) -and $definition[$index] -eq $afterDefinition[$index]){$index++}
    Write-Output ('DEFINITION_DIFF_AT='+$index+'; BEFORE='+$definition.Substring([Math]::Max(0,$index-60),[Math]::Min(180,$definition.Length-[Math]::Max(0,$index-60)))+'; AFTER='+$afterDefinition.Substring([Math]::Max(0,$index-60),[Math]::Min(180,$afterDefinition.Length-[Math]::Max(0,$index-60))))
    throw 'Presentation changes modified recipe.'
}
if(@($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count){throw 'Presentation changes executed tools.'}
$range=F 'FlowRunRangeText';$range.Text='范围：测试所选模块'
if(-not (F 'FlowRunStatusText').ToolTip.Content.Children.Contains($range)){throw 'Original execution range is not available on hover.'}
# Reuse the real 3072 result, original details route, draft/cancel, missing source and bounded error checks.
. (Join-Path $PSScriptRoot 'UIBatch04ImageResults.ps1')
(F 'FlowToolList').SelectedItem=$source
M 'SetImageContextSelection' @(('Source:'+$source.ToolId))|Out-Null;RefreshImage05
if((F 'ImageAcquisitionBarS115').IsVisible -or (F 'ImageContextDetailText').Text -notmatch '\d{4}-\d{2}-\d{2}' -or (F 'ImageSourceBarUI04').ActualHeight -gt 37){throw 'Healthy acquisition still consumes a row or lost complete diagnostic hover.'}
$goodPath=$source.Parameters.LocalImagePath
$source.Parameters.LocalImagePath=Join-Path $temp 'missing-source.png'
$failed=$false;try {M 'RunStandaloneToolCoreUI02' @($source,'UI05 actual missing source',$false)|Out-Null} catch {$failed=$true}
RefreshImage05
if(-not $failed -or -not (F 'ImageAcquisitionBarS115').IsVisible -or -not (F 'ImageAcquisitionRepairS115').IsVisible -or (M 'ResolveImageContext').HasImage){throw 'Actual source failure was hidden or retained an old image.'}
Shot 'image-reduced-source-error'
$source.Parameters.LocalImagePath=$goodPath
M 'RunStandaloneToolCoreUI02' @($source,'UI05 source recovery',$false)|Out-Null
M 'RunStandaloneToolCoreUI02' @($threshold,'UI05 result recovery',$false)|Out-Null
(F 'FlowToolList').SelectedItem=$threshold;RefreshImage05
if((F 'ImageAcquisitionBarS115').IsVisible -or -not (F 'SelectedResultOutputText').IsVisible -or (F 'SelectedResultOutputText').Text -notlike '*3072*' -or -not (F 'SelectedResultOriginTextS88').IsVisible){throw 'Recovered source or genuine selected result failed to restore.'}
M 'SetImageContextSelection' @('ModuleInput')|Out-Null;RefreshImage05
if((F 'ImageAcquisitionSummaryS115').ToolTip -ne $null -or (F 'ImageContextDetailText').Text -like '*采集：*'){throw 'Switching away from a source retained its acquisition details.'}
$save=Join-Path $temp 'image05.json';$service.SaveRecipe($save,(M 'CaptureRecipe'));$loaded=$service.LoadRecipe($save)
$savedFlow=@($loaded.Flows|Where-Object FlowId -eq $loaded.ActiveFlowId)[0]
if((@($savedFlow.Tools|ForEach-Object ToolId)-join ',') -ne (@($flow|ForEach-Object ToolId)-join ',')){throw 'Presentation changes lost saved module identities.'}
Shot 'image-reduced-actual'
if($window.IsActive -or $window.ShowActivated){throw 'Test activated desktop.'}
Write-Output 'UI05_IMAGE_REDUCTION=PASS; ThreeSizes; SingleStatusRow; PendingRowsRemoved; OriginalLayerCountsInDisplay; FlowRangeOnHover; TypedArea3072; DraftNotApplied; EditorOwnership; HealthyAcquisitionOnHover; ActualMissingSourceAndRecovery; NoStaleAcquisitionDetails; SaveIdentity; NoMouse'
