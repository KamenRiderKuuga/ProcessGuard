﻿using ProcessGuard.Common;
using ProcessGuard.Common.Models;
using ProcessGuard.Common.Utility;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.IO.Pipes;
using System.Linq;
using System.Globalization;

namespace ProcessGuard
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        private readonly MainWindowViewModel _mainWindowViewModel;

        /// <summary>
        /// 用于定时检查服务状态的Timer
        /// </summary>
        private readonly Timer _checkTimer;

        public MainWindow()
        {
            InitializeComponent();
            _mainWindowViewModel = new MainWindowViewModel() { ConfigItems = new ObservableCollection<ConfigItem>(), StatusColor = "Transparent" };
            DataContext = _mainWindowViewModel;
            this.MetroDialogOptions.AnimateShow = true;
            this.MetroDialogOptions.AnimateHide = false;
            _mainWindowViewModel.ConfigItems = ConfigHelper.LoadConfigFile();
            _mainWindowViewModel.IsRun = _mainWindowViewModel.IsRun == true ? false : true;
            _mainWindowViewModel.GlobalConfig = ConfigHelper.LoadGlobalConfigFile();

            if (string.IsNullOrEmpty(_mainWindowViewModel.GlobalConfig.Language))
            {
                if (CultureInfo.CurrentUICulture.Name == "zh-CN")
                {
                    _mainWindowViewModel.GlobalConfig.Language = "简体中文";
                }
                else
                {
                    _mainWindowViewModel.GlobalConfig.Language = "English";
                }
            }

            SetLanguageDictionary();
            UpdateServiceStatus();

            _checkTimer = new Timer();
            _checkTimer.Elapsed += CheckTimer_Elapsed;
            _checkTimer.Start();
            _checkTimer.Interval = 1000;
        }

        #region 窗体事件

        /// <summary>
        /// 按钮点击事件
        /// </summary>
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            switch (button?.Name)
            {
                case nameof(btnAdd):
                    var dialog = new CustomDialog(this.MetroDialogOptions) { Content = this.Resources["CustomAddDialog"], Title = string.Empty };
                    _mainWindowViewModel.SelectedId = Guid.NewGuid().ToString("N");
                    _mainWindowViewModel.Started = false;
                    _mainWindowViewModel.IsNew = true;

                    await this.ShowMetroDialogAsync(dialog);
                    await dialog.WaitUntilUnloadedAsync();
                    break;

                case nameof(btnMinus):
                    ConfigItem selectedItem = null;

                    if (configDataGrid.SelectedCells?.Count > 0)
                    {
                        selectedItem = configDataGrid.SelectedCells[0].Item as ConfigItem;
                    }

                    if (selectedItem == null)
                    {
                        break;
                    }

                    MessageDialogResult result = await ShowMessageDialogAsync(FindResource("Warning").ToString(), FindResource("ConfirmDelete").ToString());

                    if (result == MessageDialogResult.Affirmative)
                    {
                        _mainWindowViewModel.ConfigItems.Remove(selectedItem);
                        ConfigHelper.SaveConfigs(_mainWindowViewModel.ConfigItems);
                        selectedItem.Started = false;
                        SendCommandToService(selectedItem);
                    }

                    break;

                case "btnSelectFile":
                    var filePath = Utils.GetFileNameByFileDialog();
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        FileInfo fileInfo = new FileInfo(filePath);
                        _mainWindowViewModel.SelectedFile = fileInfo.FullName;
                        _mainWindowViewModel.SeletedProcessName = fileInfo.Name.Replace(fileInfo.Extension, string.Empty);
                    }
                    break;

                case nameof(btnStart):
                    StartService();
                    break;

                case nameof(btnStop):
                    StopService();
                    break;

                case nameof(btnUninstall):
                    await UninstallService();
                    break;

                case nameof(btnSetting):
                    var settingDialog = new CustomDialog(this.MetroDialogOptions) { Content = this.Resources["CustomSettingDialog"], Title = string.Empty };
                    await this.ShowMetroDialogAsync(settingDialog, new MetroDialogSettings { });
                    await settingDialog.WaitUntilUnloadedAsync();
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Click event of datagrid button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridButton_Click(object sender, RoutedEventArgs e)
        {
            var currentRow = this.configDataGrid.CurrentItem as ConfigItem;

            if (currentRow != null)
            {
                currentRow.Started = !currentRow.Started;
                ConfigHelper.SaveConfigs(_mainWindowViewModel.ConfigItems);
                SendCommandToService(currentRow);
            }
        }

        /// <summary>
        /// Double click event of datagrid cell
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DataGridCell_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var currentRow = this.configDataGrid.CurrentItem as ConfigItem;

            if (currentRow != null)
            {
                var dialog = new CustomDialog(this.MetroDialogOptions) { Content = this.Resources["CustomAddDialog"], Title = string.Empty };

                _mainWindowViewModel.SelectedId = currentRow.Id;
                _mainWindowViewModel.SelectedFile = currentRow.EXEFullPath;
                _mainWindowViewModel.StartupParams = currentRow.StartupParams;
                _mainWindowViewModel.SeletedProcessName = currentRow.ProcessName;
                _mainWindowViewModel.IsOnlyOpenOnce = currentRow.OnlyOpenOnce;
                _mainWindowViewModel.IsMinimize = currentRow.Minimize;
                _mainWindowViewModel.NoWindow = currentRow.NoWindow;
                _mainWindowViewModel.Started = currentRow.Started;
                _mainWindowViewModel.IsNew = false;

                await this.ShowMetroDialogAsync(dialog);
                await dialog.WaitUntilUnloadedAsync();
            }
        }

        /// <summary>
        /// Event after language comboBox selection changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConfigHelper.SaveGlobalConfigs(_mainWindowViewModel.GlobalConfig);
            SetLanguageDictionary();
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        private void StopService()
        {
            ServiceController service = null;
            try
            {
                service = new ServiceController(Constants.PROCESS_GUARD_SERVICE);
                service.Stop();
            }
            catch (Exception)
            {
                // do nothing
            }
            finally
            {
                service.Dispose();
            }
        }

        /// <summary>
        /// 卸载服务
        /// </summary>
        /// <returns>卸载过程中是否产生错误</returns>
        private async Task<bool> UninstallService()
        {
            var error = string.Empty;

            MessageDialogResult dialogResult = await ShowMessageDialogAsync(FindResource("Warning").ToString(), FindResource("ConfirmUninstall").ToString());

            if (dialogResult == MessageDialogResult.Affirmative)
            {
                CreateServiceFile();
                var servicePath = ConfigHelper.GetAppDataFilePath(Constants.FILE_GUARD_SERVICE);
                string cmd = @"%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe /U ";
                cmd += $"/LogFile={ConfigHelper.GetAppDataFilePath("myLog.InstallLog")} ";
                cmd += servicePath;

                await Task.Run(() =>
                {
                    ApplicationLoader.RunCmdAndGetOutput(cmd, out var _, out error);
                    try
                    {
                        File.Delete(servicePath);
                    }
                    catch (Exception)
                    {
                        // do nothing
                    }
                });
            }

            return !string.IsNullOrEmpty(error);
        }

        /// <summary>
        /// 若服务已存在，启动服务，否则先安装，再启动服务
        /// </summary>
        private async void StartService()
        {
            if (GetServiceStatus(Constants.PROCESS_GUARD_SERVICE) == default(ServiceControllerStatus))
            {
                CreateServiceFile();

                string cmd = @"%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe ";
                cmd += $"/LogFile={ConfigHelper.GetAppDataFilePath("myLog.InstallLog")} ";
                cmd += $@"{ConfigHelper.GetAppDataFilePath(Constants.FILE_GUARD_SERVICE)}
                             Net Start ProcessGuardService
                             sc config ProcessGuardService = auto";

                await Task.Run(() =>
                {
                    ApplicationLoader.RunCmdAndGetOutput(cmd, out var _, out var _);
                });
            }
            else
            {
                ServiceController service = null;
                try
                {
                    service = new ServiceController(Constants.PROCESS_GUARD_SERVICE);
                    service.Start();
                }
                catch (Exception)
                {
                    // do nothing
                }
                finally
                {
                    service.Dispose();
                }
            }
        }

        /// <summary>
        /// 在安装和卸载服务之前，需要确保服务文件已存在
        /// </summary>
        private void CreateServiceFile()
        {
            var serviceFileName = ConfigHelper.GetAppDataFilePath(Constants.FILE_GUARD_SERVICE);

            // 从资源文件中拷贝出服务程序
            if (!File.Exists(serviceFileName))
            {
                File.WriteAllBytes(serviceFileName, Properties.Resources.ProcessGuardService);
            }
        }

        /// <summary>
        /// 用于检查服务状态的计时器
        /// </summary>
        private void CheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateServiceStatus();
        }

        #endregion

        #region 窗体私有函数

        /// <summary>
        /// 获取指定Windows系统服务的状态，如果其没有安装，默认返回0
        /// </summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        private ServiceControllerStatus GetServiceStatus(string serviceName)
        {
            ServiceController service = null;
            try
            {
                service = new ServiceController(serviceName);
                return service.Status;
            }
            catch (Exception)
            {
                return default(ServiceControllerStatus);
            }
            finally
            {
                service.Dispose();
            }
        }

        /// <summary>
        /// 更新当前的服务状态标识
        /// </summary>
        private void UpdateServiceStatus()
        {
            string statusColor = "";
            string runStatus = "";
            bool? isRun = null;

            switch (GetServiceStatus(Constants.PROCESS_GUARD_SERVICE))
            {
                case ServiceControllerStatus.ContinuePending:
                    statusColor = "Yellow";
                    runStatus = FindResource("ContinuePending").ToString();
                    isRun = false;
                    break;
                case ServiceControllerStatus.Paused:
                    statusColor = "Orange";
                    runStatus = FindResource("Paused").ToString();
                    isRun = false;
                    break;
                case ServiceControllerStatus.PausePending:
                    statusColor = "Yellow";
                    runStatus = FindResource("PausePending").ToString();
                    isRun = true;
                    break;
                case ServiceControllerStatus.Running:
                    statusColor = "Green";
                    runStatus = FindResource("Running").ToString();
                    isRun = true;
                    break;
                case ServiceControllerStatus.StartPending:
                    statusColor = "Yellow";
                    runStatus = FindResource("StartPending").ToString();
                    isRun = false;
                    break;
                case ServiceControllerStatus.Stopped:
                    statusColor = "Orange";
                    runStatus = FindResource("Stopped").ToString();
                    isRun = false;
                    break;
                case ServiceControllerStatus.StopPending:
                    statusColor = "Yellow";
                    runStatus = FindResource("StopPending").ToString();
                    isRun = true;
                    break;
                default:
                    statusColor = "Red";
                    runStatus = FindResource("NotInstalled").ToString();
                    isRun = null;
                    break;
            }

            if (!string.IsNullOrEmpty(runStatus))
            {
                this.Dispatcher.Invoke(() =>
                {
                    _mainWindowViewModel.StatusColor = statusColor;
                    _mainWindowViewModel.RunStatus = string.Format(FindResource("ServiceStatus").ToString(), runStatus);
                    _mainWindowViewModel.IsRun = isRun;
                });
            }
        }

        /// <summary>
        /// 使用自定义的标题和消息弹出确认对话框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">要提示的消息</param>
        /// <returns></returns>
        private async Task<MessageDialogResult> ShowMessageDialogAsync(string title, string message)
        {
            var mySettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = FindResource("Yes").ToString(),
                NegativeButtonText = FindResource("Cancel").ToString(),
                ColorScheme = MetroDialogOptions.ColorScheme,
                DialogButtonFontSize = 20D,
                AnimateShow = false,
                AnimateHide = false,
            };

            MessageDialogResult result = await this.ShowMessageAsync(title, message,
                                                                     MessageDialogStyle.AffirmativeAndNegative, mySettings);

            return result;
        }

        /// <summary>
        /// Send command to service to control the process
        /// </summary>
        /// <param name="config"></param>
        private void SendCommandToService(ConfigItem config)
        {
            if (_mainWindowViewModel.IsRun != true)
            {
                return;
            }
            using (var client = new NamedPipeClientStream(".", Constants.PROCESS_GUARD_SERVICE, PipeDirection.Out))
            {
                try
                {
                    client.Connect(1000);
                    using (var writer = new StreamWriter(client))
                    {
                        writer.WriteLine(config.Serialize());
                    }
                }
                catch (Exception)
                {
                    // do nothing
                }
            }
        }

        /// <summary>
        /// 关闭当前弹出的Dialog窗口
        /// </summary>
        private async void CloseCustomDialog(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dialog = button.TryFindParent<BaseMetroDialog>();

            switch (button.Name)
            {
                case "btnConfirmAdd":
                    if (_mainWindowViewModel[nameof(_mainWindowViewModel.SelectedFile)] == null &&
                        _mainWindowViewModel[nameof(_mainWindowViewModel.SeletedProcessName)] == null)
                    {
                        var config = _mainWindowViewModel.ConfigItems.Where(c => c.Id == _mainWindowViewModel.SelectedId).FirstOrDefault();

                        if (config != null)
                        {
                            config.Id = _mainWindowViewModel.SelectedId;
                            config.EXEFullPath = _mainWindowViewModel.SelectedFile;
                            config.StartupParams = _mainWindowViewModel.StartupParams;
                            config.ProcessName = _mainWindowViewModel.SeletedProcessName;
                            config.OnlyOpenOnce = _mainWindowViewModel.IsOnlyOpenOnce;
                            config.Minimize = _mainWindowViewModel.IsMinimize;
                            config.NoWindow = _mainWindowViewModel.NoWindow;
                            config.Started = _mainWindowViewModel.Started;

                            if (config.Started)
                            {
                                SendCommandToService(config);
                            }
                        }
                        else
                        {
                            _mainWindowViewModel.ConfigItems.Add(new ConfigItem()
                            {
                                Id = _mainWindowViewModel.SelectedId,
                                EXEFullPath = _mainWindowViewModel.SelectedFile,
                                StartupParams = _mainWindowViewModel.StartupParams,
                                ProcessName = _mainWindowViewModel.SeletedProcessName,
                                OnlyOpenOnce = _mainWindowViewModel.IsOnlyOpenOnce,
                                Minimize = _mainWindowViewModel.IsMinimize,
                                NoWindow = _mainWindowViewModel.NoWindow,
                                Started = _mainWindowViewModel.Started,
                            });
                        }

                        ConfigHelper.SaveConfigs(_mainWindowViewModel.ConfigItems);
                        await this.HideMetroDialogAsync(dialog);
                    }
                    break;

                case "btnCancelAdd":
                case "btnOK":
                    await this.HideMetroDialogAsync(dialog);
                    break;
                default:
                    break;
            }
        }

        private void SetLanguageDictionary()
        {
            ResourceDictionary dict = new ResourceDictionary();
            var uriString = string.Empty;

            switch (_mainWindowViewModel.GlobalConfig?.Language)
            {
                case "简体中文":
                    uriString = "..\\Resources\\StringResources.zh-CN.xaml";
                    break;
                default:
                    uriString = "..\\Resources\\StringResources.xaml";
                    break;
            }

            dict.Source = new Uri(uriString, UriKind.Relative);

            var existedDict = Application.Current.Resources.MergedDictionaries.Where(d => d.Source.OriginalString == uriString).FirstOrDefault();

            Application.Current.Resources.MergedDictionaries.Add(dict);

            if (existedDict != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(existedDict);
            }
        }

        #endregion

    }
}
