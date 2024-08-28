using Integration.Common.FileWatcher;
using Microsoft.Extensions.Hosting;

namespace Integration.Common.Services
{
    public abstract class FileWatcherService : IHostedService
    {
        private readonly List<BufferingFileSystemWatcher> _fileWatchers = new();

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var fileSystemWatcher in _fileWatchers)
                fileSystemWatcher.EnableRaisingEvents = true;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var fileSystemWatcher in _fileWatchers)
            {
                fileSystemWatcher.EnableRaisingEvents = false;
                fileSystemWatcher.Dispose();
            }

            return Task.CompletedTask;
        }

        protected void AddFileWatcher(string path, string filter)
        {
            var fileSystemWatcher = new BufferingFileSystemWatcher(path, filter);
            fileSystemWatcher.All += OnAllChanges;
            _fileWatchers.Add(fileSystemWatcher);
        }

        protected void AddFileWatcher(string path, string filter, bool includeSubDirectories)
        {
            var fileSystemWatcher = new BufferingFileSystemWatcher(path, filter);
            fileSystemWatcher.IncludeSubdirectories = includeSubDirectories;
            fileSystemWatcher.All += OnAllChanges;
            _fileWatchers.Add(fileSystemWatcher);
        }

        protected void AddFileWatcher(string path, IEnumerable<string> filters)
        {
            var fileSystemWatcher = new BufferingFileSystemWatcher(path);

            foreach (var filter in filters)
                fileSystemWatcher.Filters.Add(filter);

            fileSystemWatcher.All += OnAllChanges;
            _fileWatchers.Add(fileSystemWatcher);
        }

        protected virtual void OnAllChanges(object sender, FileSystemEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
