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
        private readonly ILaser? _CNILaser;
        private readonly System.Timers.Timer? _timerComs;
        private static readonly string? currentPortname;
        private readonly DispatcherTimer? _timer;
        private readonly DispatcherTimer? _timerState;

        public CNILaserViewModel()
        {
            _CNILaser = Global.ServiceProvider!.GetRequiredService<ILaser>();  

            SerialComs?.AddRange(SerialPort.GetPortNames());
            if (_timerComs == null)
            {
                _timerComs = new System.Timers.Timer(500);
                _timerComs.Elapsed += OnTimedComsEvent!;
                _timerComs.AutoReset = true;
                _timerComs.Enabled = true;
            }

            _timer = new DispatcherTimer(priority: DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(100) };
            _timer.Tick += Timer_Tick;

            _timerState = new DispatcherTimer(priority: DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(500) };
            _timerState.Tick += TimerState_Tick;
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
                IsConnected = _CNILaser!.Init();
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
                IsConnected = _CNILaser!.Init(com);
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
                await _CNILaser!.SetStateAsync(1, false);
                await _CNILaser.SetStateAsync(2, false);
                await _CNILaser.SetStateAsync(3, false);
                await _CNILaser.SetStateAsync(4, false);

                var (res, col) = await _CNILaser.GetPowersAsync();
                if (res)
                {
                    //LaserChannel1ActualValue = col[1];
                    //LaserChannel2ActualValue = col[2];
                    //LaserChannel3ActualValue = col[3];
                    //LaserChannel4ActualValue = col[4];

                    LaserChannel1Value = col[1];
                    LaserChannel2Value = col[2];
                    LaserChannel3Value = col[3];
                    LaserChannel4Value = col[4];

                    _timer!.Start();
                    _timerState!.Start();

                    RefreshStateInfo();
                }
            }
        }

        /// <summary>
        /// 返回是否已全部关闭
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CloserAllLaserChannel()
        {
            if (LaserChannel1Enable) await _CNILaser!.SetStateAsync(1, false);
            if (LaserChannel2Enable) await _CNILaser!.SetStateAsync(2, false);
            if (LaserChannel3Enable) await _CNILaser!.SetStateAsync(3, false);
            if (LaserChannel4Enable) await _CNILaser!.SetStateAsync(4, false);

            var res1 = !LaserChannel1Enable || await _CNILaser!.SetStateAsync(1, false);
            var res2 = !LaserChannel2Enable || await _CNILaser!.SetStateAsync(2, false);
            var res3 = !LaserChannel3Enable || await _CNILaser!.SetStateAsync(3, false);
            var res4 = !LaserChannel4Enable || await _CNILaser!.SetStateAsync(4, false);

            return res1 && res2 && res3 && res4;
        }
    }

    public partial class CNILaserViewModel
    {
        private void Timer_Tick(object? sender, EventArgs e)
        {
            Task.Run(() =>
            {
                if (IsConnected)
                {
                    if (_CNILaser!.GetVerifyValue(1, out var channel1Value)) LaserChannel1ActualValue = channel1Value;
                    if (_CNILaser.GetVerifyValue(2, out var channel2Value)) LaserChannel2ActualValue = channel2Value;
                    if (_CNILaser.GetVerifyValue(3, out var channel3Value)) LaserChannel3ActualValue = channel3Value;
                    if (_CNILaser.GetVerifyValue(4, out var channel4Value)) LaserChannel4ActualValue = channel4Value;
                }
            });
        }

        [ObservableProperty]
        private List<string>? _controlMode = new() { "外部控制", "内部控制" };

        [ObservableProperty]
        public int _controlModeIndex = 0;

        partial void OnControlModeIndexChanged(int value)
        {
            _CNILaser!.SetControlModeAsync(value);
        }

        [ObservableProperty]
        private int laserChannel1ActualValue = 0;

        [ObservableProperty]
        private int laserChannel2ActualValue = 0;

        [ObservableProperty]
        private int _laserChannel3ActualValue = 0;

        [ObservableProperty]
        private int _laserChannel4ActualValue = 0;

        [ObservableProperty]
        private string _laserChannel1Name = "405nm";

        [ObservableProperty]
        private string _laserChannel2Name = "488nm";

        [ObservableProperty]
        private string _laserChannel3Name = "532nm";

        [ObservableProperty]
        private string _laserChannel4Name = "640nm";

        [ObservableProperty]
        private int _laserChannel1Value = 22;

        [ObservableProperty]
        private int _laserChannel2Value = 33;

        [ObservableProperty]
        private int _laserChannel3Value = 44;

        [ObservableProperty]
        private int _laserChannel4Value = 55;

        [ObservableProperty]
        private bool _laserChannel1Enable = false;

        [ObservableProperty]
        private bool _laserChannel2Enable = false;

        [ObservableProperty]
        private bool _laserChannel3Enable = false;

        [ObservableProperty]
        private bool _laserChannel4Enable = false;

        [RelayCommand]
        private async Task IncreaseLaserChannel1()
        {
            var value = Math.Min(LaserChannel1Value + 1, 100);
            var res = await _CNILaser!.SetPowerAsync(1, value);
            if (!res) Console.WriteLine("Laser1 SetPower Error!");
            LaserChannel1Value = value;
        }

        [RelayCommand]
        private async Task DecreaseLaserChannel1()
        {
            var value = Math.Max(LaserChannel1Value - 1, 0);
            var res = await _CNILaser!.SetPowerAsync(1, value);
            if (!res) Console.WriteLine("Laser1 SetPower Error!");
            LaserChannel1Value = value;
        }

        [RelayCommand]
        private async Task IncreaseLaserChannel2()
        {
            var value = Math.Min(LaserChannel2Value + 1, 100);
            var res = await _CNILaser!.SetPowerAsync(2, value);
            if (!res) Console.WriteLine("Laser2 SetPower Error!");
            LaserChannel2Value = value;
        }

        [RelayCommand]
        private async Task DecreaseLaserChannel2()
        {
            var value = Math.Max(LaserChannel2Value - 1, 0);
            var res = await _CNILaser!.SetPowerAsync(2, value);
            if (!res) Console.WriteLine("Laser2 SetPower Error!");
            LaserChannel2Value = value;
        }

        [RelayCommand]
        private async Task IncreaseLaserChannel3()
        {
            var value = Math.Min(LaserChannel3Value + 1, 100);
            var res = await _CNILaser!.SetPowerAsync(3, value);
            if (!res) Console.WriteLine("Laser3 SetPower Error!");
            LaserChannel3Value = value;
        }

        [RelayCommand]
        private async Task DecreaseLaserChannel3()
        {
            var value = Math.Max(LaserChannel3Value - 1, 0);
            var res = await _CNILaser!.SetPowerAsync(3, value);
            if (!res) Console.WriteLine("Laser3 SetPower Error!");
            LaserChannel3Value = value;
        }

        [RelayCommand]
        private async Task IncreaseLaserChannel4()
        {
            var value = Math.Min(LaserChannel4Value + 1, 100);
            var res = await _CNILaser!.SetPowerAsync(4, value);
            if (!res) Console.WriteLine("Laser4 SetPower Error!");
            LaserChannel4Value = value;
        }

        [RelayCommand]
        private async Task DecreaseLaserChannel4()
        {
            var value = Math.Max(LaserChannel4Value - 1, 0);
            var res = await _CNILaser!.SetPowerAsync(4, value);
            if (!res) Console.WriteLine("Laser4 SetPower Error!");
            LaserChannel4Value = value;
        }

        [RelayCommand]
        private async Task<bool> SetChannelFirstStatusAsync()
        {
            if (LaserChannel1Enable)
            {
                if (LaserChannel2Enable)
                {
                    if (!await _CNILaser!.SetStateAsync(2, false))
                    {
                        Console.WriteLine("Laser2 SetStatus Error!");
                        return false;
                    }
                    LaserChannel2Enable = false;
                }

                if (LaserChannel3Enable)
                {
                    if (!await _CNILaser!.SetStateAsync(3, false))
                    {
                        Console.WriteLine("Laser3 SetStatus Error!");
                        return false;
                    }

                    LaserChannel3Enable = false;
                }
                if (LaserChannel4Enable)
                {
                    if (!await _CNILaser!.SetStateAsync(4, false))
                    {
                        Console.WriteLine("Laser4 SetStatus Error!");
                        return false;
                    }

                    LaserChannel4Enable = false;
                }
            }

            if (!await _CNILaser!.SetStateAsync(1, LaserChannel1Enable))
            {
                Console.WriteLine("Laser1 SetStatus Error!");
                return false;
            }

            return true;
        }

        [RelayCommand]
        private async Task<bool> SetChannelSecondStatusAsync()
        {
            if (LaserChannel2Enable)
            {
                if (LaserChannel1Enable)
                {
                    if (!await _CNILaser!.SetStateAsync(1, false))
                    {
                        Console.WriteLine("Laser1 SetStatus Error!");
                        return false;
                    }
                    LaserChannel1Enable = false;
                }

                if (LaserChannel3Enable)
                {
                    if (!await _CNILaser!.SetStateAsync(3, false))
                    {
                        Console.WriteLine("Laser3 SetStatus Error!");
                        return false;
                    }
                    LaserChannel3Enable = false;
                }

                if (LaserChannel4Enable)
                {
                    if (!await _CNILaser!.SetStateAsync(4, false))
                    {
                        Console.WriteLine("Laser4 SetStatus Error!");
                        return false;
                    }

                    LaserChannel4Enable = false;
                }
            }

            if (!await _CNILaser!.SetStateAsync(2, LaserChannel2Enable))
            {
                Console.WriteLine("Laser2 SetStatus Error!");
                return false;
            }

            return true;

        }

        [RelayCommand]
        private async Task<bool> SetChannelThirdStatusAsync()
        {
            if (LaserChannel3Enable)
            {
                if (LaserChannel1Enable)
                {
                    if (!await _CNILaser!.SetStateAsync(1, false))
                    {
                        Console.WriteLine("Laser1 SetStatus Error!");
                        return false;
                    }
                    LaserChannel1Enable = false;
                }

                if (LaserChannel2Enable)
                {
                    if (!await _CNILaser!.SetStateAsync(2, false))
                    {
                        Console.WriteLine("Laser2 SetStatus Error!");
                        return false;
                    }
                    LaserChannel2Enable = false;
                }

                if (LaserChannel4Enable)
                {
                    if (!await _CNILaser!.SetStateAsync(4, false))
                    {
                        Console.WriteLine("Laser4 SetStatus Error!");
                        return false;
                    }

                    LaserChannel4Enable = false;
                }
            }

            if (!await _CNILaser!.SetStateAsync(3, LaserChannel3Enable))
            {
                Console.WriteLine("Laser3 SetStatus Error!");
                return false;
            }

            return true;
        }

        [RelayCommand]
        private async Task<bool> SetChannelFourthStatusAsync()
        {
            if (LaserChannel4Enable)
            {
                if (LaserChannel1Enable)
                {
                    if (!await _CNILaser!.SetStateAsync(1, false))
                    {
                        Console.WriteLine("Laser1 SetStatus Error!");
                        return false;
                    }
                    LaserChannel1Enable = false;
                }

                if (LaserChannel2Enable)
                {
                    if (!await _CNILaser!.SetStateAsync(2, false))
                    {
                        Console.WriteLine("Laser2 SetStatus Error!");
                        return false;
                    }
                    LaserChannel2Enable = false;
                }

                if (LaserChannel3Enable)
                {
                    if (!await _CNILaser!.SetStateAsync(3, false))
                    {
                        Console.WriteLine("Laser3 SetStatus Error!");
                        return false;
                    }
                    LaserChannel3Enable = false;
                }
            }

            if (!await _CNILaser!.SetStateAsync(4, LaserChannel4Enable))
            {
                Console.WriteLine("Laser4 SetStatus Error!");
                return false;
            }

            return true;
        }

        private bool _isSettingPower = false;

        async partial void OnLaserChannel1ValueChanged(int value)
        {
            if (_isSettingPower) return; // 上一次还没执行完，直接丢弃
            _isSettingPower = true;

            try
            {
                _CNILaser!.GetVerifyValue(1, out var actualValue);
                if (value != actualValue)
                {
                    var res = await _CNILaser.SetPowerAsync(1, value);
                    if (!res)
                        Console.WriteLine("Laser1 SetPower Error!");
                }
            }
            finally
            {
                _isSettingPower = false;
            }
        }

        async partial void OnLaserChannel2ValueChanged(int value)
        {
            if (_isSettingPower) return; // 上一次还没执行完，直接丢弃
            _isSettingPower = true;

            try
            {
                _CNILaser!.GetVerifyValue(2, out var actualValue);
                if (value != actualValue)
                {
                    var res = await _CNILaser.SetPowerAsync(2, value);
                    if (!res)
                        Console.WriteLine("Laser2 SetPower Error!");
                }
            }
            finally
            {
                _isSettingPower = false;
            }
        }

        async partial void OnLaserChannel3ValueChanged(int value)
        {
            if (_isSettingPower) return; // 上一次还没执行完，直接丢弃
            _isSettingPower = true;

            try
            {
                _CNILaser!.GetVerifyValue(3, out var actualValue);
                if (value != actualValue)
                {
                    var res = await _CNILaser.SetPowerAsync(3, value);
                    if (!res)
                        Console.WriteLine("Laser3 SetPower Error!");
                }
            }
            finally
            {
                _isSettingPower = false;
            }
        }

        async partial void OnLaserChannel4ValueChanged(int value)
        {
            if (_isSettingPower) return; // 上一次还没执行完，直接丢弃
            _isSettingPower = true;

            try
            {
                _CNILaser!.GetVerifyValue(4, out var actualValue);
                if (value != actualValue)
                {
                    var res = await _CNILaser.SetPowerAsync(4, value);
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

    public partial class CNILaserViewModel
    {
        void RefreshStateInfo()
        {
            if (_CNILaser!.GetDeviceInfo(out var deviceInfo) && deviceInfo != null)
            {
                var device = deviceInfo;

                LaserChannel1Electricity = device.Laser1Current;
                LaserChannel2Electricity = device.Laser2Current;
                LaserChannel4Electricity = device.Laser4Current;

                LaserChannel1Temperature = device.Laser1Temperature;
                LaserChannel2Temperature = device.Laser2Temperature;
                LaserChannel4Temperature = device.Laser4Temperature;

                LaserKeyState = device.KeyState ? "ON" : "OFF";
                LaserPreheatState = device.PreheatState.ToString();
                LaserEstopState = device.EstopError ? "Error" : "OK";
                LaserInterlockState = device.InterlockError ? "Error" : "OK";
            }
        }

        private void TimerState_Tick(object? sender, EventArgs e)
        {
            RefreshStateInfo();
        }

        [ObservableProperty]
        private double laserChannel1Electricity =-1;

        [ObservableProperty]
        private double laserChannel2Electricity = -1;

        [ObservableProperty]
        private double laserChannel3Electricity = -1;

        [ObservableProperty]
        private double laserChannel4Electricity = -1;

        [ObservableProperty]
        private double laserChannel1Temperature = -1;

        [ObservableProperty]
        private double laserChannel2Temperature = -1;

        [ObservableProperty]
        private double laserChannel3Temperature = -1;

        [ObservableProperty]
        private double laserChannel4Temperature = -1;

        [ObservableProperty]
        private string laserKeyState = "N/A";

        [ObservableProperty]
        private string laserInterlockState = "N/A";

        [ObservableProperty]
        private string laserPreheatState = "N/A";

        [ObservableProperty]
        private string laserEstopState = "N/A";
    }

}



