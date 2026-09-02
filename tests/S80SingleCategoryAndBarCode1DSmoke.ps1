param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne[Threading.ApartmentState]::STA){throw 'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Assert([bool]$ok,[string]$message){if(-not$ok){throw "Assertion failed: $message"}}
$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-s80-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$mainType=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$mainType.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function F([string]$name){$f=$mainType.GetField($name,$all);if($null-eq$f){throw "Missing field $name"};$v=$f.GetValue($window);Write-Output -NoEnumerate $v}
function M([string]$name,[object[]]$invokeArgs=@()){try{$m=@($mainType.GetMethods($private)|Where-Object Name -eq $name|Where-Object{$_.GetParameters().Count-eq$invokeArgs.Count}|Select-Object -First 1);if($null-eq$m){throw "Missing method $name/$($invokeArgs.Count)"};$m.Invoke($window,$invokeArgs)}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
function Tool([string]$kind,[string]$name){$mainType.GetMethod('CreateFlowTool',$private).Invoke($window,[object[]]@([Enum]::Parse($kindType,$kind),$name,$true,$null))}
function Out($tool,[string]$port){$value=$null;if(-not$tool.TryGetOutputValue($port,[ref]$value)){throw "Missing output $port"};Write-Output -NoEnumerate $value}
function Execute($tool){M 'ExecuteFlowTool' @($tool,'S80 smoke')}

try{
  # A: every category is directly visible in the toolbox; search and direct mode tabs do not require a nested category menu.
  $catalog=F 'toolCatalog';$view=F 'toolCatalogView';$initial=@($view).Count
  Assert($catalog.Count-eq[Enum]::GetValues($kindType).Count-and@($catalog|Select-Object -ExpandProperty Category -Unique).Count-eq10) 'Catalog does not expose every current kind / 10 categories'
  Assert((F 'toolCatalogMode')-eq'All'-and$initial-eq$catalog.Count-and$view.Groups.Count-eq10) "Default toolbox does not directly expose all categories: count=$initial groups=$($view.Groups.Count)"
  $search=F 'ToolSearchTextBox';$search.Text='Code 128';M 'RefreshToolCatalogView'|Out-Null;$searchCount=@((F 'toolCatalogView')).Count
  $searchKinds=[string]::Join(',',@((F 'toolCatalogView')|ForEach-Object{$_.Kind.ToString()}));Assert($searchCount-eq1-and@((F 'toolCatalogView'))[0].Kind.ToString()-eq'BarCode1DRecognition') ('Search did not cross categories to find the barcode tool: count='+$searchCount+' kinds='+$searchKinds)
  $search.Text='';M 'RefreshToolCatalogView'|Out-Null;Assert((F 'toolCatalogMode')-eq'All'-and@((F 'toolCatalogView')).Count-eq$initial) 'Clearing search did not restore the full direct catalog'
  $allButton=F 'ToolCatalogAllButton';Assert($null-eq$allButton.ContextMenu-and$allButton.Content.ToString()-eq'全部'-and(F 'ToolCatalogCommonButton').Visibility.ToString()-eq'Visible'-and(F 'ToolCatalogSnippetButton').Visibility.ToString()-eq'Visible') 'Direct toolbox modes are missing or still hidden in a nested menu'

  # B: real HALCON Code 128 and Code 39 samples run through the managed workflow and typed outputs.
  $flow=F 'flowTools';$flow.Clear();$source=Tool ImageSource 'S80 Barcode Image';$reader=Tool BarCode1DRecognition 'S80 Barcode';$judge=Tool NumericJudge 'S80 Count Judge';$text=Tool TextAssembly 'S80 Text Consumer'
  $flow.Add($source);$flow.Add($reader);$flow.Add($judge);$flow.Add($text);$reader.SetInputBinding('Image',$source.ToolId,'Image');$judge.SetInputBinding('Value',$reader.ToolId,'CodeCount');$judge.InputToolId=$reader.ToolId;$judge.InputPortName='CodeCount';$judge.NumericLowerLimit=1;$judge.NumericUpperLimit=32;$text.SetInputBinding('String',$reader.ToolId,'DecodedText')
  $code128=Join-Path $env:PUBLIC 'Documents\MVTec\HALCON-20.11-Progress\examples\images\barcode\code128\code12801.png';$code39=Join-Path $env:PUBLIC 'Documents\MVTec\HALCON-20.11-Progress\examples\images\barcode\code39\code3901.png';$ean13=Join-Path $env:PUBLIC 'Documents\MVTec\HALCON-20.11-Progress\examples\images\barcode\ean13\ean13_label.png';$noCode=Join-Path $env:PUBLIC 'Documents\MVTec\HALCON-20.11-Progress\examples\images\fabrik.png';Assert((Test-Path $code128)-and(Test-Path $code39)-and(Test-Path $ean13)-and(Test-Path $noCode)) 'Traceable HALCON barcode/no-result samples are missing'
  $reader.Parameters.BarCodeMaxResults=8;$reader.Parameters.BarCodeNumScanlines=0;$reader.Parameters.BarCodeMinIdenticalScanlines=1;$reader.Parameters.BarCodeOrientationDegrees=0;$reader.Parameters.BarCodeOrientationToleranceDegrees=90
  $source.Parameters.LocalImagePath=$code128;$source.Parameters.LocalImageSerialNumber='S80-C128';Execute $source|Out-Null;$reader.Parameters.BarCodeType='Code 128';$record=Execute $reader;$text128=[string](Out $reader DecodedText);$count128=[double](Out $reader CodeCount);$pose=Out $reader Pose;$xld=Out $reader XLD
  Assert($record.ResultCode-eq'OK'-and$count128-ge1-and-not[string]::IsNullOrWhiteSpace($text128)) 'HALCON Code 128 sample was not decoded'
  Assert($pose.GetValidationError()-eq''-and$xld.ObjectCount-ge1-and([string](Out $reader Diagnostics))-match'Decoded=') 'Barcode pose, managed XLD or diagnostics missing'
  Execute $judge|Out-Null;Execute $text|Out-Null;Assert($judge.ResultCode-eq'OK'-and([string](Out $text Text)).Contains($text128)) 'Barcode outputs did not drive NumericJudge/TextAssembly'
  $oldXld=$xld;$source.Parameters.LocalImagePath=$code39;$source.Parameters.LocalImageSerialNumber='S80-C39';Execute $source|Out-Null;$reader.Parameters.BarCodeType='Code 39';Execute $reader|Out-Null;$text39=[string](Out $reader DecodedText)
  Assert($reader.ResultCode-eq'OK'-and[double](Out $reader CodeCount)-ge1-and-not[string]::IsNullOrWhiteSpace($text39)-and$oldXld.IsDisposed) 'HALCON Code 39 decode or stale-XLD cleanup failed'
  $source.Parameters.LocalImagePath=$ean13;$source.Parameters.LocalImageSerialNumber='S80-EAN13-RGB';Execute $source|Out-Null;$reader.Parameters.BarCodeType='EAN-13';Execute $reader|Out-Null;$textEan=[string](Out $reader DecodedText);Assert($reader.ResultCode-eq'OK'-and-not[string]::IsNullOrWhiteSpace($textEan)-and([string](Out $reader Parameters))-match'Channels=3->1') 'HALCON RGB EAN-13 sample was not decoded through the grayscale strategy'
  $eanXld=Out $reader XLD;$source.Parameters.LocalImagePath=$noCode;$source.Parameters.LocalImageSerialNumber='S80-NO-CODE';Execute $source|Out-Null;$reader.Parameters.BarCodeType='Code 128';Execute $reader|Out-Null;Assert($reader.ResultCode-eq'NG'-and[double](Out $reader CodeCount)-eq0-and[string]::IsNullOrWhiteSpace([string](Out $reader DecodedText))-and(Out $reader XLD).ObjectCount-eq0-and$eanXld.IsDisposed) 'No-result barcode path did not return clean NG/empty outputs'
  for($i=0;$i-lt3;$i++){Execute $reader|Out-Null};Assert((F 'barCodeModelsCreatedS80')-eq(F 'barCodeModelsClearedS80')) 'HALCON barcode model create/clear counts are unbalanced';$reader.Parameters.BarCodeType='Code 39'
  $reader.Parameters.BarCodeNumScanlines=201;$validation=$reader.Parameters.Validate([Enum]::Parse($kindType,'BarCode1DRecognition'));Assert(-not[string]::IsNullOrWhiteSpace($validation)) 'Invalid barcode scanline count was accepted';$reader.Parameters.BarCodeNumScanlines=0
  # Explicit zero is different from a missing legacy field; test the actual JSON contract.
  Add-Type -AssemblyName System.Runtime.Serialization
  $reader.Parameters.BarCodeOrientationToleranceDegrees=0
  $clone=$reader.Parameters.Clone();Assert($clone.BarCodeOrientationToleranceDegrees-eq0) 'Cloning silently reset explicit zero orientation tolerance'
  $serializer=New-Object System.Runtime.Serialization.Json.DataContractJsonSerializer($clone.GetType())
  $stream=New-Object IO.MemoryStream
  try{$serializer.WriteObject($stream,$clone);$stream.Position=0;$roundtrip=$serializer.ReadObject($stream);$roundtrip.Normalize();Assert($roundtrip.BarCodeOrientationToleranceDegrees-eq0) 'JSON round trip reset explicit zero orientation tolerance'}finally{$stream.Dispose()}
  $stream=New-Object IO.MemoryStream(,[Text.Encoding]::UTF8.GetBytes('{}'))
  try{$legacy=$serializer.ReadObject($stream);$legacy.Normalize();Assert($legacy.BarCodeOrientationToleranceDegrees-eq90-and$legacy.BarCodeMaxResults-eq1-and$legacy.BarCodeMinIdenticalScanlines-eq1) 'Missing legacy barcode fields did not receive safe defaults'}finally{$stream.Dispose()}
  $clone.BarCodeMaxResults=0;Assert(-not[string]::IsNullOrWhiteSpace($clone.Clone().Validate($reader.Kind))) 'Invalid zero result count was silently normalized'
  $clone.BarCodeMaxResults=1;$clone.BarCodeMinIdenticalScanlines=0;Assert(-not[string]::IsNullOrWhiteSpace($clone.Clone().Validate($reader.Kind))) 'Invalid zero identical scanline count was silently normalized'
  (F 'FlowToolList').SelectedItem=$reader;M 'LoadDockConfigurationDraft' @($reader)|Out-Null;Assert((F 'DockBarCode1DPanel').Visibility.ToString()-eq'Visible'-and(F 'DockBarCodeTypeComboBox').SelectedValue-eq'Code 39') 'Barcode central parameter dock did not synchronize'
  $recipe=M 'CaptureRecipe';$saved=@($recipe.ToolFlow|Where-Object{$_.ToolId-eq$reader.ToolId})[0];Assert($saved.Parameters.BarCodeType-eq'Code 39'-and$saved.Parameters.BarCodeMaxResults-eq8-and$saved.Parameters.BarCodeOrientationToleranceDegrees-eq0) 'Barcode parameters did not persist in recipe'
  $xaml=Get-Content (Join-Path $ProjectRoot 'MainWindow.xaml') -Raw;Assert($xaml-match'DockBarCode1DPanel') 'Barcode central panel missing';Assert(@([regex]::Matches($xaml,'\bVM\b|VisionMaster')).Count-eq0) 'Visible XAML exposes reference-product branding'
  Write-Output ('S80_DIRECT_CATEGORY_BARCODE=PASS; Catalog=' + $catalog.Count + '; Categories=10; Default=All/'+$initial+'; Search='+$searchCount+'; Code128='+$text128+'; Code39='+$text39+'; EAN13='+$textEan+'; NoResult=NG; Models='+[string](F 'barCodeModelsCreatedS80')+'/'+[string](F 'barCodeModelsClearedS80'))
}
finally{
  try{$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close()}catch{}
  if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
}
