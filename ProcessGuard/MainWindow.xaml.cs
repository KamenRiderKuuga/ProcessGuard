using ProcessGuard.Common;
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

namespace ProcessGuard
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        private MainWindowViewModel _mainWindowViewModel;

        /// <summary>
        /// 用于定时检查服务状态的Timer
        /// </summary>
        private Timer _checkTimer;

        public MainWindow()
        {
            InitializeComponent();
            _mainWindowViewModel = new MainWindowViewModel() { ConfigItems = new ObservableCollection<ConfigItem>(), StatusColor = "Transparent" };
            UpdateServiceStatus();
            DataContext = _mainWindowViewModel;
            this.MetroDialogOptions.AnimateShow = true;
            this.MetroDialogOptions.AnimateHide = false;
            _mainWindowViewModel.ConfigItems = ConfigHelper.LoadConfigFile();
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

                    MessageDialogResult result = await ShowMessageDialogAsync("注意", "确认删除选中的内容吗？");

                    if (result == MessageDialogResult.Affirmative)
                        _mainWindowViewModel.ConfigItems.Remove(selectedItem);

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

                case nameof(btnSave):
                    SaveChanges();
                    break;


                case nameof(btnUndo):
                    UndoChanges();
                    break;


                case nameof(btnStart):
                    //var serviceFileName = nameof(Properties.Resources.Guardian) + ".exe";
                    //if (!File.Exists(serviceFileName))
                    //{
                    //    File.WriteAllBytes(serviceFileName, Properties.Resources.Guardian);
                    //}

                    System.Diagnostics.ProcessStartInfo myProcessInfo = new System.Diagnostics.ProcessStartInfo(); //Initializes a new ProcessStartInfo of name myProcessInfo
                    myProcessInfo.FileName = "cmd.exe"; // Sets the FileName property of myProcessInfo to %SystemRoot%\System32\cmd.exe where %SystemRoot% is a system variable which is expanded using Environment.ExpandEnvironmentVariables
                    myProcessInfo.Arguments = "cd.."; // Sets the arguments to cd..
                    //myProcessInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden; //Sets the WindowStyle of myProcessInfo which indicates the window state to use when the process is started to Hidden
                    myProcessInfo.Verb = "runas"; // 提升cmd程序的权限
                    System.Diagnostics.Process.Start(myProcessInfo);

                    break;


                case nameof(btnStop):

                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// 主窗体关闭时事件
        /// </summary>
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ConfigHelper.SaveConfigs(_mainWindowViewModel.ConfigItems);
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
        /// 撤销本次的所有改动
        /// </summary>
        private async void UndoChanges()
        {
            MessageDialogResult result = await ShowMessageDialogAsync("注意", "确认撤销本次的所有改动吗？");

            if (result == MessageDialogResult.Affirmative)
                this._mainWindowViewModel.ConfigItems = ConfigHelper.LoadConfigFile();
        }

        /// <summary>
        /// 保存当前配置项
        /// </summary>
        private async void SaveChanges()
        {
            MessageDialogResult result = await ShowMessageDialogAsync("注意", "这将保存当前配置并立即重启守护服务，确认吗？");

            if (result == MessageDialogResult.Affirmative)
                ConfigHelper.SaveConfigs(_mainWindowViewModel.ConfigItems);
        }

        /// <summary>
        /// 更新当前的服务状态标识
        /// </summary>
        private void UpdateServiceStatus()
        {
            ServiceController service = new ServiceController(Constants.PROCESS_GUARD_SERVICE);
            string statusColor = "";
            string runStatus = "";

            try
            {
                if (!string.IsNullOrEmpty(service?.ServiceName))
                {
                    switch (service.Status)
                    {
                        case ServiceControllerStatus.ContinuePending:
                            statusColor = "Yellow";
                            runStatus = "即将继续";
                            break;
                        case ServiceControllerStatus.Paused:
                            statusColor = "Orange";
                            runStatus = "已暂停";
                            break;
                        case ServiceControllerStatus.PausePending:
                            statusColor = "Yellow";
                            runStatus = "即将暂停";
                            break;
                        case ServiceControllerStatus.Running:
                            statusColor = "Green";
                            runStatus = "运行中";
                            break;
                        case ServiceControllerStatus.StartPending:
                            statusColor = "Yellow";
                            runStatus = "正在启动";
                            break;
                        case ServiceControllerStatus.Stopped:
                            statusColor = "Orange";
                            runStatus = "已停止";
                            break;
                        case ServiceControllerStatus.StopPending:
                            statusColor = "Yellow";
                            runStatus = "正在停止停止";
                            break;
                        default:
                            statusColor = "Red";
                            runStatus = "未安装";
                            break;
                    }
                }
            }
            catch (Exception)
            {
                statusColor = "Red";
                runStatus = "未安装";
            }
            finally
            {
                if (!string.IsNullOrEmpty(runStatus))
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        _mainWindowViewModel.StatusColor = statusColor;
                        _mainWindowViewModel.RunStatus = runStatus;
                    });
                }

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
                AffirmativeButtonText = "是的",
                NegativeButtonText = "取消",
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
        /// 关闭当前弹出的Dialog窗口
        /// </summary>
        private async void CloseCustomDialog(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dialog = button.TryFindParent<BaseMetroDialog>();

            var mySettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "是的",
                NegativeButtonText = "取消",
                ColorScheme = MetroDialogOptions.ColorScheme,
                DialogButtonFontSize = 20D,
                AnimateShow = false,
                AnimateHide = false,
            };

            switch (button.Name)
            {
                case "btnConfirmAdd":
                    if (_mainWindowViewModel[nameof(_mainWindowViewModel.SelectedFile)] == null &&
                        _mainWindowViewModel[nameof(_mainWindowViewModel.SeletedProcessName)] == null)
                    {
                        _mainWindowViewModel.ConfigItems.Add(new ConfigItem()
                        {
                            EXEFullPath = _mainWindowViewModel.SelectedFile,
                            ProcessName = _mainWindowViewModel.SeletedProcessName,
                            OnlyOpenOnce = _mainWindowViewModel.IsOnlyOpenOnce,
                        });

                        await this.HideMetroDialogAsync(dialog);
                    }
                    break;

                case "btnCancelAdd":
                    await this.HideMetroDialogAsync(dialog);
                    break;
                default:
                    break;
            }
        }

        #endregion

    }
}
