using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Simscop.Hardware.CNI.FourChannel;
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
        private static string? currentPortname;
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

            _timerState = new DispatcherTimer(priority: DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(200) };
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
                    // 检测当前连接的串口是否被拔出
                    if (IsConnected && !string.IsNullOrEmpty(currentPortname) && !com.Contains(currentPortname))
                    {
                        IsConnected = false;

                        // 停止定时器
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            _timer?.Stop();
                            _timerState?.Stop();
                        });

                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(Application.Current.MainWindow,
                                $"Serial port {currentPortname} has been disconnected. Please check the connection and try again!",
                                "Connection Lost",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        });
                    }

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
                // 从底层获取实际连接的端口名
                if (_CNILaser is CNIFourChannelLaser cniLaser && !string.IsNullOrEmpty(cniLaser.CurrentPortname))
                {
                    currentPortname = cniLaser.CurrentPortname;
                    var index = SerialComs?.IndexOf(currentPortname) ?? -1;
                    if (index >= 0) SerialIndex = index;
                }

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Connected successfully!", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                });

                await InitSetting();
            }
            else
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Connection failed!", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
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
                currentPortname = com;
                // SerialIndex 已经是用户选的，无需更新

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Connected successfully!", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                });

                await InitSetting();
            }
            else
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Connection failed!", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
        }

        async Task InitSetting()
        {
            if (IsConnected)
            {
                #region 模式
                if (!_CNILaser!.GetControlMode(out int modeIndex))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, "GetControlMode Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                else
                {
                    _isSettingControlMode = true;
                    ControlModeIndex = modeIndex;
                    _isSettingControlMode = false;
                }

                #endregion

                #region 状态初始化

                if (!await _CNILaser.SetStateAsync(1, false))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, "SetState Channel1 Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                else { LaserChannel1Enable = false; }

                if (!await _CNILaser.SetStateAsync(2, false))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, "SetState Channel2 Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                else { LaserChannel2Enable = false; }

                if (!await _CNILaser.SetStateAsync(3, false))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, "SetState Channel3 Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                else { LaserChannel3Enable = false; }

                if (!await _CNILaser.SetStateAsync(4, false))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, "SetState Channel4 Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                else { LaserChannel4Enable = false; }

                #endregion

                #region 激光数值

                if (!_CNILaser.GetPower(1, out var power1))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, "GetPower Channel1 Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                else
                {
                    _isSettingPower1 = true;
                    LaserChannel1Value = power1;
                    _isSettingPower1 = false;
                }

                if (!_CNILaser.GetPower(2, out var power2))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, "GetPower Channel2 Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                else
                {
                    _isSettingPower2 = true;
                    LaserChannel2Value = power2;
                    _isSettingPower2 = false;
                }

                if (!_CNILaser.GetPower(3, out var power3))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, "GetPower Channel3 Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                else
                {
                    _isSettingPower3 = true;
                    LaserChannel3Value = power3;
                    _isSettingPower3 = false;
                }

                if (!_CNILaser.GetPower(4, out var power4))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, "GetPower Channel4 Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                else
                {
                    _isSettingPower4 = true;
                    LaserChannel4Value = power4;
                    _isSettingPower4 = false;
                }

                #endregion

                _timer!.Start();

                _timerState!.Start();
            }
        }

        public async Task<bool> CloserAllLaserChannel()
        {
            var res1 = await _CNILaser!.SetStateAsync(1, false);
            var res2 = await _CNILaser!.SetStateAsync(2, false);
            var res3 = await _CNILaser!.SetStateAsync(3, false);
            var res4 = await _CNILaser!.SetStateAsync(4, false);

            return res1 && res2 && res3 && res4;
        }
    }

    public partial class CNILaserViewModel
    {
        private bool _isPollingValue = false;

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_isPollingValue) return;
            _isPollingValue = true;

            Task.Run(() =>
            {
                try
                {
                    if (IsConnected)
                    {
                        if (_CNILaser == null) return;
                        if (_CNILaser.GetVerifyValue(1, out var channel1Value)) LaserChannel1ActualValue = channel1Value;
                        if (_CNILaser.GetVerifyValue(2, out var channel2Value)) LaserChannel2ActualValue = channel2Value;
                        if (_CNILaser.GetVerifyValue(3, out var channel3Value)) LaserChannel3ActualValue = channel3Value;
                        if (_CNILaser.GetVerifyValue(4, out var channel4Value)) LaserChannel4ActualValue = channel4Value;
                    }
                }
                finally
                {
                    _isPollingValue = false;
                }
            });
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
        private int _laserChannel1Value = -1;

        [ObservableProperty]
        private int _laserChannel2Value = -1;

        [ObservableProperty]
        private int _laserChannel3Value = -1;

        [ObservableProperty]
        private int _laserChannel4Value = -1;

        private bool _isSettingPower1 = false;
        private bool _isSettingPower2 = false;
        private bool _isSettingPower3 = false;
        private bool _isSettingPower4 = false;

        async partial void OnLaserChannel1ValueChanged(int oldValue, int newValue)
        {
            if (_isSettingPower1) return;
            _isSettingPower1 = true;

            try
            {
                _CNILaser!.GetVerifyValue(1, out var actualValue);
                if (newValue != actualValue)
                {
                    var res = await _CNILaser.SetPowerAsync(1, newValue);
                    if (!res)
                    {
                        _laserChannel1Value = oldValue;
                        OnPropertyChanged(nameof(LaserChannel1Value));

                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(Application.Current.MainWindow, $"Laser1 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                    }
                }
            }
            finally
            {
                _isSettingPower1 = false;
            }
        }

        async partial void OnLaserChannel2ValueChanged(int oldValue, int newValue)
        {
            if (_isSettingPower2) return;
            _isSettingPower2 = true;

            try
            {
                _CNILaser!.GetVerifyValue(2, out var actualValue);
                if (newValue != actualValue)
                {
                    var res = await _CNILaser.SetPowerAsync(2, newValue);
                    if (!res)
                    {
                        _laserChannel2Value = oldValue;
                        OnPropertyChanged(nameof(LaserChannel2Value));

                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(Application.Current.MainWindow, $"Laser2 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                    }
                }
            }
            finally
            {
                _isSettingPower2 = false;
            }
        }

        async partial void OnLaserChannel3ValueChanged(int oldValue, int newValue)
        {
            if (_isSettingPower3) return;
            _isSettingPower3 = true;

            try
            {
                _CNILaser!.GetVerifyValue(3, out var actualValue);
                if (newValue != actualValue)
                {
                    var res = await _CNILaser.SetPowerAsync(3, newValue);
                    if (!res)
                    {
                        _laserChannel3Value = oldValue;
                        OnPropertyChanged(nameof(LaserChannel3Value));

                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(Application.Current.MainWindow, $"Laser3 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                    }
                }
            }
            finally
            {
                _isSettingPower3 = false;
            }
        }

        async partial void OnLaserChannel4ValueChanged(int oldValue, int newValue)
        {
            if (_isSettingPower4) return;
            _isSettingPower4 = true;

            try
            {
                _CNILaser!.GetVerifyValue(4, out var actualValue);
                if (newValue != actualValue)
                {
                    var res = await _CNILaser.SetPowerAsync(4, newValue);
                    if (!res)
                    {
                        _laserChannel4Value = oldValue;
                        OnPropertyChanged(nameof(LaserChannel4Value));

                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(Application.Current.MainWindow, $"Laser4 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                    }
                }
            }
            finally
            {
                _isSettingPower4 = false;
            }
        }

        [RelayCommand]
        private async Task IncreaseLaserChannel1()
        {
            var value = Math.Min(LaserChannel1Value + 1, 100);
            var res = await _CNILaser!.SetPowerAsync(1, value);
            if (!res)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Laser1 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }
            _isSettingPower1 = true;
            LaserChannel1Value = value;
            _isSettingPower1 = false;
        }

        [RelayCommand]
        private async Task DecreaseLaserChannel1()
        {
            var value = Math.Max(LaserChannel1Value - 1, 0);
            var res = await _CNILaser!.SetPowerAsync(1, value);
            if (!res)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Laser1 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }
            _isSettingPower1 = true;
            LaserChannel1Value = value;
            _isSettingPower1 = false;
        }

        [RelayCommand]
        private async Task IncreaseLaserChannel2()
        {
            var value = Math.Min(LaserChannel2Value + 1, 100);
            var res = await _CNILaser!.SetPowerAsync(2, value);
            if (!res)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Laser2 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }
            _isSettingPower2 = true;
            LaserChannel2Value = value;
            _isSettingPower2 = false;
        }

        [RelayCommand]
        private async Task DecreaseLaserChannel2()
        {
            var value = Math.Max(LaserChannel2Value - 1, 0);
            var res = await _CNILaser!.SetPowerAsync(2, value);
            if (!res)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Laser2 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }
            _isSettingPower2 = true;
            LaserChannel2Value = value;
            _isSettingPower2 = false;
        }

        [RelayCommand]
        private async Task IncreaseLaserChannel3()
        {
            var value = Math.Min(LaserChannel3Value + 1, 100);
            var res = await _CNILaser!.SetPowerAsync(3, value);
            if (!res)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Laser3 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }
            _isSettingPower3 = true;
            LaserChannel3Value = value;
            _isSettingPower3 = false;
        }

        [RelayCommand]
        private async Task DecreaseLaserChannel3()
        {
            var value = Math.Max(LaserChannel3Value - 1, 0);
            var res = await _CNILaser!.SetPowerAsync(3, value);
            if (!res)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Laser3 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }
            _isSettingPower3 = true;
            LaserChannel3Value = value;
            _isSettingPower3 = false;
        }

        [RelayCommand]
        private async Task IncreaseLaserChannel4()
        {
            var value = Math.Min(LaserChannel4Value + 1, 100);
            var res = await _CNILaser!.SetPowerAsync(4, value);
            if (!res)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Laser4 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }
            _isSettingPower4 = true;
            LaserChannel4Value = value;
            _isSettingPower4 = false;
        }

        [RelayCommand]
        private async Task DecreaseLaserChannel4()
        {
            var value = Math.Max(LaserChannel4Value - 1, 0);
            var res = await _CNILaser!.SetPowerAsync(4, value);
            if (!res)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Laser4 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }
            _isSettingPower4 = true;
            LaserChannel4Value = value;
            _isSettingPower4 = false;
        }

        [ObservableProperty]
        private bool _laserChannel1Enable = false;

        [ObservableProperty]
        private bool _laserChannel2Enable = false;

        [ObservableProperty]
        private bool _laserChannel3Enable = false;

        [ObservableProperty]
        private bool _laserChannel4Enable = false;

        [RelayCommand]
        private async Task<bool> SetChannelFirstStatusAsync()
        {
            if (!await _CNILaser!.SetStateAsync(1, LaserChannel1Enable))
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Laser1 SetState Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                LaserChannel1Enable = !LaserChannel1Enable;//恢复状态
                return false;
            }
            return true;
        }

        [RelayCommand]
        private async Task<bool> SetChannelSecondStatusAsync()
        {
            if (!await _CNILaser!.SetStateAsync(2, LaserChannel2Enable))
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Laser2 SetState Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                LaserChannel2Enable = !LaserChannel2Enable;//恢复状态
                return false;
            }

            return true;

        }

        [RelayCommand]
        private async Task<bool> SetChannelThirdStatusAsync()
        {
            if (!await _CNILaser!.SetStateAsync(3, LaserChannel3Enable))
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Laser3 SetState Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                LaserChannel3Enable = !LaserChannel3Enable;//恢复状态
                return false;
            }

            return true;
        }

        [RelayCommand]
        private async Task<bool> SetChannelFourthStatusAsync()
        {
            if (!await _CNILaser!.SetStateAsync(4, LaserChannel4Enable))
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"Laser4 SetState Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                LaserChannel4Enable = !LaserChannel4Enable;//恢复状态
                return false;
            }

            return true;
        }

        [ObservableProperty]
        private List<string>? _controlMode = new() { "External", "Internal" };

        [ObservableProperty]
        public int _controlModeIndex = 0;

        private bool _isSettingControlMode = false;

        async partial void OnControlModeIndexChanged(int oldValue, int newValue)
        {
            if (_isSettingControlMode) return;
            _isSettingControlMode = true;

            try
            {
                var res = await _CNILaser!.SetControlModeAsync(newValue);
                if (!res)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, $"SetControlMode failed!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });

                    _controlModeIndex = oldValue;
                    OnPropertyChanged(nameof(ControlModeIndex));
                }
            }
            finally
            {
                _isSettingControlMode = false;
            }
        }

    }

    public partial class CNILaserViewModel
    {
        private bool _isRefreshing = false;

        void RefreshStateInfo()
        {
            var res = _CNILaser!.GetDeviceInfo(out var deviceInfo);
            if (res && deviceInfo != null)
            {
                var device = deviceInfo;

                LaserChannel1Electricity = device.Laser1Current;
                LaserChannel2Electricity = device.Laser2Current;
                LaserChannel4Electricity = device.Laser4Current;

                LaserChannel1Temperature = device.Laser1Temperature;
                LaserChannel2Temperature = device.Laser2Temperature;
                LaserChannel4Temperature = device.Laser4Temperature;

                LaserChannel1State = device.Laser1OnOff ? "ON" : "OFF";
                LaserChannel2State = device.Laser2OnOff ? "ON" : "OFF";
                LaserChannel3State = device.Laser3OnOff ? "ON" : "OFF";
                LaserChannel4State = device.Laser4OnOff ? "ON" : "OFF";

                LaserKeyState = device.KeyState ? "ON" : "OFF";
                LaserPreheatState = device.PreheatState.ToString();
                LaserEstopState = device.EstopError ? "Error" : "OK";
                LaserInterlockState = device.InterlockError ? "Error" : "OK";

                // 检测到任意异常状态，关闭所有激光通道
                bool hasAbnormal = !device.KeyState          // Key 为 OFF
                                || device.EstopError          // Estop 异常
                                || device.InterlockError      // Interlock 异常
                                || LaserPreheatState != "Finished";    // Preheat 异常

                if (hasAbnormal)
                {
                    bool anyEnabled = LaserChannel1Enable || LaserChannel2Enable
                                   || LaserChannel3Enable || LaserChannel4Enable;
                    if (anyEnabled)
                    {
                        // 切回 UI 线程更新状态并发送关闭命令
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            LaserChannel1Enable = false;
                            LaserChannel2Enable = false;
                            LaserChannel3Enable = false;
                            LaserChannel4Enable = false;

                            //await _CNILaser!.SetStateAsync(1, false);//不需要手动关闭，硬件已完成关闭
                            //await _CNILaser!.SetStateAsync(2, false);
                            //await _CNILaser!.SetStateAsync(3, false);
                            //await _CNILaser!.SetStateAsync(4, false);
                        });
                    }
                }
            }
        }

        private void TimerState_Tick(object? sender, EventArgs e)
        {
            if (_isRefreshing) return;
            _isRefreshing = true;

            Task.Run(() =>
            {
                try
                {
                    RefreshStateInfo();
                }
                finally
                {
                    _isRefreshing = false;
                }
            });
        }

        [ObservableProperty]
        private double laserChannel1Electricity = -1;

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
        private string laserChannel1State = "N.A.";

        [ObservableProperty]
        private string laserChannel2State = "N.A.";

        [ObservableProperty]
        private string laserChannel3State = "N.A.";

        [ObservableProperty]
        private string laserChannel4State = "N.A.";

        [ObservableProperty]
        private string laserKeyState = "N.A.";

        [ObservableProperty]
        private string laserInterlockState = "N.A.";

        [ObservableProperty]
        private string laserPreheatState = "N.A.";

        [ObservableProperty]
        private string laserEstopState = "N.A.";

        [ObservableProperty]
        private bool _isLaserPreheatAbnormal = false;
    }

}