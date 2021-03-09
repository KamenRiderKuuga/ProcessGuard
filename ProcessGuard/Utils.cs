using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessGuard
{
    public class Utils
    {
        /// <summary>
        /// 弹出文件选择框，并返回选中的文件名
        /// </summary>
        /// <returns></returns>
        public static string GetFileNameByFileDialog()
        {
            string filename = "";

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "可执行文件 (*.exe)|*.exe;";

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                filename = dlg.FileName;
            }

            return filename;
        }
    }
}
