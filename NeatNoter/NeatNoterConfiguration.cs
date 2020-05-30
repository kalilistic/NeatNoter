using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Plugin;
using NeatNoter.Models;
using Newtonsoft.Json;

namespace NeatNoter
{
    public class NeatNoterConfiguration : IPluginConfiguration
    {
        public int Version { get; set; }

        public bool IncludeNoteBodiesInSearch { get; set; }

        public List<Note> Notes { get; set; } // TODO: Add backup functionality
        public List<Category> Categories { get; set; }

        public NeatNoterConfiguration()
        {
            Notes = new List<Note>();
            Categories = new List<Category>();
        }

        [JsonIgnore]
        private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            foreach (var category in Categories)
            {
                category.DecompressBody();
            }
            foreach (var note in Notes)
            {
                note.DecompressBody();
            }

            // v1.1 compat
            foreach (var document in Notes.Cast<UniqueDocument>().Concat(Categories))
            {
                if (document.Images == null)
                {
                    document.Images = new List<Image>();
                }
                if (document.Lines == null)
                {
                    document.Lines = new List<Tuple<Vector2, Vector2, Vector3>>();
                }

                foreach (var image in document.Images)
                {
                    image.Initialize(this.pluginInterface.UiBuilder);
                }
            }
        }

        public void Save()
        {
            foreach (var category in Categories)
            {
                category.CompressBody();
            }
            foreach (var note in Notes)
            {
                note.CompressBody();
            }

            this.pluginInterface.SavePluginConfig(this);
        }
    }
}
