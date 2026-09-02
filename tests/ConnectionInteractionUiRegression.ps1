param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$ConnectionPath = (Join-Path $ProjectRoot 'artifacts\connection-fix\connection-binding.png'),
    [string]$InputPath = (Join-Path $ProjectRoot 'artifacts\connection-fix\connection-inputs.png'),
    [int]$WindowWidth = 1366,
    [int]$WindowHeight = 768
)
$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'; $env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Threading;
public static class S79Mouse {
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
 [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
 public const uint LD=0x0002, LU=0x0004;
 public static void ClickAsync(int x, int y) { var t=new Thread(()=>{SetCursorPos(x,y);Thread.Sleep(120);mouse_event(LD,0,0,0,UIntPtr.Zero);Thread.Sleep(90);mouse_event(LU,0,0,0,UIntPtr.Zero);});t.IsBackground=true;t.Start(); }
 public static void DragAsync(int x, int y, int tx, int ty) { var t=new Thread(()=>{SetCursorPos(x,y);Thread.Sleep(120);mouse_event(LD,0,0,0,UIntPtr.Zero);for(int i=1;i<=20;i++){SetCursorPos(x+(tx-x)*i/20,y+(ty-y)*i/20);Thread.Sleep(25);}mouse_event(LU,0,0,0,UIntPtr.Zero);});t.IsBackground=true;t.Start(); }
 public static void EscapeAsync() { var t=new Thread(()=>{keybd_event(27,0,0,UIntPtr.Zero);Thread.Sleep(60);keybd_event(27,0,2,UIntPtr.Zero);});t.IsBackground=true;t.Start(); }
}
'@
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Pump([int]$ms) { $frame=New-Object Windows.Threading.DispatcherFrame; $timer=New-Object Windows.Threading.DispatcherTimer; $timer.Interval=[TimeSpan]::FromMilliseconds($ms); $timer.Add_Tick({$timer.Stop();$frame.Continue=$false}); $timer.Start(); [Windows.Threading.Dispatcher]::PushFrame($frame) }
function Shot([string]$path) { [IO.Directory]::CreateDirectory((Split-Path -Parent $path))|Out-Null;$window.UpdateLayout();$root=$window.Content;$top=$root.PointToScreen([Windows.Point]::new(0,0));$end=$root.PointToScreen([Windows.Point]::new($root.ActualWidth,$root.ActualHeight));$screen=[Windows.Forms.Screen]::FromPoint([Drawing.Point]::new([int]$top.X,[int]$top.Y)).Bounds;$bmp=New-Object Drawing.Bitmap([int][Math]::Min($end.X-$top.X,$screen.Right-$top.X),[int][Math]::Min($end.Y-$top.Y,$screen.Bottom-$top.Y));$g=[Drawing.Graphics]::FromImage($bmp);try{$g.CopyFromScreen([int]$top.X,[int]$top.Y,0,0,$bmp.Size);$bmp.Save($path,[Drawing.Imaging.ImageFormat]::Png)}finally{$g.Dispose();$bmp.Dispose()} }
$mainType=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$temp=Join-Path([IO.Path]::GetTempPath())('halcon-s79-ui-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$mainType.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function F([string]$name){$value=$mainType.GetField($name,$all).GetValue($window);Write-Output -NoEnumerate $value}
function M([string]$name,[object[]]$invokeArgs=@()){$method=$mainType.GetMethods($private)|Where-Object Name -eq $name|Where-Object{$_.GetParameters().Count -eq $invokeArgs.Count}|Select-Object -First 1;if($null -eq $method){throw "Missing $name/$($invokeArgs.Count)"};$method.Invoke($window,$invokeArgs)}
function Tool([string]$kind,[string]$name){$mainType.GetMethod('CreateFlowTool',$private).Invoke($window,[object[]]@([Enum]::Parse($kindType,$kind),$name,$true,$null))}
$app=[Windows.Application]::Current;if($null-eq$app){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown}
try {
    $window.Topmost=$true;$window.Show();$window.WindowState=[Windows.WindowState]::Normal;$window.MinWidth=0;$window.MinHeight=0;$window.Left=2;$window.Top=2;$window.Width=$WindowWidth;$window.Height=$WindowHeight;Pump 700
    $flow=F 'flowTools';M 'DisposeFlowTools'|Out-Null;$flow.Clear();$source=Tool ImageSource '01 图像 A';$target=Tool ImageThreshold '03 阈值分割';$judge=Tool NumericJudge '04 数值判定';$flow.Add($source);$sourceB=Tool ImageSource '02 图像 B';$flow.Add($sourceB);$flow.Add($target);$flow.Add($judge);M 'RefreshUiState'|Out-Null;M 'RefreshFlowGraphLayout'|Out-Null;Pump 600
    $items=F 'FlowGraphNodeItemsControl';$sourcePresenter=$items.ItemContainerGenerator.ContainerFromItem($source);$targetPresenter=$items.ItemContainerGenerator.ContainerFromItem($target)
    if($null-eq$sourcePresenter-or$null-eq$targetPresenter){throw 'Node presenters unavailable'}
    $outAnchor=$sourcePresenter.ContentTemplate.FindName('S75OutputAnchor',$sourcePresenter);$inAnchor=$targetPresenter.ContentTemplate.FindName('S75InputAnchor',$targetPresenter)
    if($outAnchor.ActualWidth-gt18-or$inAnchor.ActualWidth-gt18){throw 'Anchor hit targets still cover too much of the draggable card'}
    $card=$sourcePresenter.ContentTemplate.FindName('GraphToolCard',$sourcePresenter)
    $anchorCenter=$outAnchor.TranslatePoint([Windows.Point]::new($outAnchor.ActualWidth/2,$outAnchor.ActualHeight/2),$card)
    if([Math]::Abs($anchorCenter.Y-$card.ActualHeight)-gt5){throw ("Bottom anchor is on header, not card edge: centerY="+$anchorCenter.Y+"; cardHeight="+$card.ActualHeight)}
    $helper=New-Object Windows.Interop.WindowInteropHelper -ArgumentList $window;[S79Mouse]::SetForegroundWindow($helper.Handle)|Out-Null
    $anchorCanvasPoint=$outAnchor.TranslatePoint([Windows.Point]::new($outAnchor.ActualWidth/2,$outAnchor.ActualHeight/2),(F 'FlowGraphCanvas'))
    $point=$outAnchor.PointToScreen([Windows.Point]::new($outAnchor.ActualWidth/2,$outAnchor.ActualHeight/2));[S79Mouse]::ClickAsync([int]$point.X,[int]$point.Y);Pump 700
    $pendingAfterSource=F 'flowPortDragSession';if($null-eq$pendingAfterSource-or-not$pendingAfterSource.IsClickArmed){throw ('Source anchor did not enter two-click connection state; CanvasPoint='+$anchorCanvasPoint+'; Expected='+$source.GraphX+','+($source.GraphY+$source.GraphHeight)+'; Status='+(F 'HeaderStatusText').Text)}
    $targetCard=$targetPresenter.ContentTemplate.FindName('GraphToolCard',$targetPresenter);$point=$targetCard.PointToScreen([Windows.Point]::new(85,20));[S79Mouse]::ClickAsync([int]$point.X,[int]$point.Y);Pump 1100
    $binding=$target.GetInputBinding('Image');if($null-eq$binding-or$binding.SourceToolId-ne$source.ToolId-or$binding.SourcePortName-ne'Image'){throw ('Physical click connection failed; Status='+(F 'HeaderStatusText').Text)}
    # Re-select from the visible binding dropdown using actual mouse events.
    $combo=F 'FlowConnectionBindingComboS79';$point=$combo.PointToScreen([Windows.Point]::new($combo.ActualWidth-12,$combo.ActualHeight/2));[S79Mouse]::ClickAsync([int]$point.X,[int]$point.Y);Pump 500
    $option=@($combo.Items|Where-Object SourceToolId -eq $sourceB.ToolId)[0];$container=$combo.ItemContainerGenerator.ContainerFromItem($option)
    if($null-eq$container){throw 'Binding dropdown is not visibly open'}
    $point=$container.PointToScreen([Windows.Point]::new(50,$container.ActualHeight/2));[S79Mouse]::ClickAsync([int]$point.X,[int]$point.Y);Pump 650
    if($target.GetInputBinding('Image').SourceToolId-ne$sourceB.ToolId){throw 'Physical dropdown source reselect did not update InputBindings immediately'}
    # Drag from the source anchor to the middle of the destination card.
    [S79Mouse]::SetForegroundWindow($helper.Handle)|Out-Null;Pump 200
    $sourcePresenter=$items.ItemContainerGenerator.ContainerFromItem($source);$targetPresenter=$items.ItemContainerGenerator.ContainerFromItem($target)
    $outAnchor=$sourcePresenter.ContentTemplate.FindName('S75OutputAnchor',$sourcePresenter);$targetCard=$targetPresenter.ContentTemplate.FindName('GraphToolCard',$targetPresenter)
    $point=$outAnchor.PointToScreen([Windows.Point]::new($outAnchor.ActualWidth/2,$outAnchor.ActualHeight/2));$end=$targetCard.PointToScreen([Windows.Point]::new(85,20));[S79Mouse]::DragAsync([int]$point.X,[int]$point.Y,[int]$end.X,[int]$end.Y);Pump 1300
    if($target.GetInputBinding('Image').SourceToolId-ne$source.ToolId){throw ('Physical body drop failed: '+(F 'HeaderStatusText').Text)}
    # At 65% zoom, the visible compact dot remains an exact usable connection target.
    [S79Mouse]::SetForegroundWindow($helper.Handle)|Out-Null;Pump 200
    (F 'FlowZoomSlider').Value=0.65;M 'RefreshFlowGraphLayout'|Out-Null;(F 'FlowGraphViewport').ScrollToTop();Pump 500
    $sourceBPresenter=$items.ItemContainerGenerator.ContainerFromItem($sourceB);$targetPresenter=$items.ItemContainerGenerator.ContainerFromItem($target)
    $outAnchor=$sourceBPresenter.ContentTemplate.FindName('S75OutputAnchor',$sourceBPresenter);$targetCard=$targetPresenter.ContentTemplate.FindName('GraphToolCard',$targetPresenter)
    $point=$outAnchor.PointToScreen([Windows.Point]::new($outAnchor.ActualWidth/2,$outAnchor.ActualHeight/2));[S79Mouse]::ClickAsync([int]$point.X,[int]$point.Y);Pump 350
    $end=$targetCard.PointToScreen([Windows.Point]::new(85,20));[S79Mouse]::ClickAsync([int]$end.X,[int]$end.Y);Pump 650
    if($target.GetInputBinding('Image').SourceToolId-ne$sourceB.ToolId){throw ('Zoomed compact anchor did not auto-bind: '+(F 'HeaderStatusText').Text)}
    # Selecting an existing short connection must not accidentally start a new one.
    [S79Mouse]::SetForegroundWindow($helper.Handle)|Out-Null;Pump 200
    M 'CloseFlowConnectionInspectorButton_Click' @($null,$null)|Out-Null;Pump 250
    $linePoint=[Windows.Point]::new($sourceB.GraphX+140,($sourceB.GraphY+$sourceB.GraphHeight+$target.GraphY)/2)
    $point=(F 'FlowGraphCanvas').PointToScreen($linePoint);[S79Mouse]::ClickAsync([int]$point.X,[int]$point.Y);Pump 400
    $lineSession=F 'flowPortDragSession';$lineInspector=(F 'FlowConnectionInspectorBorder').Visibility.ToString();if($null-ne$lineSession-or$lineInspector-ne'Visible'){throw ('Selecting an existing line was stolen by the anchor hit area; Session='+($null-ne$lineSession)+'; Inspector='+$lineInspector+'; Status='+(F 'HeaderStatusText').Text)}
    # Invalid endpoint and Esc preserve the current real input binding.
    $targetPresenter=$items.ItemContainerGenerator.ContainerFromItem($target);$outAnchor=$targetPresenter.ContentTemplate.FindName('S75OutputAnchor',$targetPresenter)
    $point=$outAnchor.PointToScreen([Windows.Point]::new($outAnchor.ActualWidth/2,$outAnchor.ActualHeight/2));[S79Mouse]::ClickAsync([int]$point.X,[int]$point.Y);Pump 250
    $sourceBPresenter=$items.ItemContainerGenerator.ContainerFromItem($sourceB);$sourceCard=$sourceBPresenter.ContentTemplate.FindName('GraphToolCard',$sourceBPresenter);$point=$sourceCard.PointToScreen([Windows.Point]::new(85,20));[S79Mouse]::ClickAsync([int]$point.X,[int]$point.Y);Pump 300
    if($null-eq(F 'flowPortDragSession')-or$target.GetInputBinding('Image').SourceToolId-ne$sourceB.ToolId){throw ('Invalid click discarded the old binding or silently ended the pending connection; Status='+(F 'HeaderStatusText').Text+'; Source='+$target.GetInputBinding('Image').SourceToolId+'; Expected='+$sourceB.ToolId)}
    [S79Mouse]::EscapeAsync();Pump 300
    if($null-ne(F 'flowPortDragSession')-or$target.GetInputBinding('Image').SourceToolId-ne$sourceB.ToolId){throw 'Esc did not cancel cleanly'}
    $bar=F 'FlowConnectionInspectorBorder';if($bar.ActualWidth-gt(F 'FlowWorkspacePanel').ActualWidth-12-or$bar.ActualHeight-gt180){throw 'Binding bar is not compact/responsive'}
    (F 'FlowToolList').SelectedItem=$target;M 'RefreshUiState'|Out-Null;Pump 400;Shot $ConnectionPath
    M 'OpenCentralModuleConfigurationS77' ([object[]]@($target))|Out-Null;M 'CentralInputsButton_ClickS77' ([object[]]@($null,$null))|Out-Null;Pump 650;Shot $InputPath
    $row=@((F 'dockInputPortRows')|Where-Object PortName -eq 'Image')[0];if($row.SelectedSourceKey -ne ($sourceB.ToolId+'|Image')){throw 'Input page did not show physical auto binding'}
    Write-Output "CONNECTION_UI_REGRESSION=PASS; Size=$WindowWidth x $WindowHeight; PhysicalClickToCard=PASS; BodyDrag=PASS; DropdownReselect=PASS; Zoom65CompactAnchor=PASS; BottomAnchor=PASS; Connection=$ConnectionPath; Input=$InputPath"
}
finally { try{$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown()}catch{};if(Test-Path $temp){$resolved=[IO.Path]::GetFullPath($temp);if(-not$resolved.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()),[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe cleanup path'};Remove-Item -LiteralPath $resolved -Recurse -Force} }
