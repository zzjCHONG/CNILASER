using Simscop.Hardware.CNI.FourChannel;
using Simscop.Pl.Core.Constants;

namespace Simscop.Pl.Core.Hardwares.Interfaces
{
    public interface ILaser
    {
        /// <summary>
        /// 设备基本属性字典，比如 model，serialNumber ，FirmwareVersion等
        /// </summary>
        public Dictionary<InfoEnum, string> InfoDirectory { get; }

        /// <summary>
        /// 设备自定义名称
        /// </summary>
        public string DevicesName { get; set; }

        /// <summary>
        /// 当前已连接的串口名称
        /// </summary>
        public string CurrentPortname{ get;  }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="com">不输入为自动查找；输入则对应开启通道使用</param>
        /// <returns></returns>
        public bool Init(string com = "");

        /// <summary>
        /// 设置激光器某一通道的开关
        /// </summary>
        /// <param name="count">从1开始</param>
        /// <param name="state">true为开启</param>
        /// <returns></returns>
        public bool SetState(int count, bool state);

        /// <summary>
        /// 获取激光器某一通道的开关状态
        /// </summary>
        /// <param name="count">从1开始</param>
        /// <param name="state">true为开启</param>
        /// <returns></returns>
        public bool GetState(int count, out bool state);

        /// <summary>
        /// 设置激光器功率
        /// </summary>
        /// <param name="count">
        /// 激光通道个数
        /// </param>
        /// <param name="power">
        /// 0 - 100 这个是强度百分比
        /// </param>
        /// <returns></returns>
        public bool SetPower(int count, int power);

        /// <summary>
        /// 获取功率
        /// </summary>
        /// <param name="count">单通道</param>
        /// <param name="power">功率</param>
        /// <returns></returns>
        public bool GetPower(int count, out int power);

        /// <summary>
        /// 获取功率
        /// 一次性获取所有通道功率
        /// </summary>
        /// <param name="powers">key为通道，从1开始；value为对应power</param>
        /// <returns></returns>
        public bool GetPowers(out Dictionary<int, int> powers);

        /// <summary>
        /// 获取已校验的设置功率
        /// </summary>
        /// <param name="count">从1开始</param>
        /// <param name="value">设置时返回的功率数值</param>
        /// <returns></returns>
        public bool GetVerifyValue(int count, out int value);

        /// <summary>
        /// 设置开关状态
        /// 异步等待直至校验完成
        /// </summary>
        /// <param name="count">从1开始</param>
        /// <param name="state">true为开启</param>
        /// <returns></returns>
        public Task<bool> SetStateAsync(int count, bool state);

        /// <summary>
        /// 设置激光功率
        /// 异步等待直至校验完成
        /// </summary>
        /// <param name="count">从1开始</param>
        /// <param name="power">1~100,，百分比</param>
        /// <returns></returns>
        public Task<bool> SetPowerAsync(int count, int power);

        /// <summary>
        /// 获取所有激光功率
        /// 异步等待直至校验完成
        /// </summary>
        /// <returns>Dictionary<int, int>，返回：通道数，功率</returns>
        public Task<(bool, Dictionary<int, int>)> GetPowersAsync();

        /// <summary>
        /// 异步设置激光模式
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Task<bool> SetControlModeAsync(int index);

        /// <summary>
        /// 设置激光模式
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool SetControlMode(int index);

        /// <summary>
        /// 获取激光模式
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool GetControlMode(out int index);

        /// <summary>
        /// 获取激光设备信息
        /// </summary>
        /// <param name="deviceInfo"></param>
        /// <returns></returns>
        public bool GetDeviceInfo(out CNI.DeviceInfo? deviceInfo);

    }
}
