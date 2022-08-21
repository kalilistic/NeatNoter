using System.Collections.Generic;
using System.Linq;

using CheapLoc;

// ReSharper disable CollectionNeverQueried.Global
namespace NeatNoter
{
    /// <summary>
    /// Sort type for document list.
    /// </summary>
    public class DocumentSortType
    {
        /// <summary>
        /// List of available sort types.
        /// </summary>
        public static readonly List<DocumentSortType> DocumentSortTypes = new();

        /// <summary>
        /// List of available sort type names.
        /// </summary>
        public static readonly List<string> DocumentSortTypeNames = new();

        /// <summary>
        /// Sort Type: Name Ascending.
        /// </summary>
        public static readonly DocumentSortType NameAscending = new(0, 0, Loc.Localize("NameAscending", "Name (Ascending)"));

        /// <summary>
        /// Sort Type: Name Descending.
        /// </summary>
        public static readonly DocumentSortType NameDescending = new(1, 1, Loc.Localize("NameDescending", "Name (Descending)"));

        /// <summary>
        /// Sort Type: Created Date Ascending.
        /// </summary>
        public static readonly DocumentSortType CreatedAscending = new(2, 2, Loc.Localize("CreatedAscending", "Created (Ascending)"));

        /// <summary>
        /// Sort Type: Created Date Descending.
        /// </summary>
        public static readonly DocumentSortType CreatedDescending = new(3, 3, Loc.Localize("CreatedDescending", "Created (Descending)"));

        /// <summary>
        /// Sort Type: Modified Date Descending.
        /// </summary>
        public static readonly DocumentSortType ModifiedAscending = new(4, 4, Loc.Localize("ModifiedAscending", "Modified (Ascending)"));

        /// <summary>
        /// Sort Type: Modified Date Descending.
        /// </summary>
        public static readonly DocumentSortType ModifiedDescending = new(5, 5, Loc.Localize("ModifiedDescending", "Modified (Descending)"));

        private DocumentSortType(int index, int code, string name)
        {
            this.Index = index;
            this.Name = name;
            this.Code = code;
            DocumentSortTypes.Add(this);
            DocumentSortTypeNames.Add(name);
        }

        /// <summary>
        /// Gets or sets sort type index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets sort type code.
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets sort type name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get document sort type by index.
        /// </summary>
        /// <param name="index">sort index.</param>
        /// <returns>sort type.</returns>
        public static DocumentSortType GetDocumentSortTypeByIndex(int index)
        {
            return DocumentSortTypes.FirstOrDefault(view => view.Index == index)!;
        }

        /// <summary>
        /// Initialize names.
        /// </summary>
        public static void Init()
        {
            NameAscending.Name = Loc.Localize("NameAscending", "Name (Ascending)");
            NameDescending.Name = Loc.Localize("NameDescending", "Name (Descending)");
            CreatedAscending.Name = Loc.Localize("CreatedAscending", "Created (Ascending)");
            CreatedDescending.Name = Loc.Localize("CreatedDescending", "Created (Descending)");
            ModifiedAscending.Name = Loc.Localize("ModifiedAscending", "Modified (Ascending)");
            ModifiedDescending.Name = Loc.Localize("ModifiedDescending", "Modified (Descending)");
        }

        /// <summary>
        /// Return sort name.
        /// </summary>
        /// <returns>sort name.</returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
