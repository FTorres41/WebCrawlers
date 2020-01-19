namespace RSBM
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.RServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.RServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // RServiceProcessInstaller
            // 
            this.RServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.RServiceProcessInstaller.Password = null;
            this.RServiceProcessInstaller.Username = null;
            // 
            // RServiceInstaller
            // 
            this.RServiceInstaller.Description = "Manage robots";
            this.RServiceInstaller.DisplayName = "RService";
            this.RServiceInstaller.ServiceName = "RService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.RServiceProcessInstaller,
            this.RServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller RServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller RServiceInstaller;
    }
}