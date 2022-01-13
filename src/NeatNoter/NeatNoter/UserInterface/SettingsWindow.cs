using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.DrunkenToad;
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
            this.Size = new Vector2(300f, 450f);
            this.SizeCondition = ImGuiCond.Appearing;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            this.SaveFrequency();
            this.DrawDisplay();
            this.DrawSearch();
            this.DrawBackup();
        }

        private void SaveFrequency()
        {
            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Save", "Save Frequency"));
            ImGui.BeginChild("###Save", new Vector2(-1, 110f), true);
            {
                ImGui.Text(Loc.Localize("SaveFrequency", "Save (seconds)"));
                var saveFrequency = this.plugin.Configuration.SaveFrequency.FromMillisecondsToSeconds();
                if (ImGui.SliderInt("###NeatNoter_SaveFrequency_Slider", ref saveFrequency, 1, 300))
                {
                    this.plugin.Configuration.SaveFrequency = saveFrequency.FromSecondsToMilliseconds();
                    this.plugin.SaveConfig();
                    this.plugin.NotebookService.UpdateSaveFrequency(this.plugin.Configuration.SaveFrequency);
                }

                ImGui.Text(Loc.Localize("FullSaveFrequency", "Full Save (hours)"));
                var fullSaveFrequency = this.plugin.Configuration.FullSaveFrequency.FromMillisecondsToHours();
                if (ImGui.SliderInt("###NeatNoter_FullSaveFrequency_Slider", ref fullSaveFrequency, 1, 12))
                {
                    this.plugin.Configuration.FullSaveFrequency = fullSaveFrequency.FromHoursToMilliseconds();
                    this.plugin.SaveConfig();
                    this.plugin.NotebookService.UpdateFullSaveFrequency(this.plugin.Configuration.FullSaveFrequency);
                }
            }

            ImGui.EndChild();
            ImGui.Spacing();
        }

        private void DrawDisplay()
        {
            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Display", "Display"));
            ImGui.BeginChild("###Display", new Vector2(-1, 40f), true);
            {
                var showContentPreview = this.plugin.Configuration.ShowContentPreview;
                if (ImGui.Checkbox(Loc.Localize("ShowContentPreview", "Show content preview"), ref showContentPreview))
                {
                    this.plugin.Configuration.ShowContentPreview = showContentPreview;
                    this.plugin.SaveConfig();
                }
            }

            ImGui.EndChild();
            ImGui.Spacing();
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
                    this.plugin.SaveConfig();
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
                if (ImGui.Button(Loc.Localize("Export", "Export") + "###NeatNoter_Button_Export"))
                {
                    Task.Run(() => this.plugin.NotebookService.CreateBackup());
                }

                ImGui.SameLine();
                if (ImGui.Button(Loc.Localize("Import", "Import") + "###NeatNoter_Button_Import"))
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
