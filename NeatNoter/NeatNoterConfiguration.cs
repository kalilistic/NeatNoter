using System.Collections.Generic;
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
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}
