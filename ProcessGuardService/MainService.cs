using ProcessGuard.Common;
using ProcessGuard.Common.Models;
using ProcessGuard.Common.Utility;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessGuardService
{
    public partial class MainService : ServiceBase
    {
        private Task _guardianTask = null;
        private NamedPipeServerStream _namedPipeServer = null;
        private readonly ConcurrentDictionary<string, ConfigItemWithProcessId> _startedProcesses = new ConcurrentDictionary<string, ConfigItemWithProcessId>();
        private readonly ConcurrentDictionary<string, ConfigItemWithProcessId> _changedProcesses = new ConcurrentDictionary<string, ConfigItemWithProcessId>();

        public MainService()
        {
            InitializeComponent();
        }

        private bool _running;

        protected override void OnStart(string[] args)
        {
            _running = true;
            _guardianTask = Task.Factory.StartNew(StartGuardian, TaskCreationOptions.LongRunning);
            _ = Task.Factory.StartNew(StartNamedPipeServer, TaskCreationOptions.LongRunning);
        }

        private void StartGuardian()
        {
            LoadConfig();

            while (_running)
            {
                Thread.Sleep(3000);

                foreach (var keyValuePair in _startedProcesses)
                {
                    var config = keyValuePair.Value;

                    if (config.ProcessId > 0)
                    {
                        try
                        {
                            using (var process = Process.GetProcessById(config.ProcessId))
                            {
                                // Just for dispose
                            }
                        }
                        catch (Exception)
                        {
                            // The process has not started, should be restarted
                            config.ProcessId = 0;
                        }
                    }

                    if (config.ProcessId <= 0)
                    {
                        var startFilePath = config.EXEFullPath;

                        if (File.Exists(startFilePath))
                        {
                            ApplicationLoader.StartProcessInSession0(startFilePath, Path.GetDirectoryName(startFilePath), out var processInfo, config.Minimize,
                                string.IsNullOrEmpty(config.StartupParams) ? null : $" {config.StartupParams}", config.NoWindow);

                            if (processInfo.dwProcessId > 0)
                            {
                                config.ProcessId = (int)processInfo.dwProcessId;
                                if (config.OnlyOpenOnce)
                                {
                                    _changedProcesses.TryAdd(keyValuePair.Key, new ConfigItemWithProcessId
                                    {
                                        Id = config.Id,
                                        ChangeType = ChangeType.Remove,
                                    });
                                }
                            }
                        }
                    }
                }

                var changeInfos = _changedProcesses.ToList();

                foreach (var changeInfo in changeInfos)
                {
                    if (changeInfo.Value.ChangeType.HasFlag(ChangeType.Stop))
                    {
                        try
                        {
                            _startedProcesses.TryGetValue(changeInfo.Key, out var startedProcess);
                            if (startedProcess != null && startedProcess.ProcessId > 0)
                            {
                                using (var process = Process.GetProcessById(startedProcess.ProcessId))
                                {
                                    process.Kill();
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // do nothing
                        }
                    }

                    if (changeInfo.Value.ChangeType.HasFlag(ChangeType.Start))
                    {
                        _startedProcesses[changeInfo.Key] = changeInfo.Value;
                    }

                    if (changeInfo.Value.ChangeType.HasFlag(ChangeType.Remove))
                    {
                        _startedProcesses.TryRemove(changeInfo.Key, out _);
                    }

                    _changedProcesses.TryRemove(changeInfo.Key, out _);
                }
            }
        }

        /// <summary>
        /// Load the config file content to the dictionary
        /// </summary>
        private void LoadConfig()
        {
            var configList = ConfigHelper.LoadConfigFile();

            foreach (var item in configList)
            {
                if (item.Started)
                {
                    _startedProcesses[item.Id] = new ConfigItemWithProcessId
                    {
                        EXEFullPath = item.EXEFullPath,
                        Id = item.Id,
                        Minimize = item.Minimize,
                        NoWindow = item.NoWindow,
                        OnlyOpenOnce = item.OnlyOpenOnce,
                        ProcessName = item.ProcessName,
                        Started = item.Started,
                        StartupParams = item.StartupParams,
                    };
                }
            }
        }

        /// <summary>
        /// Start the NamedPipeServer, listen to the changes of the config
        /// </summary>
        private void StartNamedPipeServer()
        {
            _namedPipeServer = new NamedPipeServerStream(Constants.PROCESS_GUARD_SERVICE, PipeDirection.In);
            _namedPipeServer.WaitForConnection();
            StreamReader reader = new StreamReader(_namedPipeServer);

            while (_running)
            {
                try
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        throw new IOException("The client disconnected");
                    }

                    var config = line.DeserializeObject<ConfigItemWithProcessId>();
                    if (config.Started)
                    {
                        config.ChangeType = ChangeType.Start;

                        if (_startedProcesses.ContainsKey(config.Id))
                        {
                            config.ChangeType |= ChangeType.Stop;
                        }
                    }
                    else
                    {
                        config.ChangeType = ChangeType.Stop | ChangeType.Remove;
                    }

                    _changedProcesses[config.Id] = config;
                }
                catch (IOException)
                {
                    _namedPipeServer.Dispose();
                    reader.Dispose();
                    _namedPipeServer = new NamedPipeServerStream(Constants.PROCESS_GUARD_SERVICE, PipeDirection.In);
                    _namedPipeServer.WaitForConnection();
                    reader = new StreamReader(_namedPipeServer);
                }
            }
        }

        protected override void OnStop()
        {
            _running = false;
            _namedPipeServer.Dispose();
            while (!_guardianTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
        }
    }
}
