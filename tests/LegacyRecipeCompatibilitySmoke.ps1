param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$FixturePath = (Join-Path $PSScriptRoot 'fixtures\legacy-recipe-v1.json')
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) {
    throw 'Run this smoke test with powershell.exe -STA.'
}

$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-legacy-recipe-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$constructorTypes = [Type[]]@([string])
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor($constructorTypes).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor($constructorTypes).Invoke($constructorArgs)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$windowConstructor = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null)
$window = $windowConstructor.Invoke(@($recipeService, $logger))
$applyRecipe = $mainType.GetMethod('ApplyRecipe', $private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
$flow = $mainType.GetField('flowTools', $private).GetValue($window)

$recipe = $recipeService.LoadRecipe((Resolve-Path -LiteralPath $FixturePath).Path)
$applyRecipe.Invoke($window, @($recipe)) | Out-Null
$blob = $flow | Where-Object { $_.Kind.ToString() -eq 'Blob' } | Select-Object -First 1
Assert-True ($null -ne $blob) 'Legacy recipe did not migrate the enabled Blob tool.'
Assert-True ($blob.Parameters.BlobMinGray -eq 90) 'Legacy BlobMinGray changed.'
Assert-True ($blob.Parameters.BlobMaxGray -eq 245) 'Legacy BlobMaxGray changed.'
Assert-True ($blob.Parameters.BlobMinArea -eq 120) 'Legacy BlobMinArea changed.'
$captured = $captureRecipe.Invoke($window, @())
Assert-True ($captured.SchemaVersion -eq 2) 'Legacy recipe was not normalized to schema 2.'
Assert-True (($captured.ToolFlow | Where-Object ToolType -eq 'Blob').Parameters.BlobMinGray -eq 90) 'Migrated Blob parameters did not survive capture.'

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
Write-Output 'LEGACY_RECIPE=PASS; Schema=2; Blob=90/245/120'
