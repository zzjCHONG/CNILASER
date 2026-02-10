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
                if (!_CNILaser!.GetControlMode(out int modeIndex))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, "GetControlMode Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                else
                {
                    ControlModeIndex = modeIndex;
                }

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

                if (!_CNILaser.GetPower(1, out var power1))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, "GetPower Channel1 Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                else { LaserChannel1Value = power1; }

                if (!_CNILaser.GetPower(2, out var power2))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, "GetPower Channel2 Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                else { LaserChannel2Value = power2; }

                if (!_CNILaser.GetPower(3, out var power3))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, "GetPower Channel3 Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                else { LaserChannel3Value = power3; }

                if (!_CNILaser.GetPower(4, out var power4))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Application.Current.MainWindow, "GetPower Channel4 Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                else { LaserChannel4Value = power4; }

                _timer!.Start();
                _timerState!.Start();
            }
        }

        /// <summary>
        /// Returns whether all channels have been closed
        /// </summary>
        /// <returns></returns>
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
        private void Timer_Tick(object? sender, EventArgs e)
        {
            Task.Run(() =>
            {
                if (IsConnected)
                {
                    if (_CNILaser == null) return;
                    if (_CNILaser.GetVerifyValue(1, out var channel1Value)) LaserChannel1ActualValue = channel1Value;
                    if (_CNILaser.GetVerifyValue(2, out var channel2Value)) LaserChannel2ActualValue = channel2Value;
                    if (_CNILaser.GetVerifyValue(3, out var channel3Value)) LaserChannel3ActualValue = channel3Value;
                    if (_CNILaser.GetVerifyValue(4, out var channel4Value)) LaserChannel4ActualValue = channel4Value;
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

        private bool _isSettingPower = false;

        async partial void OnLaserChannel1ValueChanged(int value)
        {
            if (_isSettingPower) return; // Previous execution not finished, discard
            _isSettingPower = true;

            try
            {
                _CNILaser!.GetVerifyValue(1, out var actualValue);
                if (value != actualValue)
                {
                    var res = await _CNILaser.SetPowerAsync(1, value);
                    if (!res)
                    {
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(Application.Current.MainWindow, $"Laser1 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                    }
                }
            }
            finally
            {
                _isSettingPower = false;
            }
        }

        async partial void OnLaserChannel2ValueChanged(int value)
        {
            if (_isSettingPower) return; // Previous execution not finished, discard
            _isSettingPower = true;

            try
            {
                _CNILaser!.GetVerifyValue(2, out var actualValue);
                if (value != actualValue)
                {
                    var res = await _CNILaser.SetPowerAsync(2, value);
                    if (!res)
                    {
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(Application.Current.MainWindow, $"Laser2 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                    }
                }
            }
            finally
            {
                _isSettingPower = false;
            }
        }

        async partial void OnLaserChannel3ValueChanged(int value)
        {
            if (_isSettingPower) return; // Previous execution not finished, discard
            _isSettingPower = true;

            try
            {
                _CNILaser!.GetVerifyValue(3, out var actualValue);
                if (value != actualValue)
                {
                    var res = await _CNILaser.SetPowerAsync(3, value);
                    if (!res)
                    {
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(Application.Current.MainWindow, $"Laser3 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                    }
                }
            }
            finally
            {
                _isSettingPower = false;
            }
        }

        async partial void OnLaserChannel4ValueChanged(int value)
        {
            if (_isSettingPower) return; // Previous execution not finished, discard
            _isSettingPower = true;

            try
            {
                _CNILaser!.GetVerifyValue(4, out var actualValue);
                if (value != actualValue)
                {
                    var res = await _CNILaser.SetPowerAsync(4, value);
                    if (!res)
                    {
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(Application.Current.MainWindow, $"Laser4 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                    }
                }
            }
            finally
            {
                _isSettingPower = false;
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
            LaserChannel1Value = value;
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
            LaserChannel1Value = value;
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
            LaserChannel2Value = value;
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
            LaserChannel2Value = value;
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
                    MessageBox.Show(Application.Current.MainWindow, $"Laser13 SetPower Error!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }
            LaserChannel3Value = value;
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
            LaserChannel3Value = value;
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
            LaserChannel4Value = value;
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
            LaserChannel4Value = value;
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

        async partial void OnControlModeIndexChanged(int oldValue, int newValue)
        {
            var res = await _CNILaser!.SetControlModeAsync(newValue);
            if (!res)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"SetControlMode failed!", "Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                });

                ControlModeIndex = oldValue;
            }
        }

    }

    public partial class CNILaserViewModel
    {
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
            }
        }

        private void TimerState_Tick(object? sender, EventArgs e)
        {
            Task.Run(() =>
            {
                RefreshStateInfo();
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
        private string laserChannel1State = "N/A";

        [ObservableProperty]
        private string laserChannel2State = "N/A";

        [ObservableProperty]
        private string laserChannel3State = "N/A";

        [ObservableProperty]
        private string laserChannel4State = "N/A";

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