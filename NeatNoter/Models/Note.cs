using System.Collections.Generic;

namespace NeatNoter.Models
{
    public class Note : UniqueDocument
    {
        public ulong AssociatedCharacter { get; set; } // TODO

        public List<Category> Categories { get; set; }
    }
}
