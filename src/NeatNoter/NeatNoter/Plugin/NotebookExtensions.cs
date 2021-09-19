using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Plugin;

namespace NeatNoter
{
    /// <summary>
    /// Notebook extensions.
    /// </summary>
    public static class NotebookExtensions
    {
        /// <summary>
        /// Filter by categories.
        /// </summary>
        /// <param name="notes">notes.</param>
        /// <param name="categories">categories.</param>
        /// <returns>filtered notes.</returns>
        public static IEnumerable<Note> FilterByCategories(this IEnumerable<Note> notes, IEnumerable<Category> categories)
            => notes.Where(note => categories.All(category => note.Categories.Contains(category)));

        /// <summary>
        /// Alphabetize notes.
        /// </summary>
        /// <param name="documents">documents.</param>
        /// <param name="direction">alphabetize type.</param>
        /// <typeparam name="T">type.</typeparam>
        /// <returns>list of document.</returns>
        public static List<T> Alphabetize<T>(this IEnumerable<T> documents, SortDirection direction) where T : UniqueDocument
        {
            var docList = documents.ToList();
            docList.Sort((a, b) => direction == SortDirection.Ascending
                                       ? string.Compare(a.Name, b.Name, StringComparison.Ordinal)
                                       : string.Compare(b.Name, a.Name, StringComparison.Ordinal));
            return docList;
        }

        /// <summary>
        /// Initialize all.
        /// </summary>
        /// <param name="list">list.</param>
        /// <param name="pluginInterface">plugin interface.</param>
        /// <typeparam name="T">type.</typeparam>
        /// <exception cref="ArgumentNullException">null list exception.</exception>
        public static void InitializeAll<T>(this IList<T> list, DalamudPluginInterface pluginInterface) where T : UniqueDocument
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            foreach (var document in list)
            {
                document.DecompressBody();

                // ReSharper disable once ConstantNullCoalescingCondition
                document.Lines ??= new List<(Vector2, Vector2, Vector3, float)>();
            }
        }
    }
}
