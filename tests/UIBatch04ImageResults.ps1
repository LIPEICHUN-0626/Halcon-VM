# Focused image/result presentation test. No physical input; only one tiny source/threshold display fixture.
function RefreshDisplay04 { M 'RefreshInspector'|Out-Null;M 'RefreshUiState'|Out-Null;Pump 60 }
function ViewDisplay04([string]$key){M 'SetImageContextSelection' @($key)|Out-Null;RefreshDisplay04}
function BoundsDisplay04($element,$root){$element.TransformToAncestor($root).TransformBounds([Windows.Rect]::new(0,0,$element.ActualWidth,$element.ActualHeight))}
$flow=F 'flowTools';$source=$flow[0];$threshold=$flow[1]
(F 'FlowToolList').SelectedItem=$threshold
ViewDisplay04 'ModuleInput'
if((M 'ResolveImageContext').HasImage -or (F 'ImageEmptyStateText').Text -like '*请选择全局输入*' -or (F 'ImageContextSourceText').Text -notlike ('*'+$source.InstanceName+'*')){throw 'Missing applied input lost its actual source or suggests an unrelated image.'}
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
    $window.Width=$size[0];$window.Height=$size[1];Pump 70
    $bar=F 'ImageSourceBarUI04';$sourceBounds=BoundsDisplay04 (F 'ImageContextSourceText') $bar
    $card=F 'SelectedResultCardUI04';$detailsBounds=BoundsDisplay04 (F 'SelectedResultDetailsUI04') $card
    Write-Output ('UI04_DISPLAY_LAYOUT='+$size[0]+'x'+$size[1]+'; SourceBarHeight='+$bar.ActualHeight+'; SourceRight='+$sourceBounds.Right+'/'+$bar.ActualWidth+'; DetailsRight='+$detailsBounds.Right+'/'+$card.ActualWidth)
    Shot ('image-results-empty-'+$size[0]+'x'+$size[1])
    if($bar.ActualHeight -gt 37 -or $sourceBounds.Right -gt $bar.ActualWidth+1 -or $detailsBounds.Right -gt $card.ActualWidth+1){throw 'Compact source/result header clipped or unexpectedly expanded.'}
    if((F 'SelectedResultOutputText').ActualWidth -lt 200 -or -not (F 'SelectedResultDetailsUI04').IsVisible){throw 'Key output or result detail entry not visible.'}
}
$window.Width=1366;$window.Height=768
# Real image source and scalar result; validate presentation against typed outputs, not fake summary strings.
$path=Join-Path $temp 'display-source.png';[HalconDotNet.HObject]$image=$null
try{[HalconDotNet.HOperatorSet]::GenImageConst([ref]$image,'byte',64,48);[HalconDotNet.HOperatorSet]::WriteImage($image,'png',0,$path)}finally{if($image){$image.Dispose()}}
$source.Parameters.LocalImagePath=$path
$threshold.Parameters.ImageThresholdMinGray=0;$threshold.Parameters.ImageThresholdMaxGray=255
M 'RunStandaloneToolCoreUI02' @($source,'UI04 display source',$false)|Out-Null
M 'RunStandaloneToolCoreUI02' @($threshold,'UI04 display scalar',$false)|Out-Null
ViewDisplay04 'ModuleInput'
[double]$area=0
if(-not $threshold.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 3072 -or (F 'SelectedResultOutputText').Text -notlike '*3072*'){throw 'Overview no longer shows genuine typed result.'}
if((F 'SelectedResultOriginTextS88').Text -notlike '*非流程判定*' -or (F 'ImageContextDetailText').Text -notlike '*64×48*'){throw 'Execution qualification or image metadata lost.'}
Shot 'image-results-actual'
$version=$threshold.RuntimeOutputVersion;$applied=$threshold.Parameters.ImageThresholdMinGray
(F 'SelectedResultDetailsUI04').RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));Pump 60
if((F 'dockEditingTool') -ne $threshold -or (F 'DockConfigurationTabs').SelectedItem -ne (F 'DockResultTaskTab') -or $threshold.RuntimeOutputVersion -ne $version -or (F 'dockDraftDirty')){throw 'Details routed to wrong module or executed/applied.'}
# Selection and editor image ownership are distinct and explicitly named.
(F 'FlowToolList').SelectedItem=$source;RefreshDisplay04
if((F 'ImageContextNoticeUI04').Text -notlike ('*'+$threshold.InstanceName+'*') -or (F 'SelectedResultToolText').Text -notlike ('*'+$source.InstanceName+'*')){throw 'Editor image / selected result ownership is ambiguous.'}
ViewDisplay04 ('Source:'+$source.ToolId)
if((F 'ImageContextNoticeUI04').Text -like '*参数窗画面*'){throw 'Explicit source falsely attributed to the editing module.'}
if((F 'ImageAcquisitionSummaryS115').Text -match '\d{4}-\d{2}-\d{2}' -or (F 'ImageAcquisitionSummaryS115').ToolTip -notmatch '\d{4}-\d{2}-\d{2}'){throw 'Acquisition timestamp not moved to complete hover detail.'}
ViewDisplay04 'ModuleInput'
(F 'FlowToolList').SelectedItem=$threshold
M 'SelectCentralConfigurationTaskS77' @((F 'DockParametersTaskTab'),'Parameters')|Out-Null
(F 'DockImageThresholdMinTextBox').Text='1'
M 'DockTrialRunButton_Click' @($null,$null)|Out-Null;RefreshDisplay04
if((F 'SelectedResultOriginTextS88').Text -notlike '*未应用*' -or $threshold.Parameters.ImageThresholdMinGray -ne $applied){throw 'Draft display became applied.'}
Shot 'image-results-draft'
M 'DockRevertButton_Click' @($null,$null)|Out-Null;M 'CloseCentralModuleConfigurationS77'|Out-Null
# Explicit disabled source keeps an empty view and a specific reason.
$source.IsEnabled=$false;ViewDisplay04 ('Source:'+$source.ToolId)
if((M 'ResolveImageContext').HasImage -or (F 'ImageEmptyStateText').Text -notlike '*停用*' -or (F 'ImageEmptyStateText').Text -like '*请选择全局输入*'){throw 'Disabled source borrowed a frame or offered misleading fallback.'}
$source.IsEnabled=$true
# Diagnostic text fixture checks bounded rendering only, not algorithm error handling.
$threshold.ErrorMessage=('显示检查：缺少上游图像；' * 35);RefreshDisplay04
$errorText=F 'SelectedResultErrorText';Pump 40
if($errorText.Visibility -ne 'Visible' -or $errorText.ActualHeight -gt 24 -or $errorText.ToolTip -ne $threshold.ErrorMessage -or (F 'SelectedResultStatusText').Text -ne '异常'){throw 'Long exception overflowed, lost full hover text, or masqueraded as NG.'}
Shot 'image-results-error'
$threshold.ErrorMessage='';RefreshDisplay04
# Empty historical display fixture: no run suggestion, source remains read-only and controls disabled.
$main.GetField('historicalImageMode',$all).SetValue($window,$true);RefreshDisplay04
if((F 'ImageContextSourceText').Text -notlike '*只读*' -or (F 'ImageEmptyStateText').Text -notlike '*返回实时*' -or (F 'ImageContextComboBox').IsEnabled -or (F 'RoiToolsToggleButton').IsEnabled){throw 'Historical display lost its read-only qualification.'}
$main.GetField('historicalImageMode',$all).SetValue($window,$false);RefreshDisplay04
if($window.IsActive -or $window.ShowActivated){throw 'Test activated the desktop.'}
Write-Output 'UI04_IMAGE_RESULTS=PASS; ThreeSizes; SourceReason; TypedArea3072; DetailsNoExecution; EditorSelectionOwnership; DraftNotApplied; DisabledSourceEmpty; BoundedErrorWithFullHover; EmptyHistoryReadOnly; NoMouse'
