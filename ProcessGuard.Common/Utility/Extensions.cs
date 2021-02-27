using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ProcessGuard.Common.Utility
{
    public static class Extensions
    {

        [DllImport("Kernel32.dll")]
        private static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);

        /// <summary>
        /// 获取进程的完整可执行文件名称
        /// </summary>
        /// <param name="process">进程实体</param>
        /// <param name="buffer">缓冲区大小，用于存放文件名称</param>
        /// <returns></returns>
        public static string GetMainModuleFileName(this Process process, int buffer = 1024)
        {
            var fileNameBuilder = new StringBuilder(buffer);
            uint bufferLength = (uint)fileNameBuilder.Capacity + 1;
            return QueryFullProcessImageName(process.Handle, 0, fileNameBuilder, ref bufferLength) ?
                fileNameBuilder.ToString() :
                null;
        }
    }
}
