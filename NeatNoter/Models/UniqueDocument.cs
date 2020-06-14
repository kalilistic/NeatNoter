using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;

namespace NeatNoter.Models
{
    public abstract class UniqueDocument : IEquatable<UniqueDocument>
    {
        // This is needed so that ImGui doesn't screw up and only allow the first note with a particular name to be opened.
        // I could've just restricted names to be unique, but that felt lazy and I'm not in a lazy mood.
        [JsonIgnore]
        public string Name
        {
            get => InternalName.Substring(0, InternalName.IndexOf("#", StringComparison.Ordinal));
            set => InternalName = value + IdentifierString;
        }

        [JsonIgnore]
        public string IdentifierString => InternalName.Substring(InternalName.IndexOf("#", StringComparison.Ordinal));

        public string InternalName { get; set; }

        [JsonIgnore]
        public string Body { get; set; }

        // ReSharper disable once MemberCanBePrivate.Global
        public string InternalBody { get; set; }

        public IList<Image> Images { get; set; }

        public List<(Vector2, Vector2, Vector3, float)> Lines { get; set; }

        public void AddImage(string b64, Vector2 pos)
        {
            Images.Add(new Image
            {
                Position = pos,
                InternalTexture = b64,
            });
        }

        public void CompressBody()
        {
            using var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(Body));
            using var compressedBodyStream = new MemoryStream();

            var gzipStream = new GZipStream(compressedBodyStream, CompressionMode.Compress);
            bodyStream.CopyTo(gzipStream);
            gzipStream.Dispose();

            InternalBody = Convert.ToBase64String(compressedBodyStream.ToArray());
        }

        public void DecompressBody()
        {
            using var internalBodyStream = new MemoryStream(Convert.FromBase64String(InternalBody));
            using var decompressedBodyStream = new MemoryStream();

            var gzipStream = new GZipStream(internalBodyStream, CompressionMode.Decompress);
            gzipStream.CopyTo(decompressedBodyStream);
            gzipStream.Dispose();

            Body = Encoding.UTF8.GetString(decompressedBodyStream.ToArray());
        }

        [JsonIgnore] private string typeName; // Cached to avoid excessive reflection

        public string GetTypeName()
        {
            if (string.IsNullOrEmpty(this.typeName))
                this.typeName = this.GetType().Name;
            return this.typeName;
        }

        public bool Equals(UniqueDocument other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return GetTypeName() == other.GetTypeName() && InternalName == other.InternalName;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UniqueDocument) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = typeName.GetHashCode();
                hashCode = (hashCode * 397) ^ InternalName.GetHashCode();
                hashCode = (hashCode * 397) ^ Body.GetHashCode();
                return hashCode;
            }
        }
    }
}
