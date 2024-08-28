using System.ComponentModel;

namespace Integration.Common.FileWatcher;

public class FileWatcherErrorEventArgs : HandledEventArgs
{
    public readonly Exception Error;
    public FileWatcherErrorEventArgs(Exception exception)
    {
        this.Error = exception;
    }
}