# Page navigation and its real original run entry; no physical input or external communication.
function ModeBounds08($element,$root){$element.TransformToAncestor($root).TransformBounds([Windows.Rect]::new(0,0,$element.ActualWidth,$element.ActualHeight))}
function ClickMode08([string]$name){(F $name).RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));Pump 60}
function CheckMode08([bool]$production){
    if((F 'productionRunMode') -ne $production -or -not (F 'ReturnDeveloperModeButtonS137').IsVisible -or -not (F 'EnterProductionModeButton').IsVisible){throw 'Persistent page switch or original mode lost.'}
    $selected=F $(if($production){'EnterProductionModeButton'}else{'ReturnDeveloperModeButtonS137'})
    if($selected.Background.ToString() -ne '#FF0F766E'){throw 'Current page has no visible selected indication.'}
    if((F 'ProductionRunOnceButtonUI08').IsEnabled -ne (F 'RunOnceButton').IsEnabled -or (F 'ProductionStopButtonUI08').IsEnabled -ne (F 'StopRunButton').IsEnabled){throw 'Production actions diverged from original run/stop guards.'}
}
$flow=F 'flowTools';$ids=@($flow|ForEach-Object ToolId)-join ','
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
    $window.Width=$size[0];$window.Height=$size[1];Pump 60
    M 'RefreshUiState'|Out-Null;CheckMode08 $false
    $bar=F 'WorkspaceCommandBarUI04';$brand=ModeBounds08 (F 'DeveloperAppBar') $bar;$commands=ModeBounds08 (F 'DeveloperToolbarPanel') $bar;$pages=ModeBounds08 (F 'WorkspaceModeSwitchUI08') $bar
    if($brand.Right -gt $commands.Left+1 -or $commands.Right -gt $pages.Left+1 -or $pages.Right -gt $bar.ActualWidth+1){throw 'Page switch overlaps engineering commands or brand.'}
    Shot ('engineering-page-'+$size[0]+'x'+$size[1])
    ClickMode08 'EnterProductionModeButton';CheckMode08 $true
    if((F 'ProductionFlowContextUI08').Text -ne (F 'ProductionModeStateText').Text){throw 'Toolbar and run panel disagree on current flow.'}
    $toolbar=F 'ProductionToolbarPanel';$run=ModeBounds08 (F 'ProductionRunOnceButtonUI08') $toolbar;$stop=ModeBounds08 (F 'ProductionStopButtonUI08') $toolbar
    if($run.Right -gt $stop.Left+1 -or $stop.Right -gt $toolbar.ActualWidth+1){throw 'Run/stop controls overlap.'}
    if(@($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count -or (F 'isFlowExecutionActive')){throw 'Page navigation automatically executed flow.'}
    Shot ('run-page-'+$size[0]+'x'+$size[1])
    ClickMode08 'ReturnDeveloperModeButtonS137';CheckMode08 $false
}
# Shared disabled/busy presentation, not a claim of real SDK stop coverage.
foreach($tool in $flow){$tool.IsEnabled=$false};M 'RefreshUiState'|Out-Null;ClickMode08 'EnterProductionModeButton'
if((F 'ProductionRunOnceButtonUI08').IsEnabled){throw 'Empty enabled-tool scope offers run.'}
foreach($tool in $flow){$tool.IsEnabled=$true}
$main.GetField('isFlowExecutionActive',$all).SetValue($window,$true);M 'RefreshUiState'|Out-Null
if((F 'ProductionRunOnceButtonUI08').IsEnabled -or -not (F 'ProductionStopButtonUI08').IsEnabled){throw 'Busy presentation lost original disabled run / available stop.'}
ClickMode08 'ReturnDeveloperModeButtonS137'
if(-not (F 'isFlowExecutionActive')){throw 'Page change stopped active state.'}
$main.GetField('isFlowExecutionActive',$all).SetValue($window,$false);M 'RefreshUiState'|Out-Null
# Navigation from a real original parameter draft is not tested by cancelling a native confirmation.
M 'OpenCentralModuleConfigurationS77' @($flow[1],$false)|Out-Null
(F 'DockImageThresholdMinTextBox').Text='1';$editor=F 'moduleParameterWindowS89'
ClickMode08 'ReturnDeveloperModeButtonS137'
if((F 'moduleParameterWindowS89') -ne $editor -or -not (F 'dockDraftDirty')){throw 'Selecting current engineering page lost parameter draft.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null;M 'CloseCentralModuleConfigurationS77'|Out-Null
$path=Join-Path $temp 'page-mode.png';[HalconDotNet.HObject]$im=$null
try{[HalconDotNet.HOperatorSet]::GenImageConst([ref]$im,'byte',32,32);[HalconDotNet.HOperatorSet]::WriteImage($im,'png',0,$path)}finally{if($im){$im.Dispose()}}
$flow[0].Parameters.ImageSourceModeS114='File';$flow[0].Parameters.LocalImagePath=$path
$flow[1].Parameters.ImageThresholdMinGray=0;$flow[1].Parameters.ImageThresholdMaxGray=255
(F 'AutoSendMatchResultCheckBox').IsChecked=$false
ClickMode08 'EnterProductionModeButton';ClickMode08 'ProductionRunOnceButtonUI08'
$deadline=[DateTime]::UtcNow.AddSeconds(20)
while((F 'isFlowExecutionActive') -and [DateTime]::UtcNow -lt $deadline){Pump 50}
if((F 'isFlowExecutionActive') -or (F 'FlowRunResultText').Text -ne 'OK'){throw 'Original actual run button did not complete successfully.'}
$store=F 'productionRunStoreS135';$records=@($store.ReadRecent(20).Records)
if($records.Count -ne 1 -or $records[0].PageMode -ne 'Production' -or $records[0].FlowId -ne (F 'activeProjectFlowIdS104')){throw 'Actual button run journal lost current flow / page qualification.'}
$version=$flow[0].RuntimeOutputVersion;ClickMode08 'ReturnDeveloperModeButtonS137';ClickMode08 'EnterProductionModeButton'
if($flow[0].RuntimeOutputVersion -ne $version -or $store.ReadRecent(20).Records.Count -ne 1){throw 'Page navigation reran / relabeled results.'}
Shot 'run-page-after-explicit-run'
ClickMode08 'ReturnDeveloperModeButtonS137'
if((@($flow|ForEach-Object ToolId)-join ',') -ne $ids -or $window.IsActive){throw 'Page chrome replaced workflow or activated desktop.'}
Write-Output 'UI04_PAGE_MODE=PASS; ThreeSizes; PersistentSelectedPages; SharedRunStopGuards; NoAutoRun; BusyPresentationPreserved; CurrentPagePreservesDraft; ActualOriginalButtonRun; QualifiedJournal; NoMouse'
