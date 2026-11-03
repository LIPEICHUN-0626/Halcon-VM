# Default workspace reduction; existing actions remain under the original flow More panel.
$flow=F 'flowTools';$ids=@($flow|ForEach-Object ToolId)-join ','
$definition=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
    $window.Width=$size[0];$window.Height=$size[1];Pump 60
    foreach($name in @('FlowSecondaryHeaderUI06','FlowSecondaryFooterUI06','FlowModuleNavigatorUI05','FlowRunSelectedButtonS89','TemplateStatusBarText','TcpStatusBarText','RoiStatusBarText')){if((F $name).IsVisible){throw ('Secondary UI remains permanent: '+$name)}}
    foreach($name in @('RunOnceButton','StopRunButton','FlowMoreToggleButton','ProjectFlowPickerS104','StationEditingProtectionBadgeS137')){if(-not (F $name).IsVisible){throw ('Essential action hidden: '+$name)}}
    if(-not (F 'FlowMoreContentUI06').IsAncestorOf((F 'FlowSecondaryHeaderUI06')) -or -not (F 'FlowMoreContentUI06').IsAncestorOf((F 'FlowSecondaryFooterUI06')) -or (F 'ModeStatusText').ToolTip.Children.Count -ne 3){throw 'Existing controls/details were removed instead of relocated.'}
    Shot ('lean-default-'+$size[0]+'x'+$size[1])
}
$viewport=F 'FlowGraphViewport';$sizeBefore=$viewport.RenderSize
(F 'FlowMoreToggleButton').IsChecked=$true;Pump 60
foreach($name in @('FlowModuleNavigatorUI05','FlowRunSelectedButtonS89','FlowSelectionStatusText','FlowClipboardStatusText')){if(-not (F $name).IsVisible){throw ('Missing original advanced action: '+$name)}}
M 'RefreshFlowModuleNavigatorUI05' @('03')|Out-Null
$nav=F 'FlowModuleNavigatorUI05';$nav.SelectedItem=$nav.Items[0];Pump 50
if((F 'FlowToolList').SelectedItem -ne $flow[2] -or (F 'centralModuleConfigurationOpenS77')){throw 'Original locator no longer selects without opening parameters.'}
Shot 'lean-flow-actions'
(F 'FlowMoreToggleButton').IsChecked=$false;Pump 40
if($viewport.RenderSize -ne $sizeBefore -or $service.GetRecipeFingerprint((M 'CaptureRecipe')) -ne $definition -or @($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count){throw 'Opening secondary UI resized canvas, mutated recipe or ran tools.'}
M 'OpenCentralModuleConfigurationS77' @($flow[1],$false)|Out-Null
(F 'DockImageThresholdMinTextBox').Text='201';$editor=F 'moduleParameterWindowS89'
(F 'FlowMoreToggleButton').IsChecked=$true;Pump 30;(F 'FlowMoreToggleButton').IsChecked=$false;Pump 30
if((F 'moduleParameterWindowS89') -ne $editor -or -not (F 'dockDraftDirty')){throw 'Secondary UI opening lost current draft.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null;M 'CloseCentralModuleConfigurationS77'|Out-Null
# A paused presentation keeps Continue directly on the heading, not buried inside More.
try{
    $main.GetField('isFlowExecutionActive',$all).SetValue($window,$true);$main.GetField('isPauseRequested',$all).SetValue($window,$true);M 'RefreshUiState'|Out-Null
    if(-not (F 'DebugContinueButton').IsVisible -or -not (F 'FlowHeadingCommandsUI06').IsAncestorOf((F 'DebugContinueButton'))){throw 'Paused continuation was buried with secondary actions.'}
    foreach($width in @(1120,1366,1600)){
        $window.Width=$width;Pump 30;$heading=F 'FlowHeadingCommandsUI06'
        foreach($name in @('DebugContinueButton','FlowMoreToggleButton')){$b=F $name;$r=$b.TransformToAncestor($heading).TransformBounds([Windows.Rect]::new(0,0,$b.ActualWidth,$b.ActualHeight));if($r.Right -gt $heading.ActualWidth+1 -or $r.Left -lt -1){throw 'Paused heading command clips.'}}
    }
}finally{$main.GetField('isFlowExecutionActive',$all).SetValue($window,$false);$main.GetField('isPauseRequested',$all).SetValue($window,$false);M 'RefreshUiState'|Out-Null}
$path=Join-Path $temp 'lean05.png';[HalconDotNet.HObject]$im=$null
try{[HalconDotNet.HOperatorSet]::GenImageConst([ref]$im,'byte',64,48);[HalconDotNet.HOperatorSet]::WriteImage($im,'png',0,$path)}finally{if($im){$im.Dispose()}}
$flow[0].Parameters.ImageSourceModeS114='File';$flow[0].Parameters.LocalImagePath=$path
$flow[1].Parameters.ImageThresholdMinGray=0;$flow[1].Parameters.ImageThresholdMaxGray=255
(F 'AutoSendMatchResultCheckBox').IsChecked=$false
M 'RunStandaloneToolCoreUI02' @($flow[0],'Lean source',$false)|Out-Null;$version=$flow[0].RuntimeOutputVersion
(F 'FlowToolList').SelectedItem=$flow[1];(F 'FlowMoreToggleButton').IsChecked=$true;Pump 40
(F 'FlowRunSelectedButtonS89').RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent))
$end=[DateTime]::UtcNow.AddSeconds(20);while((F 'isFlowExecutionActive') -and [DateTime]::UtcNow -lt $end){Pump 40}
[double]$area=0
if((F 'isFlowExecutionActive') -or -not $flow[1].TryGetNumericOutput('Area',[ref]$area) -or $area -ne 3072 -or $flow[0].RuntimeOutputVersion -ne $version -or $null -ne $flow[2].LastOutputUtc){throw 'Relocated original selected run changed its scope.'}
if((@($flow|ForEach-Object ToolId)-join ',') -ne $ids -or $window.IsActive -or $window.ShowActivated){throw 'Lean workspace changed workflow identity or activated foreground.'}
Write-Output 'UI05_LEAN_WORKSPACE=PASS; ThreeSizes; DefaultSecondaryRowsRemoved; OriginalActionsRetained; LocatorNoRun; NoCanvasResize; DraftKept; PauseActionOnHeading; RealSelectedArea3072; ScopeUnchanged; TechnicalDetailsOnHover; NoMouse'
