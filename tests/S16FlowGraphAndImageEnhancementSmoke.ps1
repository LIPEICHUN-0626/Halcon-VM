param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) {
    throw 'Run this smoke test with powershell.exe -STA.'
}

$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$exePath = Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'
if (-not (Test-Path -LiteralPath $exePath)) { throw "Build Debug x64 first: $exePath" }
$assembly = [Reflection.Assembly]::LoadFrom($exePath)

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

function New-GrayFixture([string]$Path) {
    $bitmap = New-Object Drawing.Bitmap 80, 60
    try {
        for ($row = 0; $row -lt 60; $row++) {
            for ($column = 0; $column -lt 80; $column++) {
                $gray = if ($column -ge 20 -and $column -le 59 -and $row -ge 15 -and $row -le 44) { 110 } else { 20 }
                $bitmap.SetPixel($column, $row, [Drawing.Color]::FromArgb($gray, $gray, $gray))
            }
        }
        $bitmap.Save($Path, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $bitmap.Dispose() }
}

function New-ColorFixture([string]$Path) {
    $bitmap = New-Object Drawing.Bitmap 24, 20
    try {
        for ($row = 0; $row -lt 20; $row++) {
            for ($column = 0; $column -lt 24; $column++) {
                $bitmap.SetPixel($column, $row, [Drawing.Color]::FromArgb(10, 20, 30))
            }
        }
        $bitmap.Save($Path, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $bitmap.Dispose() }
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s16-smoke-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$grayPath = Join-Path $tempRoot 'gray.png'
$colorPath = Join-Path $tempRoot 'color.png'
New-GrayFixture $grayPath
New-ColorFixture $colorPath

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$allFields = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$windowConstructor = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null)
$window = $windowConstructor.Invoke([object[]]@($recipeService, $logger))

$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$catalog = $mainType.GetField('toolCatalog', $private).GetValue($window)
$connections = $mainType.GetField('flowConnections', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$refresh = $mainType.GetMethod('RefreshUiState', $private)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)
$applyDock = $mainType.GetMethod('ApplyDockConfigurationDraft', $private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
$graphView = $mainType.GetMethod('FlowGraphViewButton_Click', $private)
$listView = $mainType.GetMethod('FlowListViewButton_Click', $private)
$autoLayout = $mainType.GetMethod('AutoLayoutFlowButton_Click', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$graphViewport = $mainType.GetField('FlowGraphViewport', $allFields).GetValue($window)
$graphCanvas = $mainType.GetField('FlowGraphCanvas', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockPanel = $mainType.GetField('DockImageEnhancementPanel', $allFields).GetValue($window)
$dockMode = $mainType.GetField('DockImageEnhancementModeComboBox', $allFields).GetValue($window)
$dockScale = $mainType.GetField('DockImageEnhancementScaleTextBox', $allFields).GetValue($window)
$dockOffset = $mainType.GetField('DockImageEnhancementOffsetTextBox', $allFields).GetValue($window)
$dockMaskWidth = $mainType.GetField('DockImageEnhancementMaskWidthTextBox', $allFields).GetValue($window)
$dockFactor = $mainType.GetField('DockImageEnhancementFactorTextBox', $allFields).GetValue($window)
$dockValidation = $mainType.GetField('DockValidationText', $allFields).GetValue($window)

function New-Tool([string]$Kind, [string]$Name) {
    return $createTool.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}

function Invoke-Tool($Tool, [string]$Label) {
    try { return $executeTool.Invoke($window, [object[]]@($Tool, $Label)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

function Get-Output($Tool, [string]$Port) {
    $value = $null
    if (-not $Tool.TryGetOutputValue($Port, [ref]$value)) { throw "$($Tool.InstanceName).$Port has no output." }
    return $value
}

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool 'ImageSource' 'Input_Gray'
$enhance = New-Tool 'ImageEnhancement' 'Enhance_Linear'
$threshold = New-Tool 'ImageThreshold' 'Threshold_After_Enhance'
$blob = New-Tool 'Blob' 'Blob_After_Enhance'
$gray = New-Tool 'GrayStat' 'Gray_After_Enhance'
$edge = New-Tool 'EdgeMeasure' 'Edge_After_Enhance'
$source.Parameters.LocalImagePath = $grayPath
$source.Parameters.LocalImageSerialNumber = 'S16-GRAY'
$enhance.Parameters.ImageEnhancementMode = 'LinearScale'
$enhance.Parameters.ImageEnhancementScale = 2.0
$enhance.Parameters.ImageEnhancementOffset = 10.0
$threshold.Parameters.ImageThresholdMinGray = 200
$threshold.Parameters.ImageThresholdMaxGray = 255
$blob.Parameters.BlobMinGray = 200
$blob.Parameters.BlobMaxGray = 255
$blob.Parameters.BlobMinArea = 10
$gray.Parameters.GrayMin = 0
$gray.Parameters.GrayMax = 255
$edge.Parameters.EdgeThreshold = 5
foreach ($tool in @($source, $enhance, $threshold, $blob, $gray, $edge)) { $flow.Add($tool) }
$enhance.SetInputBinding('Image', $source.ToolId, 'Image')
$threshold.SetInputBinding('Image', $enhance.ToolId, 'Image')
foreach ($consumer in @($blob, $gray, $edge)) {
    $consumer.SetInputBinding('Image', $enhance.ToolId, 'Image')
    $consumer.SetInputBinding('ROI', $threshold.ToolId, 'Region')
}

$enhancementCatalog = $catalog | Where-Object Kind -eq ([Enum]::Parse($toolKindType, 'ImageEnhancement')) | Select-Object -First 1
Assert-True ($null -ne $enhancementCatalog -and -not [string]::IsNullOrWhiteSpace($enhancementCatalog.Name)) 'Image enhancement is missing from the visible toolbox.'

$flowToolList.SelectedItem = $enhance
$refresh.Invoke($window, @()) | Out-Null
Assert-True ($graphViewport.Visibility.ToString() -eq 'Visible' -and $flowToolList.Visibility.ToString() -eq 'Collapsed') 'The connection canvas must be the default flow view.'
Assert-True ($source.GraphX -lt $enhance.GraphX -and $enhance.GraphX -lt $threshold.GraphX -and $threshold.GraphX -lt $blob.GraphX) 'Dependency-layer graph positions are incorrect.'
Assert-True ($connections.Count -eq 8) "Expected 8 real binding paths, got $($connections.Count)."
Assert-True (($connections | Where-Object DataType -eq 'Image').Count -eq 5) 'Image paths are not typed and visible.'
Assert-True (($connections | Where-Object DataType -eq 'Region').Count -eq 3) 'Region convergence paths are not typed and visible.'
Assert-True (($connections | Where-Object StrokeThickness -eq 4).Count -eq 5) 'Selected-module incident paths are not highlighted.'
Assert-True (($connections | Where-Object { $null -eq $_.Geometry }).Count -eq 0) 'A graph path has no geometry.'
Assert-True ($graphCanvas.Width -gt 1000 -and $graphCanvas.Height -ge 700) 'The graph extent was not calculated.'

$listView.Invoke($window, @($null, $null)) | Out-Null
Assert-True ($flowToolList.Visibility.ToString() -eq 'Visible' -and $graphViewport.Visibility.ToString() -eq 'Collapsed') 'Sequence-list switching failed.'
$graphView.Invoke($window, @($null, $null)) | Out-Null
$autoLayout.Invoke($window, @($null, $null)) | Out-Null
Assert-True ($graphViewport.Visibility.ToString() -eq 'Visible') 'Auto layout did not return to the connection canvas.'

$unrunRejected = $false
try { Invoke-Tool $enhance 'S16 unrun upstream' | Out-Null } catch { $unrunRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $unrunRejected 'Image enhancement must reject an unrun explicit upstream Image.'

Invoke-Tool $source 'S16 source' | Out-Null
Invoke-Tool $enhance 'S16 linear gray' | Out-Null
$enhancedImage = Get-Output $enhance 'Image'
$backgroundPixel = $enhancedImage.GetPixelDisplay(0, 0)
$foregroundPixel = $enhancedImage.GetPixelDisplay(25, 30)
Assert-True ($enhance.ResultCode -eq 'OK' -and ($backgroundPixel -eq '50' -or $backgroundPixel -eq '50/50/50')) 'Linear gray scaling did not produce 20*2+10=50.'
Assert-True ($foregroundPixel -eq '230' -or $foregroundPixel -eq '230/230/230') 'Linear gray scaling did not produce 110*2+10=230.'
Assert-True ($enhance.Parameters.ImageEnhancementMode -eq 'LinearScale' -and -not [string]::IsNullOrWhiteSpace([string](Get-Output $enhance 'Mode'))) 'Enhancement mode output is missing.'
Assert-True ([string](Get-Output $enhance 'Parameters') -eq 'Scale=2, Offset=10') 'Enhancement parameter summary is incorrect.'

Invoke-Tool $threshold 'S16 threshold consumer' | Out-Null
Invoke-Tool $blob 'S16 blob consumer' | Out-Null
Invoke-Tool $gray 'S16 gray consumer' | Out-Null
Invoke-Tool $edge 'S16 edge consumer' | Out-Null
Assert-True ($threshold.ResultCode -eq 'OK' -and $blob.ResultCode -eq 'OK' -and $gray.ResultCode -eq 'OK' -and $edge.ResultCode -eq 'OK') 'Threshold/Blob/Gray/Edge did not consume the enhanced Image/Region chain.'
$refresh.Invoke($window, @()) | Out-Null
Assert-True (($enhance.FlowOutputPorts | Where-Object PortName -eq 'Image').EndpointText.Contains('4')) 'The enhanced Image output does not show four real consumers.'

$flowToolList.SelectedItem = $enhance
$openConfiguration.Invoke($window, @()) | Out-Null
Assert-True ([object]::ReferenceEquals($rightTabs.SelectedItem, $dockTab)) 'Image enhancement did not open in the docked configuration workbench.'
Assert-True ($dockPanel.Visibility.ToString() -eq 'Visible') 'Image enhancement parameter panel is not visible.'
$dockMode.SelectedValue = 'LocalContrast'
$dockMaskWidth.Text = '8'
$dockFactor.Text = '1.5'
Assert-True (-not [bool]$applyDock.Invoke($window, @($false))) 'The dock must reject an even local-contrast mask.'
Assert-True (-not [string]::IsNullOrWhiteSpace($dockValidation.Text)) 'Invalid local-contrast settings need an actionable error.'
$dockMaskWidth.Text = '7'
Assert-True ([bool]$applyDock.Invoke($window, @($false))) 'Applying valid local-contrast settings failed.'
Invoke-Tool $enhance 'S16 emphasize gray' | Out-Null
Assert-True ($enhance.ResultCode -eq 'OK' -and $enhance.Parameters.ImageEnhancementMode -eq 'LocalContrast' -and -not [string]::IsNullOrWhiteSpace([string](Get-Output $enhance 'Mode'))) 'HALCON emphasize mode did not run.'

$recipe = $captureRecipe.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's16-roundtrip.json'
$recipeService.SaveRecipe($recipePath, $recipe)
$loaded = $recipeService.LoadRecipe($recipePath)
$enhanceRecipe = $loaded.ToolFlow | Where-Object ToolId -eq $enhance.ToolId
Assert-True ($enhanceRecipe.Parameters.ImageEnhancementMode -eq 'LocalContrast' -and $enhanceRecipe.Parameters.ImageEnhancementMaskWidth -eq 7) 'Image enhancement parameters did not round-trip.'
Assert-True (($enhanceRecipe.InputBindings | Where-Object TargetPortName -eq 'Image').SourceToolId -eq $source.ToolId) 'Image enhancement binding did not round-trip.'

$source.Parameters.LocalImagePath = $colorPath
Invoke-Tool $source 'S16 color source' | Out-Null
$enhance.Parameters.ImageEnhancementMode = 'LinearScale'
$enhance.Parameters.ImageEnhancementScale = 2.0
$enhance.Parameters.ImageEnhancementOffset = 5.0
Invoke-Tool $enhance 'S16 linear RGB' | Out-Null
$colorOutput = Get-Output $enhance 'Image'
$colorPixel = $colorOutput.GetPixelDisplay(0, 0)
Assert-True ([int][double](Get-Output $enhance 'Channels') -eq 3) 'RGB enhancement did not preserve three channels.'
Assert-True ($colorPixel -eq '25/45/65') "RGB linear scaling is incorrect: $colorPixel"

$liveSnapshot = $colorOutput
Invoke-Tool $source 'S16 lifecycle rerun' | Out-Null
Assert-True ($liveSnapshot.IsDisposed -and $enhance.ResultCode -eq '--' -and $threshold.ResultCode -eq '--') 'Upstream rerun must dispose and invalidate the downstream enhanced Image chain.'

$enhance.Parameters.ImageEnhancementScale = 0.001
$invalidRejected = $false
try { Invoke-Tool $enhance 'S16 invalid scale' | Out-Null } catch { $invalidRejected = $_.Exception.Message.Contains('0.01') }
Assert-True ($invalidRejected -and $enhance.ResultCode -eq 'NG') 'Invalid linear scale must fail with an actionable NG state.'

$enhance.Parameters.ImageEnhancementScale = 2.0
$enhance.SetInputBinding('Image', 'missing-tool-id', 'Image')
$refresh.Invoke($window, @()) | Out-Null
$badConnection = $connections | Where-Object TargetToolId -eq $enhance.ToolId | Select-Object -First 1
Assert-True ($null -ne $badConnection -and -not [string]::IsNullOrWhiteSpace($badConnection.StatusText) -and $null -ne $badConnection.StrokeDashArray) 'A deleted/missing source must remain visible as a dashed error path.'

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()

Write-Output 'S16_FLOW_GRAPH_IMAGE_ENHANCEMENT=PASS; Graph=LAYERS/PATHS/TYPES/HIGHLIGHT/VIEW/FIT/INVALID; HALCON=SCALE_IMAGE/EMPHASIZE; Gray=PASS; RGB=PASS; Downstream=THRESHOLD/BLOB/GRAY/EDGE; Dock=PASS; Recipe=PASS; Lifecycle=PASS'
