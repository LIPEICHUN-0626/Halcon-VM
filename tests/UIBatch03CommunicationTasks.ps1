# All actual executions are explicitly offline; never open network/serial transport.
function RunComm09($tool){if(-not $tool.Parameters.CommunicationSimulationS121){throw 'Test refuses real communication.'};M 'RunStandaloneToolCoreUI02' @($tool,'UI09 explicit offline',$false)|Out-Null}
function NumberComm09($tool,[string]$port,[double]$expected){[double]$value=0;if(-not $tool.TryGetNumericOutput($port,[ref]$value) -or [Math]::Abs($value-$expected) -gt 1e-8){throw ('Wrong actual '+$port+': '+$value)}}
function MenuComm09($box,[int]$index){$menu=$box.Parent.Children[0].Children[2].Items[0].Items[$index];$menu.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.MenuItem]::ClickEvent))}
function QueueUi09($tool){return (F 'communicationQueueEditorsS149')[$tool.Kind]}
function ShotComm09($name,$box){$parent=$box.Parent;while($null -ne $parent){if($parent -is [Windows.Controls.Expander]){$parent.IsExpanded=$true};$parent=[Windows.LogicalTreeHelper]::GetParent($parent)};Pump 40;(M 'CommunicationPreviewTextUI09' @((F 'dockEditingTool').Kind)).BringIntoView();Pump 40;Shot $name (F 'moduleParameterWindowS89')}
$flow=F 'flowTools';$old=@($flow);$flow.Clear();foreach($item in $old){$item.Dispose()}
(F 'AutoSendMatchResultCheckBox').IsChecked=$false
$tcp=M 'CreateFlowTool' @([Enum]::Parse($enumType,'TcpClientExchange'),'TCP字段学习',$true,$null);$flow.Add($tcp)
$tcp.Parameters.TcpClientSendTemplate='READ';$tcp.Parameters.TcpClientWaitForResponse=$true
$tcp.Parameters.TcpResponseModeS117='Delimited';$tcp.Parameters.TcpResponseDelimiterS117=','
$tcp.Parameters.TcpResponseStringSelectorS117='0';$tcp.Parameters.TcpResponseNumberSelectorS117='1';$tcp.Parameters.TcpResponseBooleanSelectorS117='2'
$tcp.Parameters.TcpSimulationResponseS121='OK,12.5,true'
$judge=M 'CreateFlowTool' @([Enum]::Parse($enumType,'NumericJudge'),'响应数值判定',$true,$null);$flow.Add($judge)
$judge.SetInputBinding('Value',$tcp.ToolId,'ResponseNumber');$judge.InputToolId=$tcp.ToolId;$judge.InputPortName='ResponseNumber';$judge.NumericLowerLimit=10;$judge.NumericUpperLimit=20
M 'OpenCentralModuleConfigurationS77' @($tcp,$false)|Out-Null
$box=F 'DockTcpSampleResponseS117';$preview=F 'DockTcpSampleResultS117'
MenuComm09 $box 0;MenuComm09 $box 0;$box.Text='ERR,bad,false'
$ui=QueueUi09 $tcp;$ui.Picker.SelectedIndex=0
if((F 'DockTcpSimulationS121').IsChecked -or $tcp.Parameters.CommunicationSimulationS121 -or $null -ne $tcp.LastOutputUtc -or $ui.Name.Visibility -ne [Windows.Visibility]::Collapsed -or $ui.Picker.SelectedItem -notlike '1/2*'){throw 'Sample pick enabled/ran communication or duplicate name editor / numbering is wrong.'}
MenuComm09 $box 1
if($ui.Name.Visibility -ne [Windows.Visibility]::Visible){throw 'Rename did not expose current name editor.'}
$ui.Name.Text='正常数值';$ui.Picker.SelectedIndex=1;$ui.Picker.SelectedIndex=0
if($ui.Name.Visibility -ne [Windows.Visibility]::Collapsed -or $ui.Picker.SelectedItem -notlike '*正常数值*' -or (F 'moduleParameterWindowS89').IsActive){throw 'Rename was lost / left editor exposed / activated window.'}
M 'TryParseTcpSampleS117_Click' @($null,$null)|Out-Null
if($preview.Text -notlike '*12.5*' -or $null -ne $tcp.LastOutputUtc){throw 'Preview did not parse / published formal output.'}
$main.GetField('isFlowExecutionActive',$all).SetValue($window,$true)
try{M 'TryParseTcpSampleS117_Click' @($null,$null)|Out-Null;if($preview.Text -notlike '*先停止运行*'){throw 'Busy preview did not reject.'}}
finally{$main.GetField('isFlowExecutionActive',$all).SetValue($window,$false)}
M 'TryParseTcpSampleS117_Click' @($null,$null)|Out-Null
(F 'DockTcpResponseNumberS117').Text='2'
if($preview.Text -notlike '*请重新预览*' -or $preview.Foreground.ToString() -ne [Windows.Media.Brushes]::SlateGray.ToString()){throw 'Mapping edit kept old successful preview.'}
M 'TryParseTcpSampleS117_Click' @($null,$null)|Out-Null
if($preview.Text -notlike '*失败*'){throw 'Wrong number field did not fail actual parsing.'}
(F 'DockTcpResponseModeS117').SelectedValue='Json'
if((F 'DockTcpResponseDelimiterRowS117').Visibility -ne [Windows.Visibility]::Collapsed){throw 'JSON mode retained delimiter row.'}
(F 'DockTcpResponseModeS117').SelectedValue='Delimited';(F 'DockTcpResponseNumberS117').Text='1'
(F 'DockTcpSimulationS121').IsChecked=$true;M 'DockDraftValue_Changed' @((F 'DockTcpSimulationS121'),$null)|Out-Null
if(-not (M 'ApplyDockConfigurationDraft' @($true))){throw 'TCP sample apply failed.'}
RunComm09 $tcp;NumberComm09 $tcp 'ResponseNumber' 12.5
M 'RunStandaloneToolCoreUI02' @($judge,'UI09 actual typed downstream',$false)|Out-Null
if($judge.ResultCode -ne 'OK' -or $tcp.GetFormattedOutput('Connected') -ne 'False'){throw 'Typed downstream failed / simulation marked connected.'}
$ui=QueueUi09 $tcp;$ui.Picker.SelectedIndex=1
if($tcp.Parameters.CommunicationSampleQueueS149.SelectedIndex -ne 0){throw 'Selecting bad sample applied automatically.'}
M 'DockTrialRunButton_Click' @($null,$null)|Out-Null
[object]$stale=$null
if([string]::IsNullOrWhiteSpace($tcp.ErrorMessage) -or ($tcp.TryGetOutputValue('ResponseNumber',[ref]$stale) -and $null -ne $stale) -or $null -ne $judge.LastOutputUtc -or $tcp.Parameters.CommunicationSampleQueueS149.SelectedIndex -ne 0){throw 'Bad sample F5 retained old value/downstream or applied selection.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null;RunComm09 $tcp
M 'TryParseTcpSampleS117_Click' @($null,$null)|Out-Null;ShotComm09 'tcp-preview' $box
# A queue with no explicit selection cannot preview a prior payload.
MenuComm09 $box 2
M 'TryParseTcpSampleS117_Click' @($null,$null)|Out-Null
if($preview.Text -notlike '*先选取*'){throw 'Deleted queue selection silently previewed another payload.'}
M 'DockRevertButton_Click' @($null,$null)|Out-Null
M 'CloseCentralModuleConfigurationS77'|Out-Null
$commTools=@($tcp)
foreach($kind in @('ModbusTcpExchange','ModbusRtuExchange')){
    $rtu=$kind -eq 'ModbusRtuExchange'
    $tool=M 'CreateFlowTool' @([Enum]::Parse($enumType,$kind),$kind,$true,$null);$flow.Add($tool);$commTools+=,$tool
    $tool.Parameters.CommunicationSimulationS121=$true;$tool.Parameters.ModbusRegisterTypeS122='U32'
    if($rtu){$tool.Parameters.RtuQuantityS119=2}else{$tool.Parameters.ModbusQuantityS118=2}
    M 'OpenCentralModuleConfigurationS77' @($tool,$false)|Out-Null
    $box=F $(if($rtu){'DockRtuSampleFrameS119'}else{'DockModbusSampleFrameS118'})
    $preview=F $(if($rtu){'DockRtuSampleResultS119'}else{'DockModbusSampleResultS118'})
    $parse=$(if($rtu){'ParseRtuSampleS119_Click'}else{'ParseModbusSampleS118_Click'})
    M $(if($rtu){'LoadRtuSampleS119_Click'}else{'LoadModbusSampleS118_Click'}) @($null,$null)|Out-Null
    $good=$box.Text;MenuComm09 $box 0;MenuComm09 $box 0
    $bytes=@($good.Split(' ')|Where-Object {$_})
    $bytes[$bytes.Count-1]=([Convert]::ToByte($bytes[$bytes.Count-1],16) -bxor 255).ToString('X2')
    # MBAP has no CRC: make its byte count inconsistent, not just a valid changed value.
    if(-not $rtu){$bytes[8]='01'}
    $box.Text=$bytes -join ' ';$bad=$box.Text
    $ui=QueueUi09 $tool;$ui.Picker.SelectedIndex=0
    M $parse @($null,$null)|Out-Null
    if($preview.Text -notlike '*65538*' -or $null -ne $tool.LastOutputUtc){throw 'Real register preview failed / published output.'}
    $typeBox=F $(if($rtu){'DockRtuTypeS122'}else{'DockModbusTypeS122'})
    $orderRow=F $(if($rtu){'DockRtuOrderRowS122'}else{'DockModbusOrderRowS122'})
    $typeBox.SelectedValue='U16'
    if($orderRow.Visibility -ne [Windows.Visibility]::Collapsed -or $preview.Text -notlike '*请重新预览*'){throw 'Register type change kept old order row or preview.'}
    $typeBox.SelectedValue='U32'
    if($orderRow.Visibility -ne [Windows.Visibility]::Visible){throw 'Multi-word type missed ordering choice.'}
    $function=F $(if($rtu){'DockRtuFunctionS119'}else{'DockModbusFunctionS118'})
    $writeRow=F $(if($rtu){'DockRtuWriteRowS119'}else{'DockModbusWriteRowS118'})
    $function.SelectedValue='06'
    if($writeRow.Visibility -ne [Windows.Visibility]::Visible){throw 'Write selection showed stale read fields.'}
    $function.SelectedValue='03'
    if(-not (M 'ApplyDockConfigurationDraft' @($true))){throw 'Register sample apply failed.'}
    RunComm09 $tool;NumberComm09 $tool 'Value' 65538
    if($tool.GetFormattedOutput('Connected') -ne 'False' -or $tool.GetFormattedOutput('IsSimulation') -ne 'True'){throw 'Offline identity lost.'}
    $ui=QueueUi09 $tool;$ui.Picker.SelectedIndex=1
    M $parse @($null,$null)|Out-Null
    if($preview.Text -notlike '*失败*'){throw 'Malformed frame preview accepted.'}
    M 'DockTrialRunButton_Click' @($null,$null)|Out-Null
    [object]$stale=$null
    if([string]::IsNullOrWhiteSpace($tool.ErrorMessage) -or ($tool.TryGetOutputValue('Value',[ref]$stale) -and $null -ne $stale) -or $tool.Parameters.CommunicationSampleQueueS149.SelectedIndex -ne 0){throw 'Bad frame F5 retained old typed value / applied draft.'}
    M 'DockRevertButton_Click' @($null,$null)|Out-Null;RunComm09 $tool
    M $parse @($null,$null)|Out-Null;ShotComm09 $kind $box
    M 'CloseCentralModuleConfigurationS77'|Out-Null
}
$saved=Join-Path $temp 'offline-ui09.json';$service.SaveRecipe($saved,(M 'CaptureRecipe'));M 'ApplyRecipe' @($service.LoadRecipe($saved))|Out-Null
$flow=F 'flowTools'
foreach($tool in @($flow|Where-Object {$_.Kind.ToString() -in @('TcpClientExchange','ModbusTcpExchange','ModbusRtuExchange')})){
    if($tool.Parameters.CommunicationSampleQueueS149.Samples.Count -ne 2 -or $tool.Parameters.CommunicationSampleQueueS149.SelectedIndex -ne 0){throw 'Sample queue changed across save.'}
    RunComm09 $tool
    if($tool.Kind.ToString() -eq 'TcpClientExchange'){NumberComm09 $tool 'ResponseNumber' 12.5}else{NumberComm09 $tool 'Value' 65538}
}
Write-Output 'UI03_BLOCK09=PASS; PickDoesNotEnableOrRun=True; OneBasedCompactPicker=True; ActualTcp12_5Downstream=True; PreviewNotOutput=True; MappingEditClearsPreview=True; FunctionAndDecodeSelection=True; ActualModbusTcpRtu65538=True; BadTcpMbapCrcClears=True; F5DoesNotApply=True; CancelAndSaveReload=True; NoMouse=True; NoNetworkOrSerialConnection=True'
