using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using CheapLoc;
using Dalamud.DrunkenToad;
using ImGuiNET;

namespace NeatNoter
{
    /// <summary>
    /// Main window for the plugin.
    /// </summary>
    public class NotebookWindow : PluginWindow
    {
        private const int MaxNoteSize = 1024 * 4196; // You can fit the complete works of Shakespeare in 3.5MB, so this is probably fine.
        private static readonly uint TextColor = ImGui.GetColorU32(ImGuiCol.Text);

        private bool categoryWindowVisible;
        private bool deletionWindowVisible;
        private bool backgroundVisible;
        private bool minimalView;
        private bool transparencyWindowVisible;
        private float editorTransparency;
        private Category? currentCategory;
        private Note? currentNote;
        private string noteSearchEntry;
        private UIState lastState;
        private UIState state;
        private string previousNote;
        private ImGuiTabItemFlags noteTabFlags;
        private ImGuiTabItemFlags categoryTabFlags;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotebookWindow"/> class.
        /// </summary>
        /// <param name="plugin">NeatNoter plugin.</param>
        public NotebookWindow(NeatNoterPlugin plugin)
            : base(plugin, "NeatNoter")
        {
            this.plugin = plugin;
            this.state = UIState.NoteIndex;
            this.noteSearchEntry = string.Empty;
            this.backgroundVisible = true;
            this.Size = new Vector2(400, 600) * ImGui.GetIO().FontGlobalScale;
            this.SizeCondition = ImGuiCond.FirstUseEver;
            this.previousNote = string.Empty;
            this.noteTabFlags = ImGuiTabItemFlags.None;
            this.categoryTabFlags = ImGuiTabItemFlags.None;
            unsafe
            {
                this.editorTransparency = ImGui.GetStyleColorVec4(ImGuiCol.FrameBg)->W;
            }
        }

        private enum UIState
        {
            NoteIndex = 0,
            CategoryIndex = 1,
            NoteEdit = 2,
            CategoryEdit = 3,
        }

        private static float InverseFontScale => 1 / ImGui.GetIO().FontGlobalScale;

        private static float WindowSizeY => ImGui.GetWindowSize().Y * ImGui.GetIO().FontGlobalScale;

        private static float ElementSizeX => ImGui.GetWindowSize().X - (16 * InverseFontScale);

        /// <inheritdoc />
        public override void OnOpen()
        {
            this.plugin.Configuration.IsVisible = true;
            this.plugin.SaveConfig();
        }

        /// <inheritdoc />
        public override void OnClose()
        {
            this.plugin.Configuration.IsVisible = false;
            this.plugin.SaveConfig();
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            this.SetWindowFlags();
            switch (this.state)
            {
                case UIState.NoteIndex:
                    this.DrawNoteIndex();
                    break;
                case UIState.CategoryIndex:
                    this.DrawCategoryIndex();
                    break;
                case UIState.NoteEdit:
                    this.DrawNoteEditTool();
                    break;
                case UIState.CategoryEdit:
                    this.DrawCategoryEditTool();
                    break;
                default:
                    this.DrawNoteIndex();
                    break;
            }
        }

        private static bool DrawDeletionConfirmationWindow(ref bool isVisible)
        {
            if (!isVisible)
                return false;

            var ret = false;

            ImGui.Begin(Loc.Localize("DeleteConfirmationHeader", "NeatNoter Deletion Confirmation"), ImGuiWindowFlags.NoResize);

            ImGui.Text(Loc.Localize("DeleteConfirmationSubHeader", "Are you sure you want to delete this?"));
            ImGui.Text(Loc.Localize("DeleteConfirmationWarning", "This cannot be undone."));
            if (ImGui.Button(Loc.Localize("Yes", "Yes")))
            {
                isVisible = false;
                ret = true;
            }

            ImGui.SameLine();
            if (ImGui.Button(Loc.Localize("No", "No")))
            {
                isVisible = false;
            }

            ImGui.End();

            return ret;
        }

        private void SetWindowFlags()
        {
            var flags = ImGuiWindowFlags.None;

            if (!this.backgroundVisible)
            {
                flags |= ImGuiWindowFlags.NoBackground;
                flags |= ImGuiWindowFlags.NoTitleBar;
            }

            this.Flags = flags;
        }

        private void DrawNoteIndex()
        {
            if (DrawDeletionConfirmationWindow(ref this.deletionWindowVisible))
            {
                if (this.currentNote != null)
                {
                    this.plugin.NotebookService.DeleteNote(this.currentNote);
                    this.currentNote = null;
                }
            }

            this.DrawTabBar();

            if (ImGui.Button(Loc.Localize("NewNote", "New Note") + "###NeatNoter_Notes_New"))
            {
                this.currentNote = this.plugin.NotebookService.CreateNote();
                this.plugin.NotebookService.SaveNote(this.currentNote);
                this.plugin.NotebookService.SortNotes(DocumentSortType.GetDocumentSortTypeByIndex(this.plugin.Configuration.NoteSortType));
                this.SetState(UIState.NoteEdit);
                return;
            }

            var fontScale = ImGui.GetIO().FontGlobalScale;

            ImGui.SameLine();
            if (ImGui.Button(Loc.Localize("SortNotes", "Sort") + "###NeatNoter_Notes_Sort"))
            {
                ImGui.OpenPopup("###NeatNoter_Notes_Sort_ContextMenu");
            }

            if (ImGui.BeginPopup("###NeatNoter_Notes_Sort_ContextMenu"))
            {
                if (ImGui.Selectable(DocumentSortType.NameAscending.Name, this.plugin.Configuration.NoteSortType == DocumentSortType.NameAscending.Code))
                {
                    this.plugin.NotebookService.SortNotes(DocumentSortType.NameAscending);
                }
                else if (ImGui.Selectable(DocumentSortType.NameDescending.Name, this.plugin.Configuration.NoteSortType == DocumentSortType.NameDescending.Code))
                {
                    this.plugin.NotebookService.SortNotes(DocumentSortType.NameDescending);
                }
                else if (ImGui.Selectable(DocumentSortType.CreatedAscending.Name, this.plugin.Configuration.NoteSortType == DocumentSortType.CreatedAscending.Code))
                {
                    this.plugin.NotebookService.SortNotes(DocumentSortType.CreatedAscending);
                }
                else if (ImGui.Selectable(DocumentSortType.CreatedDescending.Name, this.plugin.Configuration.NoteSortType == DocumentSortType.CreatedDescending.Code))
                {
                    this.plugin.NotebookService.SortNotes(DocumentSortType.CreatedDescending);
                }
                else if (ImGui.Selectable(DocumentSortType.ModifiedAscending.Name, this.plugin.Configuration.NoteSortType == DocumentSortType.ModifiedAscending.Code))
                {
                    this.plugin.NotebookService.SortNotes(DocumentSortType.ModifiedAscending);
                }
                else if (ImGui.Selectable(DocumentSortType.ModifiedDescending.Name, this.plugin.Configuration.NoteSortType == DocumentSortType.ModifiedDescending.Code))
                {
                    this.plugin.NotebookService.SortNotes(DocumentSortType.ModifiedDescending);
                }

                ImGui.EndPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button(Loc.Localize("FilterNotes", "Filter") + "###NeatNoter_Notes_Filter"))
            {
                ImGui.OpenPopup("###NeatNoter_Notes_Filter_ContextMenu");
            }

            if (ImGui.BeginPopup("###NeatNoter_Notes_Filter_ContextMenu"))
            {
                if (ImGui.Selectable(Loc.Localize("NoCategory", "No Category"), this.plugin.Configuration.IsNoCategorySelected, ImGuiSelectableFlags.DontClosePopups))
                {
                    this.plugin.Configuration.IsNoCategorySelected = !this.plugin.Configuration.IsNoCategorySelected;
                    this.plugin.SaveConfig();
                }

                foreach (var category in this.plugin.NotebookService.GetCategories())
                {
                    if (ImGui.Selectable(category.InternalName, category.IsSelected, ImGuiSelectableFlags.DontClosePopups))
                    {
                        category.IsSelected = !category.IsSelected;
                        this.plugin.NotebookService.SaveCategory(category);
                    }
                }

                ImGui.EndPopup();
            }

            ImGui.SameLine();

            ImGui.SetNextItemWidth(-1);
            ImGui.InputText(string.Empty, ref this.noteSearchEntry, 128);
            ImGui.Separator();

            var notes = this.plugin.NotebookService.GetNotes(true, this.noteSearchEntry).Where(note => note.IsVisible).ToList();
            for (var i = 0; i < notes.Count; i++)
            {
                this.DrawNoteEntry(notes[i], i, 38 + (51 * fontScale));
            }
        }

        private void DrawCategoryIndex()
        {
            if (DrawDeletionConfirmationWindow(ref this.deletionWindowVisible))
            {
                if (this.currentCategory != null)
                {
                    this.plugin.NotebookService.DeleteCategory(this.currentCategory);
                    this.currentCategory = null;
                }
            }

            this.DrawTabBar();

            if (ImGui.Button(Loc.Localize("NewCategory", "New Category") + "###NeatNoter_Categories_New"))
            {
                this.currentCategory = this.plugin.NotebookService.CreateCategory();
                this.SetState(UIState.CategoryEdit);
                return;
            }

            var fontScale = ImGui.GetIO().FontGlobalScale;

            ImGui.SameLine();
            if (ImGui.Button(Loc.Localize("SortCategories", "Sort") + "###NeatNoter_Categories_Sort"))
            {
                ImGui.OpenPopup("###NeatNoter_Categories_Sort_ContextMenu");
            }

            if (ImGui.BeginPopup("###NeatNoter_Categories_Sort_ContextMenu"))
            {
                if (ImGui.Selectable(DocumentSortType.NameAscending.Name, this.plugin.Configuration.CategorySortType == DocumentSortType.NameAscending.Code))
                {
                    this.plugin.NotebookService.SortCategories(DocumentSortType.NameAscending);
                }
                else if (ImGui.Selectable(DocumentSortType.NameDescending.Name, this.plugin.Configuration.CategorySortType == DocumentSortType.NameDescending.Code))
                {
                    this.plugin.NotebookService.SortCategories(DocumentSortType.NameDescending);
                }
                else if (ImGui.Selectable(DocumentSortType.CreatedAscending.Name, this.plugin.Configuration.CategorySortType == DocumentSortType.CreatedAscending.Code))
                {
                    this.plugin.NotebookService.SortCategories(DocumentSortType.CreatedAscending);
                }
                else if (ImGui.Selectable(DocumentSortType.CreatedDescending.Name, this.plugin.Configuration.CategorySortType == DocumentSortType.CreatedDescending.Code))
                {
                    this.plugin.NotebookService.SortCategories(DocumentSortType.CreatedDescending);
                }
                else if (ImGui.Selectable(DocumentSortType.ModifiedAscending.Name, this.plugin.Configuration.CategorySortType == DocumentSortType.ModifiedAscending.Code))
                {
                    this.plugin.NotebookService.SortCategories(DocumentSortType.ModifiedAscending);
                }
                else if (ImGui.Selectable(DocumentSortType.ModifiedDescending.Name, this.plugin.Configuration.CategorySortType == DocumentSortType.ModifiedDescending.Code))
                {
                    this.plugin.NotebookService.SortCategories(DocumentSortType.ModifiedDescending);
                }

                ImGui.EndPopup();
            }

            ImGui.Separator();

            var categories = this.plugin.NotebookService.GetCategories();
            for (var i = 0; i < categories.Count; i++)
            {
                this.DrawCategoryEntry(categories[i], i, 38 + (51 * fontScale));
            }
        }

        private void DrawNoteEditTool()
        {
            if (!this.minimalView)
            {
                if (ImGui.Button(Loc.Localize("Back", "Back")))
                {
                    this.SetState(this
                        .lastState); // We won't have menus more then one-deep, so we don't need to set up a push-down
                }

                ImGui.SameLine();
                if (ImGui.Button(
                    this.categoryWindowVisible ? Loc.Localize("CloseCategorySelection", "Close category selection") : Loc.Localize("ChooseCategories", "Choose categories"),
                    new Vector2(ElementSizeX - (60 * ImGui.GetIO().FontGlobalScale), 23 * ImGui.GetIO().FontGlobalScale)))
                    this.categoryWindowVisible = !this.categoryWindowVisible;
                this.CategorySelectionWindow(this.currentNote?.Categories);
            }

            this.DrawDocumentEditor(this.currentNote);
        }

        private void DrawCategoryEditTool()
        {
            if (!this.minimalView)
            {
                if (ImGui.Button(Loc.Localize("Back", "Back")))
                    this.SetState(this.lastState);

                ImGui.SameLine();
                var color = this.currentCategory!.Color;
                if (ImGui.ColorEdit3(Loc.Localize("Color", "Color"), ref color))
                {
                    this.currentCategory.Color = color;
                    this.plugin.NotebookService.SaveCategory(this.currentCategory);
                }
            }

            this.DrawDocumentEditor(this.currentCategory);
        }

        private void DrawTabBar()
        {
            if (ImGui.BeginTabBar("###NeatNoter_TabBar", ImGuiTabBarFlags.NoTooltip))
            {
                if (ImGuiUtil.BeginTabItem(Loc.Localize("Notes", "Notes") + "###NeatNoter_TabItem_Notes", this.noteTabFlags))
                {
                    this.SetState(UIState.NoteIndex);
                    ImGui.EndTabItem();
                }

                if (ImGuiUtil.BeginTabItem(Loc.Localize("Categories", "Categories") + "###NeatNoter_TabItem_Categories", this.categoryTabFlags))
                {
                    this.SetState(UIState.CategoryIndex);
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            this.noteTabFlags = ImGuiTabItemFlags.None;
            this.categoryTabFlags = ImGuiTabItemFlags.None;
        }

        private void DrawNoteEntry(Note note, int index, float heightOffset)
        {
            var fontScale = ImGui.GetIO().FontGlobalScale;
            var heightMod = 4 + (25 * fontScale);
            heightOffset -= ImGui.GetScrollY();

            var lineOffset = ElementSizeX * 0.3f;
            var windowPos = ImGui.GetWindowPos();

            var color = note.Categories.Count > 0
                ? new Vector4(note.Categories[0].Color, 0.5f)
                : new Vector4(0.0f, 0.0f, 0.0f, 0.5f);
            ImGui.PushStyleColor(ImGuiCol.Button, color);

            var buttonLabel = note.Name;
            var cutNameLength = Math.Min(note.Name.Length, 70);
            for (var i = 1; i < cutNameLength && ImGui.CalcTextSize(buttonLabel).X > lineOffset - (30 * fontScale); i++)
                buttonLabel = note.Name[.. (cutNameLength - i)] + "...";
            if (ImGui.Button(note.IdentifierString, new Vector2(ElementSizeX, 25 * fontScale)) && !this.plugin.NotebookService.Loading)
            {
                this.OpenNote(note);
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(note.Name);

            ImGui.PopStyleColor();

            // Adding the text over manually because otherwise the text position is dependent on label length
            ImGui.GetWindowDrawList().AddText(windowPos + new Vector2(lineOffset - (ElementSizeX / 3.92f), (index * heightMod) + heightOffset + (4 * fontScale)), TextColor, buttonLabel);
            ImGui.GetWindowDrawList().AddLine(windowPos + new Vector2(lineOffset, (index * heightMod) + heightOffset), windowPos + new Vector2(lineOffset, (index * heightMod) + heightOffset + (25 * fontScale)), TextColor);

            var contentPreview = note.Body.Replace('\n', ' ');
            var cutBodyLength = Math.Min(note.Body.Length, 400);
            for (var i = 1; i < cutBodyLength && ImGui.CalcTextSize(contentPreview).X > ElementSizeX - lineOffset - (22 * fontScale); i++)
                contentPreview = note.Body.Replace('\n', ' ')[.. (cutBodyLength - i)] + "...";
            ImGui.GetWindowDrawList()
                .AddText(windowPos + new Vector2(lineOffset + 10, (index * heightMod) + heightOffset + (4 * fontScale)), TextColor, contentPreview);

            if (ImGui.BeginPopupContextItem("###NeatNoter_" + note.IdentifierString))
            {
                if (ImGui.Selectable(Loc.Localize("DeleteNote", "Delete Note")))
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
            this.SetState(UIState.NoteEdit);
        }

        private void CategorySelectionWindow(ICollection<Category>? selectedCategories)
        {
            if (!this.categoryWindowVisible)
                return;

            ImGui.SetNextWindowSize(new Vector2(400, 300) * ImGui.GetIO().FontGlobalScale);
            ImGui.Begin(Loc.Localize("CategorySelection", "Select Category"), ref this.categoryWindowVisible, ImGuiWindowFlags.NoResize);

            var categories = this.plugin.NotebookService.GetCategories();
            if (categories.Count != 0)
            {
                ImGui.Columns(3, "###NeatNoterCategorySelectionColumns", true);
                foreach (var category in categories)
                {
                    var isChecked = selectedCategories!.Contains(category);
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
                    if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(category.Body))
                        ImGui.SetTooltip(category.Body);
                    ImGui.NextColumn();
                }
            }
            else
            {
                ImGui.Text(Loc.Localize("NoCategoriesToChoose", "No categories to choose from."));
            }

            ImGui.End();
        }

        private void DrawCategoryEntry(Category category, int index, float heightOffset)
        {
            var fontScale = ImGui.GetIO().FontGlobalScale;
            var heightMod = 4 + (25 * fontScale);
            heightOffset -= ImGui.GetScrollY() * fontScale;

            var color = new Vector4(category.Color, 0.5f);
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(color));

            var buttonLabel = category.Name;
            var cutNameLength = Math.Min(category.Name.Length, 70);
            for (var i = 1; i < cutNameLength && ImGui.CalcTextSize(buttonLabel).X > ElementSizeX - 22; i++)
                buttonLabel = category.Name[.. (cutNameLength - i)] + "...";
            if (ImGui.Button(category.IdentifierString, new Vector2(ElementSizeX, 25 * fontScale)) && !this.plugin.NotebookService.Loading)
            {
                this.OpenCategory(category);
            }

            if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(category.Body))
                ImGui.SetTooltip(category.Body);

            ImGui.PopStyleColor();

            ImGui.GetWindowDrawList().AddText(ImGui.GetWindowPos() + new Vector2(ElementSizeX * 0.045f, (index * heightMod) + heightOffset + (4 * fontScale)), TextColor, buttonLabel);

            if (ImGui.BeginPopupContextItem("##NeatNoter_" + category.IdentifierString))
            {
                if (ImGui.Selectable(Loc.Localize("ViewNotes", "View Notes")))
                {
                    this.plugin.NotebookService.SelectOneCategory(category);
                    this.SetState(UIState.NoteIndex);
                    this.noteTabFlags = ImGuiTabItemFlags.SetSelected;
                    this.categoryTabFlags = ImGuiTabItemFlags.None;
                }

                if (ImGui.Selectable(Loc.Localize("EditCategory", "Edit Category")))
                {
                    this.OpenCategory(category);
                }

                if (ImGui.Selectable(Loc.Localize("DeleteCategory", "Delete Category")))
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
            this.SetState(UIState.CategoryEdit);
        }

        private void DrawDocumentEditor(UniqueDocument? document)
        {
            if (this.transparencyWindowVisible) this.DrawTransparencySlider(ImGui.GetWindowPos(), ImGui.GetWindowSize());

            if (!this.minimalView)
            {
                var title = document?.Name;
                if (ImGui.InputText(document?.GetTypeName() + " " + Loc.Localize("Title", "Title"), ref title, 128))
                {
                    document!.Name = title;
                    document.Modified = DateUtil.CurrentTime();
                }
            }

            if (!this.backgroundVisible)
            {
                ImGui.Dummy(new Vector2(0, 16.0f) * ImGui.GetIO().FontGlobalScale);
            }

            var body = document?.Body;
            var inputFlags = ImGuiInputTextFlags.AllowTabInput;

            Vector4 color;
            unsafe
            {
                color = *ImGui.GetStyleColorVec4(ImGuiCol.FrameBg);
                color.W = this.editorTransparency;
            }

            ImGui.PushStyleColor(ImGuiCol.FrameBg, color);
            if (ImGui.InputTextMultiline(string.Empty, ref body, MaxNoteSize, new Vector2(ElementSizeX - (16 * ImGui.GetIO().FontGlobalScale), WindowSizeY - (this.minimalView ? 40 : 94)), inputFlags))
            {
                if (document != null)
                {
                    document.Body = body;
                    document.Modified = DateUtil.CurrentTime();
                }
            }

            if (ImGui.IsItemDeactivated() && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Escape)))
            {
                if (document != null)
                {
                    document.Body = this.previousNote;
                    document.Modified = DateUtil.CurrentTime();
                }
            }

            this.previousNote = body;

            ImGui.PopStyleColor();

            if (!ImGui.BeginPopupContextItem(Loc.Localize("EditorContextMenu", "Editor Context Menu") + " " + document?.InternalName)) return;
            if (!this.minimalView && ImGui.Selectable(Loc.Localize("MinimalView", "Minimal view")))
            {
                this.minimalView = true;
            }
            else if (this.minimalView && ImGui.Selectable(Loc.Localize("EndMinimalView", "End minimal view")))
            {
                this.minimalView = false;
            }

            if (this.backgroundVisible && ImGui.Selectable(Loc.Localize("HideBackground", "Hide background")))
            {
                this.backgroundVisible = false;
            }
            else if (!this.backgroundVisible && ImGui.Selectable(Loc.Localize("ShowBackground", "Show background")))
            {
                this.backgroundVisible = true;
            }

            if (!this.transparencyWindowVisible && ImGui.Selectable(Loc.Localize("ShowEditorTransparencySlider", "Show editor transparency slider")))
            {
                this.transparencyWindowVisible = true;
            }
            else if (this.transparencyWindowVisible && ImGui.Selectable(Loc.Localize("HideEditorTransparencySlider", "Hide editor transparency slider")))
            {
                this.transparencyWindowVisible = false;
            }

            ImGui.EndPopup();
        }

        private void DrawTransparencySlider(Vector2 mainWinPos, Vector2 mainWinSize)
        {
            var flags = ImGuiWindowFlags.AlwaysAutoResize;
            if (!this.backgroundVisible)
            {
                flags |= ImGuiWindowFlags.NoBackground;
                flags |= ImGuiWindowFlags.NoTitleBar;
            }

            ImGui.SetNextWindowPos(mainWinPos + new Vector2(0, mainWinSize.Y + (8 * ImGui.GetIO().FontGlobalScale)), ImGuiCond.Always);
            ImGui.Begin(Loc.Localize("TransparencySlider", "Transparency Slider") + "##51454623463", flags);
            {
                var label = "##51454623464";
                if (!this.backgroundVisible)
                {
                    label = Loc.Localize("TransparencySlider", "Transparency Slider") + label;
                }

                ImGui.DragFloat(label, ref this.editorTransparency, 0.01f, 0, 1);
            }

            ImGui.End();
        }

        private void SetState(UIState newState)
        {
            if (newState == this.state)
                return;

            this.plugin.SaveConfig();

            this.deletionWindowVisible = false;
            this.categoryWindowVisible = false;
            this.backgroundVisible = true;

            this.lastState = this.state;
            this.state = newState;
        }
    }
}
