param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$ConnectionPath = (Join-Path $ProjectRoot 'artifacts\s79\s79-easy-connection-1600x1000.png'),
    [string]$InputPath = (Join-Path $ProjectRoot 'artifacts\s79\s79-input-reselect-1600x1000.png')
)
$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'; $env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Threading;
public static class S79Mouse {
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
 public const uint LD=0x0002, LU=0x0004;
 public static void ClickAsync(int x, int y) { var t=new Thread(()=>{SetCursorPos(x,y);Thread.Sleep(120);mouse_event(LD,0,0,0,UIntPtr.Zero);Thread.Sleep(90);mouse_event(LU,0,0,0,UIntPtr.Zero);});t.IsBackground=true;t.Start(); }
}
'@
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Pump([int]$ms) { $frame=New-Object Windows.Threading.DispatcherFrame; $timer=New-Object Windows.Threading.DispatcherTimer; $timer.Interval=[TimeSpan]::FromMilliseconds($ms); $timer.Add_Tick({$timer.Stop();$frame.Continue=$false}); $timer.Start(); [Windows.Threading.Dispatcher]::PushFrame($frame) }
function Shot([string]$path) { $dir=Split-Path -Parent $path; if(-not(Test-Path $dir)){[IO.Directory]::CreateDirectory($dir)|Out-Null}; $window.UpdateLayout(); $bmp=New-Object Windows.Media.Imaging.RenderTargetBitmap -ArgumentList 1600,1000,96,96,([Windows.Media.PixelFormats]::Pbgra32); $bmp.Render($window); $encoder=New-Object Windows.Media.Imaging.PngBitmapEncoder; $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bmp)); $stream=[IO.File]::Open([IO.Path]::GetFullPath($path),[IO.FileMode]::Create); try{$encoder.Save($stream)}finally{$stream.Dispose()} }
$mainType=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$temp=Join-Path([IO.Path]::GetTempPath())('halcon-s79-ui-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$mainType.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function F([string]$name){$value=$mainType.GetField($name,$all).GetValue($window);Write-Output -NoEnumerate $value}
function M([string]$name,[object[]]$invokeArgs=@()){$method=$mainType.GetMethods($private)|Where-Object Name -eq $name|Where-Object{$_.GetParameters().Count -eq $invokeArgs.Count}|Select-Object -First 1;if($null -eq $method){throw "Missing $name/$($invokeArgs.Count)"};$method.Invoke($window,$invokeArgs)}
function Tool([string]$kind,[string]$name){$mainType.GetMethod('CreateFlowTool',$private).Invoke($window,[object[]]@([Enum]::Parse($kindType,$kind),$name,$true,$null))}
$app=[Windows.Application]::Current;if($null-eq$app){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown}
try {
    $window.Topmost=$true;$window.Show();$window.WindowState=[Windows.WindowState]::Normal;$window.MinWidth=0;$window.MinHeight=0;$window.Left=2;$window.Top=2;$window.Width=1600;$window.Height=1000;Pump 700
    $flow=F 'flowTools';M 'DisposeFlowTools'|Out-Null;$flow.Clear();$source=Tool ImageSource '01 Image Source';$target=Tool ImageThreshold '02 Threshold';$judge=Tool NumericJudge '03 Result Judge';$flow.Add($source);$flow.Add($target);$flow.Add($judge);M 'RefreshUiState'|Out-Null;M 'RefreshFlowGraphLayout'|Out-Null;Pump 600
    $items=F 'FlowGraphNodeItemsControl';$sourcePresenter=$items.ItemContainerGenerator.ContainerFromItem($source);$targetPresenter=$items.ItemContainerGenerator.ContainerFromItem($target)
    if($null-eq$sourcePresenter-or$null-eq$targetPresenter){throw 'Node presenters unavailable'}
    $outAnchor=$sourcePresenter.ContentTemplate.FindName('S75OutputAnchor',$sourcePresenter);$inAnchor=$targetPresenter.ContentTemplate.FindName('S75InputAnchor',$targetPresenter)
    if($outAnchor.ActualWidth-gt18-or$inAnchor.ActualWidth-gt18){throw 'Anchor hit targets still cover too much of the draggable card'}
    $helper=New-Object Windows.Interop.WindowInteropHelper -ArgumentList $window;[S79Mouse]::SetForegroundWindow($helper.Handle)|Out-Null
    $point=$outAnchor.PointToScreen([Windows.Point]::new($outAnchor.ActualWidth/2,$outAnchor.ActualHeight/2));[S79Mouse]::ClickAsync([int]$point.X,[int]$point.Y);Pump 700
    $point=$inAnchor.PointToScreen([Windows.Point]::new($inAnchor.ActualWidth/2,$inAnchor.ActualHeight/2));[S79Mouse]::ClickAsync([int]$point.X,[int]$point.Y);Pump 1100
    $binding=$target.GetInputBinding('Image');if($null-eq$binding-or$binding.SourceToolId-ne$source.ToolId-or$binding.SourcePortName-ne'Image'){throw ('Physical click connection failed; Status='+(F 'HeaderStatusText').Text)}
    (F 'FlowToolList').SelectedItem=$target;M 'RefreshUiState'|Out-Null;Pump 400;Shot $ConnectionPath
    M 'OpenCentralModuleConfigurationS77' ([object[]]@($target))|Out-Null;M 'CentralInputsButton_ClickS77' ([object[]]@($null,$null))|Out-Null;Pump 650;Shot $InputPath
    $row=@((F 'dockInputPortRows')|Where-Object PortName -eq 'Image')[0];if($row.SelectedSourceKey -ne ($source.ToolId+'|Image')){throw 'Input page did not show physical auto binding'}
    Write-Output "S79_UI_CAPTURE=PASS; PhysicalClick=PASS; HitTarget=18x16; Connection=$ConnectionPath; Input=$InputPath"
}
finally { try{$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown()}catch{};if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force} }
