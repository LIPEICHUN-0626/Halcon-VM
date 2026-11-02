# Workspace titles are real locations; parameter/input entries are actions, not fake tabs.
function ClickWorkspace10([string]$name){
    $control=F $name
    $event=if($control -is [Windows.Controls.MenuItem]){[Windows.Controls.MenuItem]::ClickEvent}else{[Windows.Controls.Primitives.ButtonBase]::ClickEvent}
    $control.RaiseEvent([Windows.RoutedEventArgs]::new($event));Pump 50
}
$flow=F 'flowTools';$threshold=$flow[1];$appliedLower=$threshold.Parameters.ImageThresholdMinGray
$ids=@($flow|ForEach-Object ToolId)-join ',';$definition=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
(F 'FlowToolList').SelectedItem=$threshold;M 'RefreshInspector'|Out-Null
if(-not (F 'RightParametersTaskButton').IsEnabled -or -not (F 'RightIoTaskButton').IsEnabled){throw 'Selected configurable module has no usable parameter/input action.'}
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
    $window.Width=$size[0];$window.Height=$size[1];Pump 50
    $header=F 'RightWorkspaceHeaderUI10'
    if((F 'RightWorkspaceTitleUI10').Text -ne '结果' -or (F 'RightResultTaskButton').Visibility -ne 'Collapsed' -or (F 'RightIoTaskButton') -isnot [Windows.Controls.MenuItem]){throw 'Result header still presents parameter/input actions as workspace tabs.'}
    foreach($name in @('RightWorkspaceTitleUI10','RightParametersTaskButton','RightMoreTaskButton')){
        $element=F $name;$rect=$element.TransformToAncestor($header).TransformBounds([Windows.Rect]::new(0,0,$element.ActualWidth,$element.ActualHeight))
        if(-not $element.IsVisible -or $rect.Right -gt $header.ActualWidth+1 -or $rect.Left -lt -1){throw ('Clipped result action: '+$name)}
    }
    Shot ('results-actions-'+$size[0]+'x'+$size[1])
}
$columns=@((F 'MainWorkspaceGrid').ColumnDefinitions|ForEach-Object {$_.Width.ToString()})-join ','
$height=(F 'ImageWorkspaceRow').Height
ClickWorkspace10 'RightParametersTaskButton'
$editor=F 'moduleParameterWindowS89'
if($null -eq $editor -or (F 'dockEditingTool') -ne $threshold -or (F 'DockConfigurationTabs').SelectedItem -ne (F 'DockParametersTaskTab') -or (F 'RightWorkspaceTitleUI10').Text -ne '结果' -or (F 'RightParametersTaskButton').Background.ToString() -ne '#FFF8FAFC'){throw 'Parameter action became a selected result tab or opened the wrong editor.'}
(F 'DockImageThresholdMinTextBox').Text='1'
if(-not (F 'dockDraftDirty')){throw 'Real editable draft fixture was not established.'}
ClickWorkspace10 'RightIoTaskButton'
if((F 'moduleParameterWindowS89') -ne $editor -or (F 'DockConfigurationTabs').SelectedItem -ne (F 'DockIoTaskTab') -or -not (F 'dockDraftDirty') -or $threshold.Parameters.ImageThresholdMinGray -ne $appliedLower){throw 'Original input menu action lost draft, applied changes or replaced editor.'}
$tabRoutes=@(
    @('RightImageRoiMenuItem_Click','RoiTab'),
    @('RightLayerMenuItem_Click','LayerWorkspaceTab'),
    @('RightCommunicationMenuItem_Click','CommunicationWorkspaceTab'),
    @('RightProjectMenuItem_Click','ProjectDiagnosticsTab')
)
foreach($route in $tabRoutes){
    M $route[0] @($null,$null)|Out-Null;Pump 40
    $tab=(F 'RightTabs').SelectedItem
    if($tab -ne (F $route[1]) -or (F 'RightWorkspaceTitleUI10').Text -ne [string]$tab.Header -or (F 'RightResultTaskButton').Visibility -ne 'Visible'){throw 'Other workspace has no accurate title or result return action.'}
    $header=F 'RightWorkspaceHeaderUI10';$button=F 'RightMoreTaskButton'
    $bounds=$button.TransformToAncestor($header).TransformBounds([Windows.Rect]::new(0,0,$button.ActualWidth,$button.ActualHeight))
    if($bounds.Right -gt $header.ActualWidth+1){throw 'Return action clipped remaining workspace controls.'}
    if($route[0] -eq 'RightCommunicationMenuItem_Click'){Shot 'communication-return-to-results'}
    ClickWorkspace10 'RightResultTaskButton'
    if((F 'RightTabs').SelectedItem -ne (F 'ModuleResultTab') -or (F 'RightWorkspaceTitleUI10').Text -ne '结果' -or (F 'RightResultTaskButton').IsVisible -or (F 'moduleParameterWindowS89') -ne $editor -or -not (F 'dockDraftDirty')){throw 'Returning results changed the floating editor or draft.'}
}
ClickWorkspace10 'SelectedResultDetailsUI04'
if((F 'DockConfigurationTabs').SelectedItem -ne (F 'DockResultTaskTab') -or -not (F 'dockDraftDirty')){throw 'Original selected-result details path lost the real draft.'}
if((@((F 'MainWorkspaceGrid').ColumnDefinitions|ForEach-Object {$_.Width.ToString()})-join ',') -ne $columns -or (F 'ImageWorkspaceRow').Height -ne $height -or $service.GetRecipeFingerprint((M 'CaptureRecipe')) -ne $definition -or (@($flow|ForEach-Object ToolId)-join ',') -ne $ids -or @($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count){throw 'Workspace actions resized, mutated or executed the original flow.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null;M 'CloseCentralModuleConfigurationS77'|Out-Null
(F 'FlowToolList').SelectedItem=$null;M 'RefreshInspector'|Out-Null
if((F 'RightParametersTaskButton').IsEnabled -or (F 'RightIoTaskButton').IsEnabled){throw 'Empty selection offers an executable parameter/input action.'}
(F 'FlowToolList').SelectedItem=$threshold
if($window.IsActive -or $window.ShowActivated -or $window.Left -gt -10000){throw 'Workspace test activated foreground.'}
Write-Output 'UI04_WORKSPACE_ACTIONS=PASS; ThreeSizes; TwoPrimaryActions; RealWorkspaceTitle; OriginalParameterAndInputRoutes; OriginalDetails; OtherWorkspacesAndReturn; SameDraftAndEditor; NoLayoutMutationOrRun; NoMouse'
