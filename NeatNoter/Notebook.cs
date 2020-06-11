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

namespace NeatNoter
{
    internal class Notebook
    {
        public List<Category> Categories { get; set; }
        public List<Note> Notes { get; set; }

        public Notebook(NeatNoterConfiguration config)
        {
            Categories = config.Categories;
            Notes = config.Notes;
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
            using var saveFileDialogue = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|All files (*.*)",
                AddExtension = true,
                AutoUpgradeEnabled = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                RestoreDirectory = true,
            };
            if (saveFileDialogue.ShowDialog() != DialogResult.OK)
                return;

            dynamic obj = new ExpandoObject();
            obj.Notes = Notes;
            obj.Categories = Categories;
            File.WriteAllText(saveFileDialogue.FileName, JsonConvert.SerializeObject(obj));
        }

        public void LoadBackup()
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|All files (*.*)",
                AddExtension = true,
                AutoUpgradeEnabled = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                RestoreDirectory = true,
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var json = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
            Notes = json["Notes"].ToObject<List<Note>>();
            Categories = json["Categories"].ToObject<List<Category>>();
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
    }

    public enum SortDirection
    {
        Ascending,
        Descending,
    }
}
