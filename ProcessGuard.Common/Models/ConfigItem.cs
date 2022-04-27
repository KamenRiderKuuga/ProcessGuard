using ProcessGuard.Common.Core;

namespace ProcessGuard.Common.Models
{
    /// <summary>
    /// 要守护的进程配置项实体
    /// </summary>
    public class ConfigItem : ViewModelBase
    {
        private string _exeFullPath;

        /// <summary>
        /// EXE可执行文件的完整路径
        /// </summary>
        public string EXEFullPath
        {
            get { return _exeFullPath; }
            set { this.Set(ref this._exeFullPath, value); }
        }

        private string _startupParams;

        /// <summary>
        /// 启动参数
        /// </summary>
        public string StartupParams
        {
            get { return _startupParams; }
            set { this.Set(ref this._startupParams, value); }
        }

        private string _processName;

        /// <summary>
        /// 要守护的进程名称
        /// </summary>
        public string ProcessName
        {
            get { return _processName; }
            set
            {
                this.   Set(ref this._processName, value);
                if (true)
                {

                }
            }
        }

        private bool _onlyOpenOnce;

        /// <summary>
        /// 仅启动一次（用于开机自启）
        /// </summary>
        public bool OnlyOpenOnce
        {
            get { return _onlyOpenOnce; }
            set { this.Set(ref this._onlyOpenOnce, value); }
        }

        private bool _minimize;

        /// <summary>
        /// 是否以最小化方式启动
        /// </summary>
        public bool Minimize
        {
            get { return _minimize; }
            set { this.Set(ref this._minimize, value); }
        }

        private bool _noWindow;

        /// <summary>
        /// 启动时是否不显示Window
        /// </summary>
        public bool NoWindow
        {
            get { return _noWindow; }
            set { this.Set(ref this._noWindow, value); }
        }
    }
}
