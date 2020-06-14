using NeatNoter.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using Dalamud.Plugin;

namespace NeatNoter
{
    internal class Notebook
    {
        private readonly DalamudPluginInterface pluginInterface;
        private readonly NeatNoterConfiguration config;

        private bool Saving { get; set; }
        public bool Loading { get; private set; }
        public List<Category> Categories { get => this.config.Categories; set => this.config.Categories = value; }
        public List<Note> Notes { get => this.config.Notes; set => this.config.Notes = value; }

        public Notebook(NeatNoterConfiguration config, DalamudPluginInterface pluginInterface)
        {
            this.config = config;
            this.pluginInterface = pluginInterface;
        }

        public Note CreateNote()
        {
            var uid = DateTime.Now.ToBinary();
            var note = new Note
            {
                InternalName = "New Note##" + uid,
                Body = string.Empty,
                Categories = new List<Category>(),
                Images = new List<Image>(),
                Lines = new List<(Vector2, Vector2, Vector3, float)>(),
            };
            Notes.Insert(0, note);
            return note;
        }

        public Category CreateCategory()
        {
            var uid = DateTime.Now.ToBinary();
            var rand = new Random();
            var category = new Category
            {
                InternalName = "New Category##" + uid,
                Body = "Category description",
                Color = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()),
                Images = new List<Image>(),
                Lines = new List<(Vector2, Vector2, Vector3, float)>(),
            };
            Categories.Insert(0, category);
            return category;
        }

        public void DeleteNote(Note note) => Notes.Remove(note);

        public void DeleteCategory(Category category)
        {
            foreach (var note in Notes)
            {
                note.Categories.Remove(category);
            }
            Categories.Remove(category);
        }

        public IEnumerable<Note> SearchFor(string fragment, bool includeBodies)
        {
            if (includeBodies)
                return Notes.Where(note => note.Name.Contains(fragment) || note.Body.Contains(fragment));
            return Notes.Where(note => note.Name.Contains(fragment));
        }

        public void CreateBackup()
        {
            if (Saving)
                return;
            Saving = true;

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
                    Saving = false;
                    return;
                }
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
                Saving = false;
                return;
            }

            dynamic obj = new ExpandoObject();
            obj.Notes = Notes;
            obj.Categories = Categories;
            obj.NotesReadable = Notes.ToDictionary(note => note.InternalName, note => note.Body);
            obj.CategoriesReadable = Categories.ToDictionary(category => category.InternalName, category => category.Body);

            File.WriteAllText(saveFileDialogue.FileName, JsonConvert.SerializeObject(obj));

            Saving = false;
        }

        public void LoadBackup()
        {
            if (Loading)
                return;
            Loading = true;

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
                    Loading = false;
                    return;
                }
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
                Loading = false;
                return;
            }

            var json = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
            var importedNotes = json["Notes"].ToObject<List<Note>>();
            var importedCategories = json["Categories"].ToObject<List<Category>>();
            importedNotes.InitializeAll(pluginInterface);
            importedCategories.InitializeAll(pluginInterface);
            Notes = importedNotes;
            Categories = importedCategories;

            Loading = false;

            this.config.Save();
        }
    }

    public static class NotebookExtensions
    {
        public static IEnumerable<Note> FilterByCategories(this IEnumerable<Note> notes, IEnumerable<Category> categories)
            => notes.Where(note => categories.All(category => note.Categories.Contains(category)));

        public static List<T> Alphabetize<T>(this IEnumerable<T> documents, SortDirection direction) where T : UniqueDocument
        {
            var docList = documents.ToList();
            docList.Sort((a, b) => direction == SortDirection.Ascending
                ? string.Compare(a.Name, b.Name, StringComparison.Ordinal)
                : string.Compare(b.Name, a.Name, StringComparison.Ordinal));
            return docList;
        }

        public static void InitializeAll<T>(this IList<T> list, DalamudPluginInterface pluginInterface) where T : UniqueDocument
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            foreach (var document in list)
            {
                document.DecompressBody();

                // v1.1 compat
                if (document.Images == null)
                {
                    document.Images = new List<Image>();
                }
                if (document.Lines == null)
                {
                    document.Lines = new List<(Vector2, Vector2, Vector3, float)>();
                }

                foreach (var image in document.Images)
                {
                    image.Initialize(pluginInterface.UiBuilder);
                }
            }
        }
    }

    public enum SortDirection
    {
        Ascending,
        Descending,
    }
}
