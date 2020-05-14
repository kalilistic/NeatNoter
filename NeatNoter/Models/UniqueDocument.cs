using System;
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

        public string Body { get; set; }

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
            return GetTypeName() == other.GetTypeName() && InternalName == other.InternalName && Body == other.Body;
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
