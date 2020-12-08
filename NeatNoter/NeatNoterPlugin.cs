using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;
using System.IO;
using System.Linq;

namespace NeatNoter
{
    public class NeatNoterPlugin : IDalamudPlugin
    {
        private DalamudPluginInterface pluginInterface;
        private NeatNoterConfiguration config;
        private NeatNoterUI ui;
        private Notebook notebook;

        public string Name => "NeatNoter";

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            this.config = (NeatNoterConfiguration)this.pluginInterface.GetPluginConfig() ?? new NeatNoterConfiguration();
            this.config.Initialize(this.pluginInterface, () =>
            {
                if (this.config.JustInstalled)
                {
                    this.pluginInterface.Framework.Gui.Chat.Print("NoteNoter has been installed! Type /notebook to open the notebook.");
                    this.config.JustInstalled = false;
                }
            });

            this.notebook = new Notebook(this.config, this.pluginInterface);

            this.ui = new NeatNoterUI(this.notebook, this.config);
            this.pluginInterface.UiBuilder.OnBuildUi += this.ui.Draw;
            
            AddComandHandlers();
        }

        private void ToggleNotebook(string command, string args)
        {
            this.ui.IsVisible = !this.ui.IsVisible;
        }

        private void AddComandHandlers()
        {
            this.pluginInterface.CommandManager.AddHandler("/notebook", new CommandInfo(ToggleNotebook)
            {
                HelpMessage = "Open/close the NeatNoter notebook.",
                ShowInHelp = true,
            });
        }

        private void RemoveCommandHandlers()
        {
            this.pluginInterface.CommandManager.RemoveHandler("/notebook");
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                RemoveCommandHandlers();
                
                this.pluginInterface.SavePluginConfig(this.config);

                this.pluginInterface.UiBuilder.OnBuildUi -= this.ui.Draw;
                this.ui.Dispose();

                this.pluginInterface.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
