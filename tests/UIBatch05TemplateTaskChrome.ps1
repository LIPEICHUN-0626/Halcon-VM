# Presentation assertions inserted into the existing ui02block02 real Shape/NCC lifecycle.
# This file defines helpers only; it never creates a second training/execution path.
function TemplateBoundsUI07($element,$root) {
    $element.TransformToAncestor($root).TransformBounds([Windows.Rect]::new(0,0,$element.ActualWidth,$element.ActualHeight))
}

function TemplateLogicalAncestorUI07($ancestor,$element) {
    # Collapsed Expanders can omit their content from the visual tree.
    # Verify ownership before expansion without changing the user's task state.
    for($current=$element;$null -ne $current;$current=[Windows.LogicalTreeHelper]::GetParent($current)){
        if([object]::ReferenceEquals($ancestor,$current)){return $true}
    }
    return $false
}

function AssertTemplateChromeUI07([string]$kind,[string]$stage) {
    if($Phase -ne 'ui05template'){return}
    if($kind -notin @('Shape','Ncc') -or $stage -notin @('empty','loaded','trained')){throw 'Unknown template chrome assertion stage.'}
    $editor=F 'moduleParameterWindowS89'
    if($null -eq $editor -or $editor.IsActive -or $editor.ShowActivated -or $window.IsActive -or $editor.Left -gt -10000){
        throw ('Template UI isolation failed: Kind='+$kind+'; Stage='+$stage+'; EditorActive='+$editor.IsActive+'; ShowActivated='+$editor.ShowActivated+'; MainActive='+$window.IsActive+'; Left='+$editor.Left)
    }
    if($kind -eq 'Shape'){
        $expander=F 'ShapeTrainingExpanderS89';$preview=F 'ShapeTrainingPreviewS89';$image=F 'ShapeTrainingImageS89'
        $read=F 'ShapeReadInputButtonUI07';$make=F 'ShapeMakeTemplateButtonUI07';$status=F 'ShapeTrainingStatusS89'
        $model=F 'DockShapeModelStateText';$panel=F 'ShapeModelCardUI07'
        if(-not (TemplateLogicalAncestorUI07 $expander $make) -or -not (TemplateLogicalAncestorUI07 $panel (F 'ShapeModelOptionsUI02'))){throw 'Shape task and original file/ROI management are not grouped under the original model card.'}
    } else {
        $expander=F 'DockNccTrainingS131';$preview=F 'DockNccPreviewS131';$image=F 'DockNccPreviewImageS131'
        $read=F 'DockNccReadS131';$make=F 'DockNccTrainS131';$status=F 'DockNccTrainingStatusS131'
        $model=F 'DockNccModelStateS131';$panel=F 'DockNccS131Panel'
        if(-not (TemplateLogicalAncestorUI07 $expander $make)){throw 'NCC make action left its original training task.'}
    }
    $versions=@((F 'flowTools')|ForEach-Object RuntimeOutputVersion)-join ','
    $dirty=F 'dockDraftDirty'
    $scroll=F 'DockBasicParametersScrollViewer'
    $oldScroll=$scroll.VerticalOffset;$oldWidth=$editor.Width;$oldHeight=$editor.Height;$oldExpanded=$expander.IsExpanded
    try {
        if($stage -eq 'trained'){
            if($expander.IsExpanded -or $make.IsVisible -or $model.Text -notlike '*草稿*'){throw ($kind+' training did not return focus to matching with a draft model state.')}
            if(-not (F 'DockTrialRunButton').IsVisible -or -not (F 'DockApplyButton').IsVisible){throw 'Original trial/apply actions became unreachable after training.'}
            Shot ('template-'+$kind.ToLower()+'-trained') $editor
        } else {
            if($stage -eq 'empty' -and $null -ne $image.Source){throw ($kind+' initial preview was populated without an explicit read.')}
            if($stage -eq 'loaded' -and $null -eq $image.Source){throw ($kind+' explicit read did not provide a genuine preview.')}
            $expander.IsExpanded=$true
            foreach($size in @(@(460,420),@(520,480),@(700,620))){
                $editor.Width=$size[0];$editor.Height=$size[1];$scroll.ScrollToTop();Pump 60
                if($stage -eq 'empty'){
                    if($preview.Visibility -ne [Windows.Visibility]::Collapsed -or $preview.IsVisible -or $make.IsVisible -or $expander.ActualHeight -gt 140){throw ($kind+' empty preview still consumes a training canvas or exposes a make action without an image/ROI.')}
                } elseif(-not $preview.IsVisible -or $preview.ActualHeight -lt 145 -or $preview.ActualWidth -le 0){
                    throw ($kind+' loaded preview was hidden or lost its usable training canvas.')
                }
                $actions=if($stage -eq 'empty'){@($read)}else{@($read,$make)}
                foreach($control in $actions){
                    if(-not $control.IsVisible){throw ($kind+' training action is not reachable inside the expanded task.')}
                    $bounds=TemplateBoundsUI07 $control $expander
                    if($bounds.Left -lt -1 -or $bounds.Right -gt $expander.ActualWidth+1){throw ($kind+' training action clipped horizontally.')}
                }
                $readBounds=TemplateBoundsUI07 $read $expander;$statusBounds=TemplateBoundsUI07 $status $expander
                if($statusBounds.Right -gt $readBounds.Left+1 -or $status.ActualHeight -gt 24 -or $status.TextWrapping -ne [Windows.TextWrapping]::NoWrap -or $status.TextTrimming -ne [Windows.TextTrimming]::CharacterEllipsis){throw ($kind+' status still wraps into or competes with the primary read action.')}
                if([string]$status.ToolTip -ne $status.Text){throw ($kind+' training status no longer preserves its complete hover explanation.')}
                if($stage -eq 'loaded'){
                    $makeBounds=TemplateBoundsUI07 $make $expander
                    $previewBounds=TemplateBoundsUI07 $preview $expander
                    if([Math]::Abs($makeBounds.Top-$readBounds.Top) -gt 1 -or $makeBounds.Left -lt $readBounds.Right -or $makeBounds.Bottom -gt $previewBounds.Top+1){throw ($kind+' make action is not next to read above the preview. Read='+$readBounds+'; Make='+$makeBounds+'; Preview='+$previewBounds)}
                    $initialMake=TemplateBoundsUI07 $make $scroll
                    if($initialMake.Top -lt -1 -or $initialMake.Bottom -gt $scroll.ActualHeight+1){throw ($kind+' making the template still requires scrolling past the preview.')}
                    $make.BringIntoView();Pump 40
                    $visibleMake=TemplateBoundsUI07 $make $scroll
                    if($visibleMake.Top -lt -1 -or $visibleMake.Bottom -gt $scroll.ActualHeight+1){throw ($kind+' make action cannot be reached through the original parameter scroll area.')}
                }
                $scroll.ScrollToTop();Pump 30
                Shot ('template-'+$kind.ToLower()+'-'+$stage+'-'+$size[0]+'x'+$size[1]) $editor
            }
            # A long diagnostic remains fully available on hover without adding permanent rows.
            $oldStatus=$status.Text
            try {
                $status.Text='输入来源已改变，请重新读取后制作；原已应用模型保持。' * 18;Pump 30
                if($status.ActualHeight -gt 24 -or [string]$status.ToolTip -ne $status.Text){throw ($kind+' long training diagnostic expanded the task or lost full detail.')}
            } finally {$status.Text=$oldStatus;Pump 20}
        }
        if((@((F 'flowTools')|ForEach-Object RuntimeOutputVersion)-join ',') -ne $versions -or (F 'dockDraftDirty') -ne $dirty){throw 'Template layout inspection executed a tool or changed the existing draft.'}
    } finally {
        $editor.Width=$oldWidth;$editor.Height=$oldHeight;$expander.IsExpanded=$oldExpanded
        $scroll.ScrollToVerticalOffset($oldScroll);Pump 40
    }
}

function AssertShapeRoiTaskNavigationUI07($tool) {
    if($Phase -ne 'ui05template'){return}
    if((F 'dockDraftDirty')){throw 'ROI navigation requires a clean applied Shape draft.'}
    $layers=F 'roiLayers';$oldSelection=(F 'RoiLayerList').SelectedItem
    $oldRoiId=$tool.Parameters.ShapeTrainingRoiId;$oldPath=$tool.Parameters.ShapeModelPath;$version=$tool.RuntimeOutputVersion
    $roiType=$assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
    $geometry=$roiType.GetMethod('CreateRectangle').Invoke($null,[object[]]@([double]30,[double]35,[double]90,[double]95))
    $added=$null
    try {
        $added=M 'AddRoiLayer' @($geometry,'UI07 training selection',$null)
        M 'LoadDockConfigurationDraft' @($tool)|Out-Null
        (F 'ShapeModelOptionsUI02').IsExpanded=$true
        (F 'ShapeTrainingExpanderS89').IsExpanded=$false
        $combo=F 'DockShapeTrainingRoiComboBox'
        if(@($combo.Items|Where-Object Key -eq $added.RoiId).Count -ne 1){throw 'Real ROI was not offered by the original training-ROI selector.'}
        $combo.SelectedValue=$added.RoiId;Pump 60
        if(-not (F 'ShapeTrainingExpanderS89').IsExpanded -or -not (F 'ShapeMakeTemplateButtonUI07').IsVisible -or -not (F 'dockDraftDirty') -or $tool.Parameters.ShapeTrainingRoiId -ne $oldRoiId -or $tool.Parameters.ShapeModelPath -ne $oldPath -or $tool.RuntimeOutputVersion -ne $version){throw ('Explicit training-ROI selection did not reveal the original make task as draft-only navigation. Expanded='+(F 'ShapeTrainingExpanderS89').IsExpanded+'; MakeVisible='+(F 'ShapeMakeTemplateButtonUI07').IsVisible+'; Dirty='+(F 'dockDraftDirty')+'; Selected='+$combo.SelectedValue+'; AppliedRoi='+$tool.Parameters.ShapeTrainingRoiId+'; OldRoi='+$oldRoiId+'; SamePath='+($tool.Parameters.ShapeModelPath -eq $oldPath)+'; Version='+$tool.RuntimeOutputVersion+'; OldVersion='+$version)}
        M 'DockRevertButton_Click' @($null,$null)|Out-Null;Pump 40
        if((F 'dockDraftDirty') -or [string]$combo.SelectedValue -ne [string]$oldRoiId -or $tool.Parameters.ShapeModelPath -ne $oldPath -or $tool.RuntimeOutputVersion -ne $version){throw 'Cancelling training-ROI choice changed the applied model or output.'}
    } finally {
        if((F 'dockDraftDirty')){M 'DockRevertButton_Click' @($null,$null)|Out-Null}
        if($null -ne $added -and $layers.Contains($added)){$layers.Remove($added)|Out-Null}
        M 'RefreshRoiLayerSequence'|Out-Null
        (F 'RoiLayerList').SelectedItem=$oldSelection
        M 'SelectRoiLayer' @($oldSelection)|Out-Null
        M 'RefreshRoiLayerBindingSummaries'|Out-Null
        M 'LoadDockConfigurationDraft' @($tool)|Out-Null
        (F 'ShapeModelOptionsUI02').IsExpanded=$false
        if($null -ne $added){$added.Dispose()}
        $geometry.Dispose()
        Pump 40
    }
}

function CompleteTemplateChromeUI07 {
    if($Phase -ne 'ui05template'){return}
    if($window.IsActive -or $window.ShowActivated){throw 'Template UI test activated the desktop.'}
    Write-Output 'UI05_TEMPLATE_CHROME=PASS; ThreeEditorSizes; EmptyPreviewRemoved; EmptyMakeHidden; RealPreviewRetained; MakeActionInsideTask; SingleLineStatusWithFullHover; OriginalScrollReachability; TrainingReturnsToMatching; ExplicitRoiDraftNavigationAndCancel; OriginalShapeNccLifecycleReused; NoMouse'
}
