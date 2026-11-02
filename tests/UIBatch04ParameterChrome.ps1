# Original editor, fixed action footer and task/scroll presentation; no desktop input.
function BoundsParameter06($element,$root){$element.TransformToAncestor($root).TransformBounds([Windows.Rect]::new(0,0,$element.ActualWidth,$element.ActualHeight))}
function TaskParameter06([string]$name,[string]$key){M 'SelectCentralConfigurationTaskS77' @((F $name),$key)|Out-Null;Pump 40}
$flow=F 'flowTools';$source=$flow[0];$tool=$flow[1]
M 'OpenCentralModuleConfigurationS77' @($tool,$false)|Out-Null;Pump 60
$editor=F 'moduleParameterWindowS89';$mainColumns=@((F 'MainWorkspaceGrid').ColumnDefinitions|ForEach-Object {$_.Width.ToString()}) -join ','
foreach($size in @(@(460,420),@(520,480),@(700,620))){
    $editor.Width=$size[0];$editor.Height=$size[1];Pump 60
    $footer=BoundsParameter06 (F 'DockTaskHeaderBorder') $editor.Content
    $body=BoundsParameter06 (F 'DockConfigurationTabs') $editor.Content
    if($footer.Top -lt $body.Bottom-1 -or $footer.Bottom -gt $editor.Content.ActualHeight+1){throw 'Action footer is not fixed below the task body.'}
    foreach($name in @('DockTrialRunButton','DockContinuousTrialToggleButtonS87','DockApplyButton','DockRevertButton')){
        $b=F $name;$r=BoundsParameter06 $b $editor.Content
        if(-not $b.IsVisible -or $r.Left -lt 0 -or $r.Right -gt $editor.Content.ActualWidth+1){throw ('Missing/clipped fixed action '+$name)}
    }
    if((F 'DockApplyButton').IsEnabled -or (F 'DockRevertButton').IsEnabled){throw 'Unchanged draft actions should be visible but disabled.'}
    Shot ('parameters-clean-'+$size[0]+'x'+$size[1]) $editor
}
$editor.Width=520;$editor.Height=480
(F 'DockImageThresholdMinTextBox').Text='100';Pump 40
$footerTop=(BoundsParameter06 (F 'DockTaskHeaderBorder') $editor.Content).Top
foreach($task in @(@('DockParametersTaskTab','Parameters'),@('DockIoTaskTab','Io'),@('DockRoiTaskTab','Roi'),@('DockResultTaskTab','Result'))){
    TaskParameter06 $task[0] $task[1]
    if((F 'dockEditingTool') -ne $tool -or -not (F 'dockDraftDirty') -or (F 'DockImageThresholdMinTextBox').Text -ne '100' -or (F 'DockTaskHeaderBorder').ActualHeight -le 0){throw 'Task switching lost original draft/action footer.'}
    if([Math]::Abs((BoundsParameter06 (F 'DockTaskHeaderBorder') $editor.Content).Top-$footerTop) -gt 1){throw 'Task switch moved the fixed action footer.'}
    Shot ('parameters-task-'+$task[1]) $editor
}
TaskParameter06 'DockParametersTaskTab' 'Parameters'
(F 'DockImageThresholdMinTextBox').Text='bad-value'
if(M 'ApplyDockConfigurationDraft' @($true)){throw 'Invalid numeric draft was accepted.'}
TaskParameter06 'DockResultTaskTab' 'Result'
if(-not (F 'DockValidationText').IsVisible -or (F 'DockValidationText').Text.Length -eq 0 -or (BoundsParameter06 (F 'DockValidationText') $editor.Content).Bottom -gt $editor.Content.ActualHeight+1){throw 'Validation is hidden in a different task or outside the window.'}
Shot 'parameters-validation-pinned' $editor
M 'DockRevertButton_Click' @($null,$null)|Out-Null
Pump 50
if((F 'DockValidationText').IsVisible -or (F 'DockApplyButton').IsEnabled){throw ('Cancel state: ErrorVisible='+(F 'DockValidationText').IsVisible+'; Error='+(F 'DockValidationText').Text+'; ApplyEnabled='+(F 'DockApplyButton').IsEnabled+'; Dirty='+(F 'dockDraftDirty'))}
# Use a temporary visual spacer to test actual scroll behavior without expanding unrelated tool features.
TaskParameter06 'DockParametersTaskTab' 'Parameters'
$scroll=F 'DockBasicParametersScrollViewer';$stack=$scroll.Content;$spacer=[Windows.Controls.Border]::new();$spacer.Height=900;$stack.Children.Add($spacer)|Out-Null
try{
    Pump 30;$scroll.ScrollToBottom();Pump 40
    if($scroll.VerticalOffset -lt 50){throw 'Scroll fixture did not exercise a real scroll offset.'}
    TaskParameter06 'DockIoTaskTab' 'Io';TaskParameter06 'DockParametersTaskTab' 'Parameters'
    if($scroll.VerticalOffset -lt 50){throw 'Task switching lost its scroll position.'}
    M 'OpenCentralModuleConfigurationS77' @($source,$false)|Out-Null;Pump 40
    if($scroll.VerticalOffset -gt 1){throw 'New module inherited the previous module scroll offset.'}
}finally{$stack.Children.Remove($spacer)}
M 'CloseCentralModuleConfigurationS77'|Out-Null
# Tiny real source and original F5/apply/cancel lifecycle, not a new algorithm regression suite.
$path=Join-Path $temp 'parameter-ui06.png';[HalconDotNet.HObject]$im=$null
try{[HalconDotNet.HOperatorSet]::GenImageConst([ref]$im,'byte',64,48);[HalconDotNet.HOperatorSet]::WriteImage($im,'png',0,$path)}finally{if($im){$im.Dispose()}}
$source.Parameters.LocalImagePath=$path;M 'RunStandaloneToolCoreUI02' @($source,'UI06 source',$false)|Out-Null
$tool.Parameters.ImageThresholdMinGray=0;$tool.Parameters.ImageThresholdMaxGray=255
M 'OpenCentralModuleConfigurationS77' @($tool,$false)|Out-Null;(F 'DockImageThresholdMinTextBox').Text='1'
$editor=F 'moduleParameterWindowS89'
M 'DockTrialRunButton_Click' @($null,$null)|Out-Null;Pump 40
[double]$area=-1
if(-not $tool.TryGetNumericOutput('Area',[ref]$area) -or $area -ne 0 -or $tool.Parameters.ImageThresholdMinGray -ne 0 -or -not (F 'dockDraftDirty')){throw 'F5 did not preserve real draft / applied separation.'}
if(-not (M 'ApplyDockConfigurationDraft' @($true)) -or $tool.Parameters.ImageThresholdMinGray -ne 1 -or (F 'DockApplyButton').IsEnabled){throw 'Fixed original apply action failed.'}
(F 'DockImageThresholdMinTextBox').Text='2';M 'DockRevertButton_Click' @($null,$null)|Out-Null
if((F 'DockImageThresholdMinTextBox').Text -ne '1'){throw 'Fixed cancel did not restore applied parameters.'}
if((@((F 'MainWorkspaceGrid').ColumnDefinitions|ForEach-Object {$_.Width.ToString()}) -join ',') -ne $mainColumns -or $editor.IsActive -or $window.IsActive){throw 'Editor altered main layout or activated desktop.'}
Shot 'parameters-applied' $editor
M 'CloseCentralModuleConfigurationS77'|Out-Null
$resize=M 'CreateFlowTool' @([Enum]::Parse($enumType,'ImageResize'),'尺寸字段提示',$true,$null);$flow.Add($resize);$resize.SetInputBinding('Image',$source.ToolId,'Image')
M 'OpenCentralModuleConfigurationS77' @($resize,$false)|Out-Null
(F 'DockImageResizeWidthTextBox').Text='0'
if(M 'ApplyDockConfigurationDraft' @($true)){throw 'Invalid short size field accepted.'}
TaskParameter06 'DockResultTaskTab' 'Result'
if(-not (F 'DockValidationText').IsVisible){throw 'Field-specific validation suppressed the fixed error in another task.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null;Pump 40
if((F 'DockValidationText').IsVisible -or (F 'DockApplyButton').IsEnabled){throw 'Field-specific cancellation left a stale error/action.'}
M 'CloseCentralModuleConfigurationS77'|Out-Null
Write-Output 'UI04_PARAMETER_CHROME=PASS; ThreeSizes; FixedFooter; FourTasks; DraftAndScrollPreserved; NewModuleStartsAtTop; ValidationAcrossTasks; RealF5Area0NotApplied; ApplyCancel; NoMouse'
