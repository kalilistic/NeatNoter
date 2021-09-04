using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;

namespace NeatNoter
{
    public class NeatNoterPlugin : IDalamudPlugin
    {
        public NeatNoterPlugin()
        {
            this.config = (NeatNoterConfiguration)PluginInterface.GetPluginConfig()! ?? new NeatNoterConfiguration();
            this.config.Initialize(() =>
            {
                if (this.config.JustInstalled)
                {
                    Chat.Print("NoteNoter has been installed! Type /notebook to open the notebook.");
                    this.config.JustInstalled = false;
                }
            });

            this.notebook = new Notebook(this.config, PluginInterface);

            this.ui = new NeatNoterUI(this.notebook, this.config);
            PluginInterface.UiBuilder.Draw += this.ui.Draw;
            
            AddComandHandlers();
        }
        
        private NeatNoterConfiguration config;
        private NeatNoterUI ui;
        private Notebook notebook;

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
        
        public string Name => "NeatNoter";

        private void ToggleNotebook(string command, string args)
        {
            this.ui.IsVisible = !this.ui.IsVisible;
        }

        private void AddComandHandlers()
        {
            CommandManager.AddHandler("/notebook", new CommandInfo(ToggleNotebook)
            {
                HelpMessage = "Open/close the NeatNoter notebook.",
                ShowInHelp = true,
            });
        }

        private void RemoveCommandHandlers()
        {
            CommandManager.RemoveHandler("/notebook");
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    RemoveCommandHandlers();

                    PluginInterface.SavePluginConfig(this.config);

                    PluginInterface.UiBuilder.Draw -= this.ui.Draw;
                    this.ui.Dispose();

                    PluginInterface.Dispose();
                }
            }
            catch(Exception ex)
            {
                PluginLog.Error(ex, "Failed to dispose properly.");
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
