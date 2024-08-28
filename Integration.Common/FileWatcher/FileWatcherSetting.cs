namespace Integration.Common.FileWatcher;

public class FileWatcherSetting : IFileWatcherSetting
{
    public string Directory { get; set; }
    public IEnumerable<string> Filters { get; set; }
}