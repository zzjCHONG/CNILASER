using CommunityToolkit.Mvvm.ComponentModel;
using Simscop.Pl.Core.Hardwares.Interfaces;
using System.Windows.Threading;

namespace Simscop.Pl.Hardware.CNI.ThreeChannel
{
    public partial class ThreeChannelLaserChannel : ObservableObject, ILaserChannel
    {
        private readonly ThreeChannelLaser _parent;
        private readonly int _index;
        private readonly DispatcherTimer _statusTimer;

        public ThreeChannelLaserChannel(ThreeChannelLaser parent, int index, string name, bool isEnable = true)
        {
            _parent = parent;
            _index = index;
            Name = name;
            IsEnable = isEnable;

            _statusTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();
        }

        public string Name { get; }

        public bool IsEnable { get; }

        // 用户设置的目标功率
        [ObservableProperty]
        private int targetPower;

        // 用户设置的目标状态（用于 UI 绑定）
        [ObservableProperty]
        private bool targetIsOn;

        // 实际功率（每秒轮询更新）
        private int _actualPower;
        public int ActualPower
        {
            get => _actualPower;
            private set => SetProperty(ref _actualPower, value);
        }

        // 实际开关状态（每秒轮询更新）
        private bool _actualIsOn;
        public bool ActualIsOn
        {
            get => _actualIsOn;
            private set => SetProperty(ref _actualIsOn, value);
        }

        private void StatusTimer_Tick(object? sender, EventArgs e)
        {
            if (_parent == null) return;

            if (_parent.GetPower(_index, out int power))
                ActualPower = power;

            if (_parent.GetStatus(_index, out bool isOn))
                ActualIsOn = isOn;
        }

        public bool SetPower(int power)
        {
            if (_parent == null) return false;

            var result = _parent.SetPower(_index, power);
            if (result)
                TargetPower = power; // 仅在设置成功后更新
            return result;
        }

        public bool GetPower(out int power)
        {
            if (_parent == null)
            {
                power = 0;
                return false;
            }

            var result = _parent.GetPower(_index, out power);
            if (result)
                ActualPower = power;
            return result;
        }

        public bool OpenChannel()
        {
            if (_parent == null) return false;

            var result = _parent.SetStatus(_index, true);
            if (result)
                TargetIsOn = true;
            return result;
        }

        public bool CloseChannel()
        {
            if (_parent == null) return false;

            var result = _parent.SetStatus(_index, false);
            if (result)
                TargetIsOn = false;
            return result;
        }
    }
}
