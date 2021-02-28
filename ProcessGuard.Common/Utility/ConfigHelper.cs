using ProcessGuard.Common.Models;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;

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
            ObservableCollection<ConfigItem> result = null;

            if (!File.Exists(Constants.CONFIG_FILE_NAME))
            {
                File.WriteAllText(Constants.CONFIG_FILE_NAME, "");
            }

            var json = File.ReadAllText(Constants.CONFIG_FILE_NAME);
            result = json.DeserializeObject<ObservableCollection<ConfigItem>>();

            return result ?? new ObservableCollection<ConfigItem>();
        }


        /// <summary>
        /// 保存配置项
        /// </summary>
        /// <param name="configItems">配置项实体集合</param>
        public static void SaveConfigs(ObservableCollection<ConfigItem> configItems)
        {
            File.WriteAllText(Constants.CONFIG_FILE_NAME, configItems.Serialize());
        }
    }
}
