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

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s11-smoke-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'filter-source.png'
$bitmap = New-Object Drawing.Bitmap 81, 61
try {
    for ($row = 0; $row -lt $bitmap.Height; $row++) {
        for ($column = 0; $column -lt $bitmap.Width; $column++) {
            $color = if ($column -ge 20 -and $column -le 60 -and $row -ge 15 -and $row -le 45) {
                [Drawing.Color]::FromArgb(180, 100, 40)
            }
            else {
                [Drawing.Color]::FromArgb(20, 20, 20)
            }
            if (($column -eq 10 -and $row -eq 10) -or ($column -eq 70 -and $row -eq 50)) {
                $color = [Drawing.Color]::FromArgb(255, 255, 255)
            }
            $bitmap.SetPixel($column, $row, $color)
        }
    }
    $bitmap.Save($imagePath, [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $bitmap.Dispose()
}

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$filterModeType = $assembly.GetType('HalconWinFormsDemo.Models.VmImageFilterMode', $true)
$channelModeType = $assembly.GetType('HalconWinFormsDemo.Models.VmImageChannelMode', $true)
$roiType = $assembly.GetType('HalconWinFormsDemo.Models.RoiData', $true)
$roiEditorType = $assembly.GetType('HalconWinFormsDemo.Services.RoiGeometryEditor', $true)
$roiHandleType = $assembly.GetType('HalconWinFormsDemo.Services.RoiEditHandle', $true)
$imageServiceType = $assembly.GetType('HalconWinFormsDemo.Services.HalconImageService', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$allFields = [Reflection.BindingFlags]'Instance,NonPublic,Public'

$constructorTypes = [Type[]]@([string])
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor($constructorTypes).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor($constructorTypes).Invoke($constructorArgs)
$windowConstructor = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null)
$window = $windowConstructor.Invoke([object[]]@($recipeService, $logger))
$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$roiLayers = $mainType.GetField('roiLayers', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)
$applyDock = $mainType.GetMethod('ApplyDockConfigurationDraft', $private)
$trialDock = $mainType.GetMethod('DockTrialRunButton_Click', $private)
$revertDock = $mainType.GetMethod('DockRevertButton_Click', $private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
$addRoiLayer = $mainType.GetMethod('AddRoiLayer', $private)
$copyRoi = $mainType.GetMethod('CopySelectedRoiButton_Click', $private)
$toggleLock = $mainType.GetMethod('ToggleSelectedRoiLockButton_Click', $private)
$deleteRoi = $mainType.GetMethod('ClearRoiButton_Click', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$roiLayerList = $mainType.GetField('RoiLayerList', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockFilterPanel = $mainType.GetField('DockImageFilterPanel', $allFields).GetValue($window)
$dockMode = $mainType.GetField('DockImageFilterModeComboBox', $allFields).GetValue($window)
$dockWidth = $mainType.GetField('DockImageFilterWidthTextBox', $allFields).GetValue($window)
$dockHeight = $mainType.GetField('DockImageFilterHeightTextBox', $allFields).GetValue($window)
$dockRadius = $mainType.GetField('DockImageFilterRadiusTextBox', $allFields).GetValue($window)
$dockValidation = $mainType.GetField('DockValidationText', $allFields).GetValue($window)
$dockInputRows = $mainType.GetField('dockInputPortRows', $private).GetValue($window)
$dockDirty = $mainType.GetField('dockDraftDirty', $private)

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()

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

$meanMode = [string]$filterModeType.GetField('Mean').GetValue($null)
$medianMode = [string]$filterModeType.GetField('Median').GetValue($null)
$grayMode = [string]$channelModeType.GetField('ToGray').GetValue($null)

# Direct HALCON evidence for normal, noisy and color paths.
$imageService = [Activator]::CreateInstance($imageServiceType)
$single = New-Object HalconDotNet.HImage
$single.GenImageConst('byte', 9, 9)
$single.SetGrayval(4, 4, 255)
$meanSingle = $imageService.MeanFilter($single, 3, 3)
$medianSingle = $imageService.MedianFilter($single, 1)
Assert-True ($meanSingle.GetGrayval(4, 4).D -eq 28) 'HALCON 3x3 mean result changed for an isolated impulse.'
Assert-True ($medianSingle.GetGrayval(4, 4).D -eq 0) 'HALCON median radius 1 must remove an isolated impulse.'
$color = $single.Compose3($single, $single)
$colorMean = $imageService.MeanFilter($color, 3, 3)
Assert-True ($imageService.GetChannelCount($colorMean) -eq 3) 'Mean filter must preserve three color channels.'
$colorMean.Dispose(); $color.Dispose(); $medianSingle.Dispose(); $meanSingle.Dispose(); $single.Dispose()

$source = New-Tool 'ImageSource' 'S11_Source'
$filter = New-Tool 'ImageFilter' 'Noise_Filter'
$channel = New-Tool 'ImageChannel' 'Gray_Channel'
$blob = New-Tool 'Blob' 'Blob_AfterFilter'
$gray = New-Tool 'GrayStat' 'Gray_AfterFilter'
$edge = New-Tool 'EdgeMeasure' 'Edge_AfterFilter'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S11-FILTER-001'
$filter.Parameters.ImageFilterMode = $meanMode
$filter.Parameters.ImageFilterMaskWidth = 3
$filter.Parameters.ImageFilterMaskHeight = 3
$filter.Parameters.ImageFilterRadius = 1
$channel.Parameters.ImageChannelMode = $grayMode
$blob.Parameters.BlobMinGray = 70
$blob.Parameters.BlobMaxGray = 255
$blob.Parameters.BlobMinArea = 5
$edge.Parameters.EdgeThreshold = 5
$flow.Add($source); $flow.Add($filter); $flow.Add($channel); $flow.Add($blob); $flow.Add($gray); $flow.Add($edge)

$flowToolList.SelectedItem = $filter
$openConfiguration.Invoke($window, @()) | Out-Null
Assert-True ([object]::ReferenceEquals($rightTabs.SelectedItem, $dockTab)) 'ImageFilter must open the docked configuration workbench.'
Assert-True ($dockFilterPanel.Visibility.ToString() -eq 'Visible') 'ImageFilter parameter panel is not visible.'
Assert-True ($dockInputRows.Count -eq 1) 'ImageFilter must expose one Image subscription row.'
$sourceOption = $dockInputRows[0].SourceOptions | Where-Object { $_.SourceToolId -eq $source.ToolId -and $_.SourcePortName -eq 'Image' } | Select-Object -First 1
Assert-True ($null -ne $sourceOption) 'ImageFilter input editor did not list the previous Image source.'
$dockInputRows[0].SelectedSourceKey = $sourceOption.Key
$dockMode.SelectedValue = $meanMode
$dockWidth.Text = '5'
Assert-True ([bool]$dockDirty.GetValue($window)) 'Editing filter fields must mark the dock draft dirty.'
$dockWidth.Text = '3'; $dockHeight.Text = '3'; $dockRadius.Text = '1'
Assert-True ([bool]$applyDock.Invoke($window, @($false))) 'Applying the ImageFilter dock draft failed.'
Assert-True ($filter.GetInputBinding('Image').SourceToolId -eq $source.ToolId) 'ImageFilter Image binding did not apply.'

$dockWidth.Text = '4'
Assert-True (-not [bool]$applyDock.Invoke($window, @($false))) 'An even mean kernel width must be rejected.'
Assert-True ($dockValidation.Text -match '3.*255') 'Invalid mean kernel did not produce an actionable error.'
$revertDock.Invoke($window, @($null, $null)) | Out-Null

Invoke-Tool $source 'S11 source' | Out-Null
$sourceSnapshot = Get-Output $source 'Image'
Invoke-Tool $filter 'S11 mean filter' | Out-Null
$meanSnapshot = Get-Output $filter 'Image'
Assert-True ([double](Get-Output $filter 'Channels') -eq 3) 'Filter output must preserve RGB channels.'
Assert-True ($meanSnapshot.GetPixelDisplay(10, 10) -ne $sourceSnapshot.GetPixelDisplay(10, 10)) 'Mean filter did not change the isolated RGB impulse.'

$channel.SetInputBinding('Image', $filter.ToolId, 'Image')
$blob.SetInputBinding('Image', $channel.ToolId, 'Image')
$gray.SetInputBinding('Image', $channel.ToolId, 'Image')
$edge.SetInputBinding('Image', $channel.ToolId, 'Image')
Invoke-Tool $channel 'S11 gray after filter' | Out-Null
Invoke-Tool $blob 'S11 Blob after filter' | Out-Null
Invoke-Tool $gray 'S11 Gray after filter' | Out-Null
Invoke-Tool $edge 'S11 Edge after filter' | Out-Null
Assert-True ($blob.ResultCode -eq 'OK' -and $gray.ResultCode -eq 'OK' -and $edge.ResultCode -eq 'OK') 'ImageFilter.Image did not drive Blob/Gray/Edge through ImageChannel.'
$blobArea = [double](Get-Output $blob 'Area')
$grayMean = [double](Get-Output $gray 'Mean')
$edgeLength = [double](Get-Output $edge 'Length')

$flowToolList.SelectedItem = $filter
$dockMode.SelectedValue = $medianMode
$dockRadius.Text = '1'
$trialDock.Invoke($window, @($null, $null)) | Out-Null
Assert-True ($filter.Parameters.ImageFilterMode -eq $meanMode) 'Trial run must restore the applied mean configuration.'
$trialSnapshot = Get-Output $filter 'Image'
Assert-True ($trialSnapshot.GetPixelDisplay(10, 10) -eq '20/20/20') 'Median trial did not remove the isolated RGB impulse.'
$revertDock.Invoke($window, @($null, $null)) | Out-Null

$dockMode.SelectedValue = $medianMode
$dockRadius.Text = '-1'
Assert-True (-not [bool]$applyDock.Invoke($window, @($false))) 'A negative median radius must be rejected.'
Assert-True ($dockValidation.Text.Contains('1') -and $dockValidation.Text.Contains('100')) 'Invalid median radius error is not actionable.'
$revertDock.Invoke($window, @($null, $null)) | Out-Null

# ROI geometry editing, copy/binding and lock protection.
$rect = $roiType.GetMethod('CreateRectangle').Invoke($null, [object[]]@([double]10, [double]20, [double]40, [double]60))
$circle = $roiType.GetMethod('CreateCircle').Invoke($null, [object[]]@([double]30, [double]65, [double]8))
$rectLayer = $addRoiLayer.Invoke($window, [object[]]@($rect, 'Editable_Rect', $blob))
$circleLayer = $addRoiLayer.Invoke($window, [object[]]@($circle, 'Editable_Circle', $gray))
$rect.Dispose(); $circle.Dispose()

$pointTopLeft = New-Object Drawing.PointF -ArgumentList 20, 10
$pointInside = New-Object Drawing.PointF -ArgumentList 30, 20
$pointMoved = New-Object Drawing.PointF -ArgumentList 40, 32
$hit = $roiEditorType.GetMethod('HitTest').Invoke($null, [object[]]@($rectLayer.Geometry, $pointTopLeft, [double]3))
Assert-True ($hit.ToString() -eq 'TopLeft') 'Rectangle control-point hit testing failed.'
$moveHandle = [Enum]::Parse($roiHandleType, 'Move')
$moved = $roiEditorType.GetMethod('Transform').Invoke($null, [object[]]@($rectLayer.Geometry, $moveHandle, $pointInside, $pointMoved, [int]81, [int]61))
Assert-True ($moved.Row1 -eq 22 -and $moved.Column1 -eq 30) 'Rectangle move transform returned wrong coordinates.'
$moved.Dispose()
$topLeftHandle = [Enum]::Parse($roiHandleType, 'TopLeft')
$collapsedPoint = New-Object Drawing.PointF -ArgumentList 60,40
$collapsed = $roiEditorType.GetMethod('Transform').Invoke($null, [object[]]@($rectLayer.Geometry, $topLeftHandle, $pointTopLeft, $collapsedPoint, [int]81, [int]61))
Assert-True (($collapsed.Row2 - $collapsed.Row1) -ge 2 -and ($collapsed.Column2 - $collapsed.Column1) -ge 2) 'Rectangle resize must preserve a usable minimum span.'
Assert-True ($collapsed.Row1 -ge 0 -and $collapsed.Row2 -le 60 -and $collapsed.Column1 -ge 0 -and $collapsed.Column2 -le 80) 'Rectangle resize escaped the image bounds.'
$collapsed.Dispose()
$radiusPoint = New-Object Drawing.PointF -ArgumentList 73, 30
$circleHit = $roiEditorType.GetMethod('HitTest').Invoke($null, [object[]]@($circleLayer.Geometry, $radiusPoint, [double]3))
Assert-True ($circleHit.ToString() -eq 'Radius') 'Circle radius control-point hit testing failed.'

$roiLayerList.SelectedItem = $rectLayer
$beforeCopyCount = $roiLayers.Count
$copyRoi.Invoke($window, @($null, $null)) | Out-Null
Assert-True ($roiLayers.Count -eq $beforeCopyCount + 1) 'Copy ROI did not create a new layer.'
$copyLayer = $roiLayerList.SelectedItem
Assert-True ($copyLayer.RoiId -ne $rectLayer.RoiId) 'Copied ROI must have an independent stable RoiId.'
Assert-True ($blob.IsRoiBound($copyLayer.RoiId)) 'Copied ROI did not preserve the source tool binding.'
Assert-True (-not $copyLayer.IsLocked) 'Copied ROI must start unlocked for deliberate adjustment.'

$toggleLock.Invoke($window, @($null, $null)) | Out-Null
Assert-True $copyLayer.IsLocked 'ROI lock action did not update the selected layer.'
$lockedCount = $roiLayers.Count
$deleteRoi.Invoke($window, @($null, $null)) | Out-Null
Assert-True ($roiLayers.Count -eq $lockedCount) 'Locked ROI must reject deletion.'
$toggleLock.Invoke($window, @($null, $null)) | Out-Null
$deleteRoi.Invoke($window, @($null, $null)) | Out-Null
Assert-True ($roiLayers.Count -eq $lockedCount - 1) 'Unlocked copied ROI should be deletable.'
$roiLayerList.SelectedItem = $rectLayer
$toggleLock.Invoke($window, @($null, $null)) | Out-Null
Assert-True $rectLayer.IsLocked 'ROI lock entry did not update the selected layer.'

$recipe = $captureRecipe.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's11-roundtrip.json'
$recipeService.SaveRecipe($recipePath, $recipe)
$loaded = $recipeService.LoadRecipe($recipePath)
$filterRecipe = $loaded.ToolFlow | Where-Object ToolId -eq $filter.ToolId
$rectRecipe = $loaded.RoiLayers | Where-Object RoiId -eq $rectLayer.RoiId
Assert-True ($filterRecipe.Parameters.ImageFilterMode -eq $meanMode -and $filterRecipe.Parameters.ImageFilterMaskWidth -eq 3 -and $filterRecipe.Parameters.ImageFilterMaskHeight -eq 3) 'ImageFilter parameters did not round-trip.'
Assert-True (($filterRecipe.InputBindings | Where-Object TargetPortName -eq 'Image').SourceToolId -eq $source.ToolId) 'ImageFilter binding did not round-trip.'
Assert-True $rectRecipe.IsLocked 'ROI lock state did not round-trip.'

$oldFilterSnapshot = Get-Output $filter 'Image'
Invoke-Tool $source 'S11 source rerun' | Out-Null
Assert-True $sourceSnapshot.IsDisposed 'Source rerun must dispose its old Image snapshot.'
Assert-True $oldFilterSnapshot.IsDisposed 'Source rerun must invalidate and dispose ImageFilter output.'
Assert-True ($filter.ResultCode -eq '--' -and $channel.ResultCode -eq '--' -and $blob.ResultCode -eq '--') 'Source rerun must invalidate the complete filtered Image chain.'

Invoke-Tool $filter 'S11 filter after source rerun' | Out-Null
$finalSnapshot = Get-Output $filter 'Image'
$disposeTools.Invoke($window, @()) | Out-Null
Assert-True $finalSnapshot.IsDisposed 'Disposing the flow must dispose ImageFilter snapshots.'
$imageService.Dispose()
$closeConfirmed = $mainType.GetField('recipeCloseConfirmed', $private)
$closeConfirmed.SetValue($window, $true)
$window.Close()

Write-Output ('S11_ROI_FILTER=PASS; MeanImpulse=28; MedianImpulse=0; RGB=3; Blob={0:F3}; Gray={1:F3}; Edge={2:F3}; ROI=HIT/MOVE/COPY/LOCK; Dock=APPLY/TRIAL/REVERT; Recipe=PASS; Lifecycle=PASS' -f $blobArea, $grayMean, $edgeLength)
