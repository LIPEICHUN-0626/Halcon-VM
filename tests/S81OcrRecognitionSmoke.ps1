param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne[Threading.ApartmentState]::STA){throw 'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
if(-not('S81LoopbackServer' -as [type])){Add-Type -TypeDefinition @'
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
public sealed class S81LoopbackServer : IDisposable {
 private TcpListener listener; private Thread thread;
 public int Port { get; private set; } public string ReceivedText { get; private set; } public bool Closed { get; private set; }
 public S81LoopbackServer(){listener=new TcpListener(IPAddress.Loopback,0);listener.Start();Port=((IPEndPoint)listener.LocalEndpoint).Port;thread=new Thread(Run);thread.IsBackground=true;thread.Start();}
 private void Run(){try{using(var client=listener.AcceptTcpClient())using(var stream=client.GetStream()){var data=new byte[65536];int count=stream.Read(data,0,data.Length);ReceivedText=Encoding.UTF8.GetString(data,0,count);var reply=Encoding.UTF8.GetBytes("ACK:"+ReceivedText);stream.Write(reply,0,reply.Length);stream.Flush();}}catch{}finally{Closed=true;try{listener.Stop();}catch{}}}
 public void Dispose(){try{listener.Stop();}catch{}if(thread!=null&&thread.IsAlive)thread.Join(1000);}
}
'@}
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Assert([bool]$ok,[string]$message){if(-not$ok){throw "Assertion failed: $message"}}
$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-s81-ocr-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$mainType=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$mainType.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function F([string]$name){$f=$mainType.GetField($name,$all);if($null-eq$f){throw "Missing field $name"};$v=$f.GetValue($window);Write-Output -NoEnumerate $v}
function M([string]$name,[object[]]$invokeArgs=@()){try{$m=$mainType.GetMethods($private)|Where-Object Name -eq $name|Where-Object{$_.GetParameters().Count-eq$invokeArgs.Count}|Select-Object -First 1;if($null-eq$m){throw "Missing method $name/$($invokeArgs.Count)"};$result=$m.Invoke($window,$invokeArgs);Write-Output -NoEnumerate $result}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
function Tool([string]$kind,[string]$name){$mainType.GetMethod('CreateFlowTool',$private).Invoke($window,[object[]]@([Enum]::Parse($kindType,$kind),$name,$true,$null))}
function Out($tool,[string]$port){$value=$null;if(-not$tool.TryGetOutputValue($port,[ref]$value)){throw "Missing output $port"};Write-Output -NoEnumerate $value}
function Execute($tool){M 'ExecuteFlowTool' @($tool,'S81 OCR')}

Add-Type -ReferencedAssemblies (Join-Path $halcon 'bin\dotnet35\halcondotnet.dll') -TypeDefinition @'
using System;
using System.Collections.Generic;
using HalconDotNet;
public static class S81Samples {
 public static HObject Letters(string path) {
  HObject image=null,dark=null,dilated=null,connected=null,original=null,sorted=null;HTuple t=null;
  try {
   HOperatorSet.ReadImage(out image,path);HOperatorSet.BinaryThreshold(image,out dark,"max_separability","dark",out t);
   HOperatorSet.DilationCircle(dark,out dilated,2.5);HOperatorSet.Connection(dilated,out connected);
   HOperatorSet.Intersection(connected,dark,out original);HOperatorSet.SortRegion(original,out sorted,"character","true","row");
   return sorted;
  } finally {foreach(HObject x in new[]{image,dark,dilated,connected,original})if(x!=null)x.Dispose();if(t!=null)t.Dispose();}
 }
 public static HObject Label(string path,string outputPath) {
  var objects=new List<HObject>();var tuples=new List<HTuple>();
  HObject image,region,connected,labelRaw,labelOpen,label,deskewed,labelDeskew,reduced,foreground,blobs,split,chars,sorted,words,date,sortedWords,sortedDate;
  HTuple angle,identity,matrix,top,left,bottom,right;
  try {
   HOperatorSet.ReadImage(out image,path);objects.Add(image);
   HOperatorSet.Threshold(image,out region,128,230);objects.Add(region);
   HOperatorSet.Connection(region,out connected);objects.Add(connected);
   HOperatorSet.SelectShape(connected,out labelRaw,"width","and",350,450);objects.Add(labelRaw);
   HOperatorSet.OpeningCircle(labelRaw,out labelOpen,5);objects.Add(labelOpen);
   HOperatorSet.ShapeTrans(labelOpen,out label,"rectangle2");objects.Add(label);
   HOperatorSet.TextLineOrientation(label,image,25,-0.523599,0.523599,out angle);tuples.Add(angle);
   HOperatorSet.HomMat2dIdentity(out identity);tuples.Add(identity);
   HOperatorSet.HomMat2dRotate(identity,-angle.D,0,0,out matrix);tuples.Add(matrix);
   HOperatorSet.AffineTransImage(image,out deskewed,matrix,"constant","false");objects.Add(deskewed);
   HOperatorSet.AffineTransRegion(label,out labelDeskew,matrix,"nearest_neighbor");objects.Add(labelDeskew);
   HOperatorSet.ReduceDomain(deskewed,labelDeskew,out reduced);objects.Add(reduced);
   HOperatorSet.VarThreshold(reduced,out foreground,40,40,0.8,10,"dark");objects.Add(foreground);
   HOperatorSet.Connection(foreground,out blobs);objects.Add(blobs);
   HOperatorSet.PartitionDynamic(blobs,out split,21,40);objects.Add(split);
   using(var names=new HTuple(new[]{"width","height"}))
   using(var mins=new HTuple(new[]{10,20}))
   using(var maxs=new HTuple(new[]{30,50}))
    HOperatorSet.SelectShape(split,out chars,names,"and",mins,maxs);
   objects.Add(chars);
   HOperatorSet.SmallestRectangle1(labelDeskew,out top,out left,out bottom,out right);tuples.AddRange(new[]{top,left,bottom,right});
   HOperatorSet.SelectShape(chars,out words,"row","and",0,top.D+80);objects.Add(words);
   HOperatorSet.SelectShape(chars,out date,"row","and",top.D+80,600);objects.Add(date);
   // This known-layout fixture supplies an explicitly ordered Region array.
   // It does not claim the product's automatic row grouping corrects perspective.
   HOperatorSet.SortRegion(words,out sortedWords,"upper_left","true","column");objects.Add(sortedWords);
   HOperatorSet.SortRegion(date,out sortedDate,"upper_left","true","column");objects.Add(sortedDate);
   HOperatorSet.ConcatObj(sortedWords,sortedDate,out sorted);
   HOperatorSet.WriteImage(deskewed,"png",0,outputPath);
   return sorted;
  } finally {foreach(var x in objects)x.Dispose();foreach(var x in tuples)x.Dispose();}
 }
 public static void Rgb(string source,string target) {
  HObject image=null,rgb=null;
  try{HOperatorSet.ReadImage(out image,source);HOperatorSet.Compose3(image,image,image,out rgb);HOperatorSet.WriteImage(rgb,"png",0,target);}
  finally{if(rgb!=null)rgb.Dispose();if(image!=null)image.Dispose();}
 }
}
'@
$regionType=$assembly.GetType('HalconWinFormsDemo.Models.VmRegionSnapshot',$true)
function PublishRegion($producer,$obj){
 $snapshot=$regionType.GetMethod('Create').Invoke($null,[object[]]@($obj,$producer.ToolId,$producer.InstanceName,'Region'))
 $producer.SetOutputValue('Region',$snapshot);$producer.ResultCode='OK'
}
function Fails($action,[string]$message){$failed=$false;try{&$action|Out-Null}catch{$failed=$true};Assert $failed $message}
try{
 $flow=F 'flowTools';M 'DisposeFlowTools'|Out-Null;$flow.Clear()
 $source=Tool ImageSource 'S81 Image';$regions=Tool ImageThreshold 'S81 Segmented Characters';$ocr=Tool OcrRecognition 'S81 OCR';$text=Tool TextAssembly 'S81 Text';$judge=Tool NumericJudge 'S81 Confidence';$tcp=Tool TcpClientExchange 'S81 TCP'
 foreach($tool in @($source,$regions,$ocr,$text,$judge,$tcp)){$flow.Add($tool)}
 $regions.SetInputBinding('Image',$source.ToolId,'Image');$ocr.SetInputBinding('Image',$source.ToolId,'Image');$ocr.SetInputBinding('Characters',$regions.ToolId,'Region');$text.SetInputBinding('String',$ocr.ToolId,'Text')
 $judge.SetInputBinding('Value',$ocr.ToolId,'MeanConfidence');$judge.InputToolId=$ocr.ToolId;$judge.InputPortName='MeanConfidence';$judge.NumericLowerLimit=0;$judge.NumericUpperLimit=1
 $images=Join-Path $env:PUBLIC 'Documents\MVTec\HALCON-20.11-Progress\examples\images'
 $letters=Join-Path $images 'letters.png';$label=Join-Path $images 'label\label_01.png';$deskew=Join-Path $temp 'label-deskew.png'
 $ocr.Parameters.OcrMinimumArea=1;$ocr.Parameters.OcrMinimumConfidence=0.8
 $source.Parameters.LocalImagePath=$letters;Execute $source|Out-Null
 $obj=[S81Samples]::Letters($letters);try{PublishRegion $regions $obj}finally{$obj.Dispose()}
 Execute $ocr|Out-Null;$lettersText=[string](Out $ocr Text);$lettersCount=[int](Out $ocr CharacterCount)
 $expected='';foreach($code in 97..122){$expected+=([string][char]$code)*20};$expected+='.'*20
 Write-Output ('S81_LETTERS; Count='+$lettersCount+'; TextMatches='+($lettersText-eq$expected)+'; Result='+$ocr.ResultCode+'; Min='+[string](Out $ocr MinimumConfidence))
 Assert($lettersCount-eq540-and$lettersText-eq$expected) 'Official letters ground truth mismatch'
 Assert((Out $ocr Centers).Count-eq540-and(Out $ocr XLD).ObjectCount-ge540) 'Typed centers/managed contours missing'
 Execute $text|Out-Null;Execute $judge|Out-Null
 Assert(([string](Out $text Text)).Contains($lettersText)-and$judge.ResultCode-eq'OK') 'Text/Number downstream failed'
 $lettersXld=Out $ocr XLD
 $obj=[S81Samples]::Label($label,$deskew)
 try{$source.Parameters.LocalImagePath=$deskew;Execute $source|Out-Null;PublishRegion $regions $obj}finally{$obj.Dispose()}
 $ocr.Parameters.OcrSortMode='Source'
 Execute $ocr|Out-Null;$labelText=[string](Out $ocr Text);Write-Output ('S81_LABEL; Text='+$labelText+'; Result='+$ocr.ResultCode+'; Min='+[string](Out $ocr MinimumConfidence))
 Assert(($labelText-replace'[^A-Za-z0-9]','')-eq'BESTBEFOREEND291102') 'Official label ground truth mismatch'
 Assert($lettersXld.IsDisposed) 'Old OCR contours were not released'
 Assert($ocr.ResultCode-eq'NG'-and[double](Out $ocr MinimumConfidence)-lt0.8-and[int](Out $ocr LowConfidenceCount)-gt0) 'Uncertain label was not rejected by the confidence gate'
 $text.SetInputBinding('ResultCode',$ocr.ToolId,'ResultCode');$text.Parameters.TextAssemblyTemplate='{String}|{ResultCode}';Execute $text|Out-Null
 Assert(([string](Out $text Text))-eq($labelText+'|NG')) 'Diagnostic text output lost the OCR rejection state'
 $tcp.SetInputBinding('String',$text.ToolId,'Text');$tcp.Parameters.TcpClientHost='127.0.0.1';$tcp.Parameters.TcpClientConnectTimeoutMilliseconds=500;$tcp.Parameters.TcpClientSendTimeoutMilliseconds=500;$tcp.Parameters.TcpClientReceiveTimeoutMilliseconds=500;$tcp.Parameters.TcpClientEncoding='UTF-8';$tcp.Parameters.TcpClientLineEnding='None';$tcp.Parameters.TcpClientSendTemplate='{String}';$tcp.Parameters.TcpClientWaitForResponse=$true
 $server=[S81LoopbackServer]::new();try{$tcp.Parameters.TcpClientPort=$server.Port;Execute $tcp|Out-Null;Assert($server.ReceivedText-eq($labelText+'|NG')-and[string](Out $tcp ReceivedText)-eq('ACK:'+$labelText+'|NG')) 'OCR diagnostic text did not traverse real TCP'}finally{$server.Dispose()};Assert($server.Closed) 'TCP connection was not released'
 # Same recognized regions and source pixels, now passed through the RGB strategy.
 $rgb=Join-Path $temp 'label-rgb.png';[S81Samples]::Rgb($deskew,$rgb)
 $obj=[S81Samples]::Label($label,$deskew)
 try{$source.Parameters.LocalImagePath=$rgb;Execute $source|Out-Null;PublishRegion $regions $obj}finally{$obj.Dispose()}
 Execute $ocr|Out-Null
 Assert(([string](Out $ocr Text))-eq$labelText-and([string](Out $ocr Parameters))-match'Channels=3->1') 'RGB strategy changed OCR results'
 # Unbound-character route: real thresholding in a clean official letters row ROI.
 $ocr.RemoveInputBinding('Characters')
 $ocr.Parameters.OcrSortMode='Row'
 $source.Parameters.LocalImagePath=$letters;Execute $source|Out-Null
 $roi=$null;[HalconDotNet.HOperatorSet]::GenRectangle1([ref]$roi,0,0,30,827)
 try{PublishRegion $regions $roi}finally{$roi.Dispose()}
 $ocr.SetInputBinding('ROI',$regions.ToolId,'Region');$ocr.Parameters.OcrThreshold=128;Execute $ocr|Out-Null
 Assert(([string](Out $ocr Text))-eq('a'*20)) 'ROI threshold route did not read the first official letters row'
 # Tool-bound detection and mask layers are intersected/subtracted from the subscribed ROI.
 $roiType=$assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
 $detectionGeometry=$roiType.GetMethod('CreateRectangle').Invoke($null,[object[]]@(0.0,0.0,30.0,827.0));$maskGeometry=$roiType.GetMethod('CreateRectangle').Invoke($null,[object[]]@(0.0,410.0,30.0,500.0))
 try{$detection=M 'AddRoiLayer' @($detectionGeometry,'OCR 检测区',$ocr);$mask=M 'AddRoiLayer' @($maskGeometry,'OCR 屏蔽区',$ocr)}finally{$detectionGeometry.Dispose();$maskGeometry.Dispose()}
 $mask.IsMask=$true;Execute $ocr|Out-Null;$maskedCount=[int](Out $ocr CharacterCount)
 Assert($maskedCount-gt0-and$maskedCount-lt20-and([string](Out $ocr Parameters))-match'ROI=True') 'OCR tool-level mask did not remove candidates'
 $mask.IsEnabled=$false;Execute $ocr|Out-Null;Assert([int](Out $ocr CharacterCount)-eq20) 'Disabling the OCR mask did not restore candidates';$detection.IsEnabled=$false
 (F 'FlowToolList').SelectedItem=$ocr;M 'RefreshPortDataMonitorS64' @($ocr)|Out-Null;$monitor=(F 'portDataMonitorRows').PSObject.BaseObject
 Assert(($monitor|Where-Object{$_.Direction-eq'OUT'-and$_.PortName-eq'Text'-and$_.ValueText-match'a{20}'}).Count-eq1-and($monitor|Where-Object{$_.PortName-eq'CharacterCount'-and$_.ValueText-match'20'}).Count-ge1) 'OCR text/count are missing from the port monitor'
 $old=Out $ocr XLD
 # No characters: a white gap between the official h and g rows.
 $roi=$null;[HalconDotNet.HOperatorSet]::GenRectangle1([ref]$roi,237,0,248,827)
 try{PublishRegion $regions $roi}finally{$roi.Dispose()}
 Execute $ocr|Out-Null
 Assert($ocr.ResultCode-eq'NG'-and[int](Out $ocr CharacterCount)-eq0-and([string](Out $ocr Text))-eq''-and(Out $ocr XLD).ObjectCount-eq0-and$old.IsDisposed) 'Empty OCR results retained stale values'
 $emptyXld=Out $ocr XLD
 # Invalid model and parameters must fail without exposing the previous text.
 $validModel=$ocr.Parameters.OcrModelPath;$ocr.Parameters.OcrModelPath=Join-Path $temp 'missing.omc';Fails {Execute $ocr} 'Missing OCR model was accepted';Assert($emptyXld.IsDisposed) 'OCR exception retained the previous empty contour snapshot';$ocr.Parameters.OcrModelPath=$validModel
 $ocr.Parameters.OcrMinimumConfidence=1.1;Fails {Execute $ocr} 'Out-of-range confidence was accepted';$ocr.Parameters.OcrMinimumConfidence=0.8
 $source.ResultCode='--';Fails {Execute $ocr} 'Unrun Image source was accepted'
 # UI draft/application, clone/recipe and upstream ownership.
 (F 'FlowToolList').SelectedItem=$ocr;M 'LoadDockConfigurationDraft' @($ocr)|Out-Null
 Assert((F 'DockOcrPanel').Visibility.ToString()-eq'Visible') 'OCR panel not reachable'
 (F 'DockOcrConfidenceTextBox').Text='0.75';M 'DockApplyButton_Click' @($null,$null)|Out-Null
 Assert($ocr.Parameters.OcrMinimumConfidence-eq0.75) ('OCR UI draft was not applied; Status='+(F 'HeaderStatusText').Text+'; Validation='+(F 'DockValidationText').Text)
 # Real workflow copy retains parameters, ROI bindings and legal external Image/Region inputs, but no runtime snapshots.
 (F 'FlowToolList').SelectedItems.Clear();(F 'FlowToolList').SelectedItems.Add($ocr);Assert([int](M 'CopySelectedFlowToolsToClipboard')-eq1) 'OCR copy failed';Assert([int](M 'PasteFlowToolClipboard' @($flow.Count))-eq1) 'OCR paste failed'
 $ocrCopy=$flow[$flow.Count-1];Assert($ocrCopy.Kind.ToString()-eq'OcrRecognition'-and$ocrCopy.Parameters.OcrMinimumConfidence-eq0.75-and$ocrCopy.GetInputBinding('Image').SourceToolId-eq$source.ToolId-and$ocrCopy.BoundRoiIds.Count-eq$ocr.BoundRoiIds.Count) 'OCR copy lost parameters, valid inputs or ROI bindings';$unused=$null;Assert(-not$ocrCopy.TryGetOutputValue('Text',[ref]$unused)) 'OCR copy retained runtime text'
 $recipe=M 'CaptureRecipe';$saved=@($recipe.ToolFlow|Where-Object{$_.ToolId-eq$ocr.ToolId})[0]
 Assert($saved.Parameters.OcrModelPath-eq$validModel-and$saved.Parameters.OcrMinimumConfidence-eq0.75-and$saved.InputBindings.Count-ge2-and@($recipe.RoiLayers|Where-Object{$_.IsMask}).Count-eq1) 'OCR recipe parameters, bindings or mask missing'
 $logFile=Join-Path $logger.LogDirectory ((Get-Date).ToString('yyyyMMdd')+'.log');$log=[IO.File]::ReadAllText($logFile)
 Assert($log.Contains('S81 OCR：OCR OK')-and$log.Contains('S81 OCR：OCR NG')) 'OCR OK/NG diagnostics are missing from the persistent log'
 # Upstream release and explicit invalidation reject stale data and release the last managed contour.
 $source.Parameters.LocalImagePath=$letters;Execute $source|Out-Null;$roi=$null;[HalconDotNet.HOperatorSet]::GenRectangle1([ref]$roi,0,0,30,827);try{PublishRegion $regions $roi}finally{$roi.Dispose()};Execute $ocr|Out-Null
 $lastXld=Out $ocr XLD;$mainType.GetMethod('InvalidateToolRunResult',[Reflection.BindingFlags]'Static,NonPublic').Invoke($null,[object[]]@($ocr,'S81 lifecycle'))|Out-Null;Assert($lastXld.IsDisposed-and$ocr.ResultCode-eq'--') 'OCR invalidation retained its managed XLD/text state'
 $source.ClearRuntimeOutputs();$source.ResultCode='--';Fails {Execute $ocr} 'Released/unrun Image input was accepted'
 Assert((F 'ocrModelsLoadedS81')-eq(F 'ocrModelsClearedS81')) 'OCR models leaked'
 Assert((F 'toolCatalog').Count-eq74) 'OCR missing from toolbox'
 Write-Output ('S81_OCR_SMOKE=PASS; Letters='+$lettersCount+'; Label='+$labelText+'; Masked='+$maskedCount+'; TCP=PASS; CopyRecipeMonitorLog=PASS; Models='+[string](F 'ocrModelsLoadedS81')+'/'+[string](F 'ocrModelsClearedS81'))
}
finally{try{$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close()}catch{};if(Test-Path $temp){$root=[IO.Path]::GetFullPath($temp);if(-not$root.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()),[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe cleanup path'};Remove-Item -LiteralPath $root -Recurse -Force}}
