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

        private static float WindowSizeY => ImGui.GetWindowSize().Y;
        private static float ElementSizeX => ImGui.GetWindowSize().X - 16;
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

            ImGui.SetNextWindowSize(new Vector2(400, 600), ImGuiCond.FirstUseEver);
            ImGui.Begin("NeatNoter");
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

            if (ImGui.Button("New Category##-1"))
            {
                this.currentCategory = this.notebook.CreateCategory();
                SetState(UIState.CategoryEdit);
                return true;
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

            CategorySelectionWindow(ref this.filteredCategories);

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

        private void DrawNoteEntry(Note note, int index, int heightOffset)
        {
            const int heightConstant = 29;

            var lineOffset = ElementSizeX * 0.3f;
            var windowPos = ImGui.GetWindowPos();

            var color = note.Categories.Count > 0
                ? new Vector4(note.Categories[0].Color, 0.5f)
                : new Vector4(0.0f, 0.0f, 0.0f, 0.5f);
            ImGui.PushStyleColor(ImGuiCol.Button, color);
            var buttonLabel = note.Name;
            for (var i = 1; i < Math.Min(note.Name.Length, 70) && ImGui.CalcTextSize(buttonLabel).X > lineOffset - 30; i++)
                buttonLabel = note.Name.Substring(0, note.Name.Length - i) + "...";
            if (ImGui.Button(note.IdentifierString, new Vector2(ElementSizeX, 25)))
            {
                OpenNote(note);
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(note.Name);
            ImGui.PopStyleColor();
            
            // Adding the text over manually because otherwise the text position is dependent on label length
            ImGui.GetWindowDrawList().AddText(windowPos + new Vector2(lineOffset - ElementSizeX / 3.92f, index * heightConstant + heightOffset + 4), ImGui.GetColorU32(ImGuiCol.Text), buttonLabel);
            ImGui.GetWindowDrawList().AddLine(windowPos + new Vector2(lineOffset, index * heightConstant + heightOffset), windowPos + new Vector2(lineOffset, index * heightConstant + heightOffset + 25), ImGui.GetColorU32(ImGuiCol.Text));
            // This is super inefficient, TODO feex
            var contentPreview = note.Body.Replace('\n', ' ');
            for (var i = 1; i < Math.Min(note.Body.Length, 70) && ImGui.CalcTextSize(contentPreview).X > ElementSizeX - lineOffset - 10; i++)
                contentPreview = note.Body.Replace('\n', ' ').Substring(0, note.Body.Length - i) + "...";
            ImGui.GetWindowDrawList().AddText(windowPos + new Vector2(lineOffset + 10, index * heightConstant + heightOffset + 4), ImGui.GetColorU32(ImGuiCol.Text), contentPreview);

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
        private void DrawCategoryEntry(Category category, int index, int heightOffset)
        {
            const int heightConstant = 29;

            var color = new Vector4(category.Color, 0.5f);
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(color));
            var buttonLabel = category.Name;
            for (var i = 1; i < Math.Min(category.Name.Length, 70) && ImGui.CalcTextSize(buttonLabel).X > ElementSizeX - 22; i++)
                buttonLabel = category.Name.Substring(0, category.Name.Length - i) + "...";
            if (ImGui.Button(category.IdentifierString, new Vector2(ElementSizeX, 25)))
            {
                OpenCategory(category);
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(category.Body);
            ImGui.PopStyleColor();
            ImGui.GetWindowDrawList().AddText(ImGui.GetWindowPos() + new Vector2(ElementSizeX * 0.045f, index * heightConstant + heightOffset + 4), ImGui.GetColorU32(ImGuiCol.Text), buttonLabel);

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
            if (ImGui.InputText(item.GetTypeName() + " Title", ref title, 128))
            {
                item.Name = title;
            }

            var body = item.Body;
            if (ImGui.InputTextMultiline(string.Empty, ref body, 1024 * 6, // TODO figure out how to make it scroll and only output a part at a time
                new Vector2(ElementSizeX, WindowSizeY - 94), ImGuiInputTextFlags.AllowTabInput | ImGuiInputTextFlags.Multiline)) // TODO figure out how to auto word-wrap
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
