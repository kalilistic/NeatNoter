using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dalamud.DrunkenToad;

#pragma warning disable 618

namespace NeatNoter
{
    /// <summary>
    /// Migrate schema to newer versions.
    /// </summary>
    public static class Migrator
    {
        /// <summary>
        /// Migrate schema.
        /// </summary>
        /// <param name="plugin">plugin.</param>
        /// <returns>indicator if migration successful.</returns>
        public static bool Migrate(NeatNoterPlugin plugin)
        {
            try
            {
                // Migrate v1 -> v2
                if (plugin.NotebookService.GetVersion() == 0)
                {
                    // backup config file with embedded notes/categories
                    File.Copy(
                        NeatNoterPlugin.PluginInterface.ConfigFile.ToString(),
                        NeatNoterPlugin.PluginInterface.GetPluginConfigDirectory() + "/data/NeatNoter.json");
                    plugin.BackupManager.CreateBackup(
                        "upgrade/v" + plugin.Configuration.PluginVersion + "_");

                    // get data from config file
                    var notes = plugin.Configuration.Notes;
                    var categories = plugin.Configuration.Categories;
                    foreach (var note in notes)
                    {
                        note.DecompressBody();
                        note.IsVisible = true;
                        foreach (var category in categories)
                        {
                            category.DecompressBody();
                            category.IsVisible = true;
                        }
                    }

                    foreach (var category in categories)
                    {
                        category.DecompressBody();
                        category.IsVisible = true;
                    }

                    // update db
                    plugin.NotebookService.SaveNotes(notes);
                    plugin.NotebookService.SaveCategories(categories);
                    plugin.Configuration.Notes = new List<Note>();
                    plugin.Configuration.Categories = new List<Category>();
                    plugin.Configuration.IsNoCategorySelected = true;
                    plugin.SaveConfig();

                    // load data
                    plugin.NotebookService.SetVersion(2);
                    plugin.NotebookService.LoadDocuments();

                    // reassign categories to fix IDs
                    notes = plugin.NotebookService.GetNotes();
                    categories = plugin.NotebookService.GetCategories();
                    foreach (var note in notes)
                    {
                        for (var i = 0; i < note.Categories.Count; i++)
                        {
                            var matchingCategory = categories.FirstOrDefault(
                                category => category.InternalName.Equals(note.Categories[i].InternalName));
                            if (matchingCategory == null) continue;
                            note.Categories[i] = matchingCategory;
                        }
                    }

                    // save & reload data
                    plugin.NotebookService.SaveNotes(notes);
                    plugin.NotebookService.LoadDocuments();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to migrate.");
                return false;
            }

            return true;
        }
    }
}
