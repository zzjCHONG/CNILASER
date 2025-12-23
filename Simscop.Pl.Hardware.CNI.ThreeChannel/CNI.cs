using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace Simscop.Pl.Hardware.CNI.ThreeChannel
{
    //todo ，接收信号使用事件获取

    public partial class CNI
    {
        private readonly SerialPort? _serialPort;
        private readonly object _serialLock = new();

        private readonly byte[] commandBase = new byte[8] { 0x53, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0D };
        private readonly byte[] responseforSetting = new byte[9] { 0x41, 0x09, 0x00, 0x01, 0x4F, 0x4B, 0x21, 0x00, 0x0D };
        private readonly byte[] responseforReading = new byte[9] { 0x41, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0D };

        public int VerifyLaserChannel1 = 0;
        public int VerifyLaserChannel2 = 0;
        public int VerifyLaserChannel3 = 0;

        private bool GetValue(ChannelEnum channel, out int value)
        {
            try
            {
                value = 0;
                CommandEnum command = CommandEnum.Read;

                if (_serialPort!.IsOpen)
                {
                    var bytes = GenerateSettingBytes(channel, command);

                    return ThreadSafeSendAndReceive(bytes, channel, command, out value);
                }
                else
                {
                    Console.WriteLine($"_port.IsOpen_{_serialPort.IsOpen}");
                }
                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private bool SetValue(ChannelEnum channel, int value)
        {
            try
            {
                CommandEnum command = CommandEnum.Write;

                if (channel == ChannelEnum.PercentageCurrentSettingofLaser1) VerifyLaserChannel1 = value;
                if (channel == ChannelEnum.PercentageCurrentSettingofLaser2) VerifyLaserChannel2 = value;
                if (channel == ChannelEnum.PercentageCurrentSettingofLaser3) VerifyLaserChannel3 = value;

                if (_serialPort!.IsOpen)
                {
                    var bytes = GenerateSettingBytes(channel, command, value);

                    return ThreadSafeSendAndReceive(bytes, channel, command, out _);
                }
                else
                {
                    if (channel == ChannelEnum.PercentageCurrentSettingofLaser1) VerifyLaserChannel1 = -1;
                    if (channel == ChannelEnum.PercentageCurrentSettingofLaser2) VerifyLaserChannel2 = -1;
                    if (channel == ChannelEnum.PercentageCurrentSettingofLaser3) VerifyLaserChannel3 = -1;
                    Debug.WriteLine($"_port.IsOpen_{_serialPort.IsOpen}");
                }
                return false;
            }
            catch (Exception)
            {
                if (channel == ChannelEnum.PercentageCurrentSettingofLaser1) VerifyLaserChannel1 = -1;
                if (channel == ChannelEnum.PercentageCurrentSettingofLaser2) VerifyLaserChannel2 = -1;
                if (channel == ChannelEnum.PercentageCurrentSettingofLaser3) VerifyLaserChannel3 = -1;
                return false;
            }
        }

        private bool VerifyData(ChannelEnum channel, CommandEnum command, out int value)
        {
            value = 0;

            Thread.Sleep(200);
            string str = _serialPort!.ReadExisting();
            byte[] bytesRtn = Encoding.ASCII.GetBytes(str);

            //截取有效数据——是否需要？
            int bytesLength = command == CommandEnum.Read ? 8 : 9;
            byte[] lastBytes = new byte[bytesLength];
            if (bytesRtn.Length < 0) return false;
            if (bytesRtn.Length / bytesLength >= 1)
            {
                if (bytesRtn.Length > bytesLength)
                {
                    //读取，若返回数据是9位，则校验失败
                    return false;
                    //Array.Copy(bytesRtn, bytesRtn.Length - bytesLength, lastBytes, 0, bytesLength);
                }
                else
                {
                    lastBytes = bytesRtn;
                }
            }

            if (command == CommandEnum.Read)
            {
                //read-直接转换
                byte[] byteArray = new byte[2];
                byteArray[0] = bytesRtn[4];
                byteArray[1] = bytesRtn[5];
                value = ByteArrayToInt(byteArray);

                if (channel == ChannelEnum.PercentageCurrentSettingofLaser1) VerifyLaserChannel1 = value;
                if (channel == ChannelEnum.PercentageCurrentSettingofLaser2) VerifyLaserChannel2 = value;
                if (channel == ChannelEnum.PercentageCurrentSettingofLaser3) VerifyLaserChannel3 = value;
            }
            else
            {
                //set-校验返回
                var rtn = GenerateResponseBytes(channel, command, value);
                bool isCorrect = lastBytes.SequenceEqual(rtn);
                Console.WriteLine($"Bytes return {isCorrect}!--{string.Join(", ", rtn)}");
                if (!isCorrect)
                {
                    if (channel == ChannelEnum.PercentageCurrentSettingofLaser1) VerifyLaserChannel1 = -1;
                    if (channel == ChannelEnum.PercentageCurrentSettingofLaser2) VerifyLaserChannel2 = -1;
                    if (channel == ChannelEnum.PercentageCurrentSettingofLaser3) VerifyLaserChannel3 = -1;
                }

                return isCorrect;
            }

            return true;
        }

        private bool ThreadSafeSendAndReceive(byte[] sendData, ChannelEnum channel, CommandEnum command, out int value)
        {
            lock (_serialLock)
            {
                value = 0;
                _serialPort!.DiscardInBuffer();
                _serialPort.Write(sendData, 0, sendData.Length);
                return VerifyData(channel, command, out value);
            }
        }

        private byte[] GenerateSettingBytes(ChannelEnum channel, CommandEnum command, int input = 0)
        {
            byte[] bytes = commandBase;
            bytes[2] = (byte)channel;
            bytes[3] = (byte)command;

            //write need input
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
                bytes = responseforReading;
                bytes[2] = (byte)channel;
                bytes[3] = (byte)command;

                int value = input;
                byte high = (byte)((value >> 8) & 0xFF);
                byte low = (byte)(value & 0xFF);
                bytes[4] = high;
                bytes[5] = low;

                bytes[6] = CalculateAndAssignSum(bytes, 6);
            }
            else
            {
                bytes = responseforSetting;
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

            // 先截断到 0–255【& 0xFF】，再将高字节左移 8 位【<< 8】，最后或上低字节【|】
            return ((byteArray[0] & 0xFF) << 8) | (byteArray[1] & 0xFF);
        }

        private static bool IsPortInUse(string portName)
        {
            try
            {
                // 尝试打开串口，如果成功则表示串口未被占用
                using (SerialPort port = new SerialPort(portName))
                {
                    port.Open();
                    //port.Close();
                }
            }
            catch (UnauthorizedAccessException)
            {
                //MessageBox.Show("laser"+portName);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking port {portName}: {ex.Message}");
                return true;
            }
            return false;
        }

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
                        _serialPort!.PortName = portName;
                        if (_serialPort.IsOpen || IsPortInUse(portName))
                            continue;

                        _serialPort.Open();

                        //校验
                        var bytes = GenerateSettingBytes(ChannelEnum.ONOFFSettingofLaser1, CommandEnum.Read);//读取第一通道开关状态
                        _serialPort.Write(bytes, 0, bytes.Length);

                        Thread.Sleep(150);

                        int bytesToRead = _serialPort!.BytesToRead;
                        if (bytesToRead > 0)
                        {
                            byte[] bytesRtn = new byte[bytesToRead];
                            _serialPort.Read(bytesRtn, 0, bytesToRead);
                            if (bytesRtn[0] == 65)
                            {
                                //验证通过，关闭当前串口
                                _serialPort.Close();
                                _serialPort.PortName = portName;
                                return true;
                            }
                        }
                        _serialPort.Close();
                        continue;
                    }
                }
                else
                {
                    _serialPort!.PortName = com;
                    if (_serialPort.IsOpen || IsPortInUse(com)) return false;
                    _serialPort.Open();

                    //校验
                    var bytes = GenerateSettingBytes(ChannelEnum.ONOFFSettingofLaser1, CommandEnum.Read);//读取第一通道开关状态
                    _serialPort.Write(bytes, 0, bytes.Length);

                    Thread.Sleep(150);
                    int bytesToRead = _serialPort!.BytesToRead;
                    if (bytesToRead > 0)
                    {
                        byte[] bytesRtn = new byte[bytesToRead];
                        _serialPort.Read(bytesRtn, 0, bytesToRead);
                        if (bytesRtn[0] == 65)
                        {
                            //验证通过，关闭当前串口
                            _serialPort.Close();
                            _serialPort.PortName = com;
                            return true;
                        }
                    }
                    _serialPort.Close();
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Valid Error:{ex.Message}");
                return false;
            }
        }

        enum ChannelEnum
        {
            ONOFFSettingofLaser1 = 0x51,
            ONOFFSettingofLaser2 = 0x52,
            ONOFFSettingofLaser3 = 0x53,

            PercentageCurrentSettingofLaser1 = 0x01,
            PercentageCurrentSettingofLaser2 = 0x02,
            PercentageCurrentSettingofLaser3 = 0x03,
        }

        enum CommandEnum
        {
            Read = 0x00,
            Write = 0x01,
        }
    }

    public partial class CNI
    {
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

        public bool OpenCom(string com = "")
        {
            if (Valid(com))
            {
                _serialPort!.Open();
                return true;
            }
            return false;
        }

        public bool CloseCom()
        {
            if (_serialPort!.IsOpen)
            {
                _serialPort.Close();
                return true;
            }
            return false;
        }

        public bool SetPower(int index, int value)
        {
            ChannelEnum channelEnum;
            switch (index)
            {
                case 1:
                    channelEnum = ChannelEnum.PercentageCurrentSettingofLaser1;
                    break;
                case 2:
                    channelEnum = ChannelEnum.PercentageCurrentSettingofLaser2;
                    break;
                case 3:
                    channelEnum = ChannelEnum.PercentageCurrentSettingofLaser3;
                    break;
                default:
                    return false;
            }
            return SetValue(channelEnum, value);
        }

        public bool GetPower(int index, out int value)
        {
            value = 0;
            ChannelEnum channelEnum;
            switch (index)
            {
                case 1:
                    channelEnum = ChannelEnum.PercentageCurrentSettingofLaser1;
                    break;
                case 2:
                    channelEnum = ChannelEnum.PercentageCurrentSettingofLaser2;
                    break;
                case 3:
                    channelEnum = ChannelEnum.PercentageCurrentSettingofLaser3;
                    break;
                default:
                    return false;
            }
            return GetValue(channelEnum, out value);
        }

        public bool SetStatus(int index, bool status)
        {
            ChannelEnum channelEnum;
            switch (index)
            {
                case 1:
                    channelEnum = ChannelEnum.ONOFFSettingofLaser1;
                    break;
                case 2:
                    channelEnum = ChannelEnum.ONOFFSettingofLaser2;
                    break;
                case 3:
                    channelEnum = ChannelEnum.ONOFFSettingofLaser3;
                    break;
                default:
                    return false;
            }
            return SetValue(channelEnum, Convert.ToInt32(status));
        }

        public bool GetStatue(int index, out bool status)
        {
            status = false;
            ChannelEnum channelEnum;
            switch (index)
            {
                case 1:
                    channelEnum = ChannelEnum.ONOFFSettingofLaser1;
                    break;
                case 2:
                    channelEnum = ChannelEnum.ONOFFSettingofLaser2;
                    break;
                case 3:
                    channelEnum = ChannelEnum.ONOFFSettingofLaser3;
                    break;
                default:
                    return false;
            }
            var res = GetValue(channelEnum, out var value);
            status = Convert.ToBoolean(value);
            return res;
        }

        ~CNI() => CloseCom();

    }
}
