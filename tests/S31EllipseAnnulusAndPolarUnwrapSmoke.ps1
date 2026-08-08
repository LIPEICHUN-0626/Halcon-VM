param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}
function Pump-Ui([int]$Milliseconds) {
    $frame = New-Object Windows.Threading.DispatcherFrame
    $timer = New-Object Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds)
    $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false })
    $timer.Start()
    [Windows.Threading.Dispatcher]::PushFrame($frame)
}
function Write-RowGradientPgm([string]$Path, [int]$Width, [int]$Height) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) {
        $value = [byte][Math]::Min(255, $row)
        for ($column = 0; $column -lt $Width; $column++) { $pixels[$row * $Width + $column] = $value }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}
function Write-ColorPpm([string]$Path, [int]$Width, [int]$Height) {
    $header = [Text.Encoding]::ASCII.GetBytes("P6`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height * 3)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) {
            $offset = ($row * $Width + $column) * 3
            $pixels[$offset] = [byte][Math]::Min(255, $row)
            $pixels[$offset + 1] = [byte][Math]::Min(255, $column)
            $pixels[$offset + 2] = [byte]90
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s31-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$grayPath = Join-Path $tempRoot 'row-gradient.pgm'
$colorPath = Join-Path $tempRoot 'color.ppm'
Write-RowGradientPgm $grayPath 320 280
Write-ColorPpm $colorPath 320 280

$roiType = $assembly.GetType('HalconWinFormsDemo.Models.RoiData', $true)
$roiShapeType = $assembly.GetType('HalconWinFormsDemo.Models.RoiShapeType', $true)
$visionToolType = $assembly.GetType('HalconWinFormsDemo.Services.VisionTool', $true)
$handleType = $assembly.GetType('HalconWinFormsDemo.Services.RoiEditHandle', $true)
$editorType = $assembly.GetType('HalconWinFormsDemo.Services.RoiGeometryEditor', $true)
$roiEditorType = $assembly.GetType('HalconWinFormsDemo.Services.RoiEditor', $true)
$staticPublic = [Reflection.BindingFlags]'Static,Public'
$ellipse = $roiType.GetMethod('CreateEllipse', $staticPublic).Invoke($null, [object[]]@([double]60,[double]60,[double]0,[double]30,[double]15))
$annulus = $roiType.GetMethod('CreateAnnulus', $staticPublic).Invoke($null, [object[]]@([double]60,[double]60,[double]15,[double]30))
try {
    Assert-True ($ellipse.ShapeType.ToString() -eq 'Ellipse') 'Ellipse ROI type is missing.'
    Assert-True ($annulus.ShapeType.ToString() -eq 'Annulus') 'Annulus ROI type is missing.'
    Assert-True ([double]$ellipse.Region.Area.D -gt 1000) 'Ellipse HALCON Region is empty.'
    Assert-True ([double]$annulus.Region.Area.D -gt 0) 'Annulus HALCON Region is empty.'
    $outer = $roiType.GetMethod('CreateCircle', $staticPublic).Invoke($null, [object[]]@([double]60,[double]60,[double]30))
    $inner = $roiType.GetMethod('CreateCircle', $staticPublic).Invoke($null, [object[]]@([double]60,[double]60,[double]15))
    try { Assert-True ([Math]::Abs([double]$annulus.Region.Area.D - ([double]$outer.Region.Area.D - [double]$inner.Region.Area.D)) -lt 1) 'Annulus Region is not outer minus inner.' }
    finally { $outer.Dispose(); $inner.Dispose() }

    $hitTest = $editorType.GetMethod('HitTest', $staticPublic)
    Assert-True ($hitTest.Invoke($null, [object[]]@($ellipse,[Drawing.PointF]::new(90,60),[double]4)).ToString() -eq 'Length1End') 'Ellipse major-axis handle was not hit.'
    Assert-True ($hitTest.Invoke($null, [object[]]@($ellipse,[Drawing.PointF]::new(60,75),[double]4)).ToString() -eq 'Length2End') 'Ellipse minor-axis handle was not hit.'
    Assert-True ($hitTest.Invoke($null, [object[]]@($annulus,[Drawing.PointF]::new(75,60),[double]4)).ToString() -eq 'InnerRadius') 'Annulus inner-radius handle was not hit.'
    Assert-True ($hitTest.Invoke($null, [object[]]@($annulus,[Drawing.PointF]::new(90,60),[double]4)).ToString() -eq 'OuterRadius') 'Annulus outer-radius handle was not hit.'

    $transform = $editorType.GetMethod('TransformDetailed', $staticPublic)
    $resizedEllipse = $transform.Invoke($null, [object[]]@($ellipse,[Enum]::Parse($handleType,'Length2End'),-1,[Drawing.PointF]::new(60,75),[Drawing.PointF]::new(60,85),320,280))
    try { Assert-True ([Math]::Abs($resizedEllipse.Length2 - 25) -lt .01) 'Ellipse minor-axis drag did not resize.' }
    finally { $resizedEllipse.Dispose() }
    $rotatedEllipse = $transform.Invoke($null, [object[]]@($ellipse,[Enum]::Parse($handleType,'Rotate'),-1,[Drawing.PointF]::new(106,60),[Drawing.PointF]::new(60,20),320,280))
    try { Assert-True ([Math]::Abs([Math]::Abs($rotatedEllipse.Phi) - [Math]::PI / 2) -lt .01) 'Ellipse rotate handle did not rotate.' }
    finally { $rotatedEllipse.Dispose() }
    $innerEdited = $transform.Invoke($null, [object[]]@($annulus,[Enum]::Parse($handleType,'InnerRadius'),-1,[Drawing.PointF]::new(75,60),[Drawing.PointF]::new(95,60),320,280))
    try { Assert-True ($innerEdited.InnerRadius -le $innerEdited.OuterRadius - 1) 'Annulus inner radius crossed the outer radius.' }
    finally { $innerEdited.Dispose() }
    $outerEdited = $transform.Invoke($null, [object[]]@($annulus,[Enum]::Parse($handleType,'OuterRadius'),-1,[Drawing.PointF]::new(90,60),[Drawing.PointF]::new(400,60),320,280))
    try { Assert-True ($outerEdited.OuterRadius -le 60.01) 'Annulus outer radius escaped the image boundary.' }
    finally { $outerEdited.Dispose() }

    $roiEditor = [Activator]::CreateInstance($roiEditorType)
    try {
        $roiEditor.Tool = [Enum]::Parse($visionToolType,'EllipseRoi')
        Assert-True $roiEditor.Begin([Drawing.PointF]::new(100,100)) 'Ellipse drawing did not begin.'
        $roiEditor.Update([Drawing.PointF]::new(160,100))
        $drawnEllipse = $roiEditor.Complete([Drawing.PointF]::new(160,100))
        try { Assert-True ($drawnEllipse.ShapeType.ToString() -eq 'Ellipse' -and $drawnEllipse.Length1 -gt $drawnEllipse.Length2) 'Ellipse drawing did not produce editable geometry.' }
        finally { $drawnEllipse.Dispose() }
        $roiEditor.Tool = [Enum]::Parse($visionToolType,'AnnulusRoi')
        Assert-True $roiEditor.Begin([Drawing.PointF]::new(120,120)) 'Annulus drawing did not begin.'
        $roiEditor.Update([Drawing.PointF]::new(190,120))
        $drawnAnnulus = $roiEditor.Complete([Drawing.PointF]::new(190,120))
        try { Assert-True ($drawnAnnulus.ShapeType.ToString() -eq 'Annulus' -and $drawnAnnulus.OuterRadius -gt $drawnAnnulus.InnerRadius) 'Annulus drawing did not produce valid inner/outer radii.' }
        finally { $drawnAnnulus.Dispose() }
    }
    finally { $roiEditor.Dispose() }
}
finally { $ellipse.Dispose(); $annulus.Dispose() }

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$visionRecipeType = $assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$service = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$window = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType,$loggerType), $null).Invoke([object[]]@($service,$logger))
$flow = $mainType.GetField('flowTools',$private).GetValue($window)
$catalog = $mainType.GetField('toolCatalog',$private).GetValue($window)
$create = $mainType.GetMethod('CreateFlowTool',$private)
$execute = $mainType.GetMethod('ExecuteFlowTool',$private)
$runPolar = $mainType.GetMethod('RunPolarImageUnwrapTool',$private)
$loadDraft = $mainType.GetMethod('LoadDockConfigurationDraft',$private)
$captureFlow = $mainType.GetMethod('CaptureFlowRecipe',$private)
$toRecipeRoi = $mainType.GetMethod('ToRecipeRoi',[Reflection.BindingFlags]'Static,NonPublic')
$fromRecipeRoi = $mainType.GetMethod('FromRecipeRoi',[Reflection.BindingFlags]'Static,NonPublic')
$disposeFlow = $mainType.GetMethod('DisposeFlowTools',$private)

function New-Tool([string]$Kind,[string]$Name) { $create.Invoke($window,[object[]]@([Enum]::Parse($toolKindType,$Kind),$Name,$true,$null)) }
function Invoke-Execute($Tool,[string]$Source) {
    try { $execute.Invoke($window,[object[]]@($Tool,$Source)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Invoke-Polar($Tool,$Image) {
    try { $runPolar.Invoke($window,[object[]]@($Tool,$Image)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Get-Out($Tool,[string]$Port) {
    $value = $null
    if (-not $Tool.TryGetOutputValue($Port,[ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }
    $value
}
function Set-Polar($Tool,[double]$Start = 0,[double]$End = 360,[int]$Width = 72,[int]$Height = 64) {
    $p = $Tool.Parameters
    $p.PolarCenterRow = 140
    $p.PolarCenterColumn = 160
    $p.PolarStartAngleDegrees = $Start
    $p.PolarEndAngleDegrees = $End
    $p.PolarInnerRadius = 30
    $p.PolarOuterRadius = 80
    $p.PolarOutputWidth = $Width
    $p.PolarOutputHeight = $Height
    $p.PolarInterpolation = 'nearest_neighbor'
}

foreach ($fieldName in @('EllipseRoiButton','AnnulusRoiButton','DockPolarImageUnwrapPanel','DockPolarCenterRowTextBox','DockPolarCenterColumnTextBox','DockPolarStartAngleTextBox','DockPolarEndAngleTextBox','DockPolarInnerRadiusTextBox','DockPolarOuterRadiusTextBox','DockPolarOutputWidthTextBox','DockPolarOutputHeightTextBox','DockPolarInterpolationComboBox')) {
    Assert-True ($null -ne $mainType.GetField($fieldName,$all).GetValue($window)) "$fieldName is missing."
}
Assert-True ($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'PolarImageUnwrap' } | Select-Object -First 1)) 'Polar unwrap tool is missing from the toolbox.'
$metadata = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata',$true)
$polarKind = [Enum]::Parse($toolKindType,'PolarImageUnwrap')
$inputs = $metadata.GetMethod('GetInputPorts',$staticPublic).Invoke($null,[object[]]@($polarKind))
$outputs = $metadata.GetMethod('GetOutputPorts',$staticPublic).Invoke($null,[object[]]@($polarKind))
Assert-True (($inputs | Where-Object { $_.PortName -eq 'Image' -and $_.DataType -eq 'Image' }).Count -eq 1) 'Polar typed Image input is missing.'
foreach ($port in @('Image','Width','Height','Channels','AngleRange','RadiusRange','Interpolation','Parameters','ResultCode')) { Assert-True (($outputs | Where-Object PortName -eq $port).Count -eq 1) "Polar output $port is missing." }

$app = [Windows.Application]::Current
if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
$disposeFlow.Invoke($window,@()) | Out-Null
$flow.Clear()
$source = New-Tool ImageSource 'S31 Gradient Source'
$polar = New-Tool PolarImageUnwrap 'S31 Polar Unwrap'
$threshold = New-Tool ImageThreshold 'S31 Unwrap Threshold'
$source.Parameters.LocalImagePath = $grayPath
$source.Parameters.LocalImageSerialNumber = 'S31-GRADIENT'
Set-Polar $polar
$polar.SetInputBinding('Image',$source.ToolId,'Image')
$threshold.Parameters.ImageThresholdMinGray = 0
$threshold.Parameters.ImageThresholdMaxGray = 255
$threshold.SetInputBinding('Image',$polar.ToolId,'Image')
foreach ($tool in @($source,$polar,$threshold)) { $flow.Add($tool) }

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.Left = 2; $window.Top = 2; $window.Width = 1550; $window.Height = 970; $window.Topmost = $true
$window.Show(); $window.Activate() | Out-Null; Pump-Ui 600
Invoke-Execute $source 'S31 source' | Out-Null
$fullRecord = Invoke-Execute $polar 'S31 full-circle unwrap'
Assert-True ($fullRecord.ResultCode -eq 'OK') 'Full-circle polar unwrap failed.'
Assert-True ([int][double](Get-Out $polar Width) -eq 72 -and [int][double](Get-Out $polar Height) -eq 64) 'Polar output dimensions are wrong.'
Assert-True ((Get-Out $polar AngleRange) -match '去重首尾列') 'Full-circle output did not disclose first/last-column deduplication.'
$snapshot = Get-Out $polar Image
$unwrapped = $snapshot.CreateImageCopy()
try {
    $first = [double]$unwrapped.GetGrayval(63,0).D
    $last = [double]$unwrapped.GetGrayval(63,71).D
    Assert-True ([Math]::Abs($first - 140) -le 2) 'Polar angle 0 mapping is wrong.'
    Assert-True ([Math]::Abs($first - $last) -ge 3) 'Full-circle first and last columns were duplicated.'
}
finally { $unwrapped.Dispose() }

Set-Polar $polar 0 90 19 64
$quarterRecord = Invoke-Execute $polar 'S31 quarter unwrap'
$quarter = (Get-Out $polar Image).CreateImageCopy()
try {
    Assert-True ($quarterRecord.ResultCode -eq 'OK') 'Quarter-arc polar unwrap failed.'
    Assert-True ([Math]::Abs([double]$quarter.GetGrayval(63,0).D - 140) -le 2) 'Quarter start-angle mapping is wrong.'
    Assert-True ([Math]::Abs([double]$quarter.GetGrayval(63,18).D - 60) -le 2) 'Quarter end-angle mapping is wrong.'
}
finally { $quarter.Dispose() }
Set-Polar $polar 90 0 19 64
$reverseRecord = Invoke-Execute $polar 'S31 reverse unwrap'
$reverse = (Get-Out $polar Image).CreateImageCopy()
try {
    Assert-True ($reverseRecord.ResultCode -eq 'OK') 'Reverse polar unwrap failed.'
    Assert-True ([Math]::Abs([double]$reverse.GetGrayval(63,0).D - 60) -le 2 -and [Math]::Abs([double]$reverse.GetGrayval(63,18).D - 140) -le 2) 'Reverse angle direction is wrong.'
}
finally { $reverse.Dispose() }

Set-Polar $polar 0 360 72 64
$oldSnapshot = Get-Out $polar Image
Invoke-Execute $polar 'S31 snapshot replacement' | Out-Null
Assert-True $oldSnapshot.IsDisposed 'Polar rerun did not release the previous managed Image snapshot.'
Invoke-Execute $threshold 'S31 downstream threshold' | Out-Null
Assert-True ($threshold.ResultCode -eq 'OK' -and $null -ne (Get-Out $threshold Region)) 'Polar Image did not drive downstream threshold processing.'

$colorTool = New-Tool PolarImageUnwrap 'S31 RGB Polar'
Set-Polar $colorTool 0 180 37 64
$colorImage = [HalconDotNet.HImage]::new($colorPath)
try {
    $colorRecord = Invoke-Polar $colorTool $colorImage
    Assert-True ($colorRecord.ResultCode -eq 'OK' -and [int][double](Get-Out $colorTool Channels) -eq 3) 'RGB polar unwrap did not preserve channels.'
}
finally { $colorImage.Dispose(); $colorTool.Dispose() }

$invalid = New-Tool PolarImageUnwrap 'S31 Invalid Polar'
Set-Polar $invalid 0 0 72 64
Assert-True (-not [string]::IsNullOrWhiteSpace($invalid.Parameters.Validate($polarKind))) 'Zero polar angle span was accepted.'
Set-Polar $invalid 0 360 72 64
$invalid.Parameters.PolarOuterRadius = 500
$invalidImage = [HalconDotNet.HImage]::new($grayPath)
try {
    $rejected = $false
    try { Invoke-Polar $invalid $invalidImage | Out-Null } catch { $rejected = ($_.Exception.Message -match '安全边界') }
    Assert-True $rejected 'Out-of-bounds polar sampling was not rejected with a safe-boundary error.'
}
finally { $invalidImage.Dispose(); $invalid.Dispose() }

$ellipseForRecipe = $roiType.GetMethod('CreateEllipse',$staticPublic).Invoke($null,[object[]]@([double]90,[double]110,[double].3,[double]45,[double]22))
$annulusForRecipe = $roiType.GetMethod('CreateAnnulus',$staticPublic).Invoke($null,[object[]]@([double]120,[double]150,[double]25,[double]55))
try {
    $ellipseData = $toRecipeRoi.Invoke($null,[object[]]@($ellipseForRecipe))
    $annulusData = $toRecipeRoi.Invoke($null,[object[]]@($annulusForRecipe))
    $ellipseRoundTrip = $fromRecipeRoi.Invoke($null,[object[]]@($ellipseData))
    $annulusRoundTrip = $fromRecipeRoi.Invoke($null,[object[]]@($annulusData))
    try {
        Assert-True ($ellipseRoundTrip.ShapeType.ToString() -eq 'Ellipse' -and [Math]::Abs($ellipseRoundTrip.Length2 - 22) -lt .001) 'Ellipse recipe round-trip failed.'
        Assert-True ($annulusRoundTrip.ShapeType.ToString() -eq 'Annulus' -and [Math]::Abs($annulusRoundTrip.InnerRadius - 25) -lt .001 -and [Math]::Abs($annulusRoundTrip.OuterRadius - 55) -lt .001) 'Annulus recipe round-trip failed.'
    }
    finally { $ellipseRoundTrip.Dispose(); $annulusRoundTrip.Dispose() }
}
finally { $ellipseForRecipe.Dispose(); $annulusForRecipe.Dispose() }

$recipe = [Activator]::CreateInstance($visionRecipeType)
$recipe.ToolFlow = $captureFlow.Invoke($window,@())
$recipePath = Join-Path $tempRoot 's31-recipe.json'
$service.SaveRecipe($recipePath,$recipe)
$loaded = $service.LoadRecipe($recipePath)
$loadedPolar = $loaded.ToolFlow | Where-Object ToolType -eq 'PolarImageUnwrap' | Select-Object -First 1
Assert-True ($null -ne $loadedPolar -and $loadedPolar.Parameters.PolarOutputWidth -eq 72 -and [Math]::Abs($loadedPolar.Parameters.PolarOuterRadius - 80) -lt .001) 'Polar parameters did not survive recipe save/load.'

$list = $mainType.GetField('FlowToolList',$all).GetValue($window)
$list.SelectedItem = $polar
$loadDraft.Invoke($window,[object[]]@($polar)) | Out-Null
Assert-True ($mainType.GetField('DockPolarImageUnwrapPanel',$all).GetValue($window).Visibility.ToString() -eq 'Visible') 'Polar dock configuration is not visible.'
$mainType.GetField('DockPolarOutputWidthTextBox',$all).GetValue($window).Text = '96'
$applyDock = $mainType.GetMethod('ApplyDockConfigurationDraft',$private)
Assert-True $applyDock.Invoke($window,[object[]]@($true)) 'Polar dock draft did not apply.'
Assert-True ($polar.Parameters.PolarOutputWidth -eq 96) 'Polar dock output width did not persist.'

$sourceSnapshot = Get-Out $source Image
$sourceSnapshot.Dispose()
$releasedRejected = $false
try { Invoke-Execute $polar 'S31 disposed upstream image' | Out-Null } catch { $releasedRejected = $true }
Assert-True $releasedRejected 'Disposed upstream Image was not rejected.'

$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true)
$window.Close()
$app.Shutdown()
if (Test-Path $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
Write-Output 'S31 ellipse/annulus ROI and HALCON polar image unwrap smoke tests passed.'
