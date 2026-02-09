using Simscop.Hardware.CNI.FourChannel;
using Simscop.Pl.Core.Constants;
using Simscop.Pl.Core.Hardwares.Interfaces;

namespace CNILaser.Core
{
    public class FakeLaser : ILaser
    {
        public Dictionary<InfoEnum, string> InfoDirectory => new()
        {
            {InfoEnum.Model,"CNI"},
            {InfoEnum.Version,"v1.0"},
            { InfoEnum.FrameWork ,""}
        };

        public string DevicesName { get; set; } = "FakeLaser";

        private int power = 44;

        public bool GetPower(int count, out int value)
        {
            value = power;
            return true;
        }

        public bool GetState(int count, out bool status)
        {
            status = true;
            return true;
        }

        public bool Init(string com = "")
        {
            Thread.Sleep(100);
            return true;
        }

        public bool SetPower(int count, int value)
        {
            power = value;
            return true;
        }

        public bool SetState(int count, bool status)
        {
            Console.WriteLine($"{count}_{status} {DateTime.Now:HH-mm-ss-fff}");
            return true;
        }

        public bool GetVerifyValue(int count, out int value)
        {
            value = power;
            return true;
        }

        public async Task<bool> SetStateAsync(int count, bool status)
        {
            return await Task.Run(() => SetState(count, status));
        }

        public async Task<bool> SetPowerAsync(int count, int value)
        {
            return await Task.Run(() => SetPower(count, value));
        }

        public bool GetPowers(out Dictionary<int, int> powers)
        {
            powers = new Dictionary<int, int>() { { 1, 5 }, { 2, 15 }, { 3, 25 }, { 4, 26 } };
            return true;
        }

        public async Task<(bool, Dictionary<int, int>)> GetPowersAsync()
        {
            return await Task.Run(() =>
            {
                var res = GetPowers(out var powers);
                return (res, powers);
            });
        }

        public Task<bool> SetControlModeAsync(int index)
        {
            return Task.FromResult(true);
        }

        public bool SetControlMode(int index)
        {
            return true;
        }

        public bool GetDeviceInfo(out CNI.DeviceInfo? deviceInfo)
        {
            deviceInfo=new CNI.DeviceInfo();
            return true;
        }

    }
}
