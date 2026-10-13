param([string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
$analyzer=$assembly.GetType('HalconWinFormsDemo.Services.GrayLineProfileService',$true).GetMethod('Analyze')
function Analyze($im,[double]$r1,[double]$c1,[double]$r2,[double]$c2,[int]$count){$analyzer.Invoke($null,[object[]]@($im.PSObject.BaseObject,$r1,$c1,$r2,$c2,$count))}
function Assert([bool]$ok,[string]$message){if(-not$ok){throw $message}}
$image=New-Object HalconDotNet.HImage
$image.GenImageConst('real',10,10)
$rows=[HalconDotNet.HTuple]::new([int[]]@(0..99|ForEach-Object{[int][Math]::Floor($_/10)}))
$columns=[HalconDotNet.HTuple]::new([int[]]@(0..99|ForEach-Object{$_%10}))
$values=[HalconDotNet.HTuple]::new([double[]]@(0..99|ForEach-Object{[Math]::Floor($_/10)*10+($_%10)}))
$domain=$null;$reduced=$null;$samples=$null;$sampleRows=$null;$sampleColumns=$null
try{
 [HalconDotNet.HOperatorSet]::SetGrayval($image,$rows,$columns,$values)
 $sampleRows=[HalconDotNet.HTuple]::new([double[]]@(0,0.5,4.25,9))
 $sampleColumns=[HalconDotNet.HTuple]::new([double[]]@(0,0.5,2.5,9))
 $samples=$image.GetGrayvalInterpolated($sampleRows,$sampleColumns,'bilinear')
 $expected=@(0,5.5,45,99)
 for($i=0;$i-lt4;$i++){Assert([Math]::Abs($samples[$i].D-$expected[$i])-lt1e-10) "Bilinear sample $i differs"}
 $domain=[HalconDotNet.HRegion]::new(0.0,0.0,4.0,9.0)
 $reduced=$image.ReduceDomain($domain)
 # Confirm the documented domain bypass; the product must explicitly reject it.
 $outside=$reduced.GetGrayvalInterpolated(8.0,8.0,'bilinear')
 Assert($outside-eq88) 'Unexpected HALCON domain contract'
 $inside=$domain.TestRegionPoint(8,8);Assert($inside-eq0) 'Domain membership test failed'
 # Fractional sample straddles the reduced domain although its floor pixel is valid.
 $a=$domain.TestRegionPoint(4,2);$b=$domain.TestRegionPoint(5,2)
 Assert($a-eq1-and$b-eq0) 'Bilinear support guard cannot distinguish border neighbors'
 $profile=Analyze $image 0.5 0.5 4.5 4.5 5
 Assert($profile.Values.Count-eq5-and[Math]::Abs($profile.Mean-27.5)-lt1e-10-and$profile.MinimumIndex-eq0-and$profile.MaximumIndex-eq4) 'Production analyzer statistics failed'
 $backward=Analyze $image 4.5 4.5 0.5 0.5 5
 Assert($backward.Values[0]-eq49.5-and$backward.Values[4]-eq5.5) 'Production analyzer direction failed'
 $boundary=Analyze $image 0 0 9 9 2;Assert($boundary.Values[1]-eq99) 'Exact image boundary rejected'
 foreach($case in @(@(0,0,4.5,2,5),@(0,0,8,8,5))){$rejected=$false;try{Analyze $reduced $case[0] $case[1] $case[2] $case[3] $case[4]|Out-Null}catch{$rejected=$true};Assert($rejected) 'Production analyzer ignored domain/support pixels'}
 foreach($case in @(@(0,0,0,0,5),@(-1,0,2,2,5),@(0,0,2,2,1),@(0,0,2,2,8193),@([double]::NaN,0,2,2,5))){$rejected=$false;try{Analyze $image $case[0] $case[1] $case[2] $case[3] $case[4]|Out-Null}catch{$rejected=$true};Assert($rejected) 'Production analyzer accepted invalid geometry/count'}
 Assert($image.IsInitialized()) 'Analyzer disposed caller-owned image'
 $fr=[HalconDotNet.HTuple]::new([double[]]@(1,2,3));$fc=[HalconDotNet.HTuple]::new([double[]]@(2,3,4));$rr=[HalconDotNet.HTuple]::new([double[]]@(3,2,1));$rc=[HalconDotNet.HTuple]::new([double[]]@(4,3,2))
 $forward=$null;$reverse=$null
 try{$forward=$image.GetGrayvalInterpolated($fr,$fc,'bilinear');$reverse=$image.GetGrayvalInterpolated($rr,$rc,'bilinear');for($i=0;$i-lt3;$i++){Assert($forward[$i].D-eq$reverse[2-$i].D) 'Reversed line changed gray values'}}finally{foreach($tuple in @($forward,$reverse,$fr,$fc,$rr,$rc)){if($null-ne$tuple){$tuple.Dispose()}}}
 Write-Output 'S92_HALCON_CONTRACT=PASS; Bilinear=0/5.5/45/99; ProductionMean=27.5; DomainIgnoredByOperator=True; ExplicitNeighborGuard=True; Reverse=True; RealPrecision=True; InvalidGuard=True; SourceOwnership=True'
}finally{
 foreach($resource in @($samples,$sampleRows,$sampleColumns,$reduced,$domain,$values,$columns,$rows,$image)){if($null-ne$resource){$resource.Dispose()}}
}
