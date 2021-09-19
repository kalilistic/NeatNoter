using System.Collections.Generic;

namespace NeatNoter
{
    /// <inheritdoc />
    public class Note : UniqueDocument
    {
        /// <summary>
        /// Gets or sets character associated with the note.
        /// </summary>
        public ulong AssociatedCharacter { get; set; }

        /// <summary>
        /// Gets categories for note.
        /// </summary>
        public IList<Category> Categories { get; init; } = new List<Category>();
    }
}
