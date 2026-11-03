# Presentation checks layered onto the original block08 exact-record/offline-communication test.
# Helpers only: no second log store, navigation path, device access, or clipboard/dialog automation.
function StepBoundsUI08($element,$root) {
    $element.TransformToAncestor($root).TransformBounds([Windows.Rect]::new(0,0,$element.ActualWidth,$element.ActualHeight))
}

function StepViewStateUI08 {
    [pscustomobject]@{
        Versions=(@((F 'flowTools')|ForEach-Object RuntimeOutputVersion)-join ',')
        Dirty=(F 'dockDraftDirty')
        Definition=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
        Selected=(F 'FlowToolList').SelectedItem
        Editing=(F 'dockEditingTool')
        ImageKey=(M 'GetImageContextSelectionKeyS112')
        Zoom=(F 'FlowGraphScaleTransform').ScaleX
        NavigationGeneration=(F 'flowStepNavigationGenerationUI07')
        NavigationDefinition=(F 'flowStepNavigationDefinitionUI07')
    }
}

function AssertStepViewStateUI08($before) {
    $after=StepViewStateUI08
    if($before.Versions -ne $after.Versions -or $before.Dirty -ne $after.Dirty -or $before.Definition -ne $after.Definition -or
       -not [object]::ReferenceEquals($before.Selected,$after.Selected) -or -not [object]::ReferenceEquals($before.Editing,$after.Editing) -or
       $before.ImageKey -ne $after.ImageKey -or $before.Zoom -ne $after.Zoom -or
       $before.NavigationGeneration -ne $after.NavigationGeneration -or $before.NavigationDefinition -ne $after.NavigationDefinition){throw 'Step-view presentation changed execution, draft, selection, image, flow zoom, applied definition, or navigation authority.'}
}

function AssertStepChromeUI08([string]$stage='records') {
    if($Phase -ne 'ui05logs'){return}
    $viewer=F 'flowStepActivityWindowS71'
    if($null -eq $viewer -or $viewer.IsActive -or $viewer.ShowActivated -or $window.IsActive -or $viewer.Left -gt -10000){throw 'Step-view chrome test must remain nonactivating and off-screen.'}
    $before=StepViewStateUI08
    $follow=F 'FlowStepFollowS71';$oldFollow=$follow.IsChecked
    $title=F 'FlowStepActivityTitleText';$oldTitle=$title.Text
    $query=F 'FlowStepSearchS71';$oldQuery=$query.Text
    $tabs=F 'FlowStepActivityTabsS71';$oldTab=$tabs.SelectedIndex
    $oldWidth=$viewer.Width;$oldHeight=$viewer.Height
    $follow.IsChecked=$false
    $snapshotLog=F 'flowStepLogSnapshotS71';$snapshotComm=F 'flowStepCommunicationSnapshotS71'
    $menu=(F 'FlowStepTextMenuUI08').Items[0]
    try {
        if($menu.IsSubmenuOpen -or (F 'FlowStepCopyUI08').IsVisible -or (F 'FlowStepExportUI08').IsVisible){throw 'Low-frequency copy/export commands remain exposed in the default step-view toolbar.'}
        if(-not [string]::IsNullOrEmpty((F 'FlowStepNavigationStatusUI07').Text) -or (F 'FlowStepNavigationStatusUI07').Visibility -ne [Windows.Visibility]::Collapsed){throw 'Default navigation explanation still occupies a permanent row.'}
        foreach($size in @(@(520,360),@(660,480),@(900,620))){
            $viewer.Width=$size[0];$viewer.Height=$size[1];$query.Clear();Pump 50
            $header=F 'FlowStepActivityHeaderUI08';$filter=F 'FlowStepFilterBarUI08';$summary=F 'FlowStepSummaryRowUI08'
            foreach($row in @($header,$filter,$summary)){
                if($row.ActualHeight -gt 40 -or $row.ActualWidth -le 0){throw ('Step-view compact row wrapped or collapsed at '+$size[0]+'x'+$size[1]+'; Row='+$row.Name+'; Height='+$row.ActualHeight)}
            }
            if((F 'FlowStepClearSearchUI08').IsVisible){throw 'Clear-search action occupies space when the query is empty.'}
            if($query.ActualWidth -lt 100 -or [string]$title.ToolTip -ne $title.Text){throw 'Step search lost usable width or the title lost its full hover text.'}
            $titleBounds=StepBoundsUI08 $title $header
            $actions=@($header.Children|Where-Object {$_ -is [Windows.Controls.StackPanel]})[0]
            $actionBounds=StepBoundsUI08 $actions $header
            if($titleBounds.Right -gt $actionBounds.Left+1 -or $title.ActualHeight -gt 26 -or $title.TextTrimming -ne [Windows.TextTrimming]::CharacterEllipsis){throw 'Step title competes with refresh/close actions.'}
            $summaryText=F 'FlowStepActivitySummaryText';$summaryBounds=StepBoundsUI08 $summaryText $summary
            $locateBounds=StepBoundsUI08 (F 'FlowStepLocateUI07') $summary
            if($summaryBounds.Right -gt $locateBounds.Left+1){throw 'Step summary overlaps its original navigation actions.'}
            foreach($control in @((F 'FlowStepLocateUI07'),(F 'FlowStepParametersUI07'),$query,(F 'FlowStepTextMenuUI08'))){
                $bounds=StepBoundsUI08 $control (F 'FlowStepActivityPanel')
                if(-not $control.IsVisible -or $bounds.Left -lt -1 -or $bounds.Right -gt (F 'FlowStepActivityPanel').ActualWidth+1){throw ('Step-view action clipped: '+$control.Name)}
            }
            foreach($tab in @(0,1)){
                $tabs.SelectedIndex=$tab;Pump 25
                $text=if($tab -eq 0){F 'FlowStepLogTextBox'}else{F 'FlowStepCommunicationTextBox'}
                if(-not $text.IsVisible -or $text.ActualHeight -lt 145 -or $text.ActualWidth -lt 350){throw ('Step text lost readable space at '+$size[0]+'x'+$size[1]+'; Tab='+$tab+'; Height='+$text.ActualHeight+'; Width='+$text.ActualWidth)}
            }
            $tabs.SelectedIndex=$oldTab;Pump 25
            Shot ('step-lean-'+$stage+'-'+$size[0]+'x'+$size[1]) $viewer
            $title.Text=('Very long module name with full ownership detail / '*16);Pump 25
            $titleBounds=StepBoundsUI08 $title $header
            if($titleBounds.Right -gt $actionBounds.Left+1 -or $header.ActualHeight -gt 40 -or [string]$title.ToolTip -ne $title.Text){throw 'Long step title expanded the header or lost its complete hover text.'}
            $title.Text=$oldTitle
            $query.Text='UI08_UNMATCHED_LAYOUT_QUERY';Pump 25
            if(-not (F 'FlowStepClearSearchUI08').IsVisible -or $query.ActualWidth -lt 100){throw 'Search recovery is hidden or squeezes the query at the minimum window size.'}
            foreach($control in @($query,(F 'FlowStepClearSearchUI08'),(F 'FlowStepTextMenuUI08'))){
                $bounds=StepBoundsUI08 $control $filter
                if($bounds.Left -lt -1 -or $bounds.Right -gt $filter.ActualWidth+1){throw ('Searching makes the step filter toolbar overflow: '+$control.Name)}
            }
            $query.Clear()
        }
        if((F 'flowStepLogSnapshotS71') -ne $snapshotLog -or (F 'flowStepCommunicationSnapshotS71') -ne $snapshotComm){throw 'Step layout inspection replaced the frozen source snapshots.'}
        AssertStepViewStateUI08 $before
    } finally {
        $title.Text=$oldTitle;$query.Text=$oldQuery;$tabs.SelectedIndex=$oldTab
        $viewer.Width=$oldWidth;$viewer.Height=$oldHeight;$follow.IsChecked=$oldFollow;Pump 30
    }
}

function AssertStepSearchUI08 {
    if($Phase -ne 'ui05logs'){return}
    $follow=F 'FlowStepFollowS71'
    if($follow.IsChecked -ne $false){throw 'Search recovery must be checked against the original frozen record fixture.'}
    $before=StepViewStateUI08
    $query=F 'FlowStepSearchS71';$oldQuery=$query.Text
    $tabs=F 'FlowStepActivityTabsS71';$oldTab=$tabs.SelectedIndex
    $log=F 'FlowStepLogTextBox';$comm=F 'FlowStepCommunicationTextBox'
    $snapshotLog=F 'flowStepLogSnapshotS71';$snapshotComm=F 'flowStepCommunicationSnapshotS71'
    try {
        $query.Text='UI08_NO_MATCH_'+[Guid]::NewGuid().ToString('N');Pump 40
        foreach($tab in @(0,1)){
            $tabs.SelectedIndex=$tab;Pump 30
            $text=if($tab -eq 0){$log}else{$comm}
            $empty=if($tab -eq 0){F 'FlowStepLogEmptyUI08'}else{F 'FlowStepCommunicationEmptyUI08'}
            if($text.Text.Length -ne 0 -or -not $empty.IsVisible -or (M 'GetFlowStepViewCopyTextS71') -ne '' -or
               (F 'FlowStepCopyUI08').IsEnabled -or (F 'FlowStepExportUI08').IsEnabled){throw ('Empty-search hint became copied text or left text actions enabled; Tab='+$tab)}
        }
        Shot 'step-search-empty' (F 'flowStepActivityWindowS71')
        $query.Text='EXACT_RECORD_A';Pump 25
        $tabs.SelectedIndex=0;Pump 25
        if($log.Text -notlike '*EXACT_RECORD_A*' -or (F 'FlowStepLogEmptyUI08').IsVisible -or -not (F 'FlowStepCopyUI08').IsEnabled -or -not (F 'FlowStepExportUI08').IsEnabled){throw 'Matching log page did not restore its text actions.'}
        $log.Select(0,[Math]::Min(8,$log.Text.Length))
        if((M 'GetFlowStepViewCopyTextS71') -ne $log.SelectedText){throw 'Original selected-text copy payload changed.'}
        $log.Select(0,0)
        $tabs.SelectedIndex=1;Pump 25
        if($comm.Text.Length -ne 0 -or -not (F 'FlowStepCommunicationEmptyUI08').IsVisible -or (F 'FlowStepCopyUI08').IsEnabled -or (F 'FlowStepExportUI08').IsEnabled){throw 'Switching to an empty communication page retained the other page text-action state.'}
        (F 'FlowStepClearSearchUI08').RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));Pump 30
        if($query.Text.Length -ne 0 -or $follow.IsChecked -ne $false -or $log.Text -ne $snapshotLog -or $comm.Text -ne $snapshotComm -or
           (F 'FlowStepClearSearchUI08').IsVisible -or (F 'FlowStepLogEmptyUI08').Visibility -ne [Windows.Visibility]::Collapsed -or
           (F 'FlowStepCommunicationEmptyUI08').Visibility -ne [Windows.Visibility]::Collapsed -or
           -not (F 'FlowStepCopyUI08').IsEnabled -or -not (F 'FlowStepExportUI08').IsEnabled){throw 'Clear-search did not restore both frozen pages and their text actions without resuming follow.'}
        if((M 'GetFlowStepViewCopyTextS71') -ne $snapshotComm){throw 'Clear-search copy payload is not the original communication snapshot.'}
        if((F 'flowStepLogSnapshotS71') -ne $snapshotLog -or (F 'flowStepCommunicationSnapshotS71') -ne $snapshotComm){throw 'Search or clear changed the frozen underlying snapshots.'}
        AssertStepViewStateUI08 $before
    } finally {$query.Text=$oldQuery;$tabs.SelectedIndex=$oldTab;Pump 25}
}

function CompleteStepChromeUI08 {
    if($Phase -ne 'ui05logs'){return}
    if($window.IsActive -or $window.ShowActivated){throw 'Step-view test activated the desktop.'}
    Write-Output 'UI05_STEP_CHROME=PASS; ThreeWindowSizes; CompactHeaderFilterSummary; DefaultNavigationHelpCollapsed; TextActionsOnDemand; FullTitleHover; FrozenSearchEmptyState; ClearPreservesSnapshotAndFreeze; PerTabTextActions; OriginalCopyPayload; OriginalRecordAndCommunicationChain; NoMouse; NoDevices'
}
