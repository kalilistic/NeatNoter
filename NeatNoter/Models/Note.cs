using System.Collections.Generic;

namespace NeatNoter.Models
{
    public class Note : UniqueDocument
    {
        public List<Category> Categories { get; set; }
    }
}
