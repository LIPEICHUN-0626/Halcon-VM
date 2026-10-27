# Original commands in the bounded in-window overlay; no physical input or capture.
function ClickMore10([string]$name) {
    $button=F $name
    if(-not $button.IsEnabled){throw ('Expected enabled command: '+$name)}
    $button.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent))
    Pump 70
}
function BoundsMore10($control,$parent) {
    $point=$control.TranslatePoint([Windows.Point]::new(0,0),$parent)
    if($point.X -lt -1 -or $point.Y -lt -1 -or $point.X+$control.ActualWidth -gt $parent.ActualWidth+1 -or $point.Y+$control.ActualHeight -gt $parent.ActualHeight+1){throw ('Clipped command: '+$control.Name)}
}
$flow=F 'flowTools';$threshold=$flow[1]
(F 'FlowToolList').SelectedItem=$threshold;M 'RefreshUiState'|Out-Null
$definition=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
foreach($width in @(1366,1600)) {
    $window.Width=$width;$window.Height=$(if($width -eq 1366){768}else{900});Pump 100
    $viewport=F 'FlowGraphViewport';$size=$viewport.RenderSize;$zoom=(F 'FlowZoomSlider').Value
    $surface=F 'FlowContentSurfaceUI10';$panel=F 'FlowMorePanelUI10'
    if($panel.IsVisible){throw 'More must initially be closed.'}
    (F 'FlowMoreToggleButton').IsChecked=$true;Pump 80
    if(-not $panel.IsVisible -or $panel.ActualWidth -gt 330.1 -or $viewport.RenderSize -ne $size -or (F 'FlowZoomSlider').Value -ne $zoom){throw 'More changed flow size / zoom.'}
    BoundsMore10 $panel $surface
    foreach($name in @('DebugStepButton','DebugContinueButton','ToggleBreakpointButton','FlowRunToHereButtonUI06','FlowRunFromHereButtonUI06','ClearBreakpointsButton','OpenConnectionIssuesButton','OpenFlowSnippetLibraryButton')){BoundsMore10 (F $name) $panel}
    Shot ('more-'+$width)
    (F 'FlowMoreDisplayUI10').IsExpanded=$true;Pump 80
    BoundsMore10 $panel $surface
    foreach($name in @('FlowAllCardsModeRadioS76','FlowAllDependenciesModeRadioS74','FlowGraphViewButton','FlowListViewButton','FlowZoomSlider')){BoundsMore10 (F $name) $panel}
    Shot ('more-display-'+$width)
    M 'DismissFlowMoreOutsideUI10' @((F 'FlowZoomSlider'))|Out-Null
    if(-not (F 'FlowMoreToggleButton').IsChecked){throw 'Inside click dismissed More.'}
    M 'DismissFlowMoreOutsideUI10' @((F 'FlowMoreToggleButton'))|Out-Null
    if(-not (F 'FlowMoreToggleButton').IsChecked){throw 'Toggle preview double-dismissed.'}
    M 'DismissFlowMoreOutsideUI10' @($viewport)|Out-Null
    if((F 'FlowMoreToggleButton').IsChecked -or $viewport.RenderSize -ne $size){throw 'Outside dismissal failed / resized canvas.'}
    (F 'FlowMoreDisplayUI10').IsExpanded=$false
}
if($definition -ne $service.GetRecipeFingerprint((M 'CaptureRecipe'))){throw 'Viewing More changed recipe.'}
# Preserve the actual parameter draft; Escape closes the overlay first.
M 'OpenCentralModuleConfigurationS77' @($threshold,$false)|Out-Null
(F 'DockImageThresholdMinTextBox').Text='160'
$editor=F 'moduleParameterWindowS89'
(F 'FlowMoreToggleButton').IsChecked=$true
$key=[Windows.Input.KeyEventArgs]::new([Windows.Input.Keyboard]::PrimaryDevice,[Windows.PresentationSource]::FromVisual($window),0,[Windows.Input.Key]::Escape)
$key.RoutedEvent=[Windows.Input.Keyboard]::PreviewKeyDownEvent
M 'MainWindow_PreviewKeyDown' @($window,$key)|Out-Null
if(-not $key.Handled -or (F 'FlowMoreToggleButton').IsChecked -or -not (F 'dockDraftDirty') -or (F 'moduleParameterWindowS89') -ne $editor -or (F 'DockImageThresholdMinTextBox').Text -ne '160'){throw 'Escape lost draft or closed editor.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null;M 'CloseCentralModuleConfigurationS77'|Out-Null
# Routed original command + original display selection, not a replacement executor.
(F 'FlowMoreToggleButton').IsChecked=$true
ClickMore10 'ToggleBreakpointButton'
if((F 'FlowMoreToggleButton').IsChecked -or -not $threshold.IsBreakpoint){throw 'Original breakpoint command did not execute / close.'}
(F 'FlowMoreToggleButton').IsChecked=$true
ClickMore10 'ClearBreakpointsButton'
if($threshold.IsBreakpoint){throw 'Original clear breakpoints failed.'}
(F 'FlowMoreToggleButton').IsChecked=$true;(F 'FlowMoreDisplayUI10').IsExpanded=$true
(F 'FlowAllDependenciesModeRadioS74').IsChecked=$true;Pump 50
if((F 'flowTaskFocusModeS74') -or -not (F 'FlowMoreToggleButton').IsChecked){throw 'Display selection failed or closed panel.'}
(F 'FlowTaskFocusModeRadioS74').IsChecked=$true
M 'CloseFlowMoreUI10'|Out-Null
# Real HALCON range semantics, busy/blocked guards, STOP and reload are direct dependencies.
. (Join-Path $PSScriptRoot 'UIBatch03RunScopes.ps1')
Write-Output 'UI03_BLOCK10_MORE=PASS; TwoWidths; NoCanvasResize; OriginalCommands; DisplaySelection; OutsideAndEscapePreserveDraft; RealRangeChain; NoMouse'
