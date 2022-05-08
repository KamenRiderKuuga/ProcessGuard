using System;

namespace ProcessGuard.Common.Models
{
    public class ConfigItemWithProcessId : ConfigItem
    {
        /// <summary>
        /// The processId of started process
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// The config change type
        /// </summary>
        public ChangeType ChangeType { get; set; }
    }

    /// <summary>
    /// The config change type
    /// </summary>
    [Flags]
    public enum ChangeType
    {
        None = 0,

        /// <summary>
        /// The process should be started
        /// </summary>
        Start = 1,

        /// <summary>
        /// The process should be stopped
        /// </summary>
        Stop = 2,

        /// <summary>
        /// The process should be removed from the guard list
        /// </summary>
        Remove = 4,
    }
}
