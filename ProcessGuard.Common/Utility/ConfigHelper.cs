using ProcessGuard.Common.Models;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System;

namespace ProcessGuard.Common.Utility
{
    /// <summary>
    /// 用于操作配置文件的类
    /// </summary>
    public class ConfigHelper
    {
        /// <summary>
        /// 初始化配置配置，如果没有文件，则创建，如果已经有文件，则从里面初始化配置内容
        /// </summary>
        public static ObservableCollection<ConfigItem> LoadConfigFile()
        {
            var configFilePath = GetAppDataFilePath(Constants.CONFIG_FILE_NAME);

            ObservableCollection<ConfigItem> result = null;

            if (!File.Exists(configFilePath))
            {
                File.WriteAllText(configFilePath, "");
            }

            var json = File.ReadAllText(configFilePath);
            result = json.DeserializeObject<ObservableCollection<ConfigItem>>();

            return result ?? new ObservableCollection<ConfigItem>();
        }


        /// <summary>
        /// 保存配置项
        /// </summary>
        /// <param name="configItems">配置项实体集合</param>
        public static void SaveConfigs(ObservableCollection<ConfigItem> configItems)
        {
            var configFilePath = GetAppDataFilePath(Constants.CONFIG_FILE_NAME);
            File.WriteAllText(configFilePath, configItems.Serialize());
        }

        /// <summary>
        /// 获取在AppData目录的文件位置
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        public static string GetAppDataFilePath(string fileName)
        {
            var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Constants.PROCESS_GUARD_SERVICE);
            
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            return Path.Combine(folderPath, fileName);
        }
    }
}
