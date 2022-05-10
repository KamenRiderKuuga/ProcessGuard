using ProcessGuard.Common.Core;

namespace ProcessGuard.Common.Models
{
    /// <summary>
    /// Global configurations of application
    /// </summary>
    public class GlobalConfig : ViewModelBase
    {
        private string _language;

        /// <summary>
        /// language setting of application
        /// </summary>
        public string Language
        {
            get { return _language; }
            set { this.Set(ref this._language, value); }
        }
    }
}
