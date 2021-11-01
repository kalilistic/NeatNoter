using System;
using System.IO;
using System.IO.Compression;
using System.Text;

using LiteDB;
using Newtonsoft.Json;

namespace NeatNoter
{
    /// <summary>
    /// Common document.
    /// </summary>
    public abstract class UniqueDocument : IEquatable<UniqueDocument>
    {
        [BsonIgnore]
        [JsonIgnore]
        private string typeName = string.Empty;

        /// <summary>
        /// Gets or sets document name.
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        public string Name
        {
            get => this.InternalName[..this.InternalName.IndexOf("#", StringComparison.Ordinal)];
            set => this.InternalName = value + this.IdentifierString;
        }

        /// <summary>
        /// Gets document ID.
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        public string IdentifierString => this.InternalName[this.InternalName.IndexOf("#", StringComparison.Ordinal) ..];

        /// <summary>
        /// Gets or sets id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets document internal name.
        /// </summary>
        public string InternalName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets document body.
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets document internal body.
        /// </summary>
        public string InternalBody { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether document is visible.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets created date (unix timestamp).
        /// </summary>
        public long Created { get; set; }

        /// <summary>
        /// Gets or sets last modified date (unix timestamp).
        /// </summary>
        public long Modified { get; set; }

        /// <summary>
        /// Compress string.
        /// </summary>
        public void CompressBody()
        {
            using var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(this.Body));
            using var compressedBodyStream = new MemoryStream();

            var gzipStream = new GZipStream(compressedBodyStream, CompressionMode.Compress);
            bodyStream.CopyTo(gzipStream);
            gzipStream.Dispose();

            this.InternalBody = Convert.ToBase64String(compressedBodyStream.ToArray());
        }

        /// <summary>
        /// Decompress string.
        /// </summary>
        public void DecompressBody()
        {
            using var internalBodyStream = new MemoryStream(Convert.FromBase64String(this.InternalBody));
            using var decompressedBodyStream = new MemoryStream();

            var gzipStream = new GZipStream(internalBodyStream, CompressionMode.Decompress);
            gzipStream.CopyTo(decompressedBodyStream);
            gzipStream.Dispose();

            this.Body = Encoding.UTF8.GetString(decompressedBodyStream.ToArray());
        }

        /// <summary>
        /// Get type name.
        /// </summary>
        /// <returns>type name.</returns>
        public string GetTypeName()
        {
            if (string.IsNullOrEmpty(this.typeName))
                this.typeName = this.GetType().Name;
            return this.typeName;
        }

        /// <inheritdoc />
        public bool Equals(UniqueDocument? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.GetTypeName() == other.GetTypeName() && this.InternalName == other.InternalName;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((UniqueDocument)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                var hashCode = this.typeName.GetHashCode();

                // ReSharper disable once NonReadonlyMemberInGetHashCode
                hashCode = (hashCode * 397) ^ this.InternalName.GetHashCode();

                // ReSharper disable once NonReadonlyMemberInGetHashCode
                hashCode = (hashCode * 397) ^ this.Body.GetHashCode();
                return hashCode;
            }
        }
    }
}
