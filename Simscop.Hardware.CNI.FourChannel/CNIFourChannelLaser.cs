using Simscop.Pl.Core.Constants;
using Simscop.Pl.Core.Hardwares.Interfaces;

namespace Simscop.Hardware.CNI.FourChannel
{
    public class CNIFourChannelLaser : ILaser
    {
        private readonly CNI _cni;
        private const int MaxChannels = 4;

        public Dictionary<InfoEnum, string> InfoDirectory { get; private set; }
        public string DevicesName { get; set; } = "CNI Four Channel Laser";

        public string? CurrentPortname { get; set; }

        public CNIFourChannelLaser()
        {
            _cni = new CNI();
            InfoDirectory = new Dictionary<InfoEnum, string>();
        }

        /// <summary>
        /// 初始化设备
        /// </summary>
        /// <param name="com">不输入为自动查找；输入则对应开启通道使用</param>
        /// <returns></returns>
        public bool Init(string com = "")
        {
            try
            {
                if (!_cni.Connect(out var currentCom, com))
                {
                    Console.WriteLine("CNI Laser initialization failed");
                    return false;
                }

                CurrentPortname= currentCom;

                // 填充基本设备信息到 InfoDirectory
                InfoDirectory[InfoEnum.Model] = "CNI Four Channel Laser";
                InfoDirectory[InfoEnum.Version] = "1.0.0";

                if (_cni.ReadDeviceInfo(out var info) && info != null)
                {
                    Console.WriteLine($"  Laser1: {(info.Laser1OnOff ? "ON " : "OFF")} | Current: {info.Laser1Current,4} mA | Temp: {info.Laser1Temperature,3}°C");
                    Console.WriteLine($"  Laser2: {(info.Laser2OnOff ? "ON " : "OFF")} | Current: {info.Laser2Current,4} mA | Temp: {info.Laser2Temperature,3}°C");
                    Console.WriteLine($"  Laser4: {(info.Laser4OnOff ? "ON " : "OFF")} | Current: {info.Laser4Current,4} mA | Temp: {info.Laser4Temperature,3}°C");
                    Console.WriteLine($"  Preheat: {info.PreheatState}");
                    Console.WriteLine($"  Interlock: {(info.InterlockError ? "ERROR" : "OK")}");
                    Console.WriteLine($"  E-Stop: {(info.EstopError ? "TRIGGERED" : "OK")}");
                    Console.WriteLine($"  Keys: {info.KeyState}");
                }

                Console.WriteLine($"CNI Laser initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Init Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置激光器某一通道的开关
        /// </summary>
        /// <param name="count">从1开始</param>
        /// <param name="state">true为开启</param>
        /// <returns></returns>
        public bool SetState(int count, bool state)
        {
            if (count < 1 || count > MaxChannels)
            {
                Console.WriteLine($"Invalid channel: {count}, must be 1-{MaxChannels}");
                return false;
            }

            return _cni.SetStatus(count, state);
        }

        /// <summary>
        /// 获取激光器某一通道的开关状态
        /// </summary>
        /// <param name="count">从1开始</param>
        /// <param name="state">true为开启</param>
        /// <returns></returns>
        public bool GetState(int count, out bool state)
        {
            state = false;
            if (count < 1 || count > MaxChannels)
            {
                Console.WriteLine($"Invalid channel: {count}, must be 1-{MaxChannels}");
                return false;
            }

            return _cni.GetStatus(count, out state);
        }

        /// <summary>
        /// 设置激光器功率
        /// </summary>
        /// <param name="count">激光通道个数，从1开始</param>
        /// <param name="power">0 - 100 这个是强度百分比</param>
        /// <returns></returns>
        public bool SetPower(int count, int power)
        {
            if (count < 1 || count > MaxChannels)
            {
                Console.WriteLine($"Invalid channel: {count}, must be 1-{MaxChannels}");
                return false;
            }

            if (power < 0 || power > 100)
            {
                Console.WriteLine($"Invalid power: {power}, must be 0-100");
                return false;
            }

            return _cni.SetPower(count, power);
        }

        /// <summary>
        /// 获取功率
        /// </summary>
        /// <param name="count">单通道</param>
        /// <param name="power">功率</param>
        /// <returns></returns>
        public bool GetPower(int count, out int power)
        {
            power = 0;
            if (count < 1 || count > MaxChannels)
            {
                Console.WriteLine($"Invalid channel: {count}, must be 1-{MaxChannels}");
                return false;
            }

            return _cni.GetPower(count, out power);
        }

        /// <summary>
        /// 获取功率
        /// 一次性获取所有通道功率
        /// </summary>
        /// <param name="powers">key为通道，从1开始；value为对应power</param>
        /// <returns></returns>
        public bool GetPowers(out Dictionary<int, int> powers)
        {
            powers = new Dictionary<int, int>();
            bool allSuccess = true;

            for (int i = 1; i <= MaxChannels; i++)
            {
                if (_cni.GetPower(i, out int power))
                {
                    powers[i] = power;
                }
                else
                {
                    powers[i] = 0;
                    allSuccess = false;
                    Console.WriteLine($"Failed to get power for channel {i}");
                }
            }

            return allSuccess;
        }

        /// <summary>
        /// 获取已校验的设置功率
        /// </summary>
        /// <param name="count">从1开始</param>
        /// <param name="value">设置时返回的功率数值</param>
        /// <returns></returns>
        public bool GetVerifyValue(int count, out int value)
        {
            value = -1;
            if (count < 1 || count > MaxChannels)
            {
                Console.WriteLine($"Invalid channel: {count}, must be 1-{MaxChannels}");
                return false;
            }

            return _cni.GetVerifyValue(count, out value);
        }

        /// <summary>
        /// 设置开关状态
        /// 异步等待直至校验完成
        /// </summary>
        /// <param name="count">从1开始</param>
        /// <param name="state">true为开启</param>
        /// <returns></returns>
        public async Task<bool> SetStateAsync(int count, bool state)
        {
            if (count < 1 || count > MaxChannels)
            {
                Console.WriteLine($"Invalid channel: {count}, must be 1-{MaxChannels}");
                return false;
            }

            return await _cni.SetStatusAsync(count, state);
        }

        /// <summary>
        /// 设置激光功率
        /// 异步等待直至校验完成
        /// </summary>
        /// <param name="count">从1开始</param>
        /// <param name="power">1~100，百分比</param>
        /// <returns></returns>
        public async Task<bool> SetPowerAsync(int count, int power)
        {
            if (count < 1 || count > MaxChannels)
            {
                Console.WriteLine($"Invalid channel: {count}, must be 1-{MaxChannels}");
                return false;
            }

            if (power < 0 || power > 100)
            {
                Console.WriteLine($"Invalid power: {power}, must be 0-100");
                return false;
            }

            return await _cni.SetPowerAsync(count, power);
        }

        /// <summary>
        /// 获取所有激光功率
        /// 异步等待直至校验完成
        /// </summary>
        /// <returns>Dictionary<int, int>，返回：通道数，功率</returns>
        public async Task<(bool, Dictionary<int, int>)> GetPowersAsync()
        {
            var powers = new Dictionary<int, int>();
            bool allSuccess = true;

            for (int i = 1; i <= MaxChannels; i++)
            {
                var (success, power) = await _cni.GetPowerAsync(i);
                if (success)
                {
                    powers[i] = power;
                }
                else
                {
                    powers[i] = 0;
                    allSuccess = false;
                    Console.WriteLine($"Failed to get power for channel {i}");
                }
            }

            return (allSuccess, powers);
        }

        /// <summary>
        /// 异步设置激光模式
        /// </summary>
        /// <param name="index">0: 外部控制, 1: 内部控制</param>
        /// <returns></returns>
        public async Task<bool> SetControlModeAsync(int index)
        {
            if (index < 0 || index > 1)
            {
                Console.WriteLine($"Invalid control mode index: {index}, must be 0 (External) or 1 (Internal)");
                return false;
            }

            var mode = (CNI.CurrentControlMode)index;
            return await _cni.SetCurrentControlModeAsync(mode);
        }

        /// <summary>
        /// 设置激光模式
        /// </summary>
        /// <param name="index">0: 外部控制, 1: 内部控制</param>
        /// <returns></returns>
        public bool SetControlMode(int index)
        {
            if (index < 0 || index > 1)
            {
                Console.WriteLine($"Invalid control mode index: {index}, must be 0 (External) or 1 (Internal)");
                return false;
            }

            var mode = (CNI.CurrentControlMode)index;
            return _cni.SetCurrentControlMode(mode);
        }

        /// <summary>
        /// 获取当前控制模式
        /// </summary>
        /// <returns></returns>
        public bool GetControlMode(out int modeIndex)
        {
            modeIndex = 0;
            if (_cni.GetCurrentControlMode(out var mode))
            {
                modeIndex = (int)mode;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 异步获取当前控制模式
        /// </summary>
        /// <returns></returns>
        public async Task<(bool success, int modeIndex)> GetControlModeAsync()
        {
            var (success, mode) = await _cni.GetCurrentControlModeAsync();
            return (success, (int)mode);
        }

        /// <summary>
        /// 获取设备详细信息
        /// </summary>
        /// <returns></returns>
        public bool GetDeviceInfo(out CNI.DeviceInfo? deviceInfo)
        {
            return _cni.ReadDeviceInfo(out deviceInfo);
        }

        /// <summary>
        /// 异步获取设备详细信息
        /// </summary>
        /// <returns></returns>
        public async Task<(bool success, CNI.DeviceInfo? deviceInfo)> GetDeviceInfoAsync()
        {
            return await _cni.ReadDeviceInfoAsync();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _cni?.Dispose();
        }

    }
}