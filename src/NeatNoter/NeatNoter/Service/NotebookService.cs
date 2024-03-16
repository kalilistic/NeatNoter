using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Timers;

using CheapLoc;
using Dalamud.DrunkenToad.Helpers;
using Dalamud.Logging;
using Newtonsoft.Json;

using Timer = System.Timers.Timer;

namespace NeatNoter;

/// <summary>
/// Notebook.
/// </summary>
public class NotebookService : BaseRepository
{
    private readonly object locker = new();
    private readonly NeatNoterPlugin plugin;
    private readonly Timer saveTimer;
    private readonly Timer fullSaveTimer;
    private List<Category> categories = null!;
    private List<Note> notes = null!;
    private bool saveInProgress;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotebookService"/> class.
    /// </summary>
    /// <param name="plugin">neatnoter plugin.</param>
    public NotebookService(NeatNoterPlugin plugin)
        : base(NeatNoterPlugin.GetPluginFolder())
    {
        this.plugin = plugin;
        this.LoadDocuments();
        this.saveTimer = new Timer
        {
            Interval = this.plugin.Configuration.SaveFrequency,
            Enabled = false,
        };
        this.fullSaveTimer = new Timer
        {
            Interval = this.plugin.Configuration.FullSaveFrequency,
            Enabled = false,
        };
        this.saveTimer.Elapsed += this.SaveTimerOnElapsed;
        this.fullSaveTimer.Elapsed += this.FullSaveTimerOnElapsed;
    }

    /// <summary>
    /// Gets a value indicating whether is loading.
    /// </summary>
    public bool Loading { get; private set; }

    /// <summary>
    /// Gets or sets temporary export path.
    /// </summary>
    public string? TempExportPath { get; set; }

    private bool Saving { get; set; }

    /// <summary>
    /// Initialize all.
    /// </summary>
    /// <param name="list">list.</param>
    /// <typeparam name="T">type.</typeparam>
    /// <exception cref="ArgumentNullException">null list exception.</exception>
    public static void InitializeAll<T>(IList<T> list) where T : UniqueDocument
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));
        foreach (var document in list)
        {
            document.DecompressBody();
        }
    }

    /// <summary>
    /// Export Notes to a file.
    /// </summary>
    /// <returns>exported notes.</returns>
    public string ExportNotes()
    {
        try
        {
            var configDirectory = NeatNoterPlugin.PluginInterface.GetPluginConfigDirectory();
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            var exportDir = Path.Combine(configDirectory, "export");
            if (!Directory.Exists(exportDir))
            {
                Directory.CreateDirectory(exportDir);
            }

            var currentNotes = this.GetNotes();
            var notesToExport = new List<NoteExport>();

            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var note in currentNotes)
            {
                notesToExport.Add(new NoteExport
                {
                    Name = note.Name,
                    Id = note.Id,
                    Body = string.IsNullOrEmpty(note.Body) ? "None" : note.Body,
                    Created = note.Created,
                    Modified = note.Modified,
                    Categories = note.Categories.Any()
                                     ? note.Categories.Select(category => category.Name).Aggregate((a, b) => a + "|" + b)
                                     : "None",
                });
            }

            // Generate filename with current time in Unix timestamp format
            var timestamp = UnixTimestampHelper.CurrentTime();
            var filename = $"export_{timestamp}.csv";
            var fullPath = Path.Combine(exportDir, filename);

            // Write notes to CSV
            using (var writer = new StreamWriter(fullPath))
            using (var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                // Write headers
                csv.WriteHeader<NoteExport>();
                csv.NextRecord();

                // Write records
                foreach (var note in notesToExport)
                {
                    csv.WriteRecord(note);
                    csv.NextRecord();
                }
            }

            PluginLog.Log($"Exported {notesToExport.Count} notes to {fullPath}.");
            return $"Exported {notesToExport.Count} notes to {fullPath}.";
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Failed to export notes.");
            return "Failed to export notes: " + ex.Message;
        }
    }

    /// <summary>
    /// Start service.
    /// </summary>
    public void Start()
    {
        this.saveTimer.Enabled = true;
        this.fullSaveTimer.Enabled = true;
    }

    /// <summary>
    /// Update save frequency.
    /// </summary>
    /// <param name="frequency">new frequency.</param>
    public void UpdateSaveFrequency(int frequency)
    {
        this.saveTimer.Stop();
        this.saveTimer.Interval = frequency;
        this.saveTimer.Start();
    }

    /// <summary>
    /// Update full save frequency.
    /// </summary>
    /// <param name="frequency">new frequency.</param>
    public void UpdateFullSaveFrequency(int frequency)
    {
        this.fullSaveTimer.Stop();
        this.fullSaveTimer.Interval = frequency;
        this.fullSaveTimer.Start();
    }

    /// <summary>
    /// Save notebook including notes, categories, and configuration.
    /// </summary>
    public void SaveFullNotebook()
    {
        if (this.saveInProgress) return;
        try
        {
            this.saveInProgress = true;
            this.SaveNotes();
            this.SaveCategories();
            this.plugin.SaveConfig();
            this.plugin.NotebookService.SaveBackup(this.plugin.Configuration.AutomaticExportPath);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Failed to save notes.");
        }

        this.saveInProgress = false;
    }

    /// <summary>
    /// Load notes and categories from db.
    /// </summary>
    public void LoadDocuments()
    {
        var loadedNotes = this.GetItems<Note>().ToList();
        var loadedCategories = this.GetItems<Category>().ToList();
        foreach (var note in loadedNotes)
        {
            note.DecompressBody();
            note.IsVisible = true;
        }

        foreach (var category in loadedCategories)
        {
            category.DecompressBody();
            category.IsVisible = true;
        }

        lock (this.locker)
        {
            this.notes = loadedNotes;
            this.categories = loadedCategories;
            this.SortNotes(DocumentSortType.GetDocumentSortTypeByIndex(this.plugin.Configuration.NoteSortType));
            this.SortCategories(DocumentSortType.GetDocumentSortTypeByIndex(this.plugin.Configuration.CategorySortType));
        }
    }

    /// <summary>
    /// Gets list of categories.
    /// </summary>
    /// <returns>list of categories.</returns>
    public List<Category> GetCategories()
    {
        lock (this.locker)
        {
            return this.categories.ToList();
        }
    }

    /// <summary>
    /// Gets list of notes.
    /// </summary>
    /// <returns>list of Notes.</returns>
    public List<Note> GetNotes()
    {
        return (List<Note>)this.GetNotes(false, string.Empty);
    }

    /// <summary>
    /// Gets list of notes.
    /// </summary>
    /// <param name="filterByCategory">filter notes by selected categories.</param>
    /// <param name="text">filter by text fragment.</param>
    /// <returns>list of Notes.</returns>
    public IEnumerable<Note> GetNotes(bool filterByCategory, string text)
    {
        lock (this.locker)
        {
            var selectedNotes = this.notes.ToList();
            var selectedCategories = this.GetCategories().Where(category => category.IsSelected).ToList();
            foreach (var note in selectedNotes)
            {
                if (filterByCategory && note.Categories.Count == 0)
                {
                    note.IsVisible = this.plugin.Configuration.IsNoCategorySelected;
                }
                else if (filterByCategory && !selectedCategories.Any(selectedCategory => note.Categories.Any(category => category.Id == selectedCategory.Id)))
                {
                    note.IsVisible = false;
                }
                else if (!string.IsNullOrEmpty(text))
                {
                    if (!this.plugin.Configuration.IncludeNoteBodiesInSearch && !note.Name.Contains(text))
                    {
                        note.IsVisible = false;
                    }
                    else if (this.plugin.Configuration.IncludeNoteBodiesInSearch && !note.Name.Contains(text) &&
                             !note.Body.Contains(text))
                    {
                        note.IsVisible = false;
                    }
                    else
                    {
                        note.IsVisible = true;
                    }
                }
                else
                {
                    note.IsVisible = true;
                }
            }

            return selectedNotes;
        }
    }

    /// <summary>
    /// Select one category and unselect others.
    /// </summary>
    /// <param name="selectedCategory">category to select.</param>
    public void SelectOneCategory(Category selectedCategory)
    {
        lock (this.locker)
        {
            foreach (var category in this.categories)
            {
                category.IsSelected = false;
            }

            selectedCategory.IsSelected = true;
            this.SaveCategories(this.categories);
        }

        this.plugin.Configuration.IsNoCategorySelected = false;
        this.plugin.SaveConfig();
    }

    /// <summary>
    /// Saves list of categories.
    /// </summary>
    /// <param name="updatedCategories">updated categories list.</param>
    public void SaveCategories(IEnumerable<Category> updatedCategories)
    {
        lock (this.locker)
        {
            updatedCategories = updatedCategories.ToList();
            foreach (var category in updatedCategories)
            {
                category.CompressBody();
            }

            this.UpsertItems(updatedCategories);
        }
    }

    /// <summary>
    /// Save category.
    /// </summary>
    /// <param name="category">updated category.</param>
    public void SaveCategory(Category category)
    {
        lock (this.locker)
        {
            category.CompressBody();
            this.UpdateItem(category);
        }
    }

    /// <summary>
    /// Saves list of categories.
    /// </summary>
    public void SaveCategories()
    {
        lock (this.locker)
        {
            foreach (var category in this.categories)
            {
                category.CompressBody();
                this.UpdateItem(category);
            }
        }
    }

    /// <summary>
    /// Saves list of notes.
    /// </summary>
    /// <param name="updatedNotes">updated notes list.</param>
    public void SaveNotes(IEnumerable<Note> updatedNotes)
    {
        lock (this.locker)
        {
            updatedNotes = updatedNotes.ToList();
            foreach (var note in updatedNotes)
            {
                note.CompressBody();
            }

            this.UpsertItems(updatedNotes);
        }
    }

    /// <summary>
    /// Save note.
    /// </summary>
    /// <param name="updatedNote">updated note.</param>
    public void SaveNote(Note updatedNote)
    {
        lock (this.locker)
        {
            updatedNote.CompressBody();
            this.UpdateItem(updatedNote);
        }
    }

    /// <summary>
    /// Saves list of notes.
    /// </summary>
    public void SaveNotes()
    {
        lock (this.locker)
        {
            foreach (var note in this.notes)
            {
                note.CompressBody();
                this.UpdateItem(note);
            }
        }
    }

    /// <summary>
    /// Create note.
    /// </summary>
    /// <returns>new note.</returns>
    public Note CreateNote()
    {
        var uid = DateTime.Now.ToBinary();
        var note = new Note
        {
            InternalName = Loc.Localize("DefaultNoteName", "New Note") + "##" + uid,
            Body = string.Empty,
            Categories = new List<Category>(),
            Created = UnixTimestampHelper.CurrentTime(),
            Modified = UnixTimestampHelper.CurrentTime(),
            IsVisible = true,
        };
        lock (this.locker)
        {
            this.notes.Insert(0, note);
        }

        this.InsertItem(note);

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
            InternalName = Loc.Localize("DefaultCategoryName", "New Category") + "##" + uid,
            Body = Loc.Localize("DefaultCategoryBody", "Category description"),
            Color = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()),
            Created = UnixTimestampHelper.CurrentTime(),
            Modified = UnixTimestampHelper.CurrentTime(),
        };
        lock (this.locker)
        {
            this.categories.Insert(0, category);
        }

        this.InsertItem(category);
        return category;
    }

    /// <summary>
    /// Delete note.
    /// </summary>
    /// <param name="note">note to delete.</param>
    public void DeleteNote(Note note)
    {
        lock (this.locker)
        {
            this.notes.Remove(note);
        }

        this.DeleteItem<Note>(note.Id);
        this.notes.Remove(note);
    }

    /// <summary>
    /// Delete category.
    /// </summary>
    /// <param name="category">category to delete.</param>
    public void DeleteCategory(Category category)
    {
        lock (this.locker)
        {
            foreach (var note in this.notes)
            {
                note.Categories.Remove(category);
            }
        }

        this.SaveNotes(this.notes);
        this.DeleteItem<Category>(category.Id);
        this.categories.Remove(category);
    }

    /// <summary>
    /// Sort categories.
    /// </summary>
    /// <param name="sortType">sort type.</param>
    public void SortCategories(DocumentSortType sortType)
    {
        this.plugin.Configuration.CategorySortType = sortType.Code;
        this.plugin.SaveConfig();
        lock (this.locker)
        {
            if (sortType.Code == DocumentSortType.NameAscending.Code)
            {
                this.categories = SortByName(this.categories, SortDirection.Ascending);
            }
            else if (sortType.Code == DocumentSortType.NameDescending.Code)
            {
                this.categories = SortByName(this.categories, SortDirection.Descending);
            }
            else if (sortType.Code == DocumentSortType.ModifiedAscending.Code)
            {
                this.categories = SortByModified(this.categories, SortDirection.Ascending);
            }
            else if (sortType.Code == DocumentSortType.ModifiedDescending.Code)
            {
                this.categories = SortByModified(this.categories, SortDirection.Descending);
            }
            else if (sortType.Code == DocumentSortType.CreatedAscending.Code)
            {
                this.categories = SortByCreated(this.categories, SortDirection.Ascending);
            }
            else if (sortType.Code == DocumentSortType.CreatedDescending.Code)
            {
                this.categories = SortByCreated(this.categories, SortDirection.Descending);
            }
        }
    }

    /// <summary>
    /// Sort notes.
    /// </summary>
    /// <param name="sortType">sort type.</param>
    public void SortNotes(DocumentSortType sortType)
    {
        this.plugin.Configuration.NoteSortType = sortType.Code;
        this.plugin.SaveConfig();
        lock (this.locker)
        {
            if (sortType.Code == DocumentSortType.NameAscending.Code)
            {
                this.notes = SortByName(this.notes, SortDirection.Ascending);
            }
            else if (sortType.Code == DocumentSortType.NameDescending.Code)
            {
                this.notes = SortByName(this.notes, SortDirection.Descending);
            }
            else if (sortType.Code == DocumentSortType.ModifiedAscending.Code)
            {
                this.notes = SortByModified(this.notes, SortDirection.Ascending);
            }
            else if (sortType.Code == DocumentSortType.ModifiedDescending.Code)
            {
                this.notes = SortByModified(this.notes, SortDirection.Descending);
            }
            else if (sortType.Code == DocumentSortType.CreatedAscending.Code)
            {
                this.notes = SortByCreated(this.notes, SortDirection.Ascending);
            }
            else if (sortType.Code == DocumentSortType.CreatedDescending.Code)
            {
                this.notes = SortByCreated(this.notes, SortDirection.Descending);
            }
        }
    }

    /// <summary>
    /// Save backup.
    /// </summary>
    /// <param name="path">file path.</param>
    public void SaveBackup(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        lock (this.locker)
        {
            dynamic obj = new ExpandoObject();
            obj.Notes = this.notes;
            obj.Categories = this.categories;
            obj.NotesReadable = this.notes.ToDictionary(note => note.InternalName, note => note.Body);
            obj.CategoriesReadable = this.categories.ToDictionary(category => category.InternalName, category => category.Body);

            File.WriteAllText(path, JsonConvert.SerializeObject(obj));
        }
    }

    /// <summary>
    /// Dispose service.
    /// </summary>
    public void Dispose()
    {
        this.saveTimer.Stop();
        this.saveTimer.Elapsed -= this.SaveTimerOnElapsed;
        this.fullSaveTimer.Stop();
        this.fullSaveTimer.Elapsed -= this.FullSaveTimerOnElapsed;
        this.SaveFullNotebook();
    }

    private static List<T> SortByName<T>(IEnumerable<T> documents, SortDirection direction) where T : UniqueDocument
    {
        var docList = documents.ToList();
        docList.Sort((a, b) => direction == SortDirection.Ascending
                                   ? string.Compare(a.Name, b.Name, StringComparison.Ordinal)
                                   : string.Compare(b.Name, a.Name, StringComparison.Ordinal));
        return docList;
    }

    private static List<T> SortByCreated<T>(IEnumerable<T> documents, SortDirection direction) where T : UniqueDocument
    {
        var docList = documents.ToList();
        docList.Sort((a, b) => direction == SortDirection.Ascending
                                   ? a.Created.CompareTo(b.Created)
                                   : b.Created.CompareTo(a.Created));
        return docList;
    }

    private static List<T> SortByModified<T>(IEnumerable<T> documents, SortDirection direction) where T : UniqueDocument
    {
        var docList = documents.ToList();
        docList.Sort((a, b) => direction == SortDirection.Ascending
                                   ? a.Modified.CompareTo(b.Modified)
                                   : b.Modified.CompareTo(a.Modified));
        return docList;
    }

    private void SaveTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        var currentNote = this.plugin.WindowManager.NotebookWindow!.CurrentNote;
        if (this.plugin.WindowManager.NotebookWindow is not { IsOpen: true }) return;
        if (this.plugin.WindowManager.NotebookWindow.IsNoteDirty && currentNote != null)
        {
            this.plugin.WindowManager.NotebookWindow.IsNoteDirty = false;
            this.SaveNote(currentNote);
        }
    }

    private void FullSaveTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        this.SaveFullNotebook();
    }
}
