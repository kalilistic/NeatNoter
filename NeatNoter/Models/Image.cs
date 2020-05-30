using System;
using System.Numerics;
using Dalamud.Interface;
using ImGuiScene;
using Newtonsoft.Json;

namespace NeatNoter.Models
{
    public class Image
    {
        public Vector2 Position { get; set; }

        [JsonIgnore] public TextureWrap Texture => this.uiBuilder.LoadImage(Convert.FromBase64String(InternalTexture));

        public string InternalTexture { get; set; }

        [JsonIgnore] private UiBuilder uiBuilder;
        public void Initialize(UiBuilder builder) => uiBuilder = builder;
    }
}
