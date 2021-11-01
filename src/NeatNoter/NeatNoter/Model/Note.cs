using System.Collections.Generic;

namespace NeatNoter
{
    /// <inheritdoc />
    public class Note : UniqueDocument
    {
        /// <summary>
        /// Gets categories for note.
        /// </summary>
        public IList<Category> Categories { get; init; } = new List<Category>();
    }
}
