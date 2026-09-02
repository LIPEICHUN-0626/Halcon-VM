param([string]$HalconRoot='C:\Program Files\MVTec\HALCON-20.11-Progress')
$ErrorActionPreference='Stop'
$env:PATH=(Join-Path $HalconRoot 'bin\x64-win64')+';'+$env:PATH
$dll=Join-Path $HalconRoot 'bin\dotnet35\halcondotnet.dll'
[Reflection.Assembly]::LoadFrom($dll)|Out-Null
[HalconDotNet.HOperatorSet].GetMethods()|Where-Object{$_.Name-in@('ReadOcrClassMlp','DoOcrMultiClassMlp','ClearOcrClassMlp','SortRegion','ReadOcrClassCnn','DoOcrMultiClassCnn','ClearOcrClassCnn')}|ForEach-Object{$_.ToString()}
Add-Type -ReferencedAssemblies $dll -TypeDefinition @'
using System;
using System.Linq;
using System.Globalization;
using HalconDotNet;
public static class S81OcrProbe {
 public static string Run(string imagePath,string modelPath) {
  HObject image=null,threshold=null,connected=null,selected=null,sorted=null;
  HTuple model=null,classes=null,confidence=null;
  bool loaded=false,cleared=false,cnn=modelPath.EndsWith(".occ",StringComparison.OrdinalIgnoreCase);
  string summary;
  try {
   HOperatorSet.ReadImage(out image,imagePath);
   HOperatorSet.Threshold(image,out threshold,0,125);
   HOperatorSet.Connection(threshold,out connected);
   HOperatorSet.SelectShape(connected,out selected,"area","and",50,999999);
   HOperatorSet.SortRegion(selected,out sorted,"character","true","row");
   if(cnn)HOperatorSet.ReadOcrClassCnn(modelPath,out model);else HOperatorSet.ReadOcrClassMlp(modelPath,out model);loaded=true;
   if(cnn)HOperatorSet.DoOcrMultiClassCnn(sorted,image,model,out classes,out confidence);else HOperatorSet.DoOcrMultiClassMlp(sorted,image,model,out classes,out confidence);
   if(classes.Length==0||classes.Length!=confidence.Length)throw new Exception("Empty/mismatched OCR result");
   summary="Text="+string.Concat(classes.SArr)+"; Count="+classes.Length+"; MinConfidence="+confidence.DArr.Min().ToString("0.000000",CultureInfo.InvariantCulture);
  } finally {
   if(loaded){if(cnn)HOperatorSet.ClearOcrClassCnn(model);else HOperatorSet.ClearOcrClassMlp(model);cleared=true;}
   if(model!=null)model.Dispose();if(classes!=null)classes.Dispose();if(confidence!=null)confidence.Dispose();
   if(sorted!=null)sorted.Dispose();if(selected!=null)selected.Dispose();if(connected!=null)connected.Dispose();if(threshold!=null)threshold.Dispose();if(image!=null)image.Dispose();
  }
  return summary+"; ModelCleared="+cleared;
 }
}
'@
# Runtime capability exploration only: these outputs are not UI-tool acceptance.
foreach($sample in @('chars_01.png','chars_02.png')){
 $path=Join-Path $env:PUBLIC ('Documents\MVTec\HALCON-20.11-Progress\examples\images\ocr\'+$sample)
 foreach($model in @('Document_0-9A-Z_NoRej.omc','Industrial_0-9A-Z_NoRej.omc','Universal_0-9A-Z_NoRej.occ')){
  try {
   $result=[S81OcrProbe]::Run($path,(Join-Path $HalconRoot ('ocr\'+$model)))
   Write-Output "S81_OCR_PROBE; Sample=$sample; Model=$model; $result"
  } catch {
   if($model.EndsWith('.occ') -and $_.Exception.ToString().Contains('#7990')){
    Write-Output "S81_OCR_UNAVAILABLE; Model=$model; HALCON=7990; DL module could not be loaded; NOT an acceptance pass"
   } else {throw}
  }
}
}
