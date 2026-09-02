param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne[Threading.ApartmentState]::STA){throw 'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Assert([bool]$ok,[string]$message){if(-not$ok){throw "Assertion failed: $message"}}
$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-s77-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$mainType=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$mainType.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function F([string]$name){$f=$mainType.GetField($name,$all);if($null-eq$f){throw "Missing field $name"};$v=$f.GetValue($window);Write-Output -NoEnumerate $v}
function M([string]$name,[object[]]$invokeArgs=@()){try{$m=@($mainType.GetMethods($private)|Where-Object Name -eq $name|Where-Object{$_.GetParameters().Count-eq$invokeArgs.Count}|Select-Object -First 1);if($null-eq$m){throw "Missing method $name/$($invokeArgs.Count)"};$m.Invoke($window,$invokeArgs)}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
function Tool([string]$kind,[string]$name){$mainType.GetMethod('CreateFlowTool',$private).Invoke($window,[object[]]@([Enum]::Parse($kindType,$kind),$name,$true,$null))}
function Out($tool,[string]$port){$value=$null;if(-not$tool.TryGetOutputValue($port,[ref]$value)){throw "Missing output $port"};Write-Output -NoEnumerate $value}
function Execute($tool){M 'ExecuteFlowTool' @($tool,'S77 smoke')}
function Propose($tool,[int]$index){$args=[object[]]@($tool,$index,$null,$null);$ok=M 'TryBuildFlowReorderS77' $args;[pscustomobject]@{Ok=[bool]$ok;Order=$args[2];Reason=[string]$args[3]}}

try{
  $flow=F 'flowTools';$flow.Clear()

  # A: a dedicated vertical reorder changes the real deterministic order only when every dependency remains forward-only.
  $source=Tool ImageSource 'S77 Source';$channel=Tool ImageChannel 'S77 Channel';$threshold=Tool ImageThreshold 'S77 Threshold';$independent=Tool ImageSource 'S77 Independent'
  $flow.Add($source);$flow.Add($channel);$flow.Add($threshold);$flow.Add($independent)
  $channel.SetInputBinding('Image',$source.ToolId,'Image');$threshold.SetInputBinding('Image',$channel.ToolId,'Image')
  $baselineRecipe=M 'CaptureRecipe';$baselineFingerprint=$serviceType.GetMethod('GetRecipeFingerprint').Invoke($service,[object[]]@($baselineRecipe));$mainType.GetField('savedRecipeFingerprint',$private).SetValue($window,$baselineFingerprint)
  M 'RefreshFlowGraphLayout'|Out-Null
  $valid=Propose $independent 1;$validOrder=@($valid.Order);Assert([bool]$valid.Ok) ('Independent module was rejected: '+$valid.Reason);Assert(($validOrder[0].ToolId-eq$source.ToolId)-and($validOrder[1].ToolId-eq$independent.ToolId)) ('Valid order was wrong: '+([string]::Join(',',@($validOrder|ForEach-Object InstanceName))));Assert(-not[string]::IsNullOrWhiteSpace($valid.Reason)) 'Valid reorder preview reason missing'
  $before=M 'CaptureFlowEditSnapshot';$applyArgs=New-Object object[] 1;$applyArgs[0]=$valid.Order;M 'ApplyFlowReorderS77' $applyArgs|Out-Null;M 'CommitFlowEditHistory' @('S77 smoke reorder',$before)|Out-Null
  Assert($flow[1].ToolId-eq$independent.ToolId) ('Real order was not updated: '+([string]::Join(',',@($flow|ForEach-Object InstanceName))))
  Assert((F 'isRecipeDirty')) 'Recipe dirty state was not updated'
  Assert((F 'flowUndoHistory').Count-ge1) ('Reorder history was not recorded: '+[string](F 'flowUndoHistory').Count)
  M 'UndoFlowEditButton_Click' @($null,$null)|Out-Null;$flow=F 'flowTools';Assert($flow[0].ToolId-eq$source.ToolId-and$flow[1].ToolId-eq$channel.ToolId-and$flow[2].ToolId-eq$threshold.ToolId) 'One undo did not restore the original sequence'
  $source=@($flow|Where-Object ToolId -eq $source.ToolId)[0].PSObject.BaseObject;$channel=@($flow|Where-Object ToolId -eq $channel.ToolId)[0].PSObject.BaseObject;$threshold=@($flow|Where-Object ToolId -eq $threshold.ToolId)[0].PSObject.BaseObject;$independent=@($flow|Where-Object ToolId -eq $independent.ToolId)[0].PSObject.BaseObject
  $reject=Propose $source 3;Assert((-not$reject.Ok)-and-not[string]::IsNullOrWhiteSpace($reject.Reason)) 'Move that placed a source after its consumer was not explicitly rejected'
  $channel.GroupId='S77.Group';$channel.GroupName='预处理组';$threshold.GroupId='S77.Group';$threshold.GroupName='预处理组';$block=Propose $channel 4;Assert($block.Ok-and@($block.Order|Where-Object { $_.GroupId -eq 'S77.Group' }).Count-eq2) 'A group was not moved as one preserved block'
  $threshold.Parameters.PositionCorrectionToolId=$independent.ToolId;$positionReject=Propose $independent 4;Assert((-not$positionReject.Ok)-and-not[string]::IsNullOrWhiteSpace($positionReject.Reason)) 'Position-correction dependency was not protected';$threshold.Parameters.PositionCorrectionToolId=''
  M 'RefreshFlowGraphLayout'|Out-Null;Assert(@($flow|Where-Object{$_.GraphHeight-eq68-or$_.GraphHeight-eq32}).Count-eq$flow.Count) 'Task-lens full/compact states were broken by reorder support'

  # B: run the real HALCON QR and ECC200 samples through the same managed Image/ROI/execution/output lifecycle.
  $qr=Join-Path $env:PUBLIC 'Documents\MVTec\HALCON-20.11-Progress\examples\images\datacode\qrcode\qr_generated.png'
  $dm=Join-Path $env:PUBLIC 'Documents\MVTec\HALCON-20.11-Progress\examples\images\datacode\ecc200\ecc200_cpu_001.png'
  Assert((Test-Path -LiteralPath $qr)-and(Test-Path -LiteralPath $dm)) 'Traceable HALCON QR/ECC200 samples are missing'
  $image=Tool ImageSource 'S77 Code Image';$reader=Tool DataCode2DRecognition 'S77 2D Code';$judge=Tool NumericJudge 'S77 Count Judge';$text=Tool TextAssembly 'S77 Text Consumer'
  $flow.Add($image);$flow.Add($reader);$flow.Add($judge);$flow.Add($text)
  $reader.SetInputBinding('Image',$image.ToolId,'Image');$judge.SetInputBinding('Value',$reader.ToolId,'CodeCount');$judge.InputToolId=$reader.ToolId;$judge.InputPortName='CodeCount';$judge.NumericLowerLimit=1;$judge.NumericUpperLimit=32
  $text.SetInputBinding('String',$reader.ToolId,'DecodedText')
  $image.Parameters.LocalImagePath=$qr;$image.Parameters.LocalImageSerialNumber='S77-QR';Execute $image|Out-Null
  $reader.Parameters.DataCodeType='QR Code';$reader.Parameters.DataCodeRecognitionProfile='standard_recognition';$reader.Parameters.DataCodePolarity='dark_on_light';$reader.Parameters.DataCodeMaxResults=8;$reader.Parameters.DataCodeTimeoutMilliseconds=3000
  $qrRecord=Execute $reader;$qrText=[string](Out $reader DecodedText);$qrCount=[double](Out $reader CodeCount);$qrPose=Out $reader Pose;$qrXld=Out $reader XLD
  Assert($qrRecord.ResultCode-eq'OK'-and$qrCount-ge1-and-not[string]::IsNullOrWhiteSpace($qrText)) 'HALCON QR sample was not decoded'
  Assert($qrPose.GetValidationError()-eq''-and$qrXld.ObjectCount-ge1-and(Out $reader Diagnostics)-match'Decoded=') 'QR pose, managed XLD or diagnostics missing'
  Execute $judge|Out-Null;Execute $text|Out-Null;Assert($judge.ResultCode-eq'OK'-and([string](Out $text Text)).Contains($qrText)) 'CodeCount or DecodedText did not drive real downstream tools'
  $oldXld=$qrXld;$image.Parameters.LocalImagePath=$dm;$image.Parameters.LocalImageSerialNumber='S77-DM';Execute $image|Out-Null;$reader.Parameters.DataCodeType='Data Matrix ECC 200';$reader.Parameters.DataCodePolarity='any';Execute $reader|Out-Null
  $dmText=[string](Out $reader DecodedText);Assert($reader.ResultCode-eq'OK'-and[double](Out $reader CodeCount)-ge1-and-not[string]::IsNullOrWhiteSpace($dmText)-and$oldXld.IsDisposed) 'HALCON ECC200 decode or stale-XLD cleanup failed'
  for($i=0;$i-lt3;$i++){Execute $reader|Out-Null};Assert((F 'dataCodeModelsCreatedS77')-eq(F 'dataCodeModelsClearedS77')) 'HALCON data-code model create/clear counts are unbalanced'
  $reader.Parameters.DataCodeTimeoutMilliseconds=0;$validation=$reader.Parameters.Validate([Enum]::Parse($kindType,'DataCode2DRecognition'));Assert(-not[string]::IsNullOrWhiteSpace($validation)) 'Invalid data-code timeout was accepted';$reader.Parameters.DataCodeTimeoutMilliseconds=2000
  (F 'FlowToolList').SelectedItem=$reader;M 'LoadDockConfigurationDraft' @($reader)|Out-Null;Assert((F 'DockDataCode2DPanel').Visibility.ToString()-eq'Visible'-and(F 'DockDataCodeTypeComboBox').SelectedValue-eq'Data Matrix ECC 200') 'Data-code dock did not synchronize'
  M 'OpenCentralModuleConfigurationS77' @($reader)|Out-Null
  Assert((F 'CentralModuleConfigurationOverlayS77').Visibility.ToString()-eq'Visible') 'Double-click configuration host did not open in the central workspace'
  Assert((F 'technicalHelpMovedToHoverS77').Count-gt0) 'Long inline technical help was not moved into hover help'
  Assert([object]::ReferenceEquals((F 'CentralModuleConfigurationHostS77').Content,(F 'DockConfigurationWorkspaceContent'))) 'Central configuration copied UI instead of moving the one real workspace'
  Assert((F 'CentralParametersButtonS77').Content.ToString().Length-eq2-and(F 'CentralInputsButtonS77').Content.ToString().Length-eq2-and(F 'CentralRoiButtonS77').Content.ToString().Length-eq3-and(F 'CentralResultsButtonS77').Content.ToString().Length-eq2) 'Central task labels are not compact'
  M 'CentralInputsButton_ClickS77' @($null,$null)|Out-Null;Assert([object]::ReferenceEquals((F 'DockConfigurationTabs').SelectedItem,(F 'DockIoTaskTab'))) 'Central input task did not switch in place'
  M 'CentralParametersButton_ClickS77' @($null,$null)|Out-Null;Assert([object]::ReferenceEquals((F 'DockConfigurationTabs').SelectedItem,(F 'DockParametersTaskTab'))) 'Central common-parameter task did not switch in place'
  Assert((F 'CentralParametersButtonS77').ToolTip.ToString().Length-gt10-and(F 'CentralInputsButtonS77').ToolTip.ToString().Length-gt10) 'Technical explanations were not moved to hover help'
  Assert((M 'CloseCentralModuleConfigurationS77')) 'Central configuration refused a clean close';Assert((F 'CentralModuleConfigurationOverlayS77').Visibility.ToString()-eq'Collapsed'-and[object]::ReferenceEquals((F 'DockConfigurationTab').Content,(F 'DockConfigurationWorkspaceContent'))) 'Central configuration did not restore its one real workspace'
  $recipe=M 'CaptureRecipe';$saved=@($recipe.ToolFlow|Where-Object { $_.ToolId -eq $reader.ToolId })[0];Assert($saved.Parameters.DataCodeType-eq'Data Matrix ECC 200'-and$saved.Parameters.DataCodeMaxResults-eq8) 'Data-code parameters did not persist in recipe'
  $catalog=F 'toolCatalog';$codeKind=[Enum]::Parse($kindType,'DataCode2DRecognition');Assert($catalog.Count-eq[Enum]::GetValues($kindType).Count-and@($catalog|Where-Object { $_.Kind -eq $codeKind }).Count-eq1-and@($catalog|Select-Object -ExpandProperty Category -Unique).Count-eq10) 'S77 tool is not discoverable in the current complete 10-category catalog'
  $xaml=Get-Content (Join-Path $ProjectRoot 'MainWindow.xaml') -Raw;Assert($xaml-match'FlowReorderGrip_PreviewMouseLeftButtonDownS77'-and$xaml-match'FlowReorderInsertionLineS77'-and$xaml-match'DockDataCode2DPanel'-and$xaml-match'CentralModuleConfigurationOverlayS77') 'S77 reorder grip, insertion line, data-code dock or central configuration host is missing';Assert(@([regex]::Matches($xaml,'\bVM\b|VisionMaster')).Count-eq0) 'Visible XAML exposes VM/VisionMaster branding'
  Write-Output ('S77_VERTICAL_REORDER_DATACODE=PASS; Catalog=' + $catalog.Count + '; Categories=10; QR='+$qrText+'; DM='+$dmText+'; Models='+[string](F 'dataCodeModelsCreatedS77')+'/'+[string](F 'dataCodeModelsClearedS77')+'')
}
finally{
  try{$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close()}catch{}
  if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
}
