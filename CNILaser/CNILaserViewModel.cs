using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Simscop.Pl.Core.Hardwares.Interfaces;
using System.Diagnostics;
using System.IO.Ports;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace CNILaser
{
    public partial class CNILaserViewModel : ObservableObject
    {
        private readonly ILaser? _laserwaveLaser;
        private readonly System.Timers.Timer? _timerComs;
        private static readonly string? currentPortname;
        private readonly DispatcherTimer? _timer;

        public CNILaserViewModel()
        {
            _laserwaveLaser = Global.ServiceProvider!.GetRequiredService<ILaser>();  

            SerialComs?.AddRange(SerialPort.GetPortNames());
            if (_timerComs == null)
            {
                _timerComs = new System.Timers.Timer(500);
                _timerComs.Elapsed += OnTimedComsEvent!;
                _timerComs.AutoReset = true;
                _timerComs.Enabled = true;
            }

            _timer = new DispatcherTimer(priority: DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(100)};
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            Task.Run(() =>
            {
                if (IsConnected)
                {
                    if (_laserwaveLaser!.GetVerifyValue(1, out var channel1Value)) ChannelThreeChannel1ActualValue = channel1Value;
                    if (_laserwaveLaser.GetVerifyValue(2, out var channel2Value)) ChannelThreeChannel2ActualValue = channel2Value;
                    if (_laserwaveLaser.GetVerifyValue(3, out var channel3Value)) ChannelThreeChannel3ActualValue = channel3Value;
                    if (_laserwaveLaser.GetVerifyValue(4, out var channel4Value)) ChannelThreeChannel4ActualValue = channel4Value;
                }
            });
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOperable))]
        private bool _isConnected = false;

        public bool IsOperable => !IsConnected;

        [ObservableProperty]
        private List<string>? _serialComs = new();

        [ObservableProperty]
        public int _serialIndex = 0;

        private void OnTimedComsEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                var com = SerialPort.GetPortNames();

                bool areEqual = SerialComs?.Count == com.Length
                    && !SerialComs.Except(com).Any() && !com.Except(SerialComs).Any();
                if (!areEqual)
                {
                    SerialComs = new();
                    SerialComs.AddRange(com);
                    if (SerialComs.Count != 0)
                    {
                        if (!string.IsNullOrEmpty(currentPortname) && IsConnected)
                        {
                            int index = SerialComs.IndexOf(currentPortname);
                            SerialIndex = index;
                        }
                        else
                        {
                            SerialIndex = SerialComs.Count - 1;
                        }
                    }

                    if (!SerialComs.Contains(currentPortname!) && !string.IsNullOrEmpty(currentPortname))
                        IsConnected = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("OnTimedComsEvent" + ex.ToString());
            }
        }

        [RelayCommand]
        async Task Init()
        {
            await Task.Run(() =>
            {
                IsConnected = _laserwaveLaser!.Init();
            });

            if (IsConnected)
            {
                await InitSetting();

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"连接成功！", "连接提示", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            else
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"连接失败！", "连接提示", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
        }

        [RelayCommand]
        async Task InitManual()
        {
            var com = SerialComs![SerialIndex];
            await Task.Run(() =>
            {
                IsConnected = _laserwaveLaser!.Init(com);
            });

            if (IsConnected)
            {
                await InitSetting();

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"连接成功！", "连接提示", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            else
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"连接失败！", "连接提示", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
        }

        async Task InitSetting()
        {
            if (IsConnected)
            {
                await _laserwaveLaser!.SetStateAsync(1, false);
                await _laserwaveLaser.SetStateAsync(2, false);
                await _laserwaveLaser.SetStateAsync(3, false);
                await _laserwaveLaser.SetStateAsync(4, false);

                var (res, col) = await _laserwaveLaser.GetPowersAsync();
                if (res)
                {
                    //ChannelThreeChannel1ActualValue = col[1];
                    //ChannelThreeChannel2ActualValue = col[2];
                    //ChannelThreeChannel3ActualValue = col[3];
                    //ChannelThreeChannel4ActualValue = col[4];

                    ChannelThreeChannel1Value = col[1];
                    ChannelThreeChannel2Value = col[2];
                    ChannelThreeChannel3Value = col[3];
                    ChannelThreeChannel4Value = col[4];
                }
            }
        }

        /// <summary>
        /// 返回是否已全部关闭
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CloserAllLaserChannel()
        {
            if (ChannelThreeChannel1Enable) await _laserwaveLaser!.SetStateAsync(1, false);
            if (ChannelThreeChannel2Enable) await _laserwaveLaser!.SetStateAsync(2, false);
            if (ChannelThreeChannel3Enable) await _laserwaveLaser!.SetStateAsync(3, false);
            if (ChannelThreeChannel4Enable) await _laserwaveLaser!.SetStateAsync(4, false);

            var res1 = !ChannelThreeChannel1Enable || await _laserwaveLaser!.SetStateAsync(1, false);
            var res2 = !ChannelThreeChannel2Enable || await _laserwaveLaser!.SetStateAsync(2, false);
            var res3 = !ChannelThreeChannel3Enable || await _laserwaveLaser!.SetStateAsync(3, false);
            var res4 = !ChannelThreeChannel4Enable || await _laserwaveLaser!.SetStateAsync(4, false);

            return res1 && res2 && res3 && res4;
        }
    }

    public partial class CNILaserViewModel
    {
        [ObservableProperty]
        private List<string>? _controlMode = new() { "外部控制", "内部控制" };

        [ObservableProperty]
        public int _controlModeIndex = 0;

        partial void OnControlModeIndexChanged(int value)
        {
            _laserwaveLaser!.SetControlModeAsync(value);
        }

        [ObservableProperty]
        private int channelThreeChannel1ActualValue = 0;

        [ObservableProperty]
        private int channelThreeChannel2ActualValue = 0;

        [ObservableProperty]
        private int _channelThreeChannel3ActualValue = 0;

        [ObservableProperty]
        private int _channelThreeChannel4ActualValue = 0;

        [ObservableProperty]
        private string _channelThreeChannel1Name = "405nm";

        [ObservableProperty]
        private string _channelThreeChannel2Name = "488nm";

        [ObservableProperty]
        private string _channelThreeChannel3Name = "532nm";

        [ObservableProperty]
        private string _channelThreeChannel4Name = "640nm";

        [ObservableProperty]
        private int _channelThreeChannel1Value = 22;

        [ObservableProperty]
        private int _channelThreeChannel2Value = 33;

        [ObservableProperty]
        private int _channelThreeChannel3Value = 44;

        [ObservableProperty]
        private int _channelThreeChannel4Value = 55;

        [ObservableProperty]
        private bool _channelThreeChannel1Enable = false;

        [ObservableProperty]
        private bool _channelThreeChannel2Enable = false;

        [ObservableProperty]
        private bool _channelThreeChannel3Enable = false;

        [ObservableProperty]
        private bool _channelThreeChannel4Enable = false;

        [RelayCommand]
        private async Task IncreaseChannelThreeChannel1()
        {
            var value = Math.Min(ChannelThreeChannel1Value + 1, 100);
            var res = await _laserwaveLaser!.SetPowerAsync(1, value);
            if (!res) Console.WriteLine("Laser1 SetPower Error!");
            ChannelThreeChannel1Value = value;
        }

        [RelayCommand]
        private async Task DecreaseChannelThreeChannel1()
        {
            var value = Math.Max(ChannelThreeChannel1Value - 1, 0);
            var res = await _laserwaveLaser!.SetPowerAsync(1, value);
            if (!res) Console.WriteLine("Laser1 SetPower Error!");
            ChannelThreeChannel1Value = value;
        }

        [RelayCommand]
        private async Task IncreaseChannelThreeChannel2()
        {
            var value = Math.Min(ChannelThreeChannel2Value + 1, 100);
            var res = await _laserwaveLaser!.SetPowerAsync(2, value);
            if (!res) Console.WriteLine("Laser2 SetPower Error!");
            ChannelThreeChannel2Value = value;
        }

        [RelayCommand]
        private async Task DecreaseChannelThreeChannel2()
        {
            var value = Math.Max(ChannelThreeChannel2Value - 1, 0);
            var res = await _laserwaveLaser!.SetPowerAsync(2, value);
            if (!res) Console.WriteLine("Laser2 SetPower Error!");
            ChannelThreeChannel2Value = value;
        }

        [RelayCommand]
        private async Task IncreaseChannelThreeChannel3()
        {
            var value = Math.Min(ChannelThreeChannel3Value + 1, 100);
            var res = await _laserwaveLaser!.SetPowerAsync(3, value);
            if (!res) Console.WriteLine("Laser3 SetPower Error!");
            ChannelThreeChannel3Value = value;
        }

        [RelayCommand]
        private async Task DecreaseChannelThreeChannel3()
        {
            var value = Math.Max(ChannelThreeChannel3Value - 1, 0);
            var res = await _laserwaveLaser!.SetPowerAsync(3, value);
            if (!res) Console.WriteLine("Laser3 SetPower Error!");
            ChannelThreeChannel3Value = value;
        }

        [RelayCommand]
        private async Task IncreaseChannelThreeChannel4()
        {
            var value = Math.Min(ChannelThreeChannel4Value + 1, 100);
            var res = await _laserwaveLaser!.SetPowerAsync(4, value);
            if (!res) Console.WriteLine("Laser4 SetPower Error!");
            ChannelThreeChannel4Value = value;
        }

        [RelayCommand]
        private async Task DecreaseChannelThreeChannel4()
        {
            var value = Math.Max(ChannelThreeChannel4Value - 1, 0);
            var res = await _laserwaveLaser!.SetPowerAsync(4, value);
            if (!res) Console.WriteLine("Laser4 SetPower Error!");
            ChannelThreeChannel4Value = value;
        }

        [RelayCommand]
        private async Task<bool> SetChannelFirstStatusAsync()
        {
            if (ChannelThreeChannel1Enable)
            {
                if (ChannelThreeChannel2Enable)
                {
                    if (!await _laserwaveLaser!.SetStateAsync(2, false))
                    {
                        Console.WriteLine("Laser2 SetStatus Error!");
                        return false;
                    }
                    ChannelThreeChannel2Enable = false;
                }

                if (ChannelThreeChannel3Enable)
                {
                    if (!await _laserwaveLaser!.SetStateAsync(3, false))
                    {
                        Console.WriteLine("Laser3 SetStatus Error!");
                        return false;
                    }

                    ChannelThreeChannel3Enable = false;
                }
                if (ChannelThreeChannel4Enable)
                {
                    if (!await _laserwaveLaser!.SetStateAsync(4, false))
                    {
                        Console.WriteLine("Laser4 SetStatus Error!");
                        return false;
                    }

                    ChannelThreeChannel4Enable = false;
                }
            }

            if (!await _laserwaveLaser!.SetStateAsync(1, ChannelThreeChannel1Enable))
            {
                Console.WriteLine("Laser1 SetStatus Error!");
                return false;
            }

            return true;
        }

        [RelayCommand]
        private async Task<bool> SetChannelSecondStatusAsync()
        {
            if (ChannelThreeChannel2Enable)
            {
                if (ChannelThreeChannel1Enable)
                {
                    if (!await _laserwaveLaser!.SetStateAsync(1, false))
                    {
                        Console.WriteLine("Laser1 SetStatus Error!");
                        return false;
                    }
                    ChannelThreeChannel1Enable = false;
                }

                if (ChannelThreeChannel3Enable)
                {
                    if (!await _laserwaveLaser!.SetStateAsync(3, false))
                    {
                        Console.WriteLine("Laser3 SetStatus Error!");
                        return false;
                    }
                    ChannelThreeChannel3Enable = false;
                }

                if (ChannelThreeChannel4Enable)
                {
                    if (!await _laserwaveLaser!.SetStateAsync(4, false))
                    {
                        Console.WriteLine("Laser4 SetStatus Error!");
                        return false;
                    }

                    ChannelThreeChannel4Enable = false;
                }
            }

            if (!await _laserwaveLaser!.SetStateAsync(2, ChannelThreeChannel2Enable))
            {
                Console.WriteLine("Laser2 SetStatus Error!");
                return false;
            }

            return true;

        }

        [RelayCommand]
        private async Task<bool> SetChannelThirdStatusAsync()
        {
            if (ChannelThreeChannel3Enable)
            {
                if (ChannelThreeChannel1Enable)
                {
                    if (!await _laserwaveLaser!.SetStateAsync(1, false))
                    {
                        Console.WriteLine("Laser1 SetStatus Error!");
                        return false;
                    }
                    ChannelThreeChannel1Enable = false;
                }

                if (ChannelThreeChannel2Enable)
                {
                    if (!await _laserwaveLaser!.SetStateAsync(2, false))
                    {
                        Console.WriteLine("Laser2 SetStatus Error!");
                        return false;
                    }
                    ChannelThreeChannel2Enable = false;
                }

                if (ChannelThreeChannel4Enable)
                {
                    if (!await _laserwaveLaser!.SetStateAsync(4, false))
                    {
                        Console.WriteLine("Laser4 SetStatus Error!");
                        return false;
                    }

                    ChannelThreeChannel4Enable = false;
                }
            }

            if (!await _laserwaveLaser!.SetStateAsync(3, ChannelThreeChannel3Enable))
            {
                Console.WriteLine("Laser3 SetStatus Error!");
                return false;
            }

            return true;
        }

        [RelayCommand]
        private async Task<bool> SetChannelFourthStatusAsync()
        {
            if (ChannelThreeChannel4Enable)
            {
                if (ChannelThreeChannel1Enable)
                {
                    if (!await _laserwaveLaser!.SetStateAsync(1, false))
                    {
                        Console.WriteLine("Laser1 SetStatus Error!");
                        return false;
                    }
                    ChannelThreeChannel1Enable = false;
                }

                if (ChannelThreeChannel2Enable)
                {
                    if (!await _laserwaveLaser!.SetStateAsync(2, false))
                    {
                        Console.WriteLine("Laser2 SetStatus Error!");
                        return false;
                    }
                    ChannelThreeChannel2Enable = false;
                }

                if (ChannelThreeChannel3Enable)
                {
                    if (!await _laserwaveLaser!.SetStateAsync(3, false))
                    {
                        Console.WriteLine("Laser3 SetStatus Error!");
                        return false;
                    }
                    ChannelThreeChannel3Enable = false;
                }
            }

            if (!await _laserwaveLaser!.SetStateAsync(4, ChannelThreeChannel4Enable))
            {
                Console.WriteLine("Laser4 SetStatus Error!");
                return false;
            }

            return true;
        }

        private bool _isSettingPower = false;

        async partial void OnChannelThreeChannel1ValueChanged(int value)
        {
            if (_isSettingPower) return; // 上一次还没执行完，直接丢弃
            _isSettingPower = true;

            try
            {
                _laserwaveLaser!.GetVerifyValue(1, out var actualValue);
                if (value != actualValue)
                {
                    var res = await _laserwaveLaser.SetPowerAsync(1, value);
                    if (!res)
                        Console.WriteLine("Laser1 SetPower Error!");
                }
            }
            finally
            {
                _isSettingPower = false;
            }
        }

        async partial void OnChannelThreeChannel2ValueChanged(int value)
        {
            if (_isSettingPower) return; // 上一次还没执行完，直接丢弃
            _isSettingPower = true;

            try
            {
                _laserwaveLaser!.GetVerifyValue(2, out var actualValue);
                if (value != actualValue)
                {
                    var res = await _laserwaveLaser.SetPowerAsync(2, value);
                    if (!res)
                        Console.WriteLine("Laser2 SetPower Error!");
                }
            }
            finally
            {
                _isSettingPower = false;
            }
        }

        async partial void OnChannelThreeChannel3ValueChanged(int value)
        {
            if (_isSettingPower) return; // 上一次还没执行完，直接丢弃
            _isSettingPower = true;

            try
            {
                _laserwaveLaser!.GetVerifyValue(3, out var actualValue);
                if (value != actualValue)
                {
                    var res = await _laserwaveLaser.SetPowerAsync(3, value);
                    if (!res)
                        Console.WriteLine("Laser3 SetPower Error!");
                }
            }
            finally
            {
                _isSettingPower = false;
            }
        }

        async partial void OnChannelThreeChannel4ValueChanged(int value)
        {
            if (_isSettingPower) return; // 上一次还没执行完，直接丢弃
            _isSettingPower = true;

            try
            {
                _laserwaveLaser!.GetVerifyValue(4, out var actualValue);
                if (value != actualValue)
                {
                    var res = await _laserwaveLaser.SetPowerAsync(4, value);
                    if (!res)
                        Console.WriteLine("Laser4 SetPower Error!");
                }
            }
            finally
            {
                _isSettingPower = false;
            }
        }
    }

}



