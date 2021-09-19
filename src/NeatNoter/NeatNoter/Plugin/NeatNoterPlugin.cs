using System;

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
        private readonly NeatNoterConfiguration config;
        private readonly NeatNoterUI ui;

        /// <summary>
        /// Initializes a new instance of the <see cref="NeatNoterPlugin"/> class.
        /// </summary>
        public NeatNoterPlugin()
        {
            this.config = (NeatNoterConfiguration)PluginInterface.GetPluginConfig() !;
            this.config.Initialize(() =>
            {
                if (this.config.JustInstalled)
                {
                    Chat.Print("NoteNoter has been installed! Type /notebook to open the notebook.");
                    this.config.JustInstalled = false;
                }
            });

            var notebook = new Notebook(this.config, PluginInterface);

            this.ui = new NeatNoterUI(notebook, this.config);
            PluginInterface.UiBuilder.Draw += this.ui.Draw;

            this.AddCommandHandlers();
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

        /// <inheritdoc />
        public string Name => "NeatNoter";

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
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
                    RemoveCommandHandlers();

                    PluginInterface.SavePluginConfig(this.config);

                    PluginInterface.UiBuilder.Draw -= this.ui.Draw;
                    this.ui.Dispose();

                    PluginInterface.Dispose();
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to dispose properly.");
            }
        }

        private static void RemoveCommandHandlers()
        {
            CommandManager.RemoveHandler("/notebook");
        }

        private void ToggleNotebook(string command, string args)
        {
            this.ui.IsVisible = !this.ui.IsVisible;
        }

        private void AddCommandHandlers()
        {
            CommandManager.AddHandler("/notebook", new CommandInfo(this.ToggleNotebook)
            {
                HelpMessage = "Open/close the NeatNoter notebook.",
                ShowInHelp = true,
            });
        }
    }
}
