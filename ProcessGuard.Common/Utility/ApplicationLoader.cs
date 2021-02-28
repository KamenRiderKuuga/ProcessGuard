using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace ProcessGuard.Common.Utility
{
    public class ApplicationLoader
    {
        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        #endregion

        #region Enumerations

        enum TOKEN_TYPE : int
        {
            TokenPrimary = 1,
            TokenImpersonation = 2
        }

        enum SECURITY_IMPERSONATION_LEVEL : int
        {
            SecurityAnonymous = 0,
            SecurityIdentification = 1,
            SecurityImpersonation = 2,
            SecurityDelegation = 3,
        }

        #endregion

        #region Constants

        public const int TOKEN_DUPLICATE = 0x0002;
        public const uint MAXIMUM_ALLOWED = 0x2000000;
        public const int CREATE_NEW_CONSOLE = 0x00000010;
        public const int CREATE_NO_WINDOW = 0x08000000;
        public const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        public const int IDLE_PRIORITY_CLASS = 0x40;
        public const int NORMAL_PRIORITY_CLASS = 0x20;
        public const int HIGH_PRIORITY_CLASS = 0x80;
        public const int REALTIME_PRIORITY_CLASS = 0x100;

        #endregion

        #region Win32 API Imports

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll")]
        static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public extern static bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes, bool bInheritHandle, int dwCreationFlags, IntPtr lpEnvironment,
            string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll")]
        static extern bool ProcessIdToSessionId(uint dwProcessId, ref uint pSessionId);

        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        public extern static bool DuplicateTokenEx(IntPtr ExistingTokenHandle, uint dwDesiredAccess,
            IntPtr lpThreadAttributes, int TokenType,
            int ImpersonationLevel, ref IntPtr DuplicateTokenHandle);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("advapi32", SetLastError = true), SuppressUnmanagedCodeSecurity]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);

        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool CreateEnvironmentBlock(ref IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern uint WTSQueryUserToken(uint SessionId, ref IntPtr phToken);

        #endregion

        /// <summary>
        /// 使用完全的管理员权限启动指定路径的程序，并且绕过Windows系统的UAC
        /// </summary>
        /// <param name="applicationFullPath">要启动的应用程序的完全路径</param>
        /// <param name="startingDir">程序启动时的工作目录，通常传递要启动的程序所在目录即可，特殊情况包括需要在指定的文件夹打开cmd窗口等</param>
        /// <param name="procInfo">创建完成的进程信息</param>
        /// <param name="commandLine">表示要使用的命令内容，比如需要启动一个cmd程序，因为获取其真实路径比较麻烦，此时可以直接传"cmd"，要启动的应用程序路径留空即可</param>
        /// <returns>创建完成的进程信息</returns>
        public static bool StartProcessAndBypassUAC(string applicationFullPath, string startingDir, out PROCESS_INFORMATION procInfo, string commandLine = null)
        {
            IntPtr hUserTokenDup = IntPtr.Zero;
            IntPtr hPToken = IntPtr.Zero;
            IntPtr hProcess = IntPtr.Zero;
            IntPtr pEnv = IntPtr.Zero;

            procInfo = new PROCESS_INFORMATION();

            // 获取当前正在使用的系统用户的session id,每一个登录到系统的用户都有一个唯一的session id
            // 这一步是为了可以正确在当前登录的用户界面启动程序
            uint dwSessionId = WTSGetActiveConsoleSessionId();

            if (WTSQueryUserToken(dwSessionId, ref hPToken) == 0)
            {
                CloseHandle(hPToken);
                return false;
            }

            // 复制winlogon进程的访问令牌，产生一个新令牌
            if (!DuplicateTokenEx(hPToken, MAXIMUM_ALLOWED, IntPtr.Zero, (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, (int)TOKEN_TYPE.TokenPrimary, ref hUserTokenDup))
            {
                CloseHandle(hProcess);
                CloseHandle(hPToken);
                return false;
            }

            // lpDesktop参数是用来设置程序启动的界面，这里设置的参数是winsta0\default，代表交互式用户的默认桌面
            STARTUPINFO si = new STARTUPINFO();
            si.cb = (int)Marshal.SizeOf(si);
            si.lpDesktop = @"winsta0\default";

            // 指定进程的优先级和创建方法，这里代表是普通优先级，并且创建方法是带有UI的进程
            int dwCreationFlags = CREATE_UNICODE_ENVIRONMENT | NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE;
            
            // 创建新进程的环境变量
            if (!CreateEnvironmentBlock(ref pEnv, hUserTokenDup, false))
            {
                CloseHandle(hProcess);
                CloseHandle(hPToken);
                CloseHandle(pEnv);
                return false;
            }

            // 在当前用户的session中创建一个新进程
            bool result = CreateProcessAsUser(hUserTokenDup,        // 用户的访问令牌
                                            applicationFullPath,    // 要执行的程序的路径
                                            commandLine,            // 命令行内容
                                            IntPtr.Zero,            // 设置进程的SECURITY_ATTRIBUTES，主要用来控制对象的访问权限，这里传空值
                                            IntPtr.Zero,            // 设置线程的SECURITY_ATTRIBUTES，控制对象的访问权限，这里传空值
                                            false,                  // 开启的进程不需继承句柄
                                            dwCreationFlags,        // 创建标识
                                            pEnv,            // 传空，获取新的环境变量的拷贝 
                                            startingDir,            // 程序启动时的工作目录，通常传递要启动的程序所在目录即可 
                                            ref si,                 // 启动信息
                                            out procInfo            // 用于接收新创建的进程的信息
                                            );

            // 关闭句柄
            CloseHandle(hProcess);
            CloseHandle(hPToken);
            CloseHandle(hUserTokenDup);
            CloseHandle(pEnv);

            return result;
        }

        /// <summary>
        /// 执行命令行并且获取输出的内容(包括输出内容和错误内容)
        /// </summary>
        /// <param name="cmd">命令行内容</param>
        public static bool RunCmdAndGetOutput(string cmd, out string output, out string error)
        {
            using (var process = new Process())
            {
                output = "";
                error = "";
                string outputContent = "";
                string outputError = "";
                ProcessStartInfo processInfo = new ProcessStartInfo();
                // 隐藏可视化界面
                processInfo.CreateNoWindow = true;
                processInfo.UseShellExecute = false;
                // 对标准输出流和错误流进行重定向
                processInfo.RedirectStandardOutput = true;
                processInfo.RedirectStandardError = true;
                process.OutputDataReceived += (_, e) =>
                {
                    outputContent += e.Data;
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    outputError += e.Data;
                };

                // 对标准输入流进行重定向
                processInfo.RedirectStandardInput = true;
                processInfo.FileName = "cmd.exe";
                processInfo.Arguments = cmd;
                processInfo.Verb = "runas"; // 提升cmd程序的权限
                process.StartInfo = processInfo;

                if (!process.Start())
                {
                    return false;
                }

                process.StandardInput.WriteLine(cmd);
                process.StandardInput.WriteLine("exit");

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                output = outputContent;
                error = outputError;

                return true;
            }
        }
    }
}
