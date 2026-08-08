param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$CrosshairOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s34-crosshair.png'),
    [string]$MirrorOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s34-image-mirror.png'),
    [string]$LeftImageOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s34-crosshair-input.png'),
    [string]$RightImageOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s34-crosshair-output.png')
)

$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA){throw 'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework;Add-Type -AssemblyName System.Drawing;Add-Type -AssemblyName System.Windows.Forms
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Pump-Ui([int]$Milliseconds){$frame=New-Object Windows.Threading.DispatcherFrame;$timer=New-Object Windows.Threading.DispatcherTimer;$timer.Interval=[TimeSpan]::FromMilliseconds($Milliseconds);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false});$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}
function Save-Shot([string]$Path){
    $window.UpdateLayout();$topLeft=$window.PointToScreen([Windows.Point]::new(0,0));$bottomRight=$window.PointToScreen([Windows.Point]::new($window.ActualWidth,$window.ActualHeight))
    $width=[Math]::Max(1,[int][Math]::Ceiling($bottomRight.X-$topLeft.X));$height=[Math]::Max(1,[int][Math]::Ceiling($bottomRight.Y-$topLeft.Y))
    $capture=[Drawing.Bitmap]::new($width,$height);$graphics=[Drawing.Graphics]::FromImage($capture);$saved=$false
    try{$graphics.CopyFromScreen([int]$topLeft.X,[int]$topLeft.Y,0,0,[Drawing.Size]::new($width,$height));$capture.Save($Path,[Drawing.Imaging.ImageFormat]::Png);$saved=$true}catch{$saved=$false}finally{$graphics.Dispose();$capture.Dispose()}
    if(-not $saved){$render=[Activator]::CreateInstance([Windows.Media.Imaging.RenderTargetBitmap],[object[]]@($width,$height,96.0,96.0,[Windows.Media.PixelFormats]::Pbgra32));$render.Render($window);$encoder=New-Object Windows.Media.Imaging.PngBitmapEncoder;$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($render));$stream=[IO.File]::Open($Path,[IO.FileMode]::Create,[IO.FileAccess]::Write);try{$encoder.Save($stream)}finally{$stream.Dispose()}}
    "${width}x${height}"
}
function Save-HalconView([string]$Field,[string]$Path){$control=$main.GetField($Field,$private).GetValue($window);$view=$control.HalconWindow.DumpWindowImage();try{$view.WriteImage('png',0,$Path)}finally{$view.Dispose()}}
function Write-Pattern([string]$Path){
    $width=640;$height=440;$header=[Text.Encoding]::ASCII.GetBytes("P5`n$width $height`n255`n");$pixels=New-Object byte[] ($width*$height)
    for($row=0;$row -lt $height;$row++){for($column=0;$column -lt $width;$column++){$value=24+[int](52*$column/$width);if($row -gt 52 -and $row -lt 176 -and $column -gt 70 -and $column -lt 290){$value=210};if($row -gt 250 -and $row -lt 390 -and $column -gt 350 -and $column -lt 585){$value=135};if(($row-112)*($row-112)+($column-515)*($column-515)-lt 48*48){$value=250};$pixels[$row*$width+$column]=[byte]$value}}
    $bytes=New-Object byte[] ($header.Length+$pixels.Length);[Array]::Copy($header,0,$bytes,0,$header.Length);[Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length);[IO.File]::WriteAllBytes($Path,$bytes)
}

$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s34-ui-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$imagePath=Join-Path $temp 'asymmetric-pattern.pgm';Write-Pattern $imagePath
$app=[Windows.Application]::Current;if($null -eq $app){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown}
$main=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$args=New-Object 'object[]' 1;$args[0]=$temp.ToString();$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke($args);$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$window=$main.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger));$flow=$main.GetField('flowTools',$private).GetValue($window)
$create=$main.GetMethod('CreateFlowTool',$private);$dispose=$main.GetMethod('DisposeFlowTools',$private);$execute=$main.GetMethod('ExecuteFlowTool',$private);$loadImage=$main.GetMethod('LoadImage',$private);$refresh=$main.GetMethod('RefreshUiState',$private);$schedule=$main.GetMethod('ScheduleRefreshDisplay',$private);$setDual=$main.GetMethod('SetDualImageMode',$private);$expand=$main.GetMethod('ImageWorkspaceExpandButton_Click',$private);$loadCrosshair=$main.GetMethod('LoadCrosshairEditor',$private);$toggleCrosshair=$main.GetMethod('CrosshairPanelToggleButton_Click',$private)
function New-Tool([string]$Kind,[string]$Name){$create.Invoke($window,[object[]]@([Enum]::Parse($kindType,$Kind),$Name,$true,$null))}
$dispose.Invoke($window,@())|Out-Null;$flow.Clear();$source=New-Tool ImageSource 'Station Image Input';$mirror=New-Tool ImageMirror 'HALCON Image Mirror';$threshold=New-Tool ImageThreshold 'Mirror Output Threshold'
$source.Parameters.LocalImagePath=$imagePath;$source.Parameters.LocalImageSerialNumber='S34-MIRROR';$mirror.Parameters.ImageMirrorMode='row';$mirror.SetInputBinding('Image',$source.ToolId,'Image');$threshold.Parameters.ImageThresholdMinGray=180;$threshold.Parameters.ImageThresholdMaxGray=255;$threshold.SetInputBinding('Image',$mirror.ToolId,'Image');foreach($tool in @($source,$mirror,$threshold)){$flow.Add($tool)}
$window.WindowStartupLocation=[Windows.WindowStartupLocation]::Manual;$window.WindowState=[Windows.WindowState]::Normal;$window.Left=2;$window.Top=2;$window.Width=1900;$window.Height=1060;$window.Topmost=$true;$window.Show();$window.Activate()|Out-Null;Pump-Ui 850
$loadImage.Invoke($window,[object[]]@($imagePath.ToString(),$false))|Out-Null;foreach($tool in @($source,$mirror,$threshold)){try{$execute.Invoke($window,[object[]]@($tool,'S34 UI capture'))|Out-Null}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
$list=$main.GetField('FlowToolList',$all).GetValue($window);$context=$main.GetField('ImageContextComboBox',$all).GetValue($window);$list.SelectedItem=$mirror;$setDual.Invoke($window,[object[]]@($true))|Out-Null;$context.SelectedValue='ModuleOutput'
$right=$main.GetField('rightImageCrosshairs',$private).GetValue($window);$right.Guide1.IsEnabled=$true;$right.Guide1.X=185;$right.Guide1.Y=118;$right.Guide1.Color='cyan';$right.Guide1.Thickness=3;$right.Guide2.IsEnabled=$true;$right.Guide2.X=470;$right.Guide2.Y=332;$right.Guide2.Color='yellow';$right.Guide2.Thickness=2;$right.Guide2.IsLocked=$true
$left=$main.GetField('leftImageCrosshairs',$private).GetValue($window);$left.Guide1.IsEnabled=$true;$left.Guide1.X=118;$left.Guide1.Y=88;$left.Guide1.Color='green';$left.Guide1.Thickness=2;$left.Guide2.IsEnabled=$true;$left.Guide2.X=520;$left.Guide2.Y=310;$left.Guide2.Color='magenta';$left.Guide2.Thickness=2
$main.GetField('CrosshairTargetComboBox',$all).GetValue($window).SelectedValue='Right';$loadCrosshair.Invoke($window,@())|Out-Null;$toggleCrosshair.Invoke($window,[object[]]@($null,$null))|Out-Null
$main.GetField('RoiLayerPanel',$all).GetValue($window).Visibility=[Windows.Visibility]::Collapsed;$main.GetField('RoiLayerPanelColumn',$all).GetValue($window).Width=[Windows.GridLength]::new(0)
$expand.Invoke($window,[object[]]@($null,$null))|Out-Null;$refresh.Invoke($window,@())|Out-Null;$schedule.Invoke($window,@())|Out-Null;Pump-Ui 1100
$crosshairSize=Save-Shot $CrosshairOutputPath;Save-HalconView 'comparisonImageWindow' $LeftImageOutputPath;Save-HalconView 'imageWindow' $RightImageOutputPath
$expand.Invoke($window,[object[]]@($null,$null))|Out-Null;$rightTabs=$main.GetField('RightTabs',$all).GetValue($window);$rightTabs.SelectedItem=$main.GetField('DockConfigurationTab',$all).GetValue($window);$main.GetField('DockConfigurationTabs',$all).GetValue($window).SelectedIndex=0;$refresh.Invoke($window,@())|Out-Null;$schedule.Invoke($window,@())|Out-Null;Pump-Ui 850
$panel=$main.GetField('DockImageMirrorPanel',$all).GetValue($window);if($panel.Visibility.ToString() -ne 'Visible'){throw 'S34 image mirror dock panel is not visible.'};$panel.BringIntoView();Pump-Ui 250
$mirrorSize=Save-Shot $MirrorOutputPath
$main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown();if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
Write-Output "S34_UI_CAPTURE=PASS; CrosshairPath=$CrosshairOutputPath; MirrorPath=$MirrorOutputPath; LeftImage=$LeftImageOutputPath; RightImage=$RightImageOutputPath; CrosshairWindow=$crosshairSize; MirrorWindow=$mirrorSize; A=VM_DOUBLE_CROSSHAIR_GUIDES; B=HALCON_MIRROR_IMAGE; BSB=NOT_IMPLEMENTED_BY_USER_DIRECTIVE"
