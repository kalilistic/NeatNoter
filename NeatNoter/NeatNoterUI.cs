using ImGuiNET;
using NeatNoter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Timers;

namespace NeatNoter
{
    internal class NeatNoterUI : IDisposable
    {
        private const int WindowSizeX = 400;
        private const int WindowSizeY = 600;
        private const int ElementSizeX = 384;

        private readonly NeatNoterConfiguration config;
        private readonly Notebook notebook;
        private readonly Timer saveTimer;

        private bool categoryWindowVisible;
        private bool deletionWindowVisible;
        private IList<Category> filteredCategories;
        private Category currentCategory;
        private Note currentNote;
        private string searchEntry;
        private UIState lastState;
        private UIState state;

        public bool IsVisible { get; set; }

        public NeatNoterUI(Notebook notebook, NeatNoterConfiguration config)
        {
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

#if DEBUG
            IsVisible = true;
#endif
        }

        public void Draw()
        {
            this.saveTimer.Enabled = IsVisible;

            if (!IsVisible)
                return;

            ImGui.SetNextWindowSize(new Vector2(WindowSizeX, WindowSizeY), ImGuiCond.Always);
            ImGui.Begin("NeatNoter", ImGuiWindowFlags.NoResize);
            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = this.state switch
            {
                UIState.NoteIndex => DrawNoteIndex(),
                UIState.CategoryIndex => DrawCategoryIndex(),
                UIState.NoteEdit => DrawNoteEditTool(),
                UIState.CategoryEdit => DrawCategoryEditTool(),
                UIState.Search => DrawSearchTool(),
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

            if (ImGui.Button("New Note##-1"))
            {
                this.currentNote = this.notebook.CreateNote();
                SetState(UIState.NoteEdit);
                return true;
            }

            ImGui.Separator();

            foreach (var note in this.notebook.Notes)
            {
                DrawNoteEntry(note);
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

            if (ImGui.Button("New Category##-1"))
            {
                this.currentCategory = this.notebook.CreateCategory();
                SetState(UIState.CategoryEdit);
                return true;
            }

            ImGui.Separator();

            foreach (var category in this.notebook.Categories)
            {
                DrawCategoryEntry(category);
            }

            return true;
        }

        /// <summary>
        /// Called from <see cref="Draw"/>. Draws the note editing tool.
        /// </summary>
        private bool DrawNoteEditTool()
        {
            if (ImGui.Button("Back"))
                SetState(this.lastState); // We won't have menus more then one-deep, so we don't need to set up a pushdown

            ImGui.SameLine();
            if (ImGui.Button(this.categoryWindowVisible ? "Close category selection" : "Choose categories", new Vector2(ElementSizeX - 44, 23)))
                this.categoryWindowVisible = !this.categoryWindowVisible;
            IList<Category> categories = this.currentNote.Categories;
            CategorySelectionWindow(ref categories);
            this.currentNote.Categories = categories.ToList();

            DrawDocumentEditor(this.currentNote);

            return true;
        }

        /// <summary>
        /// Called from <see cref="Draw"/>. Draws the category editing tool.
        /// </summary>
        private bool DrawCategoryEditTool()
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

            ImGui.InputText("Search", ref this.searchEntry, (uint)this.searchEntry.Length + 1);

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

            CategorySelectionWindow(ref this.filteredCategories);

            ImGui.Separator();

            var results = this.notebook
                .SearchFor(this.searchEntry, this.config.IncludeNoteBodiesInSearch)
                .FilterByCategories(this.filteredCategories);
            foreach (var note in results)
            {
                DrawNoteEntry(note);
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
                ImGui.EndTabBar();
            }
        }

        private void DrawNoteEntry(Note note)
        {
            var color = note.Categories.Count > 0
                ? new Vector4(note.Categories[0].Color, 0.5f)
                : new Vector4(0.0f, 0.0f, 0.0f, 0.5f);
            ImGui.PushStyleColor(ImGuiCol.Button, color);
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.03f, 0.5f)); // Left-align the button text
            if (ImGui.Button(note.InternalName, new Vector2(ElementSizeX, 25)))
            {
                OpenNote(note);
            }
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();

            if (ImGui.BeginPopupContextItem("NeatNoter Note Neater Menu##" + note.IdentifierString))
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
        private void CategorySelectionWindow(ref IList<Category> selectedCategories)
        {
            if (!this.categoryWindowVisible)
                return;

            ImGui.SetNextWindowSize(new Vector2(400, 300));
            ImGui.Begin("NeatNoter Category Selection", ImGuiWindowFlags.NoResize);

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
        private void DrawCategoryEntry(Category category)
        {
            var color = new Vector4(category.Color, 0.5f);
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(color));
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.03f, 0.5f)); // Left-align the button text
            if (ImGui.Button(category.InternalName, new Vector2(ElementSizeX, 25)))
            {
                OpenCategory(category);
            }
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();

            if (ImGui.BeginPopupContextItem("NeatNoter Category Neater Menu##" + category.IdentifierString))
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

        private void SetState(UIState newState)
        {
            if (newState == this.state)
                return;

            this.deletionWindowVisible = false; // We want to disable these on state changes
            this.categoryWindowVisible = false;

            this.lastState = this.state;
            this.state = newState;
        }

        private void SaveTimerElapsed(object sender, ElapsedEventArgs e) => this.config.Save();

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

        private static void DrawDocumentEditor(UniqueDocument item)
        {
            var title = item.Name;
            if (ImGui.InputText(item.GetTypeName() + " Title", ref title, (uint)title.Length + 1))
            {
                item.Name = title;
            }

            var body = item.Body;
            if (ImGui.InputTextMultiline(string.Empty, ref body, (uint)body.Length + 1,
                new Vector2(ElementSizeX, WindowSizeY - 94)))
            {
                item.Body = body;
            }
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
        }
    }
}
