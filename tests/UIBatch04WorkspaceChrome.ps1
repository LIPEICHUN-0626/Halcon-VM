# UI-only verification: no flow execution, device calls or physical input.
function Visuals04($root){
    $stack=[Collections.Generic.Stack[Windows.DependencyObject]]::new();$stack.Push($root)
    while($stack.Count){$v=$stack.Pop();Write-Output -NoEnumerate $v
        for($i=0;$i -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($v);$i++){$stack.Push([Windows.Media.VisualTreeHelper]::GetChild($v,$i))}}
}
function Bounds04($element,$root){$element.TransformToAncestor($root).TransformBounds([Windows.Rect]::new(0,0,$element.ActualWidth,$element.ActualHeight))}
$flow=F 'flowTools';$ids=@($flow|ForEach-Object ToolId)-join ','
$before=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
    $window.Width=$size[0];$window.Height=$size[1];Pump 100
    $bar=F 'WorkspaceCommandBarUI04';$brand=Bounds04 (F 'DeveloperAppBar') $bar;$commands=Bounds04 (F 'DeveloperToolbarPanel') $bar
    if($brand.Right -gt $commands.Left+1 -or $commands.Right -gt $bar.ActualWidth+1){throw 'Top command regions overlap / clip.'}
    $workspace=F 'MainWorkspaceGrid'
    $totalWidth=($workspace.ColumnDefinitions|Measure-Object ActualWidth -Sum).Sum
    if($totalWidth -gt $workspace.ActualWidth+1){throw 'Workspace columns overflow / clip the image and result actions.'}
    foreach($name in @('SaveRecipeButton','OpenImageButton','RunOnceButton','StopRunButton','EnterProductionModeButton','DeveloperMoreToggleButton')){
        $button=F $name;$bounds=Bounds04 $button $bar
        if(-not $button.IsVisible -or $bounds.Right -gt $bar.ActualWidth+1 -or $bounds.Left -lt 0){throw ('Clipped main action: '+$name)}
    }
    if((F 'FlowHeadingUI04').ActualHeight -gt 42 -or (F 'FlowScopeNameS102').Visibility -ne 'Collapsed'){throw 'Root flow heading still duplicates identity.'}
    Write-Output ('UI04_LAYOUT='+$size[0]+'x'+$size[1]+'; Header='+$bar.ActualHeight+'; FlowHeader='+(F 'FlowHeadingUI04').ActualHeight+'; Image='+(F 'ImageWorkspaceRow').ActualHeight+'; Result='+(F 'RightConfigurationRow').ActualHeight)
    if((F 'ImageWorkspaceRow').ActualHeight -le (F 'RightConfigurationRow').ActualHeight -or (F 'RightConfigurationRow').ActualHeight -lt 179.5){throw 'Image/result hierarchy invalid.'}
    Shot ('workspace-'+$size[0]+'x'+$size[1])
}
# Changing selection reveals only its actions; invisible actions retain their reserved space.
(F 'RightPanelColumn').Width=[Windows.GridLength]::new(680)
$window.Width=1120;Pump 80
if(((F 'MainWorkspaceGrid').ColumnDefinitions|Measure-Object ActualWidth -Sum).Sum -gt (F 'MainWorkspaceGrid').ActualWidth+1){throw 'Remembered/dragged right width clipped narrow workspace.'}
$window.Width=1366;Pump 40
(F 'RightPanelColumn').Width=[Windows.GridLength]::new(1,[Windows.GridUnitType]::Star)
(F 'FlowToolList').SelectedItem=$flow[2];Pump 40
$recordButtons=@(Visuals04 (F 'FlowGraphNodeItemsControl')|Where-Object {$_ -is [Windows.Controls.Button] -and $_.Content -eq '记录'})
if($recordButtons.Count -ne 4 -or @($recordButtons|Where-Object IsVisible).Count -ne 1 -or ($recordButtons|Where-Object IsVisible).DataContext -ne $flow[2]){throw 'Card actions did not follow actual selection.'}
$columns=@((F 'MainWorkspaceGrid').ColumnDefinitions|ForEach-Object {$_.Width.ToString()}) -join ','
$imageHeight=(F 'ImageWorkspaceRow').Height
M 'OpenCentralModuleConfigurationS77' @($flow[1],$false)|Out-Null;Pump 40
if((@((F 'MainWorkspaceGrid').ColumnDefinitions|ForEach-Object {$_.Width.ToString()}) -join ',') -ne $columns -or (F 'ImageWorkspaceRow').Height -ne $imageHeight){throw 'Parameter opening squeezed the workspace.'}
M 'CloseCentralModuleConfigurationS77'|Out-Null
# Toggle rendering exposes a genuine checked state, without running or sending anything.
$more=F 'FlowMoreToggleButton';$more.IsChecked=$true;Pump 40
$surface=$more.Template.FindName('Surface',$more)
if($null -eq $surface -or $surface.Background.ToString() -ne '#FFDDF2EC'){throw 'More toggle has no checked indication.'}
$more.IsChecked=$false;Pump 30
# User-adjusted image/result split survives read-only navigation and mode return.
(F 'ImageWorkspaceRow').Height=[Windows.GridLength]::new(420)
(F 'RightConfigurationRow').Height=[Windows.GridLength]::new(200)
M 'RightResultTaskButton_Click' @($null,$null)|Out-Null
M 'SetProductionRunMode' @($true)|Out-Null;Pump 40
if((F 'DeveloperToolbarPanel').IsVisible -or -not (F 'ProductionToolbarPanel').IsVisible){throw 'Mode toolbar state was broken.'}
M 'SetProductionRunMode' @($false)|Out-Null;Pump 40
if((F 'ImageWorkspaceRow').Height.Value -ne 420 -or -not (F 'DeveloperToolbarPanel').IsVisible){throw 'Mode return reset user split or lost toolbar.'}
if(@($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count -or (@($flow|ForEach-Object ToolId)-join ',') -ne $ids){throw 'UI navigation executed or replaced the workflow.'}
# Full child breadcrumb must survive the root-heading simplification.
$main.GetField('isRecipeDirty',$all).SetValue($window,$false)
M 'LoadExampleProgramS78' @('subflow-inspection')|Out-Null
$call=@((F 'flowTools')|Where-Object {$_.Kind.ToString() -eq 'SubflowCall'})[0]
M 'EnterSubflowEditorS102' @($call)|Out-Null;Pump 50
if((F 'FlowScopeNameS102').Visibility -ne 'Visible' -or (F 'FlowScopeNameS102').Text -notlike '* / *' -or -not (F 'SubflowBackButtonS102').IsVisible -or -not (F 'SubflowBoundaryButtonS102').IsVisible){throw 'Subflow identity or return affordances hidden.'}
Shot 'workspace-subflow'
M 'ReturnSubflowEditorS102_Click' @($null,$null)|Out-Null;Pump 40
if((F 'FlowScopeNameS102').Visibility -ne 'Collapsed' -or (F 'SubflowBackButtonS102').IsVisible){throw 'Root heading was not restored.'}
Write-Output 'UI04_WORKSPACE_CHROME=PASS; ThreeSizes; TopActions; SelectedCardActions; CheckedState; EditorNoSqueeze; ModeReturnPreservesSplit; SubflowBreadcrumb; NoFlowExecution; NoMouse'
