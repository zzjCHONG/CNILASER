using System.Diagnostics;
using System.IO.Ports;
using System.Management;
using System.Text;

namespace Simscop.Hardware.CNI.FourChannel
{
    public partial class CNI
    {
        private readonly SerialPort? _serialPort;
        private string? _portName;
        private readonly ManualResetEventSlim _dataReceivedEvent = new(false);
        private byte[] _receivedDataforValid = Array.Empty<byte>();
        private readonly int _validTimeout = 300;

        public CNI()
        {
            _serialPort = new SerialPort()
            {
                BaudRate = 115200,
                StopBits = StopBits.One,
                DataBits = 8,
                Parity = Parity.None,
            };
        }

        public bool Connect(string com = "")
        {
            if (Valid(com))
            {
                Console.WriteLine($"CNI Connected: {_portName}");
                _serialPort!.Open();
                _serialPort.DataReceived += SerialPort_DataReceived;
                return true;
            }
            return false;
        }

        public bool CloseCom()
        {
            if (_serialPort!.IsOpen)
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
                _serialPort.Close();
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            CloseCom();
            _serialPort?.Dispose();
            _dataReceivedEvent?.Dispose();
            _waitHandle?.Dispose();
        }

        ~CNI() => Dispose();

        private bool Valid(string com)
        {
            try
            {
                bool isAutoMode = com == "";

                if (isAutoMode)
                {
                    string[] portNames = SerialPort.GetPortNames();
                    foreach (string portName in portNames)
                    {
                        if (!CheckPort(portName)) continue;

                        if (_serialPort!.IsOpen) _serialPort.Close();

                        _serialPort.PortName = portName;
                        _serialPort.DataReceived -= SerialPort_DataReceived_Valid;
                        _serialPort.DataReceived += SerialPort_DataReceived_Valid;

                        _dataReceivedEvent.Reset();
                        _receivedDataforValid = Array.Empty<byte>();

                        _serialPort.Open();

                        // 发送验证命令：读取第一通道开关状态
                        var bytes = GenerateSettingBytes(ChannelEnum.ONOFFSettingofLaser1, CommandEnum.Read);
                        _serialPort.Write(bytes, 0, bytes.Length);

                        if (_dataReceivedEvent.Wait(_validTimeout))
                        {
                            if (_receivedDataforValid.Length > 0 && _receivedDataforValid[0] == 0x41)
                            {
                                _portName = portName;
                                _serialPort.Close();
                                _serialPort.DataReceived -= SerialPort_DataReceived_Valid;
                                return true;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"校验{portName}，非目标串口！");
                        }

                        _serialPort.Close();
                    }

                    _serialPort!.DataReceived -= SerialPort_DataReceived_Valid;
                    return !string.IsNullOrEmpty(_portName);
                }
                else
                {
                    if (!CheckPort(com)) return false;

                    if (_serialPort!.IsOpen) _serialPort.Close();

                    _serialPort.PortName = com;
                    _serialPort.DataReceived -= SerialPort_DataReceived_Valid;
                    _serialPort.DataReceived += SerialPort_DataReceived_Valid;

                    _dataReceivedEvent.Reset();
                    _receivedDataforValid = Array.Empty<byte>();

                    _serialPort.Open();

                    var bytes = GenerateSettingBytes(ChannelEnum.ONOFFSettingofLaser1, CommandEnum.Read);
                    _serialPort.Write(bytes, 0, bytes.Length);

                    if (_dataReceivedEvent.Wait(_validTimeout))
                    {
                        if (_receivedDataforValid.Length > 0 && _receivedDataforValid[0] == 0x41)
                        {
                            _portName = com;
                            _serialPort.Close();
                            _serialPort.DataReceived -= SerialPort_DataReceived_Valid;
                            return true;
                        }
                    }

                    _serialPort.Close();
                    _serialPort.DataReceived -= SerialPort_DataReceived_Valid;
                    return !string.IsNullOrEmpty(_portName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Valid Error: {ex.Message}");
                return false;
            }
            finally
            {
                _serialPort!.DataReceived -= SerialPort_DataReceived_Valid;
            }
        }

        private void SerialPort_DataReceived_Valid(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int bytesToRead = _serialPort!.BytesToRead;
                if (bytesToRead > 0)
                {
                    byte[] buffer = new byte[bytesToRead];
                    _serialPort.Read(buffer, 0, bytesToRead);
                    _receivedDataforValid = buffer;
                    _dataReceivedEvent.Set();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SerialPort_DataReceived_Valid Error: {ex.Message}");
            }
        }

        private static bool CheckPort(string portName)
        {
            if (IsBluetoothPort(portName))
            {
                Console.WriteLine($"串口 {portName} 识别为蓝牙设备，已自动跳过以防止挂起。");
                return false;
            }

            SerialPort port = new(portName);
            try
            {
                port.Open();
                if (port.IsOpen) port.Close();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"串口 {portName} 已被占用");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"打开串口 {portName} 发生错误: {ex.Message}");
                return false;
            }
        }

        private static bool IsBluetoothPort(string portName)
        {
            try
            {
                // 查询 PnP 实体中包含该串口号的设备描述
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT Caption, Description FROM Win32_PnPEntity WHERE Caption LIKE '%({portName})%'");

                foreach (var device in searcher.Get())
                {
                    string caption = device["Caption"]?.ToString() ?? "";
                    string description = device["Description"]?.ToString() ?? "";

                    // 检查是否包含"Bluetooth"或"蓝牙"关键字
                    if (caption.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase)
                        || description.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase)
                        || caption.Contains("蓝牙", StringComparison.OrdinalIgnoreCase)
                        || description.Contains("蓝牙", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果 WMI 查询失败，为了安全起见可以记录日志
                Debug.WriteLine($"WMI 查询失败: {ex.Message}");
            }
            return false;
        }
    }

    public partial class CNI
    {
        public int VerifyLaserChannel1 { get; private set; } = 0;
        public int VerifyLaserChannel2 { get; private set; } = 0;
        public int VerifyLaserChannel3 { get; private set; } = 0;
        public int VerifyLaserChannel4 { get; private set; } = 0;

        private bool Channel1Enable = false;
        private bool Channel2Enable = false;
        private bool Channel3Enable = false;
        private bool Channel4Enable = false;

        /// <summary>
        /// 异步设置激光功率
        /// </summary>
        /// <param name="index">激光通道 1-4</param>
        /// <param name="value">功率值 0-100</param>
        /// <returns></returns>
        public async Task<bool> SetPowerAsync(int index, int value)
        {
            ChannelEnum channelEnum = index switch
            {
                1 => ChannelEnum.PercentageCurrentSettingofLaser1,
                2 => ChannelEnum.PercentageCurrentSettingofLaser2,
                3 => ChannelEnum.PercentageCurrentSettingofLaser3,
                4 => ChannelEnum.PercentageCurrentSettingofLaser4,
                _ => throw new ArgumentException("Invalid channel index, must be 1-4", nameof(index))
            };

            var result = await SetValueAsync(channelEnum, value);

            if (!result)
            {
                switch (index)
                {
                    case 1: VerifyLaserChannel1 = -1; break;
                    case 2: VerifyLaserChannel2 = -1; break;
                    case 3: VerifyLaserChannel3 = -1; break;
                    case 4: VerifyLaserChannel4 = -1; break;
                }
            }

            return result;
        }

        /// <summary>
        /// 异步获取激光功率
        /// </summary>
        /// <param name="index">激光通道 1-4</param>
        /// <returns></returns>
        public async Task<(bool success, int value)> GetPowerAsync(int index)
        {
            ChannelEnum channelEnum = index switch
            {
                1 => ChannelEnum.PercentageCurrentSettingofLaser1,
                2 => ChannelEnum.PercentageCurrentSettingofLaser2,
                3 => ChannelEnum.PercentageCurrentSettingofLaser3,
                4 => ChannelEnum.PercentageCurrentSettingofLaser4,
                _ => throw new ArgumentException("Invalid channel index, must be 1-4", nameof(index))
            };

            return await GetValueAsync(channelEnum);
        }

        /// <summary>
        /// 异步设置激光开关状态
        /// </summary>
        /// <param name="index">激光通道 1-4</param>
        /// <param name="status">开关状态</param>
        /// <returns></returns>
        public async Task<bool> SetStatusAsync(int index, bool status)
        {
            ChannelEnum channelEnum = index switch
            {
                1 => ChannelEnum.ONOFFSettingofLaser1,
                2 => ChannelEnum.ONOFFSettingofLaser2,
                3 => ChannelEnum.ONOFFSettingofLaser3,
                4 => ChannelEnum.ONOFFSettingofLaser4,
                _ => throw new ArgumentException("Invalid channel index, must be 1-4", nameof(index))
            };

            var result = await SetValueAsync(channelEnum, status ? 1 : 0);

            if (result)
            {
                switch (index)
                {
                    case 1: Channel1Enable = status; break;
                    case 2: Channel2Enable = status; break;
                    case 3: Channel3Enable = status; break;
                    case 4: Channel4Enable = status; break;
                }
            }
            else
            {
                switch (index)
                {
                    case 1: Channel1Enable = false; break;
                    case 2: Channel2Enable = false; break;
                    case 3: Channel3Enable = false; break;
                    case 4: Channel4Enable = false; break;
                }
            }

            return result;
        }

        /// <summary>
        /// 异步获取激光开关状态
        /// </summary>
        /// <param name="index">激光通道 1-4</param>
        /// <returns></returns>
        public async Task<(bool success, bool status)> GetStatusAsync(int index)
        {
            ChannelEnum channelEnum = index switch
            {
                1 => ChannelEnum.ONOFFSettingofLaser1,
                2 => ChannelEnum.ONOFFSettingofLaser2,
                3 => ChannelEnum.ONOFFSettingofLaser3,
                4 => ChannelEnum.ONOFFSettingofLaser4,
                _ => throw new ArgumentException("Invalid channel index, must be 1-4", nameof(index))
            };

            var (success, value) = await GetValueAsync(channelEnum);
            return (success, value != 0);
        }

        /// <summary>
        /// 同步设置激光功率
        /// </summary>
        public bool SetPower(int index, int value)
        {
            ChannelEnum channelEnum = index switch
            {
                1 => ChannelEnum.PercentageCurrentSettingofLaser1,
                2 => ChannelEnum.PercentageCurrentSettingofLaser2,
                3 => ChannelEnum.PercentageCurrentSettingofLaser3,
                4 => ChannelEnum.PercentageCurrentSettingofLaser4,
                _ => throw new ArgumentException("Invalid channel index, must be 1-4", nameof(index))
            };

            var result = SetValue(channelEnum, value);

            if (!result)
            {
                switch (index)
                {
                    case 1: VerifyLaserChannel1 = -1; break;
                    case 2: VerifyLaserChannel2 = -1; break;
                    case 3: VerifyLaserChannel3 = -1; break;
                    case 4: VerifyLaserChannel4 = -1; break;
                }
            }

            return result;
        }

        /// <summary>
        /// 同步获取激光功率
        /// </summary>
        public bool GetPower(int index, out int value)
        {
            value = 0;
            ChannelEnum channelEnum = index switch
            {
                1 => ChannelEnum.PercentageCurrentSettingofLaser1,
                2 => ChannelEnum.PercentageCurrentSettingofLaser2,
                3 => ChannelEnum.PercentageCurrentSettingofLaser3,
                4 => ChannelEnum.PercentageCurrentSettingofLaser4,
                _ => throw new ArgumentException("Invalid channel index, must be 1-4", nameof(index))
            };

            return GetValue(channelEnum, out value);
        }

        /// <summary>
        /// 同步设置激光开关状态
        /// </summary>
        public bool SetStatus(int index, bool status)
        {
            ChannelEnum channelEnum = index switch
            {
                1 => ChannelEnum.ONOFFSettingofLaser1,
                2 => ChannelEnum.ONOFFSettingofLaser2,
                3 => ChannelEnum.ONOFFSettingofLaser3,
                4 => ChannelEnum.ONOFFSettingofLaser4,
                _ => throw new ArgumentException("Invalid channel index, must be 1-4", nameof(index))
            };

            var result = SetValue(channelEnum, status ? 1 : 0);

            if (result)
            {
                switch (index)
                {
                    case 1: Channel1Enable = status; break;
                    case 2: Channel2Enable = status; break;
                    case 3: Channel3Enable = status; break;
                    case 4: Channel4Enable = status; break;
                }
            }
            else
            {
                switch (index)
                {
                    case 1: Channel1Enable = false; break;
                    case 2: Channel2Enable = false; break;
                    case 3: Channel3Enable = false; break;
                    case 4: Channel4Enable = false; break;
                }
            }

            return result;
        }

        /// <summary>
        /// 同步获取激光开关状态
        /// </summary>
        public bool GetStatus(int index, out bool status)
        {
            status = false;
            ChannelEnum channelEnum = index switch
            {
                1 => ChannelEnum.ONOFFSettingofLaser1,
                2 => ChannelEnum.ONOFFSettingofLaser2,
                3 => ChannelEnum.ONOFFSettingofLaser3,
                4 => ChannelEnum.ONOFFSettingofLaser4,
                _ => throw new ArgumentException("Invalid channel index, must be 1-4", nameof(index))
            };

            var res = GetValue(channelEnum, out var value);
            status = value != 0;
            return res;
        }

        /// <summary>
        /// 获取缓存的开关状态（不查询硬件）
        /// </summary>
        public bool GetCachedStatus(int index, out bool status)
        {
            status = false;
            if (index < 1 || index > 4) return false;

            status = index switch
            {
                1 => Channel1Enable,
                2 => Channel2Enable,
                3 => Channel3Enable,
                4 => Channel4Enable,
                _ => false,
            };
            return true;
        }

        /// <summary>
        /// 获取校验数据
        /// </summary>
        public bool GetVerifyValue(int index, out int power)
        {
            power = -1;
            if (index < 1 || index > 4) return false;

            power = index switch
            {
                1 => VerifyLaserChannel1,
                2 => VerifyLaserChannel2,
                3 => VerifyLaserChannel3,
                4 => VerifyLaserChannel4,
                _ => -1,
            };
            return true;
        }

        /// <summary>
        /// 设置电流控制模式
        /// </summary>
        /// <param name="mode">0: 外部控制, 1: 内部控制</param>
        public async Task<bool> SetCurrentControlModeAsync(CurrentControlMode mode)
        {
            return await SetValueAsync(ChannelEnum.CurrentControlModeSetting, (int)mode);
        }

        /// <summary>
        /// 获取电流控制模式
        /// </summary>
        public async Task<(bool success, CurrentControlMode mode)> GetCurrentControlModeAsync()
        {
            var (success, value) = await GetValueAsync(ChannelEnum.CurrentControlModeSetting);
            return (success, (CurrentControlMode)value);
        }

        /// <summary>
        /// 读取设备详细信息（80字节）
        /// </summary>
        public async Task<(bool success, DeviceInfo? info)> ReadDeviceInfoAsync()
        {
            try
            {
                if (!_serialPort!.IsOpen)
                {
                    Console.WriteLine($"Serial port not open");
                    return (false, null);
                }

                var bytes = GenerateSettingBytes(ChannelEnum.ReadInformation, CommandEnum.Read);

                var (ok, response) = await SendCommandAsync(bytes, _sendcommandTimeout);
                if (ok && response.Length >= 86) // 0x41 + 0x56(86) + 0x0A + 0x00 + 80字节数据 + checksum + 0x0D
                {
                    var info = ParseDeviceInfo(response);
                    return (true, info);
                }

                return (false, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReadDeviceInfoAsync Error: {ex.Message}");
                return (false, null);
            }
        }

        /// <summary>
        /// 同步设置电流控制模式
        /// </summary>
        public bool SetCurrentControlMode(CurrentControlMode mode)
        {
            return SetValue(ChannelEnum.CurrentControlModeSetting, (int)mode);
        }

        /// <summary>
        /// 同步获取电流控制模式
        /// </summary>
        public bool GetCurrentControlMode(out CurrentControlMode mode)
        {
            mode = CurrentControlMode.ExternalControl;
            if (GetValue(ChannelEnum.CurrentControlModeSetting, out var value))
            {
                mode = (CurrentControlMode)value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 同步读取设备详细信息
        /// </summary>
        public bool ReadDeviceInfo(out DeviceInfo? info)
        {
            info = null;
            try
            {
                if (!_serialPort!.IsOpen)
                {
                    Console.WriteLine($"Serial port not open");
                    return false;
                }

                var bytes = GenerateSettingBytes(ChannelEnum.ReadInformation, CommandEnum.Read);
                _serialPort.Write(bytes, 0, bytes.Length);
                byte[] response = WaitResponse(2000);

                if (response.Length >= 86)
                {
                    info = ParseDeviceInfo(response);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReadDeviceInfo Error: {ex.Message}");
                return false;
            }
        }

    }

    public partial class CNI
    {
        private readonly int _sendcommandTimeout = 1000;
        private readonly byte[] commandBase = new byte[8] { 0x53, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0D };
        private readonly byte[] responseforSetting = new byte[9] { 0x41, 0x09, 0x00, 0x01, 0x4F, 0x4B, 0x21, 0x00, 0x0D };
        private readonly byte[] responseforReading = new byte[8] { 0x41, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0D };

        private TaskCompletionSource<byte[]>? _commandTcs;
        private byte[] _receiveBuffer = Array.Empty<byte>();

        private readonly ManualResetEventSlim _waitHandle = new(false);
        private byte[] _lastResponse = Array.Empty<byte>();

        public async Task<(bool success, int value)> GetValueAsync(ChannelEnum channel)
        {
            try
            {
                if (!_serialPort!.IsOpen)
                {
                    Console.WriteLine($"Serial port not open");
                    return (false, 0);
                }

                CommandEnum command = CommandEnum.Read;
                var bytes = GenerateSettingBytes(channel, command);

                var (ok, response) = await SendCommandAsync(bytes, _sendcommandTimeout);
                if (ok)
                {
                    if (VerifyResponse(channel, command, response, out int value))
                    {
                        UpdateVerifyValue(channel, value);
                        return (true, value);
                    }
                }

                return (false, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetValueAsync Error: {ex.Message}");
                return (false, 0);
            }
        }

        private async Task<bool> SetValueAsync(ChannelEnum channel, int value)
        {
            try
            {
                if (!_serialPort!.IsOpen)
                {
                    Console.WriteLine($"Serial port not open");
                    return false;
                }

                CommandEnum command = CommandEnum.Write;
                var bytes = GenerateSettingBytes(channel, command, value);

                var (ok, response) = await SendCommandAsync(bytes, _sendcommandTimeout);
                if (ok)
                {
                    if (VerifyResponse(channel, command, response, out _))
                    {
                        UpdateVerifyValue(channel, value);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetValueAsync Error: {ex.Message}");
                return false;
            }
        }

        private async Task<(bool, byte[])> SendCommandAsync(byte[] command, int timeoutMs = 1500)
        {
            if (!_serialPort!.IsOpen)
                throw new InvalidOperationException("串口未打开");

            _commandTcs = new TaskCompletionSource<byte[]>();
            _receiveBuffer = Array.Empty<byte>();

            Console.WriteLine($"[SEND] {BitConverter.ToString(command)}");
            _serialPort.Write(command, 0, command.Length);

            var completedTask = await Task.WhenAny(_commandTcs.Task, Task.Delay(timeoutMs));
            if (completedTask == _commandTcs.Task)
            {
                byte[] response = _commandTcs.Task.Result;
                Console.WriteLine($"[RECV] {BitConverter.ToString(response)}");
                return (true, response);
            }
            else
            {
                Console.WriteLine($"[TIMEOUT] Command timeout");
                return (false, Array.Empty<byte>());
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int bytesToRead = _serialPort!.BytesToRead;
                if (bytesToRead > 0)
                {
                    byte[] buffer = new byte[bytesToRead];
                    _serialPort.Read(buffer, 0, bytesToRead);

                    byte[] combined = new byte[_receiveBuffer.Length + buffer.Length];
                    Buffer.BlockCopy(_receiveBuffer, 0, combined, 0, _receiveBuffer.Length);
                    Buffer.BlockCopy(buffer, 0, combined, _receiveBuffer.Length, buffer.Length);
                    _receiveBuffer = combined;

                    // 检查是否收到完整消息（以0x0D结束）
                    if (_receiveBuffer.Length > 0 && _receiveBuffer[_receiveBuffer.Length - 1] == 0x0D)
                    {
                        byte[] fullResponse = _receiveBuffer;
                        _commandTcs?.TrySetResult(fullResponse);

                        // 给同步方法用
                        _lastResponse = fullResponse;
                        _waitHandle.Set();

                        _receiveBuffer = Array.Empty<byte>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SerialPort_DataReceived Error: {ex.Message}");
                _commandTcs?.TrySetException(ex);
                _waitHandle.Set();
            }
        }

        public bool GetValue(ChannelEnum channel, out int value)
        {
            try
            {
                value = 0;
                if (!_serialPort!.IsOpen)
                {
                    Console.WriteLine($"Serial port not open");
                    return false;
                }

                CommandEnum command = CommandEnum.Read;
                var bytes = GenerateSettingBytes(channel, command);

                _serialPort.Write(bytes, 0, bytes.Length);
                byte[] response = WaitResponse(1500);

                if (response.Length > 0 && VerifyResponse(channel, command, response, out value))
                {
                    UpdateVerifyValue(channel, value);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetValue Error: {ex.Message}");
                value = 0;
                return false;
            }
        }

        private bool SetValue(ChannelEnum channel, int value)
        {
            try
            {
                if (!_serialPort!.IsOpen)
                {
                    Console.WriteLine($"Serial port not open");
                    return false;
                }

                CommandEnum command = CommandEnum.Write;
                var bytes = GenerateSettingBytes(channel, command, value);

                _serialPort.Write(bytes, 0, bytes.Length);
                byte[] response = WaitResponse(1500);

                if (response.Length > 0 && VerifyResponse(channel, command, response, out _))
                {
                    UpdateVerifyValue(channel, value);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetValue Error: {ex.Message}");
                return false;
            }
        }

        private byte[] WaitResponse(int timeoutMs = 1500)
        {
            _lastResponse = Array.Empty<byte>();
            _waitHandle.Reset();

            if (_waitHandle.Wait(timeoutMs))
                return _lastResponse;
            else
                return Array.Empty<byte>();
        }

        private bool VerifyResponse(ChannelEnum channel, CommandEnum command, byte[] response, out int value)
        {
            value = 0;

            if (response.Length == 0)
            {
                Console.WriteLine("Response is empty");
                return false;
            }

            int expectedLength = command == CommandEnum.Read ? 8 : 9;

            // 如果返回数据长度不匹配，尝试提取最后的有效字节
            byte[] validResponse;
            if (response.Length > expectedLength)
            {
                validResponse = new byte[expectedLength];
                Array.Copy(response, response.Length - expectedLength, validResponse, 0, expectedLength);
            }
            else if (response.Length == expectedLength)
            {
                validResponse = response;
            }
            else
            {
                Console.WriteLine($"Invalid response length: {response.Length}, expected: {expectedLength}");
                return false;
            }

            if (command == CommandEnum.Read)
            {
                // 读取命令：解析返回值
                if (validResponse[0] != 0x41)
                {
                    Console.WriteLine($"Invalid response header: 0x{validResponse[0]:X2}");
                    return false;
                }

                byte[] valueBytes = new byte[2];
                valueBytes[0] = validResponse[4];
                valueBytes[1] = validResponse[5];
                value = ByteArrayToInt(valueBytes);

                Console.WriteLine($"Read Success: Channel={channel}, Value={value}");
                return true;
            }
            else
            {
                // 写入命令：验证返回
                var expectedResponse = GenerateResponseBytes(channel, command, value);
                bool isCorrect = validResponse.SequenceEqual(expectedResponse);

                if (isCorrect)
                {
                    Console.WriteLine($"Write Success: Channel={channel}");
                }
                else
                {
                    Console.WriteLine($"\r\n##############Write Failed: Channel={channel}");
                    Console.WriteLine($"##############Expected: {BitConverter.ToString(expectedResponse)}");
                    Console.WriteLine($"##############Received: {BitConverter.ToString(validResponse)}\r\n");
                }

                return isCorrect;
            }
        }

        private void UpdateVerifyValue(ChannelEnum channel, int value)
        {
            switch (channel)
            {
                case ChannelEnum.PercentageCurrentSettingofLaser1:
                    VerifyLaserChannel1 = value;
                    break;
                case ChannelEnum.PercentageCurrentSettingofLaser2:
                    VerifyLaserChannel2 = value;
                    break;
                case ChannelEnum.PercentageCurrentSettingofLaser3:
                    VerifyLaserChannel3 = value;
                    break;
                case ChannelEnum.PercentageCurrentSettingofLaser4:
                    VerifyLaserChannel4 = value;
                    break;
            }
        }

        private byte[] GenerateSettingBytes(ChannelEnum channel, CommandEnum command, int input = 0)
        {
            byte[] bytes = (byte[])commandBase.Clone();
            bytes[2] = (byte)channel;
            bytes[3] = (byte)command;

            if (command == CommandEnum.Write)
            {
                bytes[4] = (byte)((input >> 8) & 0xFF);
                bytes[5] = (byte)(input & 0xFF);
            }

            bytes[6] = CalculateAndAssignSum(bytes, 6);

            return bytes;
        }

        private byte[] GenerateResponseBytes(ChannelEnum channel, CommandEnum command, int input = 0)
        {
            byte[] bytes;
            if (command == CommandEnum.Read)
            {
                bytes = (byte[])responseforReading.Clone();
                bytes[2] = (byte)channel;
                bytes[3] = (byte)command;

                byte high = (byte)((input >> 8) & 0xFF);
                byte low = (byte)(input & 0xFF);
                bytes[4] = high;
                bytes[5] = low;

                bytes[6] = CalculateAndAssignSum(bytes, 6);
            }
            else
            {
                bytes = (byte[])responseforSetting.Clone();
                bytes[2] = (byte)channel;
                bytes[3] = (byte)command;
                bytes[7] = CalculateAndAssignSum(bytes, 7);
            }
            return bytes;
        }

        private static byte CalculateAndAssignSum(byte[] bytes, int count)
        {
            if (bytes.Length < count)
                throw new ArgumentException($"Array must have at least {count} elements.");

            int sum = 0;
            for (int i = 0; i < count; i++)
            {
                sum += bytes[i];
            }

            return (byte)(sum & 0xFF);
        }

        private static int ByteArrayToInt(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length != 2)
                throw new ArgumentException("数组长度必须为 2", nameof(byteArray));

            return ((byteArray[0] & 0xFF) << 8) | (byteArray[1] & 0xFF);
        }
    }

    public partial class CNI
    {
        /// <summary>
        /// 设备信息数据结构（从0x0A命令返回的80字节数据）
        /// 根据 SIG81 RS-232 协议精确对应
        /// </summary>
        public class DeviceInfo
        {
            // Laser1 电流值 (mA) - Byte 1-2
            public int Laser1Current { get; set; }
            // Laser1 温度 (℃) - Byte 5
            public int Laser1Temperature { get; set; }

            // Laser2 电流值 (mA) - Byte 6-7
            public int Laser2Current { get; set; }
            // Laser2 温度 (℃) - Byte 10
            public int Laser2Temperature { get; set; }

            // Laser4 电流值 (mA) - Byte 16-17 (注意：协议中没有Laser3，直接是Laser4)
            public int Laser4Current { get; set; }
            // Laser4 温度 (℃) - Byte 20
            public int Laser4Temperature { get; set; }

            // 按键状态 (Byte 41): 0=off, 1=on
            public bool KeyState { get; set; }

            // 预热状态 (Byte 42): 0=预热中, 1=完成, 2=系统错误
            public PreheatStatus PreheatState { get; set; }

            // 互锁状态 (Byte 43): 0=正常, 1=错误
            public bool InterlockError { get; set; }

            // 急停状态 (Byte 44): 0=正常, 1=错误
            public bool EstopError { get; set; }

            // 激光器开关标志
            public bool Laser1OnOff { get; set; } // Byte 59
            public bool Laser2OnOff { get; set; } // Byte 60
            public bool Laser3OnOff { get; set; } // Byte 61
            public bool Laser4OnOff { get; set; } // Byte 62
        }

        /// <summary>
        /// 解析设备信息 - 严格按照协议文档
        /// 协议格式：0x41 + 0x56 + 0x0A + 0x00 + 80字节数据 + checksum + 0x0D
        /// 总长度：86字节
        /// </summary>
        private static DeviceInfo ParseDeviceInfo(byte[] response)
        {
            var info = new DeviceInfo();

            if (response.Length < 86)
            {
                Console.WriteLine($"ParseDeviceInfo: Invalid response length {response.Length}, expected 86");
                return info;
            }

            // 验证帧头：0x41, 0x56(86), 0x0A, 0x00
            if (response[0] != 0x41 || response[1] != 0x56 || response[2] != 0x0A || response[3] != 0x00)
            {
                Console.WriteLine($"ParseDeviceInfo: Invalid header [0x{response[0]:X2}, 0x{response[1]:X2}, 0x{response[2]:X2}, 0x{response[3]:X2}]");
                return info;
            }

            // 验证帧尾：0x0D
            if (response[85] != 0x0D)
            {
                Console.WriteLine($"ParseDeviceInfo: Invalid footer 0x{response[85]:X2}, expected 0x0D");
            }

            // 验证校验和
            int calculatedChecksum = 0;
            for (int i = 0; i < 84; i++) // 从0到83，共84字节
            {
                calculatedChecksum += response[i];
            }
            calculatedChecksum &= 0xFF;

            if (response[84] != calculatedChecksum)
            {
                Console.WriteLine($"ParseDeviceInfo: Checksum mismatch. Expected 0x{calculatedChecksum:X2}, got 0x{response[84]:X2}");
            }

            // 数据从 response[4] 开始（前4字节是协议头）
            // 80字节有效数据的索引范围：response[4] 到 response[83]
            // 为了方便理解，我们创建一个从0开始的data数组
            byte[] data = new byte[80];
            Array.Copy(response, 4, data, 0, 80);

            try
            {
                // ==================== Laser1 数据 ====================
                // Byte 1-2: Laser1 电流值 (高字节在前, mA)
                info.Laser1Current = (data[0] << 8) | data[1];

                // Byte 5: Laser1 温度 (℃)
                info.Laser1Temperature = data[4];

                // ==================== Laser2 数据 ====================
                // Byte 6-7: Laser2 电流值 (高字节在前, mA)
                info.Laser2Current = (data[5] << 8) | data[6];

                // Byte 10: Laser2 温度 (℃)
                info.Laser2Temperature = data[9];

                // ==================== Laser4 数据 ====================
                // Byte 16-17: Laser4 电流值 (高字节在前, mA)
                info.Laser4Current = (data[15] << 8) | data[16];

                // Byte 20: Laser4 温度 (℃)
                info.Laser4Temperature = data[19];

                // ==================== 状态字节 ====================
                // Byte 41 (data[40]): 按键状态
                // 只取最低位 (bit 0)
                info.KeyState = (data[40] & 0x01) != 0;

                // Byte 42 (data[41]): 预热状态
                // 0=预热中, 1=完成, 2=系统错误
                int preheatValue = data[41];
                info.PreheatState = preheatValue switch
                {
                    0 => PreheatStatus.Preheating,
                    1 => PreheatStatus.Finished,
                    2 => PreheatStatus.SystemError,
                    _ => PreheatStatus.Unknown
                };

                // Byte 43 (data[42]): 互锁状态
                // 0=正常, 1=错误
                info.InterlockError = data[42] != 0;

                // Byte 44 (data[43]): 急停状态
                // 0=正常, 1=错误
                info.EstopError = data[43] != 0;

                // ==================== 激光器开关标志 ====================
                // Byte 59 (data[58]): Laser1 开关标志
                info.Laser1OnOff = data[58] != 0;

                // Byte 60 (data[59]): Laser2 开关标志
                info.Laser2OnOff = data[59] != 0;

                // Byte 61 (data[60]): Laser3 开关标志
                info.Laser3OnOff = data[60] != 0;

                // Byte 62 (data[61]): Laser4 开关标志
                info.Laser4OnOff = data[61] != 0;

                bool isOutputDebug = false;
                if (isOutputDebug)
                {
                    // 输出调试信息
                    Console.WriteLine($"=== Device Info Parsed ===");
                    Console.WriteLine($"Laser1: Current={info.Laser1Current}mA, Temp={info.Laser1Temperature}℃, OnOff={info.Laser1OnOff}");
                    Console.WriteLine($"Laser2: Current={info.Laser2Current}mA, Temp={info.Laser2Temperature}℃, OnOff={info.Laser2OnOff}");
                    Console.WriteLine($"Laser3: Current= N/A mA, Temp= N/A ℃,OnOff={info.Laser3OnOff}");
                    Console.WriteLine($"Laser4: Current={info.Laser4Current}mA, Temp={info.Laser4Temperature}℃, OnOff={info.Laser4OnOff}");
                    Console.WriteLine($"KeyState={info.KeyState}, PreheatState={info.PreheatState}");
                    Console.WriteLine($"InterlockError={info.InterlockError}, EstopError={info.EstopError}");
                    Console.WriteLine($"========================");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"ParseDeviceInfo Error: {ex.Message}");
            }

            return info;
        }

        #region Enums

        public enum ChannelEnum
        {
            ONOFFSettingofLaser1 = 0x51,
            ONOFFSettingofLaser2 = 0x52,
            ONOFFSettingofLaser3 = 0x53,
            ONOFFSettingofLaser4 = 0x54,

            PercentageCurrentSettingofLaser1 = 0x01,
            PercentageCurrentSettingofLaser2 = 0x02,
            PercentageCurrentSettingofLaser3 = 0x03,
            PercentageCurrentSettingofLaser4 = 0x04,

            CurrentControlModeSetting = 0x5A,
            ReadInformation = 0x0A,
        }

        public enum CommandEnum
        {
            Read = 0x00,
            Write = 0x01,
        }

        public enum CurrentControlMode
        {
            ExternalControl = 0,
            InternalControl = 1,
        }

        public enum PreheatStatus
        {
            Preheating = 0,
            Finished = 1,
            SystemError = 2,
            Unknown = -1
        }

        #endregion
    }
}