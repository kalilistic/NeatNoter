using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Policy;
using NeatNoter.Models;
using Newtonsoft.Json.Bson;

namespace NeatNoter
{
    internal class Notebook
    {
        public List<Category> Categories { get; }
        public List<Note> Notes { get; }

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
            };
            Notes.Insert(0, note);
            return note;
        }

        public Category CreateCategory()
        {
            var uid = DateTime.Now.ToBinary();
            var rand = new Random();
            var colorBytes = new byte[3];
            rand.NextBytes(colorBytes);
            var category = new Category
            {
                InternalName = "New Category##" + uid,
                Body = "Category description",
                Color = new Vector3(colorBytes[0], colorBytes[1], colorBytes[2]), // TODO make not random
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
                return Notes.Where(note => note.Name.Contains(fragment) || note.Body.Contains(fragment)); // Can add fuzzy-matching algorithm later
            return Notes.Where(note => note.Name.Contains(fragment));
        }
    }

    public static class NotebookExtensions
    {
        public static IEnumerable<Note> FilterByCategories(this IEnumerable<Note> notes, IEnumerable<Category> categories)
            => notes.Where(note => categories.All(category => note.Categories.Contains(category)));
    }
}
