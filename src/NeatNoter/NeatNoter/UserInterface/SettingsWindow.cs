using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace NeatNoter
{
    /// <summary>
    /// Settings window for the plugin.
    /// </summary>
    public class SettingsWindow : PluginWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        /// <param name="plugin">NeatNoter plugin.</param>
        public SettingsWindow(NeatNoterPlugin plugin)
            : base(plugin, Loc.Localize("Settings", "NeatNoter Settings"))
        {
            this.plugin = plugin;
            this.RespectCloseHotkey = true;
            this.Size = new Vector2(300f, 250f);
            this.SizeCondition = ImGuiCond.Appearing;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            this.DrawSearch();
            this.DrawBackup();
        }

        private void DrawSearch()
        {
            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Search", "Search"));
            ImGui.BeginChild("###Search", new Vector2(-1, 40f), true);
            {
                var includeBodies = this.plugin.Configuration.IncludeNoteBodiesInSearch;
                if (ImGui.Checkbox(Loc.Localize("IncludeNoteContents", "Include note contents"), ref includeBodies))
                {
                    this.plugin.Configuration.IncludeNoteBodiesInSearch = includeBodies;
                }
            }

            ImGui.EndChild();
            ImGui.Spacing();
        }

        private void DrawBackup()
        {
            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Backup", "Backup"));
            ImGui.BeginChild("###Backup", new Vector2(-1, 95f), true);
            {
                if (ImGui.Button(Loc.Localize("Export", "Export") + "###NeatNoter"))
                {
                    Task.Run(() => this.plugin.NotebookService.CreateBackup());
                }

                ImGui.SameLine();
                if (ImGui.Button(Loc.Localize("Import", "Import") + "###NeatNoter"))
                {
                    Task.Run(() => this.plugin.NotebookService.LoadBackup());
                }

                ImGui.Spacing();
                ImGui.Separator();

                ImGui.Text(Loc.Localize("SecondaryBackupDirectoryPath", "Secondary Backup Directory"));
                var existing = this.plugin.Configuration.AutomaticExportPath ?? string.Empty;
                if (ImGui.InputText("####SecondaryBackupPath", ref existing, 350000))
                {
                    this.plugin.Configuration.AutomaticExportPath = existing;
                    this.plugin.SaveConfig();
                }

                ImGui.SameLine();
                if (ImGui.Button(Loc.Localize("Browse", "Browse")))
                {
                    Task.Run(() => this.plugin.NotebookService.CreateBackup(true));
                }

                if (this.plugin.NotebookService.TempExportPath != null)
                {
                    this.plugin.Configuration.AutomaticExportPath = this.plugin.NotebookService.TempExportPath;
                    this.plugin.NotebookService.TempExportPath = null;
                    this.plugin.SaveConfig();
                }
            }

            ImGui.EndChild();
        }
    }
}
