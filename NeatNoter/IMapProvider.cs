using System.IO;

namespace NeatNoter
{
    public interface IMapProvider
    {
        MemoryStream GetCurrentMap();
    }
}
