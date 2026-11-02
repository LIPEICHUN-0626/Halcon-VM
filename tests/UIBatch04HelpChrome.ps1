# Original read-only help navigation and learning links. No physical input or example loading.
$flow=F 'flowTools';$threshold=$flow[1]
M 'OpenCentralModuleConfigurationS77' @($threshold,$false)|Out-Null
(F 'DockImageThresholdMinTextBox').Text='1'
(F 'FlowToolList').SelectedItem=$flow[0]
M 'RecipeAutosaveTimer_Tick' @($null,$null)|Out-Null
$definition=$service.GetRecipeFingerprint((M 'CaptureRecipe'))
$ids=@($flow|ForEach-Object ToolId)-join ',';$dirty=F 'isRecipeDirty';$draft=F 'dockDraftDirty';$editor=F 'moduleParameterWindowS89'
M 'OpenCurrentModuleHelpMenuItem_ClickS78' @($null,$null)|Out-Null;Pump 80
$help=F 'helpCenterWindowS78';$ht=$help.GetType()
function HF09([string]$name){Write-Output -NoEnumerate ($ht.GetField($name,$all).GetValue($help))}
function ClickHelp09([string]$name){(HF09 $name).RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));Pump 40}
function HelpText09{$d=HF09 'HelpDocument';[Windows.Documents.TextRange]::new($d.ContentStart,$d.ContentEnd).Text}
if((HF09 'HelpTopicList').SelectedItem.ToolKind -ne $threshold.Kind -or (HF09 'HelpBackUI09').IsEnabled){throw 'Initial current-tool help has incorrect ownership or synthetic navigation history.'}
$topicList=HF09 'HelpTopicList';$currentItem=$topicList.ItemContainerGenerator.ContainerFromItem($topicList.SelectedItem)
if($null -eq $currentItem){throw 'Initial current-module directory item was not brought into view.'}
$currentBounds=$currentItem.TransformToAncestor($topicList).TransformBounds([Windows.Rect]::new(0,0,$currentItem.ActualWidth,$currentItem.ActualHeight))
if($currentBounds.Top -lt -1 -or $currentBounds.Bottom -gt $topicList.ActualHeight+1){throw 'Initial current-module help directory remains scrolled to another topic.'}
foreach($size in @(@(760,520),@(960,660),@(1200,780))){
    $help.Width=$size[0];$help.Height=$size[1];Pump 50
    $scope=HF09 'HelpScopeUI10'
    for($i=0;$i -lt 3;$i++){
        $item=$scope.Items[$i]
        $bounds=$item.TransformToAncestor($scope).TransformBounds([Windows.Rect]::new(0,0,$item.ActualWidth,$item.ActualHeight))
        if(-not $item.IsVisible -or $item.ActualWidth -lt 40 -or $bounds.Right -gt $scope.ActualWidth+1){throw 'Direct help scope is hidden or clipped.'}
    }
    foreach($name in @('HelpBackUI09','HelpHomeUI09','HelpSectionPickerUI10')){if(-not (HF09 $name).IsVisible){throw ('Missing help navigation: '+$name)}}
    Shot ('module-'+$size[0]+'x'+$size[1]) $help
}
(HF09 'HelpScopeUI10').SelectedIndex=2
(HF09 'HelpSearchTextBox').Text='NccTemplateMatch';Pump 40
if((HF09 'visibleTopics').Count -ne 1 -or (HF09 'HelpTopicList').SelectedItem.ToolKind.ToString() -ne 'NccTemplateMatch'){throw 'Actual module metadata search failed.'}
$ncc=(HF09 'HelpTopicList').SelectedItem
(HF09 'HelpSearchTextBox').Text='ui04-help-no-match';Pump 30
if((HF09 'helpSectionsUI10').Count -or (HF09 'HelpSectionBarUI10').Visibility -ne 'Collapsed' -or (HelpText09) -notlike '*没有匹配*'){throw 'No-match search kept stale article or chapter anchors.'}
Shot 'no-matching-help' $help
ClickHelp09 'HelpClearSearchUI10'
if((HF09 'HelpTopicList').SelectedItem -ne $ncc -or (HF09 'HelpScopeUI10').SelectedIndex -ne 2){throw 'Clearing search lost the last real module or selected scope.'}
ClickHelp09 'HelpBackUI09'
if((HF09 'HelpTopicList').SelectedItem.ToolKind -ne $threshold.Kind -or (HF09 'HelpSearchTextBox').Text -ne '' -or (HF09 'HelpScopeUI10').SelectedIndex -ne 0 -or (HF09 'HelpBackUI09').IsEnabled){throw 'Back did not restore the actual previous topic, or pushed transient filter topics.'}
ClickHelp09 'HelpHomeUI09'
if((HF09 'HelpTopicList').SelectedItem.Key -ne 'QuickStart' -or (HelpText09) -notlike '*六*' -and (HelpText09) -notlike '*保存：*'){throw 'Home did not open the original six-step quick start.'}
Shot 'quick-start' $help
$link=$null
foreach($block in (HF09 'HelpDocument').Blocks){if($block -is [Windows.Documents.Paragraph]){foreach($inline in $block.Inlines){if($inline -is [Windows.Documents.Hyperlink] -and [Windows.Documents.TextRange]::new($inline.ContentStart,$inline.ContentEnd).Text -eq '示例与完整学习项目'){$link=$inline}}}}
if($null -eq $link){throw 'Original learning help link missing.'}
$link.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Documents.Hyperlink]::ClickEvent));Pump 40
if((HF09 'HelpTopicList').SelectedItem.Key -ne 'LearningProjects'){throw 'Learning link did not open the original article.'}
ClickHelp09 'HelpBackUI09'
if((HF09 'HelpTopicList').SelectedItem.Key -ne 'QuickStart'){throw 'Back from learning article missed quick start.'}
$help.SelectToolTopicUI10($threshold.Kind);Pump 30
$picker=HF09 'HelpSectionPickerUI10';$doc=HF09 'HelpDocument'
$picker.SelectedItem=@($picker.Items|Where-Object Title -eq '输出')[0];Pump 40
if($picker.SelectedItem.Title -ne '输出' -or -not $doc.Blocks.Contains($picker.SelectedItem.Target)){throw 'Chapter picker targets a stale document.'}
Shot 'module-output-section' $help
M 'OpenCurrentModuleHelpMenuItem_ClickS78' @($null,$null)|Out-Null
if((F 'helpCenterWindowS78') -ne $help -or (HF09 'HelpTopicList').SelectedItem.ToolKind -ne $threshold.Kind){throw 'Help entry duplicated the window or followed single-selected source.'}
# Bounded local reading history; restoring does not create a forward/back cycle.
for($i=0;$i -lt 36;$i++){ClickHelp09 'HelpHomeUI09';$help.SelectToolTopicUI10($threshold.Kind)}
if((HF09 'topicHistoryUI09').Count -ne 32){throw 'Help reading history is not bounded to 32 topics.'}
for($i=0;$i -lt 32;$i++){ClickHelp09 'HelpBackUI09'}
if((HF09 'topicHistoryUI09').Count -or (HF09 'HelpBackUI09').IsEnabled){throw 'Back created a navigation cycle instead of consuming history.'}
if((F 'moduleParameterWindowS89') -ne $editor -or (F 'dockEditingTool') -ne $threshold -or (F 'dockDraftDirty') -ne $draft -or (F 'isRecipeDirty') -ne $dirty -or $service.GetRecipeFingerprint((M 'CaptureRecipe')) -ne $definition -or (@($flow|ForEach-Object ToolId)-join ',') -ne $ids -or @($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count){throw 'Read-only help navigation changed draft, project or executed a tool.'}
if($help.IsActive -or $help.ShowActivated -or $help.Left -gt -10000 -or $window.IsActive){throw 'Help test activated foreground.'}
$help.Close();M 'DockRevertButton_Click' @($null,$null)|Out-Null;M 'CloseCentralModuleConfigurationS77'|Out-Null
Write-Output 'UI04_HELP_CHROME=PASS; ThreeSizes; DirectScopes; EditedModuleOwnership; ActualMetadataSearch; ClearRestoresLastModule; GenuineHomeBackLearningLink; BoundedHistory; OriginalChapters; SingleWindow; DraftAndProjectUnchanged; NoExampleLoadOrRun; NoMouse'
