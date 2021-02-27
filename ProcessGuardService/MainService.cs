using ProcessGuard.Common.Utility;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;


namespace ProcessGuardService
{
    public partial class MainService : ServiceBase
    {
        /// <summary>
        /// 用来对进程进行守护的线程
        /// </summary>
        private Thread _guardianThread;

        public MainService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _guardianThread = new Thread(new ThreadStart(StartGuardian));
            _guardianThread.Name = nameof(_guardianThread);
            _guardianThread.IsBackground = true;
            _guardianThread.Start();
        }

        private void StartGuardian()
        {
            while (true)
            {
                Thread.Sleep(1000);
                var currentProcesses = Process.GetProcessesByName("Everything");

                if (currentProcesses?.Length == 0)
                {
                    var startFilePath = @"E:\Program Files\Everything-1.4.1.1005.x64\Everything.exe";

                    if (File.Exists(startFilePath))
                    {
                        ApplicationLoader.StartProcessAndBypassUAC(startFilePath, Path.GetDirectoryName(startFilePath), out var _);
                    }
                }
            }
        }
    }
}
