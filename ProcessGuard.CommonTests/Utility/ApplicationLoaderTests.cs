using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProcessGuard.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ProcessGuard.Common.Utility.Tests
{
    [TestClass()]
    public class ApplicationLoaderTests
    {
        [TestMethod()]
        public void RunCmdAndGetOutputTest()
        {
            string cmd = @"%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe ProcessGuardService.exe
                            Net Start ProcessGuardService
                            sc config ProcessGuardService = auto";
            ApplicationLoader.RunCmdAndGetOutput(cmd, out var output,out var error);
            Assert.AreNotEqual(output, "");
            Assert.AreNotEqual(error, "");
        }

        [TestMethod()]
        public void StartAppTest()
        {
            string filePath = @"E:\Program Files\Everything-1.4.1.1005.x64\Everything.exe";
            ApplicationLoader.StartProcessInSession0(filePath,System.IO.Path.GetDirectoryName(filePath), out var _);
        }
    }
}