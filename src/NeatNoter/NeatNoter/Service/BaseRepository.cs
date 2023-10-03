namespace NeatNoter;

public class BaseRepository : Repository
{
    protected BaseRepository(string pluginFolder)
        : base(pluginFolder)
    {
    }

    public int GetSchemaVersion()
    {
        return this.GetVersion();
    }

    public void SetSchemaVersion(int version)
    {
        this.SetVersion(version);
    }
}
