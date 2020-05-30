using System.IO;

namespace NeatNoter.Models
{
    public interface IMapProvider
    {
        MemoryStream GetCurrentMap();
    }
}
