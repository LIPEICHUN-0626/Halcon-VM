param([string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress'
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
$type=$assembly.GetType('HalconWinFormsDemo.Services.ImageViewportController',$true)
$view=[Activator]::CreateInstance($type);$panel=New-Object Windows.Forms.Panel;$panel.Size=[Drawing.Size]::new(800,400)
function Assert([bool]$value,[string]$message){if(-not$value){throw $message}}
function Part($v){$args=[object[]]@($panel.ClientSize.Width,$panel.ClientSize.Height,0.0,0.0,0.0,0.0);$type.GetMethod('GetDisplayPart',[Reflection.BindingFlags]'Instance,NonPublic').Invoke($v,$args)|Out-Null;[pscustomobject]@{Top=$args[2];Left=$args[3];Bottom=$args[4];Right=$args[5];Scale=($args[4]-$args[2]+1)/$panel.ClientSize.Height;ScaleX=($args[5]-$args[3]+1)/$panel.ClientSize.Width}}
function Equal([double]$a,[double]$b,[string]$message){Assert([Math]::Abs($a-$b)-lt0.0002) ($message+': '+$a+' != '+$b)}
try{
 $view.SetImageSize(200,200);$fit=Part $view;Equal $fit.Scale $fit.ScaleX 'Square pixels were stretched';Equal $fit.Scale 0.5 'Fit crop/scale incorrect'
 $view.SetImageSize(1000,800);$cursor=[Drawing.Point]::new(500,220);$before=$view.WindowToImage($cursor,$panel);$view.ZoomAt($cursor,$panel,120);$after=$view.WindowToImage($cursor,$panel)
 Equal $before.X $after.X 'Zoom moved the image point under the cursor horizontally';Equal $before.Y $after.Y 'Zoom moved the image point under the cursor vertically'
 $view.SetImageSize(200,100);$view.OriginalSize($panel);$part=Part $view;Equal $part.Scale 1 'Small-image 1:1 is not one screen pixel per image pixel'
 $view.PanByWindowDelta(25,-13,$panel);$panned=Part $view;Equal $panned.Scale 1 'Panning a small 1:1 image changed its scale';Equal ($panned.Left-$part.Left) -25 'Pan column delta mismatch';Equal ($panned.Top-$part.Top) 13 'Pan row delta mismatch'
 $center=$view.WindowToImage([Drawing.Point]::new(400,200),$panel);$copy=[Activator]::CreateInstance($type);$copy.CopyFrom($view);$copyCenter=$copy.WindowToImage([Drawing.Point]::new(400,200),$panel);Equal $center.X $copyCenter.X 'Copied viewport lost pan'
 $linked=[Activator]::CreateInstance($type);$linked.SetImageSize(100,50);$linked.SynchronizeNormalizedViewFrom($view);$linkedPart=Part $linked;Equal ($linkedPart.Scale/100) ($panned.Scale/200) 'Linked view lost normalized visible fraction'
 foreach($size in @(@(390,500),@(580,220),@(800,400))){$panel.Size=[Drawing.Size]::new($size[0],$size[1]);$view.Fit();$p=Part $view;Equal $p.Scale $p.ScaleX 'Resizing stretched pixels';Assert($p.Left-le0.000001-and$p.Top-le0.000001-and$p.Right-ge198.999999-and$p.Bottom-ge98.999999) 'Fit clips input image'}
 $view.Clear();Assert(-not$view.HasImage) 'Clear retained image context'
 Write-Output 'S81_VIEWPORT_GEOMETRY=PASS; Aspect=Equal; ZoomAnchor=Stable; SmallImageOneToOne=PASS; PanScale=Stable; Copy=PASS; LinkedFraction=PASS; ResizeFit=PASS'
}finally{$panel.Dispose()}
