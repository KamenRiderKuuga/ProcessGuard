using System;
using System.IO;
using System.Security.Cryptography;

namespace ProcessGuard.Common.Utility
{
    public class StringUtil
    {
        /// <summary>
        /// 计算文件的MD5值
        /// </summary>
        /// <param name="filename">文件路径</param>
        /// <returns></returns>
        public static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
