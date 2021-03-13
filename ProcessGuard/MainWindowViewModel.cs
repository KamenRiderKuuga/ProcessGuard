using ProcessGuard.Common.Core;
using ProcessGuard.Common.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace ProcessGuard
{
    public class MainWindowViewModel : ViewModelBase, IDataErrorInfo
    {
        private ObservableCollection<ConfigItem> _configItems;
        /// <summary>
        /// 配置项集合
        /// </summary>
        public ObservableCollection<ConfigItem> ConfigItems
        {
            get { return _configItems; }
            set { this.Set(ref this._configItems, value); }
        }

        private string _selectedFile;

        /// <summary>
        /// 选中的文件
        /// </summary>
        public string SelectedFile
        {
            get { return _selectedFile; }
            set { this.Set(ref this._selectedFile, value); }
        }

        private string _statusColor;

        /// <summary>
        /// 服务运行状态对应的状态颜色
        /// </summary>
        public string StatusColor
        {
            get { return _statusColor; }
            set { this.Set(ref this._statusColor, value); }
        }

        private string _runStatus;

        /// <summary>
        /// 服务运行状态
        /// </summary>
        public string RunStatus
        {
            get { return $"服务状态({_runStatus})"; }
            set { this.Set(ref this._runStatus, value); }
        }

        private string _selectedProcessName;

        /// <summary>
        /// 当前的进程名
        /// </summary>
        public string SeletedProcessName
        {
            get { return _selectedProcessName; }
            set { this.Set(ref this._selectedProcessName, value); }
        }

        private bool _isOnlyOpenOnce;

        /// <summary>
        /// 当前是否勾选只启动一次
        /// </summary>
        public bool IsOnlyOpenOnce
        {
            get { return _isOnlyOpenOnce; }
            set { this.Set(ref this._isOnlyOpenOnce, value); }
        }

        private bool _isMinimize;

        /// <summary>
        /// 当前是否勾选以最小化方式启动
        /// </summary>
        public bool IsMinimize
        {
            get { return _isMinimize; }
            set { this.Set(ref this._isMinimize, value); }
        }

        private bool _canStart;

        /// <summary>
        /// 是否可以点击开始按钮
        /// </summary>
        public bool CanStart
        {
            get { return _canStart; }
            set { this.Set(ref this._canStart, value); }
        }

        private bool _canStop;

        /// <summary>
        /// 是否可以点击停止按钮
        /// </summary>
        public bool CanStop
        {
            get { return _canStop; }
            set { this.Set(ref this._canStop, value); }
        }

        private bool _canUnistall;

        /// <summary>
        /// 是否可以点击卸载按钮
        /// </summary>
        public bool CanUnistall
        {
            get { return _canUnistall; }
            set { this.Set(ref this._canUnistall, value); }
        }

        private bool? _isRun;

        /// <summary>
        /// 设置服务运行状态
        /// </summary>
        public bool? IsRun
        {
            get { return _isRun; }
            set
            {
                if (_isRun == value)
                {
                    return;
                }

                _isRun = value;

                switch (value)
                {
                    case true:
                        CanStart = false;
                        CanStop = true;
                        CanUnistall = true;
                        break;

                    case false:
                        CanStart = true;
                        CanStop = false;
                        CanUnistall = true;
                        break;

                    default:
                        CanStart = true;
                        CanStop = false;
                        CanUnistall = false;
                        break;
                }
            }
        }


        [Description("Test-Property")]
        public string Error => string.Empty;

        public int MyProperty { get; set; }

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(SelectedFile):
                        if (string.IsNullOrWhiteSpace(SelectedFile))
                        {
                            return "参数是必填的！";
                        }

                        if (!File.Exists(SelectedFile) || !SelectedFile.EndsWith(".exe"))
                        {
                            return "文件路径不正确，请检查！";
                        }

                        if (ConfigItems.Select(item => item.EXEFullPath).Contains(SelectedFile))
                        {
                            return "该配置项已存在，请删除后重新添加";
                        }

                        break;

                    case nameof(SeletedProcessName):
                        if (string.IsNullOrWhiteSpace(SeletedProcessName))
                        {
                            return "参数是必填的！";
                        }
                        break;

                    default:
                        break;
                }

                return null;
            }
        }
    }
}
