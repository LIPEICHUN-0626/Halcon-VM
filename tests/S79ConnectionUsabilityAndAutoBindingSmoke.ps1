param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Assert([bool]$ok, [string]$message) { if (-not $ok) { throw "Assertion failed: $message" } }
$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$serviceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$kindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-s79-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp) | Out-Null
$service = $serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$window = $mainType.GetConstructor($private, $null, [Type[]]@($serviceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
function F([string]$name) { $value = $mainType.GetField($name, $all).GetValue($window); Write-Output -NoEnumerate $value }
function M([string]$name, [object[]]$invokeArgs = @()) { try { $method = $mainType.GetMethods($private) | Where-Object Name -eq $name | Where-Object { $_.GetParameters().Count -eq $invokeArgs.Count } | Select-Object -First 1; if ($null -eq $method) { throw "Missing method $name/$($invokeArgs.Count)" }; $method.Invoke($window, $invokeArgs) } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException } }
function Tool([string]$kind, [string]$name) { $mainType.GetMethod('CreateFlowTool', $private).Invoke($window, [object[]]@([Enum]::Parse($kindType, $kind), $name, $true, $null)) }
try {
    $flow = F 'flowTools'; $flow.Clear()
    $sourceA = Tool ImageSource '01 Image A'
    $sourceB = Tool ImageSource '02 Image B'
    $target = Tool ImageThreshold '03 Threshold'
    $flow.Add($sourceA); $flow.Add($sourceB); $flow.Add($target)
    M 'RefreshFlowPortVisualization' | Out-Null
    M 'RefreshFlowGraphLayout' | Out-Null

    $origin = (New-Object Windows.Controls.Border).PSObject.BaseObject
    M 'BeginAggregateFlowPortDragS79' ([object[]]@($sourceA, 'OUT', $origin, [Windows.Point]::new(120, 120))) | Out-Null
    M 'ArmFlowPortClickConnectionS79' | Out-Null
    $appliedA = M 'TryCompleteArmedAggregateFlowConnectionS79' ([object[]]@($target, 'IN'))
    $binding = $target.GetInputBinding('Image')
    Assert([bool]$appliedA -and $null -ne $binding) ('Two-click aggregate connection did not create a binding; SourcePorts=' + $sourceA.FlowOutputPorts.Count + '; TargetPorts=' + $target.FlowInputPorts.Count + '; Status=' + (F 'HeaderStatusText').Text)
    Assert($binding.SourceToolId -eq $sourceA.ToolId -and $binding.SourcePortName -eq 'Image') 'Auto binding did not select Image -> Image'
    Assert((F 'HeaderStatusText').Text.Length -gt 20) 'Success status did not explain the completed binding'

    M 'BeginAggregateFlowPortDragS79' ([object[]]@($sourceB, 'OUT', $origin, [Windows.Point]::new(120, 180))) | Out-Null
    M 'ArmFlowPortClickConnectionS79' | Out-Null
    $appliedB = M 'TryCompleteArmedAggregateFlowConnectionS79' ([object[]]@($target, 'IN'))
    $binding = $target.GetInputBinding('Image')
    Assert([bool]$appliedB -and $binding.SourceToolId -eq $sourceB.ToolId) 'A second connection did not automatically replace the target input binding'

    (F 'FlowToolList').SelectedItem = $target
    M 'LoadDockConfigurationDraft' ([object[]]@($target)) | Out-Null
    M 'OpenCentralModuleConfigurationS77' ([object[]]@($target)) | Out-Null
    M 'CentralInputsButton_ClickS77' ([object[]]@($null, $null)) | Out-Null
    $rows = F 'dockInputPortRows'
    $imageRow = @($rows | Where-Object PortName -eq 'Image')[0]
    Assert($null -ne $imageRow -and $imageRow.SourceOptions.Count -ge 3) 'Input page did not expose default and both compatible sources'
    Assert($imageRow.SelectedSourceKey -eq ($sourceB.ToolId + '|Image')) 'Input page did not show the auto-bound source'
    $imageRow.SelectedSourceKey = $sourceA.ToolId + '|Image'
    M 'ApplyDockInputDraft' ([object[]]@($target)) | Out-Null
    Assert($target.GetInputBinding('Image').SourceToolId -eq $sourceA.ToolId) 'Input-page source re-selection did not update the real binding'

    $xaml = Get-Content (Join-Path $ProjectRoot 'MainWindow.xaml') -Raw
    Assert($xaml -match 'x:Name="S75InputAnchor"[^>]+Width="18"[^>]+Height="16"') 'Input anchor is not compact enough for direct card dragging'
    Assert($xaml -match 'x:Name="S75OutputAnchor"[^>]+Width="18"[^>]+Height="16"') 'Output anchor is not compact enough for direct card dragging'
    Assert($xaml -match 'SelectedValue="\{Binding SelectedSourceKey') 'Input binding selector is not present'
    Assert((F 'flowUndoHistory').Count -ge 2) 'Automatic create/replace bindings were not recorded as undoable edits'
    Write-Output ('S79_CONNECTION_USABILITY=PASS; HitTarget=18x16; ClickConnect=PASS; AutoBind=Image->Image; Replace=PASS; InputReselect=PASS; Undo=' + (F 'flowUndoHistory').Count)
}
finally {
    try { $mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true); $window.Close() } catch {}
    if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
