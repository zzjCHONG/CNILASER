using Simscop.Pl.Core.Constants;
using Simscop.Pl.Core.Hardwares.Interfaces;

namespace Simscop.Pl.Hardware.CNI.ThreeChannel
{
    public class ThreeChannelLaser : ILaser
    {
        public string DevicesName { get; set; } = "ThreeChannel";

        private readonly CNI _CNI;

        public ThreeChannelLaser()
        {
            _CNI = new CNI();
        }

        public Dictionary<InfoEnum, string> InfoDirectory => new()
        {
            {InfoEnum.Model,"CNI-ThreeChnnel"},
            {InfoEnum.Version,"v1.00"}
        };

        public bool GetPower(int count, out int value)
            => _CNI.GetPower(count, out value);

        public bool GetStatus(int count, out bool status)
            => _CNI.GetStatue(count, out status);

        public bool Init()
            => _CNI.OpenCom();

        public bool SetPower(int count, int value)
            => _CNI.SetPower(count, value);

        public bool SetStatus(int count, bool status)
            => _CNI.SetStatus(count, status);

        public bool GetVerifyValue(int count, out int value)
        {
            var res = -1;
            switch (count)
            {
                case 1:
                    res = _CNI.VerifyLaserChannel1;
                    break;
                case 2:
                    res = _CNI.VerifyLaserChannel2;
                    break;
                case 3:
                    res = _CNI.VerifyLaserChannel3;
                    break;
                default:
                    break;
            }

            value = res;
            return true;
        }

        public async Task<bool> SetStatusAsync(int count, bool status)
        {
            return await Task.Run(() => SetStatus(count, status));
        }

        public async Task<bool> SetPowerAsync(int count, int value)
        {
            return await Task.Run(() => SetPower(count, value));
        }

        public bool Init(string com = "")
        {
            throw new NotImplementedException();
        }

        public bool SetState(int count, bool state)
        {
            throw new NotImplementedException();
        }

        public bool GetState(int count, out bool state)
        {
            throw new NotImplementedException();
        }

        public bool GetPowers(out Dictionary<int, int> powers)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetStateAsync(int count, bool state)
        {
            throw new NotImplementedException();
        }

        public Task<(bool, Dictionary<int, int>)> GetPowersAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetControlModeAsync(int index)
        {
            throw new NotImplementedException();
        }

        public bool SetControlMode(int index)
        {
            throw new NotImplementedException();
        }

        ~ThreeChannelLaser()
            => _CNI!.CloseCom();
    }
}
