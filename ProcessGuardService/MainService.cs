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
            var configList = ConfigHelper.LoadConfigFile();

            while (true)
            {
                Thread.Sleep(5000);

                for (int index = configList.Count - 1; index >= 0; index--)
                {
                    var config = configList[index];

                    var currentProcesses = Process.GetProcessesByName(config.ProcessName);

                    if (currentProcesses?.Length == 0)
                    {
                        var startFilePath = config.EXEFullPath;

                        if (File.Exists(startFilePath))
                        {
                            ApplicationLoader.StartProcessAndBypassUAC(startFilePath, Path.GetDirectoryName(startFilePath), out var _);
                            if (config.OnlyOpenOnce)
                            {
                                configList.Remove(config);
                            }
                        }

                        Thread.Sleep(1000);
                    }
                    else
                    {
                        foreach (var process in currentProcesses)
                        {
                            process.Dispose();
                        }
                    }
                }
            }
        }
    }
}
