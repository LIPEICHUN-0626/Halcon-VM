# Display-only interactions and bounded panel content; never open a native popup on the desktop.
function DisplayBounds05($element,$root){$element.TransformToAncestor($root).TransformBounds([Windows.Rect]::new(0,0,$element.ActualWidth,$element.ActualHeight))}
function ClickDisplay05([string]$name){(F $name).RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent));Pump 50}
function RefreshDisplay05 {M 'RefreshUiState'|Out-Null;Pump 50}
$popup=F 'ImageDisplayPopupUI05';$panel=F 'ImageDisplayPanelUI05';$toggle=F 'ImageTaskToggleButton'
$binding=[Windows.Data.BindingOperations]::GetBinding($popup,[Windows.Controls.Primitives.Popup]::IsOpenProperty)
if($null -eq $binding -or $binding.ElementName -ne 'ImageTaskToggleButton' -or $binding.Mode -ne 'TwoWay' -or $popup.StaysOpen -or $popup.PlacementTarget -ne $toggle){throw 'Original toggle / popup close contract missing.'}
# Isolate native popup placement from automated checks. Test its actual content in a non-activated offscreen host.
[Windows.Data.BindingOperations]::ClearBinding($popup,[Windows.Controls.Primitives.Popup]::IsOpenProperty)
$popup.IsOpen=$false;$popup.Child=$null
$hostWindow=[Windows.Window]::new();$hostWindow.ShowActivated=$false;$hostWindow.ShowInTaskbar=$false
$hostWindow.WindowStartupLocation='Manual';$hostWindow.WindowStyle='None';$hostWindow.ResizeMode='NoResize'
$hostWindow.Left=-12000;$hostWindow.Top=-12000;$hostWindow.SizeToContent='WidthAndHeight';$hostWindow.Resources=$window.Resources;$hostWindow.Content=$panel
try {
    $hostWindow.Show();Pump 80
    foreach($size in @(@(1120,700),@(1366,768),@(1600,900))){
        $window.Width=$size[0];$window.Height=$size[1];Pump 60
        $toolbar=F 'ImageWorkspaceToolbarPanel';$before=(F 'ImageWorkspacePanel').RenderSize
        $toggle.IsChecked=$true;Pump 30
        if((F 'ImageWorkspacePanel').RenderSize -ne $before -or $popup.IsOpen){throw 'Display state resized canvas or opened native popup during test.'}
        foreach($name in @('ImageSelectButton','ImagePanButton','ImageFitButton','ImageOneToOneButton','RoiToolsToggleButton','ImageTaskToggleButton')){
            $bounds=DisplayBounds05 (F $name) $toolbar
            if($bounds.Left -lt -1 -or $bounds.Right -gt $toolbar.ActualWidth+1){throw ('Toolbar action clipped: '+$name)}
        }
        (F 'HeaderStatusText').Text='当前操作未完成：请先选择正确来源，再执行操作。' * 8
        (F 'ImagePixelStatusText').Text='像素：R 12345, C 67890, G RGB(255, 128, 64)';Pump 30
        $status=F 'ImageStatusBarUI05';$feedback=DisplayBounds05 (F 'HeaderStatusText') $status;$pixel=DisplayBounds05 (F 'ImagePixelStatusText') $status
        if($feedback.Width -lt 150 -or $feedback.Right -gt $pixel.Left+1 -or $pixel.Right -gt $status.ActualWidth+1 -or $pixel.Width -lt 100 -or $status.ActualHeight -gt 28 -or (F 'HeaderStatusText').ToolTip -ne (F 'HeaderStatusText').Text){throw 'Single-line feedback / pixel status overlap, lose full detail or overflow.'}
        Shot ('display-tools-'+$size[0]+'x'+$size[1])
        M 'CloseImageDisplayUI05'|Out-Null
    }
    foreach($name in @('ImageWorkspaceExpandButton','ImageLayoutToggleButton','ShowRoiOverlayCheckBox','ShowResultOverlayCheckBox','ToggleRoiLayerPanelButton','CrosshairPanelToggleButton','ImageQueuePanelToggleButton','XldContourSelectionToggleButton','EditMeasurementDomainButton')){
        $bounds=DisplayBounds05 (F $name) $panel
        if($bounds.Left -lt 0 -or $bounds.Right -gt $panel.ActualWidth+1 -or $bounds.Bottom -gt $panel.ActualHeight+1){throw ('Display group clipped: '+$name)}
    }
    if($panel.ActualWidth -gt 309 -or $panel.ActualHeight -gt 380){throw 'Display panel is not bounded.'}
    Shot 'display-panel-content' $hostWindow
    # The separate historical return affordance cannot push common toolbar actions out of a narrow view.
    $window.Width=1120;$window.Height=700;(F 'ReturnLiveImageButton').Visibility='Visible';Pump 40
    if((DisplayBounds05 (F 'ImageTaskToggleButton') (F 'ImageWorkspaceToolbarPanel')).Right -gt (F 'ImageWorkspaceToolbarPanel').ActualWidth+1){throw 'Historical return displaced common toolbar.'}
    (F 'ReturnLiveImageButton').Visibility='Collapsed'
    # One tiny real image/output fixture. Subsequent display operations must preserve its snapshots and bindings.
    $flow=F 'flowTools';$source=$flow[0];$threshold=$flow[1]
    $path=Join-Path $temp 'display-tools.png';[HalconDotNet.HObject]$im=$null
    try{[HalconDotNet.HOperatorSet]::GenImageConst([ref]$im,'byte',64,48);[HalconDotNet.HOperatorSet]::WriteImage($im,'png',0,$path)}finally{if($im){$im.Dispose()}}
    $source.Parameters.LocalImagePath=$path;$threshold.Parameters.ImageThresholdMinGray=0;$threshold.Parameters.ImageThresholdMaxGray=255
    M 'RunStandaloneToolCoreUI02' @($source,'UI05 display fixture',$false)|Out-Null
    M 'RunStandaloneToolCoreUI02' @($threshold,'UI05 output fixture',$false)|Out-Null
    (F 'FlowToolList').SelectedItem=$threshold;M 'SetImageContextSelection' @('ModuleInput')|Out-Null;M 'UpdateImageContextViewport' @($true)|Out-Null
    $layers=F 'roiLayers';$roiType=$assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
    foreach($i in @(0,1,2)){
        $layer=[Activator]::CreateInstance($assembly.GetType('HalconWinFormsDemo.Models.VmRoiLayer',$true))
        $layer.Name='显示层'+$i;$layer.Geometry=$roiType.GetMethod('CreateRectangle').Invoke($null,[object[]]@(2.0,2.0,8.0,8.0))
        $layer.ImageContextId=$(if($i -eq 2){'Unrelated.Image'}else{M 'ResolveTargetImageContextIdS86' @($threshold)})
        $layer.IsMask=($i -eq 1);$layers.Add($layer)
    }
    RefreshDisplay05
    if((F 'RoiOverlayLegendText').Text -ne 'ROI：2 · 屏蔽 1'){throw 'Short ROI count includes unrelated coordinates or loses mask role.'}
    $version=$threshold.RuntimeOutputVersion;$sourceVersion=$source.RuntimeOutputVersion;$bindingSource=$threshold.GetInputBinding('Image').SourceToolId
    [object]$snapshot=$null;$source.TryGetOutputValue('Image',[ref]$snapshot)|Out-Null
    ClickDisplay05 'ImageOneToOneButton'
    if((F 'viewport').ZoomText -notlike '*100%*'){throw 'Original 1:1 action no longer updates viewport.'}
    ClickDisplay05 'ImageFitButton';ClickDisplay05 'ImagePanButton'
    if((F 'HeaderStatusText').Text -notlike '*右键*'){throw 'Pan guidance no longer matches right-button behavior.'}
    ClickDisplay05 'ImageSelectButton'
    $toggle.IsChecked=$true;(F 'ShowRoiOverlayCheckBox').IsChecked=$false;(F 'ShowResultOverlayCheckBox').IsChecked=$false;RefreshDisplay05
    if(-not $toggle.IsChecked -or (F 'OverlayStatusText').Text -ne '结果：已隐藏' -or (F 'RoiOverlayLegendText').Text -ne 'ROI：隐藏' -or $layers.Count -ne 3){throw 'Overlay toggles closed panel or removed data / lost hidden semantics.'}
    (F 'ShowRoiOverlayCheckBox').IsChecked=$true;(F 'ShowResultOverlayCheckBox').IsChecked=$true
    M 'SetAllResultLayerVisibility' @($false)|Out-Null
    if((F 'OverlayStatusText').Text -ne '结果：全部隐藏' -or (F 'OverlayStatusText').ToolTip -notlike '*输出保留*'){throw 'Original layer visibility path restored verbose/stale status.'}
    M 'SetAllResultLayerVisibility' @($true)|Out-Null
    if((F 'OverlayStatusText').Text.Length -gt 32 -or (F 'OverlayStatusText').ToolTip -notlike '*有效*'){throw 'Layer status not compact with full details.'}
    (F 'OverlayStatusText').Text='stale verbose status fixture'
    M 'ResultLayerVisibilityCheckBox_Changed' @($null,$null)|Out-Null
    if((F 'OverlayStatusText').Text -like '*fixture*'){throw 'Individual visibility path bypassed compact status refresh.'}
    (F 'ResultLayerList').SelectedItem=@((F 'resultOverlayLayers'))[0]
    M 'IsolateResultLayerButton_Click' @($null,$null)|Out-Null
    if((F 'OverlayStatusText').Text.Length -gt 32 -or (F 'OverlayStatusText').ToolTip -notlike '*有效*'){throw 'Isolate path restored long/stale legend.'}
    M 'SetAllResultLayerVisibility' @($true)|Out-Null
    $toggle.IsChecked=$true;ClickDisplay05 'ImageLayoutToggleButton'
    if(-not (F 'dualImageMode') -or $toggle.IsChecked -or (F 'LinkDualImageViewsCheckBox').Visibility -ne 'Visible'){throw 'Original dual-view action/link state or action dismissal failed.'}
    (F 'LinkDualImageViewsCheckBox').IsChecked=$false
    if(M 'AreDualImageViewsLinked'){throw 'Original independent view toggle failed.'}
    (F 'LinkDualImageViewsCheckBox').IsChecked=$true;ClickDisplay05 'ImageLayoutToggleButton'
    $toggle.IsChecked=$true;(F 'XldContourSelectionToggleButton').IsChecked=$true;ClickDisplay05 'XldContourSelectionToggleButton'
    if($toggle.IsChecked -or -not (F 'xldContourSelectionMode')){throw 'Contour interaction left display panel open.'}
    (F 'XldContourSelectionToggleButton').IsChecked=$false;ClickDisplay05 'XldContourSelectionToggleButton'
    # Escape closes only this display panel, preserving the original parameter draft.
    M 'OpenCentralModuleConfigurationS77' @($threshold,$false)|Out-Null;(F 'DockImageThresholdMinTextBox').Text='1';$editor=F 'moduleParameterWindowS89'
    $toggle.IsChecked=$true
    $key=[Windows.Input.KeyEventArgs]::new([Windows.Input.Keyboard]::PrimaryDevice,[Windows.PresentationSource]::FromVisual($window),0,[Windows.Input.Key]::Escape)
    $key.RoutedEvent=[Windows.Input.Keyboard]::PreviewKeyDownEvent
    M 'MainWindow_PreviewKeyDown' @($window,$key)|Out-Null
    if(-not $key.Handled -or $toggle.IsChecked -or -not (F 'dockDraftDirty') -or (F 'moduleParameterWindowS89') -ne $editor){throw 'Escape dismissed parameter draft instead of display panel.'}
    M 'DockRevertButton_Click' @($null,$null)|Out-Null;M 'CloseCentralModuleConfigurationS77'|Out-Null
    $toggle.IsChecked=$true;M 'SetProductionRunMode' @($true)|Out-Null
    if($toggle.IsChecked -or (F 'ImageWorkspaceToolbarPanel').IsVisible){throw 'Run mode retained engineering display panel.'}
    M 'SetProductionRunMode' @($false)|Out-Null
    [object]$after=$null;$source.TryGetOutputValue('Image',[ref]$after)|Out-Null
    if($threshold.RuntimeOutputVersion -ne $version -or $source.RuntimeOutputVersion -ne $sourceVersion -or -not [object]::ReferenceEquals($snapshot,$after) -or $threshold.GetInputBinding('Image').SourceToolId -ne $bindingSource -or $layers.Count -ne 3){throw 'Display interaction changed runtime outputs, source binding or ROI data.'}
    if($hostWindow.IsActive -or $window.IsActive -or $hostWindow.ShowActivated){throw 'Display check activated desktop.'}
    Write-Output 'UI04_DISPLAY_TOOLS=PASS; ThreeSizes; BoundedRealPanelContent; FeedbackPixelSeparation; ActualImagePreserved; FitAnd1to1; CurrentRoiCounts; OverlayDataPreserved; DualViewLink; ContourDismissal; EscapePreservesDraft; ModeCloses; NativePopupPlacementNotTested; NoMouse'
} finally {
    $toggle.IsChecked=$false;$hostWindow.Content=$null;$hostWindow.Close();$popup.Child=$panel
    [Windows.Data.BindingOperations]::SetBinding($popup,[Windows.Controls.Primitives.Popup]::IsOpenProperty,$binding)|Out-Null
}
