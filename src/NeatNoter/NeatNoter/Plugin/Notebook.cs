using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

using Dalamud.Logging;
using Dalamud.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NeatNoter
{
    /// <summary>
    /// Notebook.
    /// </summary>
    public class Notebook
    {
        private readonly DalamudPluginInterface pluginInterface;
        private readonly NeatNoterConfiguration config;

        /// <summary>
        /// Initializes a new instance of the <see cref="Notebook"/> class.
        /// </summary>
        /// <param name="config">plugin config.</param>
        /// <param name="pluginInterface">plugin interface.</param>
        public Notebook(NeatNoterConfiguration config, DalamudPluginInterface pluginInterface)
        {
            this.config = config;
            this.pluginInterface = pluginInterface;
        }

        /// <summary>
        /// Gets a value indicating whether is loading.
        /// </summary>
        public bool Loading { get; private set; }

        /// <summary>
        /// Gets or sets temporary export path.
        /// </summary>
        public string? TempExportPath { get; set; }

        /// <summary>
        /// Gets or sets list of categories.
        /// </summary>
        public List<Category> Categories { get => this.config.Categories; set => this.config.Categories = value; }

        /// <summary>
        /// Gets or sets list of notes.
        /// </summary>
        public List<Note> Notes { get => this.config.Notes; set => this.config.Notes = value; }

        private bool Saving { get; set; }

        /// <summary>
        /// Create note.
        /// </summary>
        /// <returns>new note.</returns>
        public Note CreateNote()
        {
            var uid = DateTime.Now.ToBinary();
            var note = new Note
            {
                InternalName = "New Note##" + uid,
                Body = string.Empty,
                Categories = new List<Category>(),
                Lines = new List<(Vector2, Vector2, Vector3, float)>(),
            };
            this.Notes.Insert(0, note);
            return note;
        }

        /// <summary>
        /// Create category.
        /// </summary>
        /// <returns>new category.</returns>
        public Category CreateCategory()
        {
            var uid = DateTime.Now.ToBinary();
            var rand = new Random();
            var category = new Category
            {
                InternalName = "New Category##" + uid,
                Body = "Category description",
                Color = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()),
                Lines = new List<(Vector2, Vector2, Vector3, float)>(),
            };
            this.Categories.Insert(0, category);
            return category;
        }

        /// <summary>
        /// Delete note.
        /// </summary>
        /// <param name="note">note to delete.</param>
        public void DeleteNote(Note? note) => this.Notes.Remove(note!);

        /// <summary>
        /// Delete category.
        /// </summary>
        /// <param name="category">category to delete.</param>
        public void DeleteCategory(Category? category)
        {
            foreach (var note in this.Notes)
            {
                note.Categories.Remove(category!);
            }

            this.Categories.Remove(category!);
        }

        /// <summary>
        /// Search for fragment.
        /// </summary>
        /// <param name="fragment">fragment.</param>
        /// <param name="includeBodies">check note bodies.</param>
        /// <returns>note results.</returns>
        public IEnumerable<Note> SearchFor(string fragment, bool includeBodies)
        {
            if (includeBodies)
                return this.Notes.Where(note => note.Name.Contains(fragment) || note.Body.Contains(fragment));
            return this.Notes.Where(note => note.Name.Contains(fragment));
        }

        /// <summary>
        /// Create backup.
        /// </summary>
        /// <param name="writeAutomaticPath">automatic path.</param>
        public void CreateBackup(bool writeAutomaticPath = false)
        {
            if (this.Saving)
                return;
            this.Saving = true;

            using var saveFileDialogue = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                AddExtension = true,
                AutoUpgradeEnabled = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                RestoreDirectory = true,
                ShowHelp = true,
            };
            try
            {
                if (saveFileDialogue.ShowDialog(null) != DialogResult.OK)
                {
                    this.Saving = false;
                    return;
                }
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
                this.Saving = false;
                return;
            }

            this.SaveBackup(saveFileDialogue.FileName);

            if (writeAutomaticPath)
            {
                this.TempExportPath = saveFileDialogue.FileName;
            }

            this.Saving = false;
        }

        /// <summary>
        /// Save backup.
        /// </summary>
        /// <param name="path">file path.</param>
        public void SaveBackup(string? path)
        {
            dynamic obj = new ExpandoObject();
            obj.Notes = this.Notes;
            obj.Categories = this.Categories;
            obj.NotesReadable = this.Notes.ToDictionary(note => note.InternalName, note => note.Body);
            obj.CategoriesReadable = this.Categories.ToDictionary(category => category.InternalName, category => category.Body);

            File.WriteAllText(path, JsonConvert.SerializeObject(obj));
        }

        /// <summary>
        /// Load backup.
        /// </summary>
        public void LoadBackup()
        {
            if (this.Loading)
                return;
            this.Loading = true;

            using var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                AddExtension = true,
                AutoUpgradeEnabled = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                RestoreDirectory = true,
                ShowHelp = true,
            };
            try
            {
                if (openFileDialog.ShowDialog(null) != DialogResult.OK)
                {
                    this.Loading = false;
                    return;
                }
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
                this.Loading = false;
                return;
            }

            var json = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
            var importedNotes = json[nameof(this.Notes)]?.ToObject<List<Note>>();
            var importedCategories = json[nameof(this.Categories)]?.ToObject<List<Category>>();
            importedNotes!.InitializeAll(this.pluginInterface);
            importedCategories!.InitializeAll(this.pluginInterface);
            this.Notes = importedNotes!;
            this.Categories = importedCategories!;

            this.Loading = false;

            this.config.Save();
        }
    }
}
