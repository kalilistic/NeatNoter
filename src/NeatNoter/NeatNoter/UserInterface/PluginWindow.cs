using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace NeatNoter
{
    /// <summary>
    /// Plugin window which extends window with NeatNoter.
    /// </summary>
    public abstract class PluginWindow : Window
    {
        /// <summary>
        /// Gets NeatNoter for window.
        /// </summary>
        protected NeatNoterPlugin plugin;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginWindow"/> class.
        /// </summary>
        /// <param name="plugin">NeatNoter plugin.</param>
        /// <param name="windowName">Name of the window.</param>
        /// <param name="flags">ImGui flags.</param>
        protected PluginWindow(NeatNoterPlugin plugin, string windowName, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
            : base(windowName, flags)
        {
            this.plugin = plugin;
            this.RespectCloseHotkey = false;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
        }
    }
}
