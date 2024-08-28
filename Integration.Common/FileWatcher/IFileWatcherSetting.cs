namespace Integration.Common.FileWatcher;

public interface IFileWatcherSetting
{
    string Directory { get; set; }
    IEnumerable<string> Filters { get; set; }
}