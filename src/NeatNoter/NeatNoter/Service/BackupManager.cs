using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dalamud.DrunkenToad.Helpers;
using Dalamud.Logging;

namespace NeatNoter;

public class BackupManager
{
    private readonly string dataPath;

    public BackupManager(string pluginFolder)
    {
        this.dataPath = pluginFolder + "/data/";
    }

    public void CreateBackup(string prefix = "")
    {
        try
        {
            var backupDir = $"{this.dataPath}{prefix}{UnixTimestampHelper.CurrentTime()}/";
            Directory.CreateDirectory(backupDir);
            var files = Directory.GetFiles(this.dataPath);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                File.Copy(this.dataPath + fileName, backupDir + fileName, true);
            }
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Failed to create backup.");
        }
    }

    public void DeleteBackups(int max)
    {
        // don't delete everything
        if (max == 0)
        {
            return;
        }

        try
        {
            // loop through directories and get those without prefix
            var dirs = Directory.GetDirectories(this.dataPath);
            var dirNames = new List<long>();
            foreach (var dir in dirs)
            {
                try
                {
                    var dirName = new DirectoryInfo(dir).Name;
                    if (dirName.Any(char.IsLetter))
                    {
                        continue;
                    }

                    dirNames.Add(Convert.ToInt64(dirName));
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            // if don't exceed max then out
            if (dirs.Length <= max)
            {
                return;
            }

            dirNames.Sort();
            Directory.Delete(this.dataPath + dirNames.First(), true);
            this.DeleteBackups(max);
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Failed to delete backup.");
        }
    }
}
