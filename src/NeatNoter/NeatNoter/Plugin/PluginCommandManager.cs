using Dalamud.Game.Command;

namespace NeatNoter
{
    /// <summary>
    /// Manage plugin commands.
    /// </summary>
    public class PluginCommandManager
    {
        private readonly NeatNoterPlugin plugin;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginCommandManager"/> class.
        /// </summary>
        /// <param name="plugin">plugin.</param>
        public PluginCommandManager(NeatNoterPlugin plugin)
        {
            this.plugin = plugin;
            NeatNoterPlugin.CommandManager.AddHandler("/notebook", new CommandInfo(this.ToggleNotebook)
            {
                HelpMessage = "Open/close the NeatNoter notebook.",
                ShowInHelp = true,
            });
            NeatNoterPlugin.CommandManager.AddHandler("/notebookconfig", new CommandInfo(this.ToggleSettings)
            {
                HelpMessage = "Open/close the NeatNoter settings.",
                ShowInHelp = true,
            });
        }

        /// <summary>
        /// Dispose command manager.
        /// </summary>
        public void Dispose()
        {
            NeatNoterPlugin.CommandManager.RemoveHandler("/notebook");
            NeatNoterPlugin.CommandManager.RemoveHandler("/notebookconfig");
        }

        private void ToggleNotebook(string command, string args)
        {
            this.plugin.WindowManager.NotebookWindow?.Toggle();
        }

        private void ToggleSettings(string command, string arguments)
        {
            this.plugin.WindowManager.SettingsWindow?.Toggle();
        }
    }
}
