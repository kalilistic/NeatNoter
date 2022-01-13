using System;
using System.Collections.Generic;

using Dalamud.Configuration;

namespace NeatNoter
{
    /// <inheritdoc />
    public class NeatNoterConfiguration : IPluginConfiguration
    {
        /// <inheritdoc />
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether include note bodies while searching.
        /// </summary>
        public bool IncludeNoteBodiesInSearch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether fresh install.
        /// </summary>
        public bool JustInstalled { get; set; } = true;

        /// <summary>
        /// Gets or sets automatic export path.
        /// </summary>
        public string? AutomaticExportPath { get; set; }

        /// <summary>
        /// Gets or sets save frequency in ms.
        /// </summary>
        public int SaveFrequency { get; set; } = 5000;

        /// <summary>
        /// Gets or sets save frequency in ms to perform full backup.
        /// </summary>
        public int FullSaveFrequency { get; set; } = 14400000;

        /// <summary>
        /// Gets or sets a value indicating whether no category notes are selected.
        /// </summary>
        public bool IsNoCategorySelected { get; set; } = true;

        /// <summary>
        /// Gets or sets note sort type.
        /// </summary>
        public int NoteSortType { get; set; } = 4;

        /// <summary>
        /// Gets or sets category sort type.
        /// </summary>
        public int CategorySortType { get; set; } = 4;

        /// <summary>
        /// Gets or sets number of backups to keep before deleting the oldest.
        /// </summary>
        public int BackupRetention { get; set; } = 7;

        /// <summary>
        /// Gets or sets backup frequency in ms.
        /// </summary>
        public long BackupFrequency { get; set; } = 86400000;

        /// <summary>
        /// Gets or sets last backup in ms.
        /// </summary>
        public long LastBackup { get; set; }

        /// <summary>
        /// Gets or sets plugin version to use for special processing on upgrades.
        /// </summary>
        public int PluginVersion { get; set; }

        /// <summary>
        /// Gets or sets notes.
        /// </summary>
        [Obsolete("Use DB now")]
        public List<Note> Notes { get; set; } = new ();

        /// <summary>
        /// Gets or sets categories.
        /// </summary>
        [Obsolete("Use DB now")]
        public List<Category> Categories { get; set; } = new ();

        /// <summary>
        /// Gets or sets a value indicating whether notebook window is visible.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show content preview.
        /// </summary>
        public bool ShowContentPreview { get; set; } = true;
    }
}
