param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne[Threading.ApartmentState]::STA){throw 'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Assert([bool]$ok,[string]$message){if(-not$ok){throw "Assertion failed: $message"}}
$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-s76-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$mainType=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true);$lineType=$assembly.GetType('HalconWinFormsDemo.Models.VmLine2D',$true);$ellipseType=$assembly.GetType('HalconWinFormsDemo.Models.VmEllipse2D',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$static=[Reflection.BindingFlags]'Static,Public'
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$mainType.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function F([string]$name){$f=$mainType.GetField($name,$all);if($null-eq$f){throw "Missing field $name"};$v=$f.GetValue($window);Write-Output -NoEnumerate $v}
function M([string]$name,[object[]]$invokeArgs=@()){try{$m=@($mainType.GetMethods($private)|Where-Object Name -eq $name|Where-Object{$_.GetParameters().Count-eq$invokeArgs.Count}|Select-Object -First 1);if($null-eq$m){throw "Missing method $name/$($invokeArgs.Count)"};$m.Invoke($window,$invokeArgs)}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
function Tool([string]$kind,[string]$name){$mainType.GetMethod('CreateFlowTool',$private).Invoke($window,[object[]]@([Enum]::Parse($kindType,$kind),$name,$true,$null))}
function Out($tool,[string]$port){$value=$null;if(-not$tool.TryGetOutputValue($port,[ref]$value)){throw "Missing output $port"};Write-Output -NoEnumerate $value}
function Line([double]$r1,[double]$c1,[double]$r2,[double]$c2,[string]$ctx='S76.Image'){$lineType.GetMethod('CreateImagePixel',$static).Invoke($null,[object[]]@($r1,$c1,$r2,$c2,$ctx))}
function Ellipse([double]$row,[double]$column,[double]$phi,[double]$r1,[double]$r2,[string]$ctx='S76.Image'){$ellipseType.GetMethod('CreateImagePixel',$static).Invoke($null,[object[]]@($row,$column,$phi,$r1,$r2,$ctx))}
function Execute($tool){M 'ExecuteFlowTool' @($tool,'S76 smoke')}

try{
  $flow=F 'flowTools'

  # A: long-flow task lens keeps the current task, direct neighbors, breakpoint and abnormal step full-size.
  $cards=@();for($i=0;$i-lt12;$i++){if($i-eq0){$t=Tool ImageSource ('S76 Step '+($i+1))}else{$t=Tool ImageChannel ('S76 Step '+($i+1));$t.SetInputBinding('Image',$cards[$i-1].ToolId,'Image')};$cards+=,$t;$flow.Add($t)}
  $cards[1].IsBreakpoint=$true;$cards[9].ResultCode='NG';$cards[10].ErrorMessage='模拟阻塞'
  $flowList=F 'FlowToolList';$flowList.SelectedItems.Clear();$flowList.SelectedItems.Add($cards[5])|Out-Null;M 'RefreshFlowSelectionFlags'|Out-Null;M 'FlowToolList_SelectionChanged' @($flowList,$null)|Out-Null;M 'RefreshFlowGraphLayout'|Out-Null
  $full=@($cards|Where-Object{-not$_.IsTaskLensCompactS76});$compact=@($cards|Where-Object{$_.IsTaskLensCompactS76})
  Assert($full.Count-eq6-and$compact.Count-eq6) ('Task lens focus count wrong: full='+$full.Count+', compact='+$compact.Count)
  Assert($cards[4].GraphHeight-eq68-and$cards[5].GraphHeight-eq68-and$cards[6].GraphHeight-eq68-and$compact[0].GraphHeight-eq32) 'Task lens card heights are not 68/32'
  Assert($cards[4].IsTaskLensCompactS76-eq$false-and$cards[6].IsTaskLensCompactS76-eq$false) 'Direct upstream/downstream did not remain full'
  $clickTarget=$compact[-1];$card=New-Object Windows.Controls.Border;$card=$card.PSObject.BaseObject;$card.Tag=$clickTarget;$mouse=New-Object Windows.Input.MouseButtonEventArgs([Windows.Input.Mouse]::PrimaryDevice,0,[Windows.Input.MouseButton]::Left);$mouse=$mouse.PSObject.BaseObject;$mouse.RoutedEvent=[Windows.UIElement]::MouseLeftButtonDownEvent
  M 'FlowGraphNode_MouseLeftButtonDown' @($card,$mouse)|Out-Null;Assert(-not$clickTarget.IsTaskLensCompactS76-and$null-eq(F 'flowNodeDragSession')) 'Compact-row click did not expand immediately or incorrectly began drag'
  M 'SelectAdjacentFlowToolS72' @(-1,'S76 keyboard')|Out-Null;$keyboardTarget=@($flow|Where-Object{$_.IsFlowSelected})[0];Assert($null-ne$keyboardTarget-and-not$keyboardTarget.IsTaskLensCompactS76) 'Keyboard adjacent navigation did not expand the target'
  (F 'FlowAllCardsModeRadioS76').IsChecked=$true;M 'UpdateFlowTaskLensStatesS76'|Out-Null;M 'RefreshFlowGraphLayout'|Out-Null;Assert(@($cards|Where-Object{$_.IsTaskLensCompactS76}).Count-eq0-and@($cards|Where-Object{$_.GraphHeight-eq68}).Count-eq12) 'All-expanded mode left compact rows'
  (F 'FlowTaskLensModeRadioS76').IsChecked=$true;M 'RefreshFlowGraphLayout'|Out-Null;Assert(@($cards|Where-Object{$_.IsTaskLensCompactS76}).Count-gt0) 'Task-lens mode did not restore compact rows'

  # B: finite Line2D segment + Ellipse2D -> real HALCON contour intersection.
  $lineSource=Tool PointPairLineConstructionMeasurement 'S76 Line Source';$ellipseSource=Tool TypedPointSetEllipseFittingMeasurement 'S76 Ellipse Source';$measure=Tool LineEllipseIntersectionMeasurement 'S76 Segment Ellipse';$judge=Tool NumericJudge 'S76 Chord Judge';$flow.Add($lineSource);$flow.Add($ellipseSource);$flow.Add($measure);$flow.Add($judge)
  $measure.SetInputBinding('Line',$lineSource.ToolId,'MainLine');$measure.SetInputBinding('Ellipse',$ellipseSource.ToolId,'MainEllipse');$judge.SetInputBinding('Value',$measure.ToolId,'ChordLength');$judge.InputToolId=$measure.ToolId;$judge.InputPortName='ChordLength';$judge.NumericLowerLimit=119.5;$judge.NumericUpperLimit=120.5
  $ellipseSource.SetOutputValue('MainEllipse',(Ellipse 100 150 0 60 30));$ellipseSource.ResultCode='OK';$lineSource.SetOutputValue('MainLine',(Line 100 70 100 230));$lineSource.ResultCode='OK'
  $record=Execute $measure;$points=Out $measure IntersectionPoints;$xld=Out $measure XLD
  Assert($record.ResultCode-eq'OK'-and(Out $measure Relation)-eq'Secant'-and$points.Count-eq2-and[math]::Abs([double](Out $measure ChordLength)-120)-lt0.1) 'Secant/chord result is wrong'
  Assert($points.Points[0].Column-lt$points.Points[1].Column-and$points.Points[0].Column-lt100-and$points.Points[1].Column-gt200) 'Intersections are not ordered from line start to end'
  Assert($xld.ObjectCount-ge4) 'Managed line/ellipse/cross XLD missing';Execute $judge|Out-Null;Assert($judge.ResultCode-eq'OK') 'ChordLength did not drive NumericJudge'
  $oldXld=$xld;$lineSource.SetOutputValue('MainLine',(Line 70 70 70 230));Execute $measure|Out-Null;Assert((Out $measure Relation)-eq'Tangent'-and[double](Out $measure IntersectionCount)-eq1-and$oldXld.IsDisposed) 'Tangent or old-XLD cleanup failed'
  $lineSource.SetOutputValue('MainLine',(Line 60 70 60 230));Execute $measure|Out-Null;Assert((Out $measure Relation)-eq'Disjoint'-and[double](Out $measure IntersectionCount)-eq0-and$measure.ResultCode-eq'NG') 'Disjoint result failed'
  $lineSource.SetOutputValue('MainLine',(Line 100 90 100 150));Execute $measure|Out-Null;Assert([double](Out $measure IntersectionCount)-eq1-and[math]::Abs((Out $measure Point1).Column-90)-lt0.1) 'Finite endpoint intersection failed'
  $lineSource.SetOutputValue('MainLine',(Line 100 120 100 180));Execute $measure|Out-Null;Assert([double](Out $measure IntersectionCount)-eq0) 'Short segment inside ellipse was incorrectly treated as infinite line'
  $phi=0.4;$ellipseSource.SetOutputValue('MainEllipse',(Ellipse 100 150 $phi 60 30));$lineSource.SetOutputValue('MainLine',(Line (100+90*[math]::Sin($phi)) (150-90*[math]::Cos($phi)) (100-90*[math]::Sin($phi)) (150+90*[math]::Cos($phi))));Execute $measure|Out-Null;Assert((Out $measure Relation)-eq'Secant'-and[math]::Abs([double](Out $measure ChordLength)-120)-lt0.2) 'Rotated ellipse major-axis secant failed'
  $ellipseSource.SetOutputValue('MainEllipse',(Ellipse 80 90 0.7 30 29.999));$lineSource.SetOutputValue('MainLine',(Line 80 40 80 140));Execute $measure|Out-Null;Assert([double](Out $measure IntersectionCount)-eq2) 'Near-circle ellipse intersection failed'
  $lineSource.ResultCode='NG';$stale=$false;try{Execute $measure|Out-Null}catch{$stale=$true};Assert($stale) 'Stale Line2D source was accepted';$lineSource.ResultCode='OK'
  $ellipseSource.SetOutputValue('MainEllipse',(Ellipse 80 90 0 30 20 'Other.Image'));$mixed=$false;try{Execute $measure|Out-Null}catch{$mixed=$true};Assert($mixed) 'Mixed coordinate contexts were accepted'
  $lineSource.SetOutputValue('MainLine',(Line 1 1 1 1.00000000001));$ellipseSource.SetOutputValue('MainEllipse',(Ellipse 80 90 0 30 20));$degenerate=$false;try{Execute $measure|Out-Null}catch{$degenerate=$true};Assert($degenerate) 'Degenerate line segment was accepted'
  $measure.Parameters.LineEllipseIntersectionTolerance=0;$paramError=$measure.Parameters.Validate([Enum]::Parse($kindType,'LineEllipseIntersectionMeasurement'));Assert(-not[string]::IsNullOrWhiteSpace($paramError)) 'Invalid intersection tolerance accepted';$measure.Parameters.LineEllipseIntersectionTolerance=0.4;$measure.Parameters.LineEllipseOverlayCrossSize=12;$measure.Parameters.LineEllipseContourResolution=0.2
  (F 'FlowToolList').SelectedItem=$measure;M 'LoadDockConfigurationDraft' @($measure)|Out-Null;Assert((F 'DockLineEllipseIntersectionPanel').Visibility.ToString()-eq'Visible'-and(F 'DockLineEllipseToleranceTextBox').Text-eq'0.4') 'Dock line-ellipse configuration did not synchronize'
  $recipe=M 'CaptureRecipe';$saved=@($recipe.ToolFlow|Where-Object{$_.ToolId-eq$measure.ToolId})[0];Assert($saved.Parameters.LineEllipseIntersectionTolerance-eq0.4-and$saved.Parameters.LineEllipseContourResolution-eq0.2) 'Line-ellipse parameters did not persist in recipe'
  $catalog=F 'toolCatalog';$kind=[Enum]::Parse($kindType,'LineEllipseIntersectionMeasurement');Assert($catalog.Count-eq74-and@($catalog|Where-Object{$_.Kind-eq$kind}).Count-eq1) 'S76 tool is not discoverable in the 74-tool catalog after S81'
  $xaml=Get-Content (Join-Path $ProjectRoot 'MainWindow.xaml') -Raw;Assert($xaml-match'TaskLensCompactPanelS76'-and$xaml-match'FlowTaskLensModeRadioS76'-and$xaml-match'S75InputAnchor'-and$xaml-match'S75OutputAnchor') 'Task-lens UI or aggregate anchors missing';Assert(@([regex]::Matches($xaml,'\bVM\b|VisionMaster')).Count-eq0) 'Visible XAML still exposes VM/VisionMaster branding'
  Write-Output ('S76_TASK_LENS_LINE_ELLIPSE=PASS; Full='+$full.Count+'; Compact='+$compact.Count+'; Catalog=74; Chord=120')
}
finally{
  try{$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close()}catch{}
  if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
}
