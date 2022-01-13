using System;
using System.Reflection;
using System.Timers;

using Dalamud.DrunkenToad;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;

namespace NeatNoter
{
    /// <inheritdoc />
    public class NeatNoterPlugin : IDalamudPlugin
    {
        /// <summary>
        /// Plugin configuration.
        /// </summary>
        public readonly NeatNoterConfiguration Configuration;

        /// <summary>
        /// Notebook.
        /// </summary>
        public readonly NotebookService NotebookService;

        /// <summary>
        /// Window Manager.
        /// </summary>
        public readonly WindowManager WindowManager = null!;

        /// <summary>
        /// Backup manager.
        /// </summary>
        public BackupManager BackupManager;

        private readonly Timer backupTimer;
        private readonly Localization localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="NeatNoterPlugin"/> class.
        /// </summary>
        public NeatNoterPlugin()
        {
            // load config
            try
            {
                this.Configuration = PluginInterface.GetPluginConfig() as NeatNoterConfiguration ??
                                     new NeatNoterConfiguration();
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to load config so creating new one.", ex);
                this.Configuration = new NeatNoterConfiguration();
                this.SaveConfig();
            }

            // load services
            this.localization = new Localization(PluginInterface, CommandManager);
            this.BackupManager = new BackupManager(PluginInterface.GetPluginConfigDirectory());
            this.NotebookService = new NotebookService(this);

            // run backup
            this.backupTimer = new Timer { Interval = this.Configuration.BackupFrequency, Enabled = false };
            this.backupTimer.Elapsed += this.BackupTimerOnElapsed;
            var pluginVersion = Assembly.GetExecutingAssembly().VersionNumber();
            if (this.Configuration.PluginVersion < pluginVersion)
            {
                Logger.LogInfo("Running backup since new version detected.");
                this.RunUpgradeBackup();
                this.Configuration.PluginVersion = pluginVersion;
                this.SaveConfig();
            }
            else
            {
                this.BackupTimerOnElapsed(this, null);
            }

            // attempt to migrate if needed
            var success = Migrator.Migrate(this);
            if (success)
            {
                this.HandleJustInstalled();
                this.NotebookService.Start();
                this.backupTimer.Enabled = true;
                DocumentSortType.Init();
                this.WindowManager = new WindowManager(this);
                this.PluginCommandManager = new PluginCommandManager(this);
            }
        }

        /// <summary>
        /// Gets pluginInterface.
        /// </summary>
        [PluginService]
        [RequiredVersion("1.0")]
        public static DalamudPluginInterface PluginInterface { get; private set; } = null!;

        /// <summary>
        /// Gets chat gui.
        /// </summary>
        [PluginService]
        [RequiredVersion("1.0")]
        public static ChatGui Chat { get; private set; } = null!;

        /// <summary>
        /// Gets command manager.
        /// </summary>
        [PluginService]
        [RequiredVersion("1.0")]
        public static CommandManager CommandManager { get; private set; } = null!;

        /// <summary>
        /// Gets client state.
        /// </summary>
        [PluginService]
        [RequiredVersion("1.0")]
        public static ClientState ClientState { get; private set; } = null!;

        /// <summary>
        /// Gets or sets command manager to handle user commands.
        /// </summary>
        public PluginCommandManager PluginCommandManager { get; set; } = null!;

        /// <inheritdoc />
        public string Name => "NeatNoter";

        /// <summary>
        /// Get plugin folder.
        /// </summary>
        /// <returns>plugin folder name.</returns>
        public static string GetPluginFolder()
        {
            return PluginInterface.GetPluginConfigDirectory();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Save configuration.
        /// </summary>
        public void SaveConfig()
        {
            PluginInterface.SavePluginConfig(this.Configuration);
        }

        /// <summary>
        /// Dispose plugin.
        /// </summary>
        /// <param name="disposing">indicator whether disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    this.WindowManager.Dispose();
                    this.backupTimer.Elapsed -= this.BackupTimerOnElapsed;
                    this.backupTimer.Dispose();
                    this.PluginCommandManager.Dispose();
                    PluginInterface.SavePluginConfig(this.Configuration);
                    this.NotebookService.Dispose();
                    this.localization.Dispose();
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to dispose properly.");
            }
        }

        private void HandleJustInstalled()
        {
            if (!this.Configuration.JustInstalled)
            {
                return;
            }

            this.NotebookService.SetVersion(2);
            Chat.PluginPrintNotice("NoteNoter has been installed! Type /notebook to open the notebook.");
            this.Configuration.JustInstalled = false;
            this.SaveConfig();
        }

        private void BackupTimerOnElapsed(object sender, ElapsedEventArgs? e)
        {
            if (DateUtil.CurrentTime() > this.Configuration.LastBackup + this.Configuration.BackupFrequency)
            {
                Logger.LogInfo("Running backup due to frequency timer.");
                this.Configuration.LastBackup = DateUtil.CurrentTime();
                this.BackupManager.CreateBackup();
                this.BackupManager.DeleteBackups(this.Configuration.BackupRetention);
            }
        }

        private void RunUpgradeBackup()
        {
            this.Configuration.LastBackup = DateUtil.CurrentTime();
            this.BackupManager.CreateBackup("upgrade/v" + this.Configuration.PluginVersion + "_");
            this.BackupManager.DeleteBackups(this.Configuration.BackupRetention);
        }
    }
}
