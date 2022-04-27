using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProcessGuard.Common.Utility
{
    public class ApplicationLoader
    {
        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
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

        private enum TOKEN_TYPE : int
        {
            TokenPrimary = 1,
            TokenImpersonation = 2
        }

        private enum SECURITY_IMPERSONATION_LEVEL : int
        {
            SecurityAnonymous = 0,
            SecurityIdentification = 1,
            SecurityImpersonation = 2,
            SecurityDelegation = 3,
        }

        private enum SW : int
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
        }

        #endregion

        #region Constants

        private const uint MAXIMUM_ALLOWED = 0x2000000;
        private const int CREATE_NEW_CONSOLE = 0x00000010;
        private const int CREATE_NO_WINDOW = 0x08000000;
        private const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        private const int NORMAL_PRIORITY_CLASS = 0x20;
        private const int STARTF_USESHOWWINDOW = 0x00000001;

        #endregion

        #region Win32 API Imports

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private extern static bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes, bool bInheritHandle, int dwCreationFlags, IntPtr lpEnvironment,
            string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        private extern static bool DuplicateTokenEx(IntPtr ExistingTokenHandle, uint dwDesiredAccess,
            IntPtr lpThreadAttributes, int TokenType,
            int ImpersonationLevel, ref IntPtr DuplicateTokenHandle);

        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool CreateEnvironmentBlock(ref IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern uint WTSQueryUserToken(uint SessionId, ref IntPtr phToken);

        #endregion

        /// <summary>
        /// 在Seesion 0，主要是为了在Windows Service中启动带有交互式界面的程序
        /// </summary>
        /// <param name="applicationFullPath">要启动的应用程序的完全路径</param>
        /// <param name="startingDir">程序启动时的工作目录，通常传递要启动的程序所在目录即可，特殊情况包括需要在指定的文件夹打开cmd窗口等</param>
        /// <param name="procInfo">创建完成的进程信息</param>
        /// <param name="minimize">是否最小化窗体</param>
        /// <param name="commandLine">表示要使用的命令内容，比如需要启动一个cmd程序，因为获取其真实路径比较麻烦，此时可以直接传"cmd"，要启动的应用程序路径留空即可</param>
        /// <returns>创建完成的进程信息</returns>
        public static bool StartProcessInSession0(string applicationFullPath, string startingDir, out PROCESS_INFORMATION procInfo, bool minimize = false, string commandLine = null, bool noWindow = false)
        {
            IntPtr hUserTokenDup = IntPtr.Zero;
            IntPtr hPToken = IntPtr.Zero;
            IntPtr pEnv = IntPtr.Zero;
            procInfo = new PROCESS_INFORMATION();
            bool result = false;

            try
            {
                procInfo = new PROCESS_INFORMATION();

                // 获取当前正在使用的系统用户的session id,每一个登录到系统的用户都有一个唯一的session id
                // 这一步是为了可以正确在当前登录的用户界面启动程序
                uint dwSessionId = WTSGetActiveConsoleSessionId();

                if (WTSQueryUserToken(dwSessionId, ref hPToken) == 0)
                {
                    return false;
                }

                // 复制当前用户的访问令牌，产生一个新令牌
                if (!DuplicateTokenEx(hPToken, MAXIMUM_ALLOWED, IntPtr.Zero, (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, (int)TOKEN_TYPE.TokenPrimary, ref hUserTokenDup))
                {
                    return false;
                }

                // lpDesktop参数是用来设置程序启动的界面，这里设置的参数是winsta0\default，代表交互式用户的默认桌面
                STARTUPINFO si = new STARTUPINFO();
                si.cb = (int)Marshal.SizeOf(si);
                si.lpDesktop = @"winsta0\default";

                if (minimize)
                {
                    si.dwFlags = STARTF_USESHOWWINDOW;
                    si.wShowWindow = (short)SW.SW_MINIMIZE;
                }

                // 指定进程的优先级和创建方法，这里代表是普通优先级，并且创建方法是带有UI的进程
                int dwCreationFlags = CREATE_UNICODE_ENVIRONMENT | NORMAL_PRIORITY_CLASS | (noWindow ? CREATE_NO_WINDOW : CREATE_NEW_CONSOLE);

                // 创建新进程的环境变量
                if (!CreateEnvironmentBlock(ref pEnv, hUserTokenDup, false))
                {
                    return false;
                }

                // 在当前用户的session中创建一个新进程
                result = CreateProcessAsUser(hUserTokenDup,        // 用户的访问令牌
                                               applicationFullPath,    // 要执行的程序的路径
                                               commandLine,            // 命令行内容
                                               IntPtr.Zero,            // 设置进程的SECURITY_ATTRIBUTES，主要用来控制对象的访问权限，这里传空值
                                               IntPtr.Zero,            // 设置线程的SECURITY_ATTRIBUTES，控制对象的访问权限，这里传空值
                                               false,                  // 开启的进程不需继承句柄
                                               dwCreationFlags,        // 创建标识
                                               pEnv,                   // 新的环境变量
                                               startingDir,            // 程序启动时的工作目录，通常传递要启动的程序所在目录即可 
                                               ref si,                 // 启动信息
                                               out procInfo            // 用于接收新创建的进程的信息
                                               );

                if (!result)
                {
                    Debug.WriteLine(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                // 关闭句柄
                CloseHandle(hPToken);
                CloseHandle(hUserTokenDup);
                if (pEnv != IntPtr.Zero)
                {
                    DestroyEnvironmentBlock(pEnv);
                }
                CloseHandle(procInfo.hThread);
                CloseHandle(procInfo.hProcess);
            }

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
