param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) {
    throw 'Run this smoke test with powershell.exe -STA.'
}

$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$halconBin = Join-Path $halconRoot 'bin\x64-win64'
$halconDotNet = Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll'
$exePath = Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Build Debug x64 first: $exePath"
}

$env:PATH = $halconBin + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
[Reflection.Assembly]::LoadFrom($halconDotNet) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom($exePath)

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s09-smoke'
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$image1Path = Join-Path $tempRoot 'source-a.png'
$image2Path = Join-Path $tempRoot 'source-b.png'

function New-SmokeImage([string]$Path, [int]$Width, [int]$Height, [Drawing.Rectangle]$WhiteArea) {
    $bitmap = New-Object Drawing.Bitmap $Width, $Height
    try {
        $graphics = [Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([Drawing.Color]::Black)
            $graphics.FillRectangle([Drawing.Brushes]::White, $WhiteArea)
        }
        finally {
            $graphics.Dispose()
        }
        $bitmap.Save($Path, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
}

New-SmokeImage $image1Path 100 60 ([Drawing.Rectangle]::new(10, 10, 20, 20))
New-SmokeImage $image2Path 100 60 ([Drawing.Rectangle]::new(10, 10, 40, 20))

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$contextOptionType = $assembly.GetType('HalconWinFormsDemo.Models.VmImageContextOption', $true)
$stringConstructorTypes = [Type[]]@([string])
$constructorArguments = New-Object 'object[]' 1
$constructorArguments[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor($stringConstructorTypes).Invoke($constructorArguments)
$logger = $loggerType.GetConstructor($stringConstructorTypes).Invoke($constructorArguments)
$constructor = $mainType.GetConstructor(
    [Reflection.BindingFlags]'Instance,NonPublic',
    $null,
    [Type[]]@($recipeServiceType, $loggerType),
    $null)
$window = $constructor.Invoke(@($recipeService, $logger))

$privateInstance = [Reflection.BindingFlags]'Instance,NonPublic'
$flowField = $mainType.GetField('flowTools', $privateInstance)
$createTool = $mainType.GetMethod('CreateFlowTool', $privateInstance)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $privateInstance)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $privateInstance)
$bindingErrorMethod = $mainType.GetMethod('GetInputBindingConfigurationError', $privateInstance)
$resolveContext = $mainType.GetMethod('ResolveImageContext', $privateInstance)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $privateInstance)
$flowToolListField = $mainType.GetField('FlowToolList', [Reflection.BindingFlags]'Instance,NonPublic,Public')
$contextComboField = $mainType.GetField('ImageContextComboBox', [Reflection.BindingFlags]'Instance,NonPublic,Public')
$flow = $flowField.GetValue($window)
$flowToolList = $flowToolListField.GetValue($window)
$contextCombo = $contextComboField.GetValue($window)

function New-Tool([string]$Kind, [string]$Name) {
    $enumValue = [Enum]::Parse($toolKindType, $Kind)
    return $createTool.Invoke($window, @($enumValue, $Name, $true, $null))
}

function Invoke-Tool($Tool, [string]$Label) {
    try {
        return $executeTool.Invoke($window, @($Tool, $Label))
    }
    catch [Reflection.TargetInvocationException] {
        throw $_.Exception.InnerException
    }
}

function Get-Output($Tool, [string]$Port) {
    $value = $null
    if (-not $Tool.TryGetOutputValue($Port, [ref]$value)) {
        throw "$($Tool.InstanceName).$Port has no output."
    }
    return $value
}

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) {
        throw "Assertion failed: $Message"
    }
}

$source = New-Tool 'ImageSource' 'LocalImage_A'
$blob = New-Tool 'Blob' 'Blob_Image'
$gray = New-Tool 'GrayStat' 'Gray_Image'
$edge = New-Tool 'EdgeMeasure' 'Edge_Image'
$regionGray = New-Tool 'GrayStat' 'Gray_RegionChain'
$numeric = New-Tool 'NumericJudge' 'Judge_NumberChain'
$source.Parameters.LocalImagePath = $image1Path
$source.Parameters.LocalImageSerialNumber = 'SN-A'
$blob.Parameters.BlobMinGray = 200
$blob.Parameters.BlobMaxGray = 255
$blob.Parameters.BlobMinArea = 10
$edge.Parameters.EdgeThreshold = 10
$blob.SetInputBinding('Image', $source.ToolId, 'Image')
$gray.SetInputBinding('Image', $source.ToolId, 'Image')
$edge.SetInputBinding('Image', $source.ToolId, 'Image')
$regionGray.SetInputBinding('Image', $source.ToolId, 'Image')
$regionGray.SetInputBinding('ROI', $blob.ToolId, 'SelectedRegion')
$numeric.InputToolId = $gray.ToolId
$numeric.InputPortName = 'Mean'
$numeric.NumericLowerLimit = 0
$numeric.NumericUpperLimit = 255
$flow.Add($source)
$flow.Add($blob)
$flow.Add($gray)
$flow.Add($edge)
$flow.Add($regionGray)
$flow.Add($numeric)

Invoke-Tool $source 'S09 smoke A' | Out-Null
$snapshotA = Get-Output $source 'Image'
Invoke-Tool $blob 'S09 smoke A' | Out-Null
Invoke-Tool $gray 'S09 smoke A' | Out-Null
Invoke-Tool $edge 'S09 smoke A' | Out-Null
Invoke-Tool $regionGray 'S09 Region regression A' | Out-Null
Invoke-Tool $numeric 'S09 Number regression A' | Out-Null
$numericA = $numeric.ResultCode
$areaA = [double](Get-Output $blob 'Area')
$meanA = [double](Get-Output $gray 'Mean')
$edgeA = [double](Get-Output $edge 'Length')
$regionMeanA = [double](Get-Output $regionGray 'Mean')
Assert-True ($areaA -eq 400) "Image A Blob area should be 400, actual $areaA"
Assert-True ($regionMeanA -eq 255) "Region chain mean should be 255, actual $regionMeanA"
Assert-True ($numericA -eq 'OK') 'Number chain should remain OK.'
Assert-True ($snapshotA.Width -eq 100 -and $snapshotA.Height -eq 60) 'Image A snapshot dimensions are wrong.'
Assert-True ((Get-Output $source 'SN') -eq 'SN-A') 'Image A SN output is wrong.'

$source.Parameters.LocalImagePath = $image2Path
$source.Parameters.LocalImageSerialNumber = 'SN-B'
Invoke-Tool $source 'S09 smoke B' | Out-Null
Assert-True $snapshotA.IsDisposed 'The previous Image snapshot must be disposed after source rerun.'
Assert-True ($blob.ResultCode -eq '--') 'Downstream result must be invalidated after source rerun.'
$snapshotB = Get-Output $source 'Image'
Invoke-Tool $blob 'S09 smoke B' | Out-Null
Invoke-Tool $gray 'S09 smoke B' | Out-Null
Invoke-Tool $edge 'S09 smoke B' | Out-Null
Invoke-Tool $regionGray 'S09 Region regression B' | Out-Null
Invoke-Tool $numeric 'S09 Number regression B' | Out-Null
$numericB = $numeric.ResultCode
$areaB = [double](Get-Output $blob 'Area')
$meanB = [double](Get-Output $gray 'Mean')
$edgeB = [double](Get-Output $edge 'Length')
Assert-True ($areaB -eq 800) "Image B Blob area should be 800, actual $areaB"
Assert-True ($areaA -ne $areaB -and $meanA -ne $meanB -and $edgeA -ne $edgeB) 'Both images must produce distinct Blob, Gray, and Edge results.'
Assert-True ((Get-Output $source 'Path') -eq $image2Path) 'Image B path output is wrong.'
Assert-True ((Get-Output $source 'SN') -eq 'SN-B') 'Image B SN output is wrong.'
Assert-True ($snapshotB.GetPixelDisplay(15, 15) -like '255*') 'Image snapshot pixel read is wrong.'

$badType = New-Tool 'Blob' 'Blob_TypeError'
$badType.SetInputBinding('Image', $gray.ToolId, 'Mean')
$flow.Add($badType)
$badBinding = $badType.GetInputBinding('Image')
$badTypeError = [string]$bindingErrorMethod.Invoke($window, @($badType, $badBinding))
Assert-True ($badTypeError.Length -gt 0) 'Image <- Number must be rejected.'

$forwardTarget = New-Tool 'Blob' 'Blob_ForwardError'
$forwardSource = New-Tool 'ImageSource' 'LocalImage_AfterTarget'
$forwardSource.Parameters.LocalImagePath = $image1Path
$flow.Add($forwardTarget)
$flow.Add($forwardSource)
$forwardTarget.SetInputBinding('Image', $forwardSource.ToolId, 'Image')
$forwardError = [string]$bindingErrorMethod.Invoke($window, @($forwardTarget, $forwardTarget.GetInputBinding('Image')))
Assert-True ($forwardError.Length -gt 0) 'Forward Image binding must be rejected.'

$waitingSource = New-Tool 'ImageSource' 'LocalImage_NotRun'
$waitingTarget = New-Tool 'GrayStat' 'Gray_Waiting'
$waitingSource.Parameters.LocalImagePath = $image1Path
$flow.Insert(0, $waitingSource)
$flow.Insert(1, $waitingTarget)
$waitingTarget.SetInputBinding('Image', $waitingSource.ToolId, 'Image')
$missingRejected = $false
try {
    Invoke-Tool $waitingTarget 'S09 missing snapshot' | Out-Null
}
catch {
    $missingRejected = $true
}
Assert-True $missingRejected 'A downstream Image subscriber must reject a source that has not run.'

$recipe = $captureRecipe.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's09-roundtrip.json'
$recipeService.SaveRecipe($recipePath, $recipe)
$loadedRecipe = $recipeService.LoadRecipe($recipePath)
$sourceRecipe = $loadedRecipe.ToolFlow | Where-Object ToolId -eq $source.ToolId
$blobRecipe = $loadedRecipe.ToolFlow | Where-Object ToolId -eq $blob.ToolId
$regionGrayRecipe = $loadedRecipe.ToolFlow | Where-Object ToolId -eq $regionGray.ToolId
Assert-True ($sourceRecipe.Parameters.LocalImagePath -eq $image2Path -and $sourceRecipe.Parameters.LocalImageSerialNumber -eq 'SN-B') 'Local image parameters and SN did not round-trip.'
Assert-True (($blobRecipe.InputBindings | Where-Object TargetPortName -eq 'Image').SourceToolId -eq $source.ToolId) 'Image input binding did not round-trip.'
Assert-True (($regionGrayRecipe.InputBindings | Where-Object TargetPortName -eq 'ROI').SourceToolId -eq $blob.ToolId) 'Region input binding regressed during recipe round-trip.'

$flowToolList.SelectedItem = $blob
$contextCombo.SelectedValue = $contextOptionType.GetField('ModuleInput').GetValue($null)
$moduleInputContext = $resolveContext.Invoke($window, @())
Assert-True ($moduleInputContext.HasImage -and $moduleInputContext.SourceText -like '*LocalImage_A.Image*') 'Module input context did not resolve the live snapshot.'
$contextCombo.SelectedValue = $contextOptionType.GetField('ModuleOutput').GetValue($null)
$moduleOutputFallback = $resolveContext.Invoke($window, @())
Assert-True ($moduleOutputFallback.HasImage) 'A visual module without Image output must fall back to its real input image.'
$flowToolList.SelectedItem = $source
$contextCombo.SelectedValue = $contextOptionType.GetField('ModuleOutput').GetValue($null)
$moduleOutputContext = $resolveContext.Invoke($window, @())
Assert-True ($moduleOutputContext.HasImage -and $moduleOutputContext.SourceText -like '*LocalImage_A.Image*') 'Module output context did not resolve the live snapshot.'

$snapshotB.Dispose()
$disposedRejected = $false
try {
    Invoke-Tool $blob 'S09 disposed snapshot' | Out-Null
}
catch {
    $disposedRejected = $true
}
Assert-True $disposedRejected 'A disposed Image snapshot must be rejected.'
Invoke-Tool $source 'S09 restore after disposed snapshot' | Out-Null
$snapshotC = Get-Output $source 'Image'
$disposeTools.Invoke($window, @()) | Out-Null
Assert-True $snapshotC.IsDisposed 'Disposing the flow must dispose Image snapshots.'
$recipeCloseField = $mainType.GetField('recipeCloseConfirmed', $privateInstance)
$recipeCloseField.SetValue($window, $true)
$window.Close()

Write-Output ('S09_IMAGE_CHAIN=PASS; Area={0}->{1}; Mean={2:F3}->{3:F3}; Edge={4:F3}->{5:F3}; RegionMean={6:F3}; Number={7}->{8}; SnapshotLifecycle=PASS; Recipe=PASS; Context=PASS' -f $areaA, $areaB, $meanA, $meanB, $edgeA, $edgeB, $regionMeanA, $numericA, $numericB)
