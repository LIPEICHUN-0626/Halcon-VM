# Original OCR project and UI events, real local HALCON, no desktop input.
function RunOcr04($tool){M 'RunStandaloneToolCoreUI02' @($tool,'UI04 explicit OCR',$false)|Out-Null}
function ValueOcr04($tool,[string]$port){[object]$v=$null;if(-not $tool.TryGetOutputValue($port,[ref]$v)){throw ('Missing '+$port+': '+$tool.ErrorMessage)};Write-Output -NoEnumerate $v}
$main.GetField('isRecipeDirty',$all).SetValue($window,$false)
Write-Output 'OCR_STAGE=LoadExample'
M 'LoadExampleProgramS78' @('ocr-character-validation')|Out-Null;Pump 60
$flow=F 'flowTools';$source=$flow[0];$ocr=$flow[1];$stats=$flow[2];$first=$flow[3]
if($flow.Count -ne 4 -or @($flow|Where-Object {$null -ne $_.LastOutputUtc}).Count){throw 'OCR example auto-ran.'}
(F 'AutoSendMatchResultCheckBox').IsChecked=$false
Write-Output 'OCR_STAGE=OpenEditor'
M 'OpenCentralModuleConfigurationS77' @($ocr,$false)|Out-Null;Pump 40
$editor=F 'moduleParameterWindowS89'
if($editor.IsActive -or $editor.ShowActivated -or $window.IsActive -or $editor.Left -gt -10000){throw 'Foreground activation.'}
$mode=F 'DockOcrValidationModeS133'
Write-Output 'OCR_STAGE=SelectRules'
$mode.SelectedIndex=1
if((F 'DockOcrExactGroupS133').Visibility -ne 'Visible' -or (F 'DockOcrPatternGroupS133').Visibility -ne 'Collapsed'){throw 'Selecting Exact did not immediately show the expected-text field.'}
$mode.SelectedIndex=2
if((F 'DockOcrPatternGroupS133').Visibility -ne 'Visible' -or (F 'DockOcrExactGroupS133').Visibility -ne 'Collapsed'){throw 'Selecting Pattern did not immediately show the format field.'}
$mode.SelectedIndex=0
if((F 'DockOcrIgnoreCaseS133').Visibility -ne 'Collapsed'){throw 'None left a redundant case option.'}
Write-Output 'OCR_STAGE=RunSource'
RunOcr04 $source
Write-Output 'OCR_STAGE=RunOcr'
RunOcr04 $ocr
$actual=ValueOcr04 $ocr 'Text'
$count=ValueOcr04 $ocr 'CharacterCount'
if($count -le 0 -or [string]::IsNullOrWhiteSpace($actual)){throw 'No real characters.'}
Write-Output ('OCR_ACTUAL_COUNT='+$count+'; Minimum='+(ValueOcr04 $ocr 'MinimumConfidence'))
if((F 'DockOcrConfidenceTextBox').ActualWidth -gt 140){throw 'Numeric field remains full width.'}
M 'DockOcrSourceTaskUI04_Click' @($null,$null)|Out-Null
if(-not (F 'DockIoTaskTab').IsSelected -or (F 'dockEditingTool') -ne $ocr){throw 'Source link changed editor or failed to open inputs.'}
M 'SelectCentralConfigurationTaskS77' @((F 'DockParametersTaskTab'),'Parameters')|Out-Null
Shot 'ocr-common' $editor
# Explicit trusted local dictionary import, draft only; no file picker or external code.
M 'ImportOcrModelS133' @($ocr.Parameters.OcrModelPath)|Out-Null
if([string]::IsNullOrWhiteSpace((F 'ocrValidationDraftS133').ModelSha256) -or -not [string]::IsNullOrWhiteSpace($ocr.Parameters.OcrValidationS133.ModelSha256)){throw 'Import was not a managed draft.'}
$mode.SelectedIndex=1;(F 'DockOcrExpectedS133').Text=$actual
M 'DockTrialRunButton_Click' @($null,$null)|Out-Null
if(-not (ValueOcr04 $ocr 'TextRuleMatch') -or $ocr.Parameters.OcrValidationS133.Mode -ne 'None' -or $null -ne $stats.LastOutputUtc){throw 'F5 rule not applied temporarily / ran downstream.'}
if(-not (M 'ApplyDockConfigurationDraft' @($true))){throw ('Apply: '+(F 'DockValidationText').Text)}
RunOcr04 $ocr;RunOcr04 $stats;RunOcr04 $first
if((ValueOcr04 $stats 'Count') -ne $count -or [Math]::Abs((ValueOcr04 $stats 'Min')-(ValueOcr04 $ocr 'MinimumConfidence')) -gt 1e-10 -or (ValueOcr04 $first 'Text') -ne (ValueOcr04 $ocr 'FirstCharacter')){throw 'Typed character downstream mismatch.'}
$mode.SelectedIndex=2;(F 'DockOcrPatternS133').Text='[0-9]{1}'
M 'DockTrialRunButton_Click' @($null,$null)|Out-Null
if((ValueOcr04 $ocr 'TextRuleMatch') -or $ocr.ResultCode -ne 'NG' -or $ocr.Parameters.OcrValidationS133.Mode -ne 'Exact'){throw 'Format mismatch not NG / applied draft.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null
if($mode.SelectedValue -ne 'Exact' -or (F 'DockOcrExpectedS133').Text -ne $actual){throw 'Cancel did not restore exact rule.'}
# Real low-confidence rows, not a fabricated grid. Rule success must not hide rejection.
(F 'DockOcrConfidenceTextBox').Text='1'
M 'DockTrialRunButton_Click' @($null,$null)|Out-Null
if(-not (ValueOcr04 $ocr 'TextRuleMatch') -or (ValueOcr04 $ocr 'LowConfidenceCount') -le 0 -or $ocr.ResultCode -ne 'NG'){throw 'Rule success masked low confidence.'}
(F 'DockOcrOnlyAbnormalS133').IsChecked=$true
M 'RefreshOcrCharacterGridS133' @($ocr)|Out-Null
if((F 'DockOcrCharactersS133').Items.Count -lt 1 -or @((F 'DockOcrCharactersS133').Items|Where-Object {-not $_.IsLowConfidence -and -not $_.IsRejected}).Count){throw 'Abnormal filter fabricated or retained normal rows.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null
M 'CloseCentralModuleConfigurationS77'|Out-Null
$saved=Join-Path $temp 'ocr-roundtrip.json'
$service.SaveRecipe($saved,(M 'CaptureRecipe'))
M 'ApplyRecipe' @($service.LoadRecipe($saved))|Out-Null
$flow=F 'flowTools';$source=$flow[0];$ocr=$flow[1];$stats=$flow[2];$first=$flow[3]
RunOcr04 $source;RunOcr04 $ocr;RunOcr04 $stats;RunOcr04 $first
if((ValueOcr04 $ocr 'Text') -ne $actual -or -not (ValueOcr04 $ocr 'TextRuleMatch') -or [string]::IsNullOrWhiteSpace($ocr.Parameters.OcrValidationS133.ModelSha256)){throw 'Managed dictionary/rule save-reload failed.'}
$path=$ocr.Parameters.OcrModelPath;$ocr.Parameters.OcrModelPath=Join-Path $temp 'missing.omc'
$failed=$false;try{RunOcr04 $ocr}catch{$failed=$true}
[object]$old=$null
if(-not $failed -or ($ocr.TryGetOutputValue('CharacterTexts',[ref]$old) -and $null -ne $old) -or $null -ne (M 'GetOcrCharacterStateS133' @($ocr))){throw 'Missing dictionary retained characters.'}
$ocr.Parameters.OcrModelPath=$path;RunOcr04 $ocr
$child=M 'CreateFlowTool' @([Enum]::Parse($enumType,'OcrRecognition'),'前序字符识别',$true,$null)
$child.Parameters=$ocr.Parameters.Clone();$flow.Add($child)
$child.SetInputBinding('Image',$source.ToolId,'Image');$child.SetInputBinding('Characters',$ocr.ToolId,'CharactersRegion')
# The original 0.8 confidence correctly produces NG; Region consumers must reject it.
$rejected=$false;try{RunOcr04 $child}catch{$rejected=$true}
if(-not $rejected){throw 'NG Region was accepted.'}
# Explicit test-only threshold change; no shipped example or production accuracy claim.
M 'OpenCentralModuleConfigurationS77' @($ocr,$false)|Out-Null
(F 'DockOcrConfidenceTextBox').Text='0.5'
if(-not (M 'ApplyDockConfigurationDraft' @($true))){throw 'Explicit confidence apply failed.'}
M 'CloseCentralModuleConfigurationS77'|Out-Null
RunOcr04 $ocr
if($ocr.ResultCode -ne 'OK'){throw 'Test threshold did not yield a qualified Region.'}
RunOcr04 $child
if((ValueOcr04 $child 'Text') -ne $actual -or (ValueOcr04 $child 'CharacterCount') -ne $count){throw 'Real same-source Characters recognition failed.'}
M 'OpenCentralModuleConfigurationS77' @($child,$false)|Out-Null;Pump 30
if((F 'DockOcrThresholdTextBox').Visibility -ne 'Collapsed' -or (F 'DockOcrSourceTaskUI04').Content -notlike '*前序区域*'){throw 'Subscribed Characters kept unused threshold.'}
$row=@((F 'dockInputPortRows')|Where-Object PortName -eq 'Characters')[0]
$row.SelectedSourceKey=($row.SourceOptions|Where-Object IsDefault|Select-Object -First 1).Key
M 'DockDraftValue_Changed' @((F 'DockOcrSourceTaskUI04'),[Windows.RoutedEventArgs]::new())|Out-Null
if((F 'DockOcrThresholdTextBox').Visibility -ne 'Visible' -or (F 'DockOcrSourceTaskUI04').Content -notlike '*未应用*' -or $null -eq $child.GetInputBinding('Characters')){throw 'Draft source did not restore threshold / applied automatically.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null
if((F 'DockOcrThresholdTextBox').Visibility -ne 'Collapsed'){throw 'Cancel lost source-specific fields.'}
M 'SelectCentralConfigurationTaskS77' @((F 'DockParametersTaskTab'),'Parameters')|Out-Null
Shot 'ocr-subscribed' (F 'moduleParameterWindowS89')
M 'CloseCentralModuleConfigurationS77'|Out-Null
Write-Output 'UI04_OCR_PROGRESS=PASS; RealCharacters; ExactAndPattern; DraftApplyCancel; LowConfidenceNotMasked; TypedDownstream; ManagedDictionarySaveReload; MissingModelClears; SameSourceCharacters; SourceDraftVisibility; NoMouse'
