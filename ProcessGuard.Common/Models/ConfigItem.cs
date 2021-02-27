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
    }
}
