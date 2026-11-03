# Layout and read-only navigation assertions for the original ui02block09 learning chain.
# Helpers only: do not run the main task action, training, help windows, or file dialogs.
function LearningBoundsUI09($element,$root) {
    $element.TransformToAncestor($root).TransformBounds([Windows.Rect]::new(0,0,$element.ActualWidth,$element.ActualHeight))
}

function LearningHoverTextUI09($element) {
    $tip=$element.ToolTip
    if($tip -is [Windows.Controls.ToolTip]){$tip=$tip.Content}
    if($tip -is [Windows.Controls.TextBlock]){return [string]$tip.Text}
    return [string]$tip
}

function LearningViewStateUI09 {
    [pscustomobject]@{
        Versions=(@((F 'flowTools')|ForEach-Object RuntimeOutputVersion)-join ',')
        Definition=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
        Dirty=(F 'dockDraftDirty')
        SourceDraft=(F 'DockLocalImagePathTextBox').Text
        ThresholdDraft=(F 'DockImageThresholdMinTextBox').Text
        Selected=(F 'FlowToolList').SelectedItem
        Editing=(F 'dockEditingTool')
        ImageKey=(M 'GetImageContextSelectionKeyS112')
        Zoom=(F 'FlowGraphScaleTransform').ScaleX
        Session=(F 'learningSessionS146')
        Index=(F 'learningStepsListS146').SelectedIndex
    }
}

function AssertLearningViewStateUI09($before,[bool]$checkIndex=$true) {
    $after=LearningViewStateUI09
    if($before.Versions -ne $after.Versions -or $before.Definition -ne $after.Definition -or $before.Dirty -ne $after.Dirty -or
       $before.SourceDraft -ne $after.SourceDraft -or $before.ThresholdDraft -ne $after.ThresholdDraft -or
       -not [object]::ReferenceEquals($before.Selected,$after.Selected) -or -not [object]::ReferenceEquals($before.Editing,$after.Editing) -or
       -not [object]::ReferenceEquals($before.Session,$after.Session) -or $before.ImageKey -ne $after.ImageKey -or $before.Zoom -ne $after.Zoom -or
       ($checkIndex -and $before.Index -ne $after.Index)){throw 'Learning presentation/navigation changed the applied project, runtime, draft, editor, image, selection, or session.'}
}

function AssertLearningChromeUI09([string]$stage='loaded') {
    if($Phase -ne 'ui05learning'){return}
    if($stage -notmatch '^[a-zA-Z0-9-]+$'){throw 'Invalid learning layout stage.'}
    $guide=F 'learningGuideWindowS146'
    if($null -eq $guide -or $guide.IsActive -or $guide.ShowActivated -or $window.IsActive -or $guide.Left -gt -10000){throw 'Learning chrome must stay nonactivating and off-screen.'}
    if(-not (M 'IsLearningSessionCurrentS146')){throw 'Learning chrome requires the original current session.'}
    $before=LearningViewStateUI09
    $root=$guide.Content
    $header=F 'learningGuideHeaderUI09';$selector=F 'learningGuideStepSelectorUI09'
    $scroll=F 'learningGuideContentUI09';$actionRow=F 'learningGuideActionRowUI09'
    $action=F 'learningStepActionS146';$help=F 'learningStepHelpUI09'
    $title=F 'learningStepTitleS146';$list=F 'learningStepsListS146'
    $previous=F 'learningPreviousS146';$next=F 'learningNextS146'
    $details=F 'learningStepDetailsS148';$detailText=F 'learningStepDetailS148';$state=F 'learningStepStateS146'
    $oldWidth=$guide.Width;$oldHeight=$guide.Height;$oldSizing=$guide.SizeToContent
    $oldExpanded=$details.IsExpanded;$oldScroll=$scroll.VerticalOffset;$oldDetail=$detailText.Text
    $oldIndex=$list.SelectedIndex
    try {
        if($details.IsExpanded -or $state.IsVisible){throw 'Learning technical status still occupies the default task surface.'}
        if($scroll.IsAncestorOf($action) -or $action.Parent -ne $actionRow){throw 'Main learning action still belongs to the scrolling instructions.'}
        $selectedTitle=[string]$list.SelectedItem
        if([string]::IsNullOrWhiteSpace($selectedTitle) -or -not (LearningHoverTextUI09 $title).Contains($selectedTitle) -or
           -not (LearningHoverTextUI09 $list).Contains($selectedTitle)){throw 'Learning title/step selector no longer preserve the complete current task on hover.'}
        $guide.SizeToContent=[Windows.SizeToContent]::Manual
        foreach($size in @(@(360,240),@(430,300),@(600,420))){
            $guide.Width=$size[0];$guide.Height=$size[1];$details.IsExpanded=$false;$scroll.ScrollToTop();Pump 50
            if($header.ActualHeight -gt 42 -or $selector.ActualHeight -gt 40 -or $list.ActualWidth -lt 120){throw ('Learning heading/step selector crowded at '+$size[0]+'x'+$size[1])}
            foreach($control in @($title,$help,$previous,$list,$next,$action)){
                $bounds=LearningBoundsUI09 $control $root
                if(-not $control.IsVisible -or $bounds.Left -lt -1 -or $bounds.Right -gt $root.ActualWidth+1 -or
                   $bounds.Top -lt -1 -or $bounds.Bottom -gt $root.ActualHeight+1){throw ('Learning control clipped at '+$size[0]+'x'+$size[1]+'; Control='+$control.GetType().Name+'; Bounds='+$bounds)}
            }
            $titleBounds=LearningBoundsUI09 $title $header;$helpBounds=LearningBoundsUI09 $help $header
            if($titleBounds.Right -gt $helpBounds.Left+1 -or $title.TextTrimming -ne [Windows.TextTrimming]::CharacterEllipsis){throw 'Learning title overlaps its help action.'}
            $previousBounds=LearningBoundsUI09 $previous $selector;$listBounds=LearningBoundsUI09 $list $selector;$nextBounds=LearningBoundsUI09 $next $selector
            if($previousBounds.Right -gt $listBounds.Left+1 -or $listBounds.Right -gt $nextBounds.Left+1){throw 'Learning step navigation overlaps the direct step selector.'}
            $scrollBounds=LearningBoundsUI09 $scroll $root;$actionBounds=LearningBoundsUI09 $action $root
            if($scroll.ViewportHeight -lt 35 -or $scrollBounds.Bottom -gt $actionBounds.Top+1){throw 'Learning body hides or competes with the fixed primary action.'}
            Shot ('learning-lean-'+$stage+'-'+$size[0]+'x'+$size[1]) $guide
            # Long technical content must scroll without moving the action or header.
            $detailText.Text=($oldDetail+"`r`n")*5;$details.IsExpanded=$true;Pump 35
            if(-not $state.IsVisible -or $scroll.ScrollableHeight -le 0){throw 'Expanded learning details no longer expose actual state through the original scroll area.'}
            $scroll.ScrollToEnd();Pump 35
            $scrolledAction=LearningBoundsUI09 $action $root
            if([Math]::Abs($scrolledAction.Top-$actionBounds.Top) -gt 1 -or $scrolledAction.Bottom -gt $root.ActualHeight+1 -or
               -not $action.IsVisible -or $scroll.VerticalOffset -le 0){throw 'Long learning detail scrolled away the current task action.'}
            $detailText.Text=$oldDetail;$details.IsExpanded=$false;$scroll.ScrollToTop();Pump 25
        }
        # Use the original previous/next buttons, never the module/save action.
        $button=if($next.IsEnabled){$next}else{$previous}
        $expected=if([object]::ReferenceEquals($button,$next)){$oldIndex+1}else{$oldIndex-1}
        if($expected -lt 0 -or $expected -ge $list.Items.Count){throw 'Learning fixture has no safe neighbouring step.'}
        $details.IsExpanded=$true;Pump 25;$scroll.ScrollToEnd();Pump 25
        $button.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));Pump 40
        if($list.SelectedIndex -ne $expected -or $details.IsExpanded -or $scroll.VerticalOffset -gt 1){throw 'Original learning navigation did not select the expected step and reset its detail viewport.'}
        AssertLearningViewStateUI09 $before $false
        $reverse=if([object]::ReferenceEquals($button,$next)){$previous}else{$next}
        $reverse.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));Pump 40
        if($list.SelectedIndex -ne $oldIndex){throw 'Original reverse learning navigation did not restore the current task.'}
        AssertLearningViewStateUI09 $before
    } finally {
        if($list.SelectedIndex -ne $oldIndex -and (M 'IsLearningSessionCurrentS146')){$list.SelectedIndex=$oldIndex}
        $detailText.Text=$oldDetail;$details.IsExpanded=$oldExpanded
        $guide.Width=$oldWidth;$guide.Height=$oldHeight;$guide.SizeToContent=$oldSizing
        $scroll.ScrollToVerticalOffset($oldScroll);Pump 30
    }
}

function CompleteLearningChromeUI09 {
    if($Phase -ne 'ui05learning'){return}
    if($window.IsActive -or $window.ShowActivated){throw 'Learning guide test activated the desktop.'}
    Write-Output 'UI05_LEARNING_CHROME=PASS; ThreeWindowSizes; StepSelectorAndNavigationTogether; HelpInHeader; TechnicalStateOnDemand; FixedPrimaryAction; LongDetailsScroll; FullTaskHover; OriginalPreviousNextEvents; DraftProjectRuntimeAndImagePreserved; OriginalLearningChainReused; NoMouse; NoDevices'
}
