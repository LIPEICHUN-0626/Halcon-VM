# Reuse the original six-tool learning example; run only trusted local code.
function RunScript08($tool){M 'RunStandaloneToolCoreUI02' @($tool,'UI08 explicit script task',$false)|Out-Null}
function MeanScript08([double]$expected){[double]$actual=0;if(-not $statistics.TryGetNumericOutput('Mean',[ref]$actual) -or [Math]::Abs($actual-$expected) -gt 1e-8){throw ('Wrong real script mean: '+$actual)}}
$flow=F 'flowTools';$oldTools=@($flow);$flow.Clear();foreach($old in $oldTools){$old.Dispose()}
M 'BuildScriptArrayExampleS107'|Out-Null;M 'RefreshFlowGraphLayout'|Out-Null
$numbers=$flow[0];$labels=$flow[1];$script=$flow[2];$statistics=$flow[3]
foreach($item in @($flow)){RunScript08 $item}
MeanScript08 10.15
$other=M 'CreateFlowTool' @([Enum]::Parse($enumType,'CSharpDllScript'),'Another script',$true,$null);$flow.Add($other)
M 'OpenCentralModuleConfigurationS77' @($script,$false)|Out-Null
(F 'FlowToolList').SelectedItem=$other
$width=(F 'ToolCatalogColumn').Width;$zoom=(F 'FlowGraphScaleTransform').ScaleX
M 'OpenScriptInterfaceButton_Click' @((F 'DockScriptInterfaceButtonUI08'),$null)|Out-Null;Pump 60
$designer=F 'scriptInterfaceWindowUI08'
if((F 'scriptInterfaceTool') -ne $script -or $designer.IsActive -or $designer.ShowActivated -or (F 'MainWorkspaceGrid').Visibility -ne [Windows.Visibility]::Visible -or (F 'ToolCatalogColumn').Width -ne $width -or (F 'FlowGraphScaleTransform').ScaleX -ne $zoom){throw 'Interface targeted selection instead of editor / hid or altered flow / activated.'}
$offset=@((F 'scriptInterfaceDraftMembers')|Where-Object Name -eq 'Offset')[0]
(F 'ScriptInterfaceMemberListBox').SelectedItem=$offset
(F 'ScriptInterfaceDefaultTextBox').Text='0.2'
Shot 'script-interface' $designer
if((F 'ScriptInterfaceDefaultTextBox').ActualWidth -lt 100 -or (F 'ScriptInterfaceDefaultTextBox').ActualWidth -gt 140){throw 'Numeric parameter editor is too narrow or unnecessarily long.'}
# Reopening the same tool must preserve the current interface draft.
M 'OpenScriptInterfaceButton_Click' @((F 'DockScriptInterfaceButtonUI08'),$null)|Out-Null
if((F 'ScriptInterfaceDefaultTextBox').Text -ne '0.2'){throw 'Reopening discarded member edit.'}
M 'ApplyScriptInterfaceButton_Click' @($null,$null)|Out-Null
if(@($script.Parameters.ScriptInterfaceMembers|Where-Object Name -eq 'Offset')[0].DefaultValue -ne '0.2' -or $null -ne $script.LastOutputUtc -or $null -ne $statistics.LastOutputUtc){throw 'Apply lost named parameter or retained old outputs.'}
M 'CloseScriptInterfaceButton_Click' @($null,$null)|Out-Null
RunScript08 $script;RunScript08 $statistics;MeanScript08 10.3
[object]$array=$null;$numbers.TryGetOutputValue('Array',[ref]$array)|Out-Null
if([Math]::Abs($array[0]-10.1) -gt 1e-8){throw 'Script modified original upstream array.'}
# New named input becomes available in the original parameter window without reopening.
M 'OpenScriptInterfaceButton_Click' @((F 'DockScriptInterfaceButtonUI08'),$null)|Out-Null
M 'AddScriptInputButton_Click' @($null,$null)|Out-Null
(F 'ScriptInterfaceNameTextBox').Text='OptionalExtra'
(F 'ScriptInterfaceRequiredCheckBox').IsChecked=$false
M 'ApplyScriptInterfaceButton_Click' @($null,$null)|Out-Null
$extra=@($script.Parameters.ScriptInterfaceMembers|Where-Object Name -eq 'OptionalExtra')[0]
if($null -eq $extra -or @((F 'dockInputPortRows')|Where-Object PortName -eq $extra.PortName).Count -ne 1){throw 'Applied new input did not appear in original input task.'}
# Changing direction updates mode-dependent fields immediately, not one selection late.
(F 'ScriptInterfaceDirectionComboBox').SelectedValue='Output'
if((F 'ScriptInterfaceDefaultRow').Visibility -ne [Windows.Visibility]::Collapsed){throw 'Output still displayed default editor.'}
(F 'ScriptInterfaceDirectionComboBox').SelectedValue='Parameter'
if((F 'ScriptInterfaceRequiredCheckBox').IsEnabled -or (F 'ScriptInterfaceDefaultRow').Visibility -ne [Windows.Visibility]::Visible){throw 'Parameter constraint/default state is stale.'}
M 'CancelScriptInterfaceButton_Click' @($null,$null)|Out-Null
if(@($script.Parameters.ScriptInterfaceMembers|Where-Object Name -eq 'OptionalExtra')[0].Direction -ne 'Input'){throw 'Cancelled direction edit applied.'}
# A later independent parameter draft must survive rejection of stale interface apply.
(F 'DockScriptParameterNumberTextBox').Text='12'
M 'ApplyScriptInterfaceButton_Click' @($null,$null)|Out-Null
if((F 'ScriptInterfaceValidationText').Text -notlike '*参数草稿或方案已改变*' -or -not (F 'dockDraftDirty') -or (F 'DockScriptParameterNumberTextBox').Text -ne '12'){throw 'Interface apply overwrote another pending draft.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null
M 'CloseScriptInterfaceButton_Click' @($null,$null)|Out-Null
# Explicitly exercise the original DLL mode using only the self-owned compiled example.
$formalCode=$script.Parameters.ScriptInlineCode
(F 'DockScriptInlineCodeTextBox').Text=$formalCode.Replace('values[i] += offset;','values[i] += offset + 1;')
M 'DockTrialRunButton_Click' @($null,$null)|Out-Null
$draftOutput=@($script.Parameters.ScriptInterfaceMembers|Where-Object Name -eq 'Corrected')[0]
[object]$draftArray=$null
if(-not $script.TryGetOutputValue($draftOutput.PortName,[ref]$draftArray) -or [Math]::Abs($draftArray[0]-11.3) -gt 1e-8 -or $script.Parameters.ScriptInlineCode -ne $formalCode -or -not (F 'dockDraftDirty') -or $null -ne $other.LastOutputUtc){throw 'F5 did not use only the current code draft or applied it / ran other selection.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null
if((F 'dockDraftDirty') -or (F 'DockScriptInlineCodeTextBox').Text -ne $formalCode){throw 'Cancel failed to restore applied script code.'}
$scriptServiceType=$assembly.GetType('HalconWinFormsDemo.Services.ScriptToolService',$true)
$compile=$scriptServiceType.GetMethod('CompileInlineAssembly',[Reflection.BindingFlags]'Static,NonPublic')
$dll=$compile.Invoke($null,[object[]]@($script.Parameters.ScriptInlineCode))
(F 'DockScriptExecutionModeComboBox').SelectedValue='Dll'
if((F 'DockScriptDllPanel').Visibility -ne [Windows.Visibility]::Visible -or (F 'DockScriptInlinePanel').Visibility -ne [Windows.Visibility]::Collapsed){throw 'DLL selection displayed the previous code mode.'}
(F 'DockScriptResourceComboBox').SelectedIndex=-1
(F 'DockScriptAssemblyPathTextBox').Text=$dll
(F 'DockScriptTypeNameTextBox').Text='HalconVmUserScript.ScriptEntry'
(F 'DockScriptMethodNameTextBox').Text='Execute'
if(-not (M 'ApplyDockConfigurationDraft' @($true))){throw 'Explicit trusted DLL configuration apply failed.'}
RunScript08 $script;RunScript08 $statistics;MeanScript08 10.3
# Actual declared-array mismatch throws and must clear old named output.
$labels.Parameters.ArrayText='P1';RunScript08 $labels
$failed=$false;try{RunScript08 $script}catch{$failed=$true}
$output=@($script.Parameters.ScriptInterfaceMembers|Where-Object Name -eq 'Corrected')[0]
[object]$stale=$null
if(-not $failed -or [string]::IsNullOrWhiteSpace($script.ErrorMessage) -or $null -ne $statistics.LastOutputUtc -or ($script.TryGetOutputValue($output.PortName,[ref]$stale) -and $null -ne $stale)){throw 'DLL failure retained old array / downstream or concealed error.'}
$labels.Parameters.ArrayText='P1,P2,P3,P4,P5';RunScript08 $labels;RunScript08 $script;RunScript08 $statistics;MeanScript08 10.3
$saved=Join-Path $temp 'script-ui08.json';$service.SaveRecipe($saved,(M 'CaptureRecipe'))
M 'OpenScriptInterfaceButton_Click' @((F 'DockScriptInterfaceButtonUI08'),$null)|Out-Null
M 'CloseCentralModuleConfigurationS77'|Out-Null
M 'ApplyRecipe' @($service.LoadRecipe($saved))|Out-Null
M 'ApplyScriptInterfaceButton_Click' @($null,$null)|Out-Null
if((F 'ScriptInterfaceValidationText').Text -notlike '*已不存在*'){throw 'Old interface window edited reloaded instances.'}
M 'CloseScriptInterfaceButton_Click' @($null,$null)|Out-Null
$flow=F 'flowTools';$statistics=$flow[3]
foreach($item in @($flow|Select-Object -First 6)){RunScript08 $item}
MeanScript08 10.3
Write-Output 'UI03_BLOCK08=PASS; OriginalIndependentDesigner=True; EditorIdentity=True; FlowNotHidden=True; NamedOffsetMean10_15To10_3=True; ParameterDraftPreserved=True; DynamicInputRefreshed=True; DirectionAndDllUi=True; F5Draft11_3NoApply=True; TrustedDllActualRun=True; ExceptionClearsNamedArrayAndDownstream=True; SourceArrayIndependent=True; SaveReloadRun=True; StaleWindowRejected=True; NoMouse=True; NoDevices=True'
