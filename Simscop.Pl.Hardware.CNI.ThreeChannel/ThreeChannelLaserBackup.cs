using Simscop.Pl.Core.Constants;
using Simscop.Pl.Core.Hardwares.Interfaces;

namespace Simscop.Pl.Hardware.CNI.ThreeChannel
{
    public class ThreeChannelLaser : ILaserBackup
    {
        private readonly CNI? _laser;
        private readonly List<ILaserChannel>? _channels;

        public ThreeChannelLaser()
        {
            _laser = new CNI();
            _channels = new List<ILaserChannel>
            {
                new ThreeChannelLaserChannel(this, 0, "405nm"),
                new ThreeChannelLaserChannel(this, 1, "488nm"),
                new ThreeChannelLaserChannel(this, 2, "561nm"),
            };
        }

        public IReadOnlyList<ILaserChannel> Channels => _channels!;

        public string DevicesName => "ThreeChannelLaser";

        public bool Init()=> _laser!.OpenCom();

        public Dictionary<InfoEnum, string> InfoDirectory => new()
        {
            {InfoEnum.Model,"CNI-ThreeChnnel"},
            {InfoEnum.Version,"v1.00"}
        };

        public bool GetPower(int count, out int value)
            => _laser!.GetPower(count, out value);

        public bool GetStatus(int count, out bool status)
            => _laser!.GetStatue(count, out status);

        public bool SetPower(int count, int value)
            => _laser!.SetPower(count, value);

        public bool SetStatus(int count, bool status)
            => _laser!.SetStatus(count, status);

        ~ThreeChannelLaser()
            => _laser!.CloseCom();
    }
}
