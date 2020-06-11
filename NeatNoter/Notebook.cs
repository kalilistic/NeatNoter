using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Numerics;
using NeatNoter.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NeatNoter
{
    internal class Notebook
    {
        public List<Category> Categories { get; private set; }
        public List<Note> Notes { get; private set; }

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

        public void CreateBackup(string outputLocation)
        {
            dynamic obj = new ExpandoObject();
            obj.Notes = Notes;
            obj.Categories = Categories;
            File.WriteAllText(outputLocation, JsonConvert.SerializeObject(obj));
        }

        public void LoadBackup(string inputLocation)
        {
            var json = JObject.Parse(File.ReadAllText(inputLocation));
            Notes = json["Notes"].ToObject<List<Note>>();
            Categories = json["Categories"].ToObject<List<Category>>();
        }
    }

    public static class NotebookExtensions
    {
        public static IEnumerable<Note> FilterByCategories(this IEnumerable<Note> notes, IEnumerable<Category> categories)
            => notes.Where(note => categories.All(category => note.Categories.Contains(category)));

        public static IList<UniqueDocument> Alphabetize(this IEnumerable<UniqueDocument> documents, SortDirection direction)
        {
            var docList = documents.ToList();
            docList.Sort((a, b) => direction == SortDirection.Ascending
                ? string.Compare(a.Name, b.Name, StringComparison.Ordinal)
                : string.Compare(b.Name, a.Name, StringComparison.Ordinal));
            return docList;
        }

        public enum SortDirection
        {
            Ascending,
            Descending,
        }
    }
}
