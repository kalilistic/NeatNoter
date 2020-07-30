using ImGuiNET;
using NeatNoter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace NeatNoter
{
    internal class NeatNoterUI : IDisposable
    {
        private const int MaxNoteSize = 1024 * 4196; // You can fit the complete works of Shakespeare in 3.5MB, so this is probably fine.

        private static readonly uint TextColor = ImGui.GetColorU32(ImGuiCol.Text);

        private static float WindowSizeY => ImGui.GetWindowSize().Y;
        private static float ElementSizeX => ImGui.GetWindowSize().X - 16;

        private readonly IList<Category> filteredCategories;
        private readonly IMapProvider mapProvider;
        private readonly NeatNoterConfiguration config;
        private readonly Notebook notebook;
        private readonly Timer saveTimer;

        private bool categoryWindowVisible;
        private bool deletionWindowVisible;
        private bool drawing;
        private bool errorWindowVisible;
        private bool minimalView;
        private Category currentCategory;
        private Note currentNote;
        private Stack<IEnumerable<(Vector2, Vector2, Vector3, float)>> undoRedo;
        private string currentErrorMessage;
        private string searchEntry;
        private UIState lastState;
        private UIState state;

        public bool IsVisible { get; set; }

        public NeatNoterUI(Notebook notebook, NeatNoterConfiguration config, IMapProvider mapProvider)
        {
            this.mapProvider = mapProvider;
            this.config = config;
            this.notebook = notebook;

            this.saveTimer = new Timer
            {
                Interval = 3000,
                Enabled = false,
            };
            this.saveTimer.Elapsed += SaveTimerElapsed;

            this.filteredCategories = new List<Category>();
            this.state = UIState.NoteIndex;
            this.searchEntry = string.Empty;
            this.currentErrorMessage = string.Empty;

            this.undoRedo = new Stack<IEnumerable<(Vector2, Vector2, Vector3, float)>>();

#if DEBUG
            IsVisible = true;
#endif
        }

        public void Draw()
        {
            this.saveTimer.Enabled = IsVisible;

            if (!IsVisible)
                return;

            var flags = ImGuiWindowFlags.None;
            if (this.drawing)
            {
                flags |= ImGuiWindowFlags.NoMove;

                var drawThickness = this.config.PenThickness;
                var drawColor = this.config.PenColor;
                DrawPenToolWindow(ref drawColor, ref drawThickness);
                if (Math.Abs(drawThickness - this.config.PenThickness) > 0.001 || drawColor != this.config.PenColor)
                {
                    this.config.PenThickness = drawThickness;
                    this.config.PenColor = drawColor;
                    this.config.Save();
                }
            }

            DrawErrorWindow(this.currentErrorMessage, ref this.errorWindowVisible);

            ImGui.SetNextWindowSize(new Vector2(400, 600), ImGuiCond.FirstUseEver);
            ImGui.Begin("NeatNoter", flags);
            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = this.state switch
            {
                UIState.NoteIndex => DrawNoteIndex(),
                UIState.CategoryIndex => DrawCategoryIndex(),
                UIState.NoteEdit => DrawNoteEditTool(),
                UIState.CategoryEdit => DrawCategoryEditTool(),
                UIState.Search => DrawSearchTool(),
                UIState.Settings => DrawSettings(),
                _ => throw new ArgumentOutOfRangeException(),
            };
            ImGui.End();
        }

        /// <summary>
        /// Called from <see cref="Draw"/>. Draws the note list.
        /// </summary>
        private bool DrawNoteIndex()
        {
            if (DrawDeletionConfirmationWindow(ref this.deletionWindowVisible))
            {
                this.notebook.DeleteNote(this.currentNote);
                this.currentNote = null;
            }

            DrawTabBar();

            if (ImGui.Button("New Note##NeatNoter-1"))
            {
                this.currentNote = this.notebook.CreateNote();
                SetState(UIState.NoteEdit);
                return true;
            }

            ImGui.SameLine(ElementSizeX - 133);
            if (ImGui.Button("Sort##NeatNoter-1"))
            {
                ImGui.OpenPopup("Sort Context Menu##NeatNoter");
            }
            if (ImGui.BeginPopup("Sort Context Menu##NeatNoter"))
            {
                if (ImGui.Selectable("Name (Ascending)"))
                {
                    this.notebook.Notes = this.notebook.Notes.Alphabetize(SortDirection.Ascending);
                }
                else if (ImGui.Selectable("Name (Descending)"))
                {
                    this.notebook.Notes = this.notebook.Notes.Alphabetize(SortDirection.Descending);
                }
                ImGui.EndPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button("Export##NeatNoter-2"))
            {
                Task.Run(() => this.notebook.CreateBackup());
            }

            ImGui.SameLine();
            if (ImGui.Button("Import##NeatNoter-2"))
            {
                Task.Run(() => this.notebook.LoadBackup());
            }

            ImGui.Separator();

            for (var i = 0; i < this.notebook.Notes.Count; i++)
            {
                DrawNoteEntry(this.notebook.Notes[i], i, 89);
            }

            return true;
        }

        /// <summary>
        /// Called from <see cref="Draw"/>. Draws the category list.
        /// </summary>
        private bool DrawCategoryIndex()
        {
            if (DrawDeletionConfirmationWindow(ref this.deletionWindowVisible))
            {
                this.notebook.DeleteCategory(this.currentCategory);
                this.currentCategory = null;
            }

            DrawTabBar();

            if (ImGui.Button("New Category##NeatNoter-1"))
            {
                this.currentCategory = this.notebook.CreateCategory();
                SetState(UIState.CategoryEdit);
                return true;
            }

            ImGui.SameLine(ElementSizeX - 133);
            if (ImGui.Button("Sort##NeatNoter-1"))
            {
                ImGui.OpenPopup("Category Sort Context Menu##NeatNoter");
            }
            if (ImGui.BeginPopup("Category Sort Context Menu##NeatNoter"))
            {
                if (ImGui.Selectable("Name (Ascending)"))
                {
                    this.notebook.Categories = this.notebook.Categories.Alphabetize(SortDirection.Ascending);
                }
                else if (ImGui.Selectable("Name (Descending)"))
                {
                    this.notebook.Categories = this.notebook.Categories.Alphabetize(SortDirection.Descending);
                }
                ImGui.EndPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button("Export##NeatNoter-1"))
            {
                Task.Run(() => this.notebook.CreateBackup());
            }

            ImGui.SameLine();
            if (ImGui.Button("Import##NeatNoter-1"))
            {
                Task.Run(() => this.notebook.LoadBackup());
            }

            ImGui.Separator();

            for (var i = 0; i < this.notebook.Categories.Count; i++)
            {
                DrawCategoryEntry(this.notebook.Categories[i], i, 89);
            }

            return true;
        }

        /// <summary>
        /// Called from <see cref="Draw"/>. Draws the note editing tool.
        /// </summary>
        private bool DrawNoteEditTool()
        {
            if (!this.minimalView)
            {
                if (ImGui.Button("Back"))
                    SetState(this
                        .lastState); // We won't have menus more then one-deep, so we don't need to set up a pushdown

                ImGui.SameLine();
                if (ImGui.Button(this.categoryWindowVisible ? "Close category selection" : "Choose categories",
                    new Vector2(ElementSizeX - 44, 23)))
                    this.categoryWindowVisible = !this.categoryWindowVisible;
                CategorySelectionWindow(this.currentNote.Categories);
            }

            DrawDocumentEditor(this.currentNote);

            return true;
        }

        /// <summary>
        /// Called from <see cref="Draw"/>. Draws the category editing tool.
        /// </summary>
        private bool DrawCategoryEditTool()
        {
            if (!this.minimalView)
            {
                if (ImGui.Button("Back"))
                    SetState(this.lastState);

                ImGui.SameLine();
                var color = this.currentCategory.Color;
                if (ImGui.ColorEdit3("Color", ref color))
                {
                    this.currentCategory.Color = color;
                    this.config.Save();
                }
            }

            DrawDocumentEditor(this.currentCategory);

            return true;
        }

        /// <summary>
        /// Called from <see cref="Draw"/>. Draws the search tool.
        /// </summary>
        private bool DrawSearchTool()
        {
            if (DrawDeletionConfirmationWindow(ref this.deletionWindowVisible))
            {
                this.notebook.DeleteNote(this.currentNote);
                this.currentNote = null;
            }

            DrawTabBar();

            ImGui.InputText("Search", ref this.searchEntry, 128);

            var includeBodies = this.config.IncludeNoteBodiesInSearch;
            if (ImGui.Checkbox("Include note contents in search", ref includeBodies))
            {
                this.config.IncludeNoteBodiesInSearch = includeBodies;
            }

            ImGui.SameLine();
            if (ImGui.Button("Filter categories"))
            {
                this.categoryWindowVisible = !this.categoryWindowVisible;
            }

            CategorySelectionWindow(this.filteredCategories);

            ImGui.Separator();

            var results = this.notebook
                .SearchFor(this.searchEntry, this.config.IncludeNoteBodiesInSearch)
                .FilterByCategories(this.filteredCategories)
                .ToList();
            for (var i = 0; i < results.Count; i++)
            {
                DrawNoteEntry(results[i], i, 116);
            }

            return true;
        }

        /// <summary>
        /// Called from <see cref="Draw"/>. Draws the settings.
        /// </summary>
        private bool DrawSettings()
        {
            DrawTabBar();

            var existing = this.config.AutomaticExportPath ?? "";
            if (ImGui.InputText("Automatic backup path", ref existing, 350000))
            {
                this.config.AutomaticExportPath = existing;
                this.config.Save();
            }

            if (ImGui.Button("Set automatic backup location##NeatNoter-2"))
            {
                Task.Run(() => this.notebook.CreateBackup(true));
            }

            if (this.notebook.TempExportPath != null)
            {
                this.config.AutomaticExportPath = this.notebook.TempExportPath;
                this.notebook.TempExportPath = null;
                this.config.Save();
            }

            return true;
        }

        /// <summary>
        /// Used in <see cref="DrawNoteIndex"/>, <see cref="DrawCategoryIndex"/> and <see cref="DrawSearchTool"/>. Draws the tab bar.
        /// </summary>
        private void DrawTabBar()
        {
            if (ImGui.BeginTabBar("NeatNoter Tab Bar", ImGuiTabBarFlags.NoTooltip))
            {
                if (ImGui.BeginTabItem("Notes"))
                {
                    SetState(UIState.NoteIndex);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Categories"))
                {
                    SetState(UIState.CategoryIndex);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Search"))
                {
                    SetState(UIState.Search);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Settings"))
                {
                    SetState(UIState.Settings);
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }

        private void DrawNoteEntry(Note note, int index, float heightOffset)
        {
            const int heightMod = 29;
            heightOffset -= ImGui.GetScrollY();

            var lineOffset = ElementSizeX * 0.3f;
            var windowPos = ImGui.GetWindowPos();

            var color = note.Categories.Count > 0
                ? new Vector4(note.Categories[0].Color, 0.5f)
                : new Vector4(0.0f, 0.0f, 0.0f, 0.5f);
            ImGui.PushStyleColor(ImGuiCol.Button, color);

            var buttonLabel = note.Name;
            var cutNameLength = Math.Min(note.Name.Length, 70);
            for (var i = 1; i < cutNameLength && ImGui.CalcTextSize(buttonLabel).X > lineOffset - 30; i++)
                buttonLabel = note.Name.Substring(0, cutNameLength - i) + "...";
            if (ImGui.Button(note.IdentifierString, new Vector2((int)Math.Truncate(ImGui.GetScrollMaxY()) != 0 ? ElementSizeX - 16 : ElementSizeX, 25)) && !this.notebook.Loading)
            {
                OpenNote(note);
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(note.Name);

            ImGui.PopStyleColor();

            // Adding the text over manually because otherwise the text position is dependent on label length
            ImGui.GetWindowDrawList().AddText(windowPos + new Vector2(lineOffset - ElementSizeX / 3.92f, index * heightMod + heightOffset + 4), TextColor, buttonLabel);
            ImGui.GetWindowDrawList().AddLine(windowPos + new Vector2(lineOffset, index * heightMod + heightOffset), windowPos + new Vector2(lineOffset, index * heightMod + heightOffset + 25), TextColor);

            var contentPreview = note.Body.Replace('\n', ' ');
            var cutBodyLength = Math.Min(note.Body.Length, 400);
            for (var i = 1; i < cutBodyLength && ImGui.CalcTextSize(contentPreview).X > ElementSizeX - lineOffset - 22; i++)
                contentPreview = note.Body.Replace('\n', ' ').Substring(0, cutBodyLength - i) + "...";
            ImGui.GetWindowDrawList()
                .AddText(windowPos + new Vector2(lineOffset + 10, index * heightMod + heightOffset + 4), TextColor, contentPreview);

            if (ImGui.BeginPopupContextItem("NeatNoter Note Neater Menu##NeatNoter" + note.IdentifierString))
            {
                if (ImGui.Selectable("Delete"))
                {
                    this.currentNote = note;
                    this.deletionWindowVisible = true;
                }
                ImGui.EndPopup();
            }
        }

        private void OpenNote(Note note)
        {
            this.currentNote = note;
            SetState(UIState.NoteEdit);
        }

        /// <summary>
        /// Called from <see cref="DrawNoteEditTool"/> and <see cref="DrawSearchTool"/>. Draws the category selection window.
        /// </summary>
        private void CategorySelectionWindow(ICollection<Category> selectedCategories)
        {
            if (!this.categoryWindowVisible)
                return;

            ImGui.SetNextWindowSize(new Vector2(400, 300));
            ImGui.Begin("NeatNoter Category Selection", ref this.categoryWindowVisible, ImGuiWindowFlags.NoResize);

            if (this.notebook.Categories.Count != 0)
            {
                ImGui.Columns(3, "NeatNoter Category Columns", true);
                foreach (var category in this.notebook.Categories)
                {
                    var isChecked = selectedCategories.Contains(category);
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(category.Color, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.CheckMark, new Vector4(category.Color, 1.0f));
                    if (ImGui.Checkbox(category.InternalName, ref isChecked))
                    {
                        if (isChecked)
                            selectedCategories.Add(category);
                        else
                            selectedCategories.Remove(category);
                    }
                    ImGui.PopStyleColor(2);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(category.Body);
                    ImGui.NextColumn();
                }
            }
            else
            {
                ImGui.Text("No categories to choose from.");
            }

            ImGui.End();
        }

        /// <summary>
        /// Called from <see cref="DrawCategoryIndex"/>. Draws the category entry.
        /// </summary>
        private void DrawCategoryEntry(Category category, int index, float heightOffset)
        {
            const int heightMod = 29;
            heightOffset -= ImGui.GetScrollY();

            var color = new Vector4(category.Color, 0.5f);
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(color));

            var buttonLabel = category.Name;
            var cutNameLength = Math.Min(category.Name.Length, 70);
            for (var i = 1; i < cutNameLength && ImGui.CalcTextSize(buttonLabel).X > ElementSizeX - 22; i++)
                buttonLabel = category.Name.Substring(0, cutNameLength - i) + "...";
            if (ImGui.Button(category.IdentifierString, new Vector2((int)Math.Truncate(ImGui.GetScrollMaxY()) != 0 ? ElementSizeX - 16 : ElementSizeX, 25)) && !this.notebook.Loading)
            {
                OpenCategory(category);
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(category.Body);

            ImGui.PopStyleColor();

            ImGui.GetWindowDrawList().AddText(ImGui.GetWindowPos() + new Vector2(ElementSizeX * 0.045f, index * heightMod + heightOffset + 4), TextColor, buttonLabel);

            if (ImGui.BeginPopupContextItem("NeatNoter Category Neater Menu##NeatNoter" + category.IdentifierString))
            {
                if (ImGui.Selectable("Delete"))
                {
                    this.deletionWindowVisible = true;
                    this.currentCategory = category;
                }
                ImGui.EndPopup();
            }
        }

        private void OpenCategory(Category category)
        {
            this.currentCategory = category;
            SetState(UIState.CategoryEdit);
        }

        /// <summary>
        /// Called from <see cref="DrawCategoryEditTool"/> and <see cref="DrawNoteEditTool"/>. Draws the text editor.
        /// </summary>
        private void DrawDocumentEditor(UniqueDocument document)
        {
            //var windowPos = ImGui.GetWindowPos();

            if (!this.minimalView)
            {
                var title = document.Name;
                if (ImGui.InputText(document.GetTypeName() + " Title", ref title, 128))
                {
                    document.Name = title;
                }
            }

            var body = document.Body;
            var inputFlags = ImGuiInputTextFlags.AllowTabInput;
            if (this.drawing)
                inputFlags |= ImGuiInputTextFlags.ReadOnly;
            if (ImGui.InputTextMultiline(string.Empty, ref body, MaxNoteSize, new Vector2(ElementSizeX, WindowSizeY - (this.minimalView ? 40 : 94)), inputFlags))
            {
                document.Body = body;
            }

            if (ImGui.BeginPopupContextItem("Editor Context Menu " + document.InternalName))
            {
                /*if (ImGui.Selectable("Insert current minimap"))
                {
                    var mapData = this.mapProvider.GetCurrentMap();
                    if (mapData.Length == 0)
                    {
                        this.currentErrorMessage = "No map is currently loaded!";
                        this.errorWindowVisible = true;
                    }
                    document.Images.Add(new Image
                    {
                        Position = Vector2.Zero,
                        InternalTexture = Convert.ToBase64String(mapData.ToArray()),
                    });
                }
                if (!this.drawing && ImGui.Selectable("Insert drawing"))
                {
                    this.drawing = true;
                }
                else if (this.drawing && ImGui.Selectable("Stop drawing"))
                {
                    this.drawing = false;
                }*/
                if (!this.minimalView && ImGui.Selectable("Minimal view"))
                {
                    this.minimalView = true;
                }
                else if (this.minimalView && ImGui.Selectable("End minimal view"))
                {
                    this.minimalView = false;
                }
                ImGui.EndPopup();
            }

            /*// Draw images next
            var toRemove = new List<Image>();
            foreach (var image in document.Images)
            {
                if (string.IsNullOrEmpty(image.InternalTexture))
                {
                    toRemove.Add(image);
                    continue;
                }
                ImGui.GetWindowDrawList().AddImage(image.Texture.ImGuiHandle, windowPos + image.Position, windowPos + image.Position + new Vector2(image.Texture.Width, image.Texture.Height));
            }
            foreach (var image in toRemove)
            {
                document.Images.Remove(image);
            }

            // Draw pen tool stuff
            if (this.drawing && ImGui.IsItemHovered())
            {
                if (ImGui.IsMouseDown(0))
                {
                    var delta = ImGui.GetMouseDragDelta(0);
                    ImGui.ResetMouseDragDelta();
                    var a = ImGui.GetMousePos() - windowPos - delta;
                    var b = ImGui.GetMousePos() - windowPos;
                    document.Lines.Add((a, b, this.config.PenColor, this.config.PenThickness));
                }
                else if (ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Z))) // Undo
                {
                    var original = document.Lines;
                    document.Lines = original.Take(document.Lines.Count - 100).ToList();
                    this.undoRedo.Push(document.Lines.Except(original));
                }
                else if (ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Y))) // Redo
                {
                    document.Lines.AddRange(this.undoRedo.Pop());
                }
            }
            foreach (var (a, b, col, thickness) in document.Lines)
            {
                var lineBegin = windowPos + a;
                var lineEnd = windowPos + b;

                ImGui.GetWindowDrawList().AddLine(lineBegin, lineEnd, ImGui.GetColorU32(new Vector4(col, 1.0f)), thickness);
            }*/
        }

        private void SetState(UIState newState)
        {
            if (newState == this.state)
                return;

            this.config.Save();

            this.deletionWindowVisible = false; // We want to disable these on state changes
            this.categoryWindowVisible = false;
            this.errorWindowVisible = false;

            this.drawing = false;

            this.lastState = this.state;
            this.state = newState;
        }

        private void SaveTimerElapsed(object sender, ElapsedEventArgs e)
        {
            this.config.Save();

            if (this.config.AutomaticExportPath != null)
            {
                this.notebook.SaveBackup(this.config.AutomaticExportPath);
            }
        }

        private static bool DrawDeletionConfirmationWindow(ref bool isVisible)
        {
            if (!isVisible)
                return false;

            var ret = false;

            ImGui.Begin("NeatNoter Deletion Confirmation", ImGuiWindowFlags.NoResize);

            ImGui.Text("Are you sure you want to delete this?");
            ImGui.Text("This cannot be undone.");
            if (ImGui.Button("Yes"))
            {
                isVisible = false;
                ret = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("No"))
            {
                isVisible = false;
            }

            ImGui.End();

            return ret;
        }

        private static void DrawErrorWindow(string message, ref bool visible)
        {
            if (!visible)
                return;

            ImGui.SetNextWindowSize(new Vector2(250, 81));
            ImGui.SetNextWindowFocus();
            ImGui.Begin("NeatNoter Error", ImGuiWindowFlags.NoResize);
            ImGui.Text(message);
            if (ImGui.Button("Ok"))
            {
                visible = false;
            }
            ImGui.End();
        }

        private static void DrawPenToolWindow(ref Vector3 color, ref float thickness)
        {
            ImGui.SetNextWindowSize(new Vector2(300, 316));
            ImGui.Begin("NeatNoter Pen Color Picker", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize);
            ImGui.ColorPicker3(string.Empty, ref color);
            ImGui.SliderFloat("Line thickness", ref thickness, 0, 255);
            ImGui.End();
        }

        public void Dispose()
        {
            saveTimer.Dispose();
        }

        private enum UIState
        {
            NoteIndex,
            CategoryIndex,
            NoteEdit,
            CategoryEdit,
            Search,
            Settings,
        }
    }
}
