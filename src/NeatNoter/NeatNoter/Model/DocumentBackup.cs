using System.Collections.Generic;

namespace NeatNoter
{
    /// <summary>
    /// Document backup.
    /// </summary>
    public class DocumentBackup
    {
        /// <summary>
        /// Gets or sets list of notes.
        /// </summary>
        public List<Note>? Notes { get; set; } = new ();

        /// <summary>
        /// Gets or sets list of categories.
        /// </summary>
        public List<Category>? Categories { get; set; } = new ();
    }
}
