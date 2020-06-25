using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
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
        public float PenThickness { get; set; }
        public Vector3 PenColor { get; set; }

        public List<Note> Notes { get; set; }
        public List<Category> Categories { get; set; }

        public bool JustInstalled { get; set; }

        public NeatNoterConfiguration()
        {
            Notes = new List<Note>();
            Categories = new List<Category>();

            PenThickness = 2.0f;
            PenColor = new Vector3(1.0f, 1.0f, 1.0f);

            JustInstalled = true;
        }

        [JsonIgnore]
        private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pi, Action onPlayerLoad = null)
        {
            this.pluginInterface = pi;

            Notes.InitializeAll(this.pluginInterface);
            Categories.InitializeAll(this.pluginInterface);

            OnPlayerLoad(onPlayerLoad);
        }

        private void OnPlayerLoad(Action fn)
        {
            if (fn == null) return;

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    if (this.pluginInterface.ClientState.LocalPlayer != null)
                    {
                        fn();
                    }
                    await Task.Delay(1000);
                }
            });
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
