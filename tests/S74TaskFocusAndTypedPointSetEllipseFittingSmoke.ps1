param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne[Threading.ApartmentState]::STA){throw'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Runtime.Serialization
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Assert([bool]$c,[string]$m){if(-not$c){throw $m}}
function Pump([int]$ms){$f=New-Object Windows.Threading.DispatcherFrame;$t=New-Object Windows.Threading.DispatcherTimer;$t.Interval=[TimeSpan]::FromMilliseconds($ms);$t.Add_Tick({$t.Stop();$f.Continue=$false});$t.Start();[Windows.Threading.Dispatcher]::PushFrame($f)}
$main=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true);$arrayType=$assembly.GetType('HalconWinFormsDemo.Models.VmPoint2DArray',$true);$ellipseType=$assembly.GetType('HalconWinFormsDemo.Models.VmEllipse2D',$true);$pointType=$assembly.GetType('HalconWinFormsDemo.Models.VmPoint2D',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$static=[Reflection.BindingFlags]'Static,Public'
$temp=Join-Path([IO.Path]::GetTempPath())('halcon-vision-s74-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$main.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
$app=[Windows.Application]::Current;$ownsApp=$null-eq$app;if($ownsApp){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown};$window.ShowInTaskbar=$false;$window.Left=-10000;$window.MinWidth=0;$window.MinHeight=0;$window.Width=1366;$window.Height=768;$window.Show();Pump 350
function F([string]$n){$v=$main.GetField($n,$all).GetValue($window);Write-Output -NoEnumerate $v}
function M([string]$n,[object[]]$p=@()){try{$main.GetMethod($n,$private).Invoke($window,$p)}catch{$e=$_.Exception;while($e.InnerException){$e=$e.InnerException};throw($n+': '+$e.Message)}}
function K([string]$n){[Enum]::Parse($kindType,$n)}
function Tool([string]$k,[string]$n){M 'CreateFlowTool' ([object[]]@((K $k),$n,$true,$null))}
function Run($t){M 'ExecuteFlowTool' ([object[]]@($t,'S74 smoke'))}
function Out($t,[string]$p){$a=[object[]]@($p,$null);if(-not$t.GetType().GetMethod('TryGetOutputValue').Invoke($t,$a)){throw"Missing output $p"};$a[1]}
function Has($t,[string]$p){$a=[object[]]@($p,$null);[bool]$t.GetType().GetMethod('TryGetOutputValue').Invoke($t,$a)}
function Points([double[]]$r,[double[]]$c,[string]$ctx){$arrayType.GetMethod('CreateImagePixel',$static).Invoke($null,[object[]]@($r,$c,$ctx))}
function SelectTools([object[]]$tools){(F 'FlowToolList').SelectedItems.Clear();foreach($tool in $tools){(F 'FlowToolList').SelectedItems.Add($tool)}}
try{
  Assert ((F 'toolCatalog').Count-eq73 -and (F 'toolCatalog').Count-eq[Enum]::GetValues($kindType).Count) 'S74 compatibility catalog mismatch after S80.'
  Assert ((F 'DeveloperToolbarPanel').Children.Count-eq7) 'S74 added a permanent top command.'
  Assert ($window.Title-notmatch'\bVM\b|VisionMaster') 'Visible window title still exposes VM branding.'
  $xaml=[IO.File]::ReadAllText((Join-Path $ProjectRoot 'MainWindow.xaml'));Assert ($xaml-notmatch'Text="HALCON VM"') 'Visible page text still exposes VM branding.';Assert ($xaml.Contains('x:Name="S75InputAnchor"') -and $xaml.Contains('x:Name="S75OutputAnchor"')) 'Compact cards do not expose top/bottom connection dots.'

  # A: compact vertical cards, simple orthogonal lines, focus/all visual modes, scroll-synchronized draggable viewport.
  $flow=F 'flowTools';M 'DisposeFlowTools'|Out-Null;$flow.Clear();$tools=@();for($i=0;$i-lt18;$i++){if($i-eq0){$t=Tool 'ImageSource' ('Step '+($i+1))}else{$t=Tool 'ImageFilter' ('Step '+($i+1));$t.SetInputBinding('Image',$tools[$i-1].ToolId,'Image')};$flow.Add($t);$tools+=$t}
  (F 'FlowToolList').SelectedItem=$tools[8];M 'RefreshFlowGraphLayout'|Out-Null;Pump 250
  $connections=F 'flowConnections';$visible=@($connections|Where-Object{$_.IsVisible});Write-Output ('S74_FOCUS_LINES='+$visible.Count+'/'+$connections.Count);Assert ($connections.Count-eq17 -and $visible.Count-eq2) 'Task focus did not reduce the default chain to direct upstream/downstream dependencies.'
  $geometry=[string]$connections[0].Geometry;Assert ($geometry-notmatch'[Cc]') ('Connection geometry still uses decorative Bezier curves: '+$geometry)
  (F 'FlowAllDependenciesModeRadioS74').IsChecked=$true;Pump 120;Assert (@($connections|Where-Object{$_.IsVisible}).Count-eq17) 'All-dependencies mode did not restore every real InputBinding line.'
  (F 'FlowTaskFocusModeRadioS74').IsChecked=$true;Pump 120;Assert (@($connections|Where-Object{$_.IsVisible}).Count-eq2) 'Task-focus mode did not restore its quiet view.'
  $viewport=F 'FlowGraphViewport';$thumb=F 'FlowNavigatorViewportThumbS74';Assert ($viewport.ScrollableHeight-gt100 -and $thumb.Visibility.ToString()-eq'Visible' -and $thumb.ActualHeight-ge18) 'Long-flow navigator viewport indicator is missing.'
  $topBefore=[Windows.Controls.Canvas]::GetTop($thumb);M 'ScrollFlowFromNavigatorS74' ([object[]]@(((F 'FlowNavigatorTrackS73').ActualHeight*0.72)));Pump 100;$topAfter=[Windows.Controls.Canvas]::GetTop($thumb);Assert ($viewport.VerticalOffset-gt0 -and $topAfter-gt$topBefore) 'Navigator viewport click/drag mapping did not move the real vertical viewport.'
  Assert (@($tools|Where-Object{$_.GraphHeight-gt74.1}).Count-eq0) 'Default module cards are no longer compact.'

  # B: Point2D[] -> HALCON ellipse fit/distance_pc -> typed Ellipse2D + scalar outputs + managed XLD.
  M 'DisposeFlowTools'|Out-Null;$flow.Clear();$source=Tool 'CircularCaliperCircleFinder' 'Point Producer';$fit=Tool 'TypedPointSetEllipseFittingMeasurement' 'Typed Ellipse Fit';$judge=Tool 'NumericJudge' 'Residual Judge';foreach($tool in @($source,$fit,$judge)){$flow.Add($tool)}
  $fit.SetInputBinding('Points',$source.ToolId,'EdgePoints');$judge.SetInputBinding('Value',$fit.ToolId,'MaxResidual');$judge.InputToolId=$fit.ToolId;$judge.InputPortName='MaxResidual';$judge.NumericOperator='LessOrEqual';$judge.NumericUpperLimit=0.1
  $ctx='S74.Image';$count=48;$rows=New-Object 'double[]' $count;$cols=New-Object 'double[]' $count;$phi=0.35;for($i=0;$i-lt$count;$i++){$a=2*[Math]::PI*$i/$count;$cols[$i]=180+65*[Math]::Cos($phi)*[Math]::Cos($a)-28*[Math]::Sin($phi)*[Math]::Sin($a);$rows[$i]=120+65*[Math]::Sin($phi)*[Math]::Cos($a)+28*[Math]::Cos($phi)*[Math]::Sin($a)}
  $source.SetOutputValue('EdgePoints',(Points $rows $cols $ctx));$source.ResultCode='OK';$fit.Parameters.PointSetMinimumCount=8;$fit.Parameters.XldEllipseFitAlgorithm='FTukey';$fit.Parameters.XldEllipseFitIterations=10;$result=Run $fit
  $ellipse=Out $fit 'MainEllipse';$center=Out $fit 'CenterPoint';$residual=[double](Out $fit 'MaxResidual');$xld=Out $fit 'XLD';Write-Output ('S74_TYPED_ELLIPSE='+$result.ResultCode+'|'+$ellipse.ToDisplayString()+'|MaxResidual='+$residual+'|XLD='+$xld.ObjectCount)
  Assert (($result.ResultCode-eq'OK') -and $ellipse.GetType()-eq$ellipseType -and $ellipse.CoordinateContextId-eq$ctx -and [Math]::Abs($ellipse.CenterRow-120)-lt0.2 -and [Math]::Abs($ellipse.CenterColumn-180)-lt0.2 -and [Math]::Abs($ellipse.Radius1-65)-lt0.3 -and [Math]::Abs($ellipse.Radius2-28)-lt0.3 -and $residual-lt0.2 -and $xld.ObjectCount-eq98) 'HALCON typed point-set ellipse fit outputs are wrong.'
  Assert ($center.GetType()-eq$pointType -and [double](Out $fit 'ValidCount')-eq$count -and (Has $fit 'Phi') -and (Has $fit 'AxisRatio')) 'Typed center/count/axis outputs are missing.'
  Assert ((Run $judge).ResultCode-eq'OK') 'Ellipse residual did not drive NumericJudge.'
  (F 'FlowToolList').SelectedItem=$fit;M 'LoadDockConfigurationDraft' ([object[]]@($fit))|Out-Null;Assert ((F 'DockTypedPointSetEllipseFitPanel').Visibility.ToString()-eq'Visible' -and (F 'DockTypedPointSetEllipseFitAlgorithmComboBox').SelectedValue-eq'FTukey') 'Typed ellipse parameters are not in the persistent dock.'
  M 'RefreshPortDataMonitorS64' ([object[]]@($fit))|Out-Null;Assert (@((F 'portDataMonitorRows')|Where-Object{$_.DataType-eq'Point2D[]' -or $_.DataType-eq'Ellipse2D'}).Count-ge2) 'I/O monitor did not expose Point2D[]/Ellipse2D ports.'
  $serializer=New-Object Runtime.Serialization.Json.DataContractJsonSerializer($ellipseType);$stream=New-Object IO.MemoryStream;try{$serializer.WriteObject($stream,$ellipse);$stream.Position=0;$roundTrip=$serializer.ReadObject($stream);Assert ($roundTrip.GetValidationError()-eq'' -and [Math]::Abs($roundTrip.Radius1-$ellipse.Radius1)-lt1e-9) 'Ellipse2D JSON round trip failed.'}finally{$stream.Dispose()}
  $old=Out $fit 'XLD';$source.SetOutputValue('EdgePoints',(Points $rows $cols $ctx));$source.ResultCode='OK';Run $fit|Out-Null;Assert $old.IsDisposed 'Rerun did not dispose old managed ellipse XLD.'
  $fit.Parameters.PointSetAbnormalPolicy='SkipInvalid';$mixed=Points $rows $cols $ctx;$mixed.Points[4].IsValid=$false;$source.SetOutputValue('EdgePoints',$mixed);$source.ResultCode='OK';$skip=Run $fit;Assert (($skip.ResultCode-eq'NG') -and [double](Out $fit 'RejectedCount')-eq1 -and (Has $fit 'MainEllipse')) 'SkipInvalid ellipse-fit semantics failed.'
  $fit.Parameters.PointSetAbnormalPolicy='Reject';$failed=$false;try{Run $fit|Out-Null}catch{$failed=$true};Assert $failed 'Reject policy accepted an invalid point.'
  $lineRows=[double[]](10,20,30,40,50,60,70,80);$lineCols=[double[]](20,40,60,80,100,120,140,160);$source.SetOutputValue('EdgePoints',(Points $lineRows $lineCols $ctx));$source.ResultCode='OK';$failed=$false;try{Run $fit|Out-Null}catch{$failed=$true};Assert $failed 'Collinear point set was accepted as an ellipse.'
  $source.SetOutputValue('EdgePoints',(Points $rows $cols $ctx));$source.ResultCode='--';$failed=$false;try{Run $fit|Out-Null}catch{$failed=$true};Assert $failed 'Stale/unrun Point2D[] source was accepted.'
  $source.ResultCode='OK';$fit.Parameters.PointSetAbnormalPolicy='Reject';$recipe=M 'CaptureRecipe';$saved=$recipe.ToolFlow|Where-Object ToolId -eq $fit.ToolId|Select-Object -First 1;Assert ($saved.Parameters.XldEllipseFitAlgorithm-eq'FTukey' -and $saved.Parameters.PointSetMinimumCount-eq8) 'Recipe lost typed point-set ellipse parameters.'
  SelectTools @($source,$fit);$copied=[int](M 'CopySelectedFlowToolsToClipboard');$pasted=[int](M 'PasteFlowToolClipboard' ([object[]]@($flow.Count)));$copyBlock=@($flow|Select-Object -Last 2);$copyFit=$copyBlock|Where-Object{$_.Kind.ToString()-eq'TypedPointSetEllipseFittingMeasurement'}|Select-Object -First 1;Assert (($copied-eq2)-and($pasted-eq2)-and$copyFit.GetInputBinding('Points').SourceToolId-eq$copyBlock[0].ToolId) 'Typed ellipse-fit block copy did not remap Points source.'
  Write-Output 'S74 quiet top/bottom-node vertical flow + focus/all connections + draggable navigator viewport + HALCON typed Point2D[] ellipse fitting/distance_pc/dock/I-O/NumericJudge/JSON/recipe/copy/lifecycle/failures passed'
}finally{$main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();if($ownsApp){$app.Shutdown()};if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}}
