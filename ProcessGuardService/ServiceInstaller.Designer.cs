
namespace ProcessGuardService
{
    partial class ServiceInstaller
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.ProcessGuardServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.ProcessGuardServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // ProcessGuardServiceProcessInstaller
            // 
            this.ProcessGuardServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.ProcessGuardServiceProcessInstaller.Password = null;
            this.ProcessGuardServiceProcessInstaller.Username = null;
            // 
            // ProcessGuardServiceInstaller
            // 
            this.ProcessGuardServiceInstaller.DisplayName = "进程守护服务";
            this.ProcessGuardServiceInstaller.ServiceName = "ProcessGuardService";
            this.ProcessGuardServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ServiceInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ProcessGuardServiceInstaller,
            this.ProcessGuardServiceProcessInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller ProcessGuardServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller ProcessGuardServiceInstaller;
    }
}