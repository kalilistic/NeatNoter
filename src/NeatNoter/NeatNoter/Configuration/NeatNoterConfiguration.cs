using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Configuration;

namespace NeatNoter
{
    /// <inheritdoc />
    public class NeatNoterConfiguration : IPluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NeatNoterConfiguration"/> class.
        /// </summary>
        public NeatNoterConfiguration()
        {
            this.Notes = new List<Note>();
            this.Categories = new List<Category>();

            this.PenThickness = 2.0f;
            this.PenColor = new Vector3(1.0f, 1.0f, 1.0f);

            this.JustInstalled = true;
        }

        /// <inheritdoc />
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether include note bodies while searching.
        /// </summary>
        public bool IncludeNoteBodiesInSearch { get; set; }

        /// <summary>
        /// Gets or sets pen thickness.
        /// </summary>
        public float PenThickness { get; set; }

        /// <summary>
        /// Gets or sets pen color.
        /// </summary>
        public Vector3 PenColor { get; set; }

        /// <summary>
        /// Gets or sets notes.
        /// </summary>
        public List<Note> Notes { get; set; }

        /// <summary>
        /// Gets or sets categories.
        /// </summary>
        public List<Category> Categories { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether fresh install.
        /// </summary>
        public bool JustInstalled { get; set; }

        /// <summary>
        /// Gets or sets automatic export path.
        /// </summary>
        public string? AutomaticExportPath { get; set; }

        /// <summary>
        /// Initialize config.
        /// </summary>
        /// <param name="onPlayerLoad">on player load.</param>
        public void Initialize(Action? onPlayerLoad = null)
        {
            this.Notes.InitializeAll(NeatNoterPlugin.PluginInterface);
            this.Categories.InitializeAll(NeatNoterPlugin.PluginInterface);

            OnPlayerLoad(onPlayerLoad);
        }

        /// <summary>
        /// Save notes.
        /// </summary>
        public void Save()
        {
            foreach (var category in this.Categories)
            {
                category.CompressBody();
            }

            foreach (var note in this.Notes)
            {
                note.CompressBody();
            }

            NeatNoterPlugin.PluginInterface.SavePluginConfig(this);
        }

        private static void OnPlayerLoad(Action? fn)
        {
            if (fn == null) return;

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    if (NeatNoterPlugin.ClientState.LocalContentId != 0)
                    {
                        fn();
                        break;
                    }

                    await Task.Delay(1000);
                }
            });
        }
    }
}
