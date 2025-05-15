#nullable disable
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Integration.Common.FileWatcher;

/// <devdoc>
/// Features:
/// - Buffers FileSystemWatcher events in a BlockingCollection to prevent InternalBufferOverflowExceptions.
/// - Does not break the original FileSystemWatcher API.
/// - Supports reporting existing files via a new Existed event.
/// - Supports sorting events by oldest (existing) file first.
/// - Supports an new event Any reporting any FSW change.
/// - Offers the Error event in Win Forms designer (via [Browsable[true)]
/// - Does not prevent duplicate files occuring.
/// Notes:
///   We contain FilSystemWatcher to follow the principle of composition over inheritance
///   and because System.IO.FileSystemWatcher is not designed to be inherited from:
///   Event handlers and Dispose(disposing) are not virtual.
/// </devdoc>
public class BufferingFileSystemWatcher : Component
{
    private readonly System.IO.FileSystemWatcher _containedFsw;

    private FileSystemEventHandler _onExistedHandler;
    private FileSystemEventHandler _onAllChangesHandler;

    private FileSystemEventHandler _onCreatedHandler;
    private FileSystemEventHandler _onChangedHandler;
    private FileSystemEventHandler _onDeletedHandler;
    private RenamedEventHandler _onRenamedHandler;

    private ErrorEventHandler _onErrorHandler;

    //We use a single buffer for all change types. Alternatively we could use one buffer per event type, costing additional enumerate tasks.
    private BlockingCollection<FileSystemEventArgs> _fileSystemEventBuffer;
    private CancellationTokenSource _cancellationTokenSource;

    #region Contained FileSystemWatcher
    public BufferingFileSystemWatcher()
    {
        _containedFsw = new System.IO.FileSystemWatcher();
    }

    public BufferingFileSystemWatcher(string path)
    {
        _containedFsw = new System.IO.FileSystemWatcher(path, "*.*");
    }

    public BufferingFileSystemWatcher(string path, string filter)
    {
        _containedFsw = new System.IO.FileSystemWatcher(path, filter);
    }

    public bool EnableRaisingEvents
    {
        get
        {
            return _containedFsw.EnableRaisingEvents;
        }
        set
        {
            if (_containedFsw.EnableRaisingEvents == value) return;

            StopRaisingBufferedEvents();
            _cancellationTokenSource = new CancellationTokenSource();

            //We EnableRaisingEvents, before NotifyExistingFiles
            //  to prevent missing any events
            //  accepting more duplicates (which may occure anyway).
            _containedFsw.EnableRaisingEvents = value;
            if (value)
                RaiseBufferedEventsUntilCancelled();
        }
    }

    public string Filter
    {
        get { return _containedFsw.Filter; }
        set { _containedFsw.Filter = value; }
    }

    public Collection<string> Filters => _containedFsw.Filters;

    public bool IncludeSubdirectories
    {
        get { return _containedFsw.IncludeSubdirectories; }
        set { _containedFsw.IncludeSubdirectories = value; }
    }

    public int InternalBufferSize
    {
        get { return _containedFsw.InternalBufferSize; }
        set { _containedFsw.InternalBufferSize = value; }
    }

    public NotifyFilters NotifyFilter
    {
        get { return _containedFsw.NotifyFilter; }
        set { _containedFsw.NotifyFilter = value; }
    }

    public string Path
    {
        get { return _containedFsw.Path; }
        set { _containedFsw.Path = value; }
    }

    public ISynchronizeInvoke SynchronizingObject
    {
        get { return _containedFsw.SynchronizingObject; }
        set { _containedFsw.SynchronizingObject = value; }
    }

    public override ISite Site
    {
        get { return _containedFsw.Site; }
        set { _containedFsw.Site = value; }
    }

    #endregion

    [DefaultValue(false)]
    public bool OrderByOldestFirst { get; set; } = false;

    public int EventQueueCapacity { get; set; } = int.MaxValue;

    #region New BufferingFileSystemWatcher specific events
    public event FileSystemEventHandler Existed
    {
        add
        {
            _onExistedHandler += value;
        }
        remove
        {
            _onExistedHandler -= value;
        }
    }

    public event FileSystemEventHandler All
    {
        add
        {
            if (_onAllChangesHandler == null)
            {
                _containedFsw.Created += BufferEvent;
                _containedFsw.Changed += BufferEvent;
                _containedFsw.Renamed += BufferEvent;
                _containedFsw.Deleted += BufferEvent;
            }
            _onAllChangesHandler += value;
        }
        remove
        {
            _containedFsw.Created -= BufferEvent;
            _containedFsw.Changed -= BufferEvent;
            _containedFsw.Renamed -= BufferEvent;
            _containedFsw.Deleted -= BufferEvent;
            _onAllChangesHandler -= value;
        }
    }

    #endregion

    #region Standard FSW events
    //- The _fsw events add to the buffer.
    //- The public events raise from the buffer to the consumer.
    public event FileSystemEventHandler Created
    {
        add
        {
            if (_onCreatedHandler == null)
                _containedFsw.Created += BufferEvent;
            _onCreatedHandler += value;
        }
        remove
        {
            _containedFsw.Created -= BufferEvent;
            _onCreatedHandler -= value;
        }
    }

    public event FileSystemEventHandler Changed
    {
        add
        {
            if (_onChangedHandler == null)
                _containedFsw.Changed += BufferEvent;
            _onChangedHandler += value;
        }
        remove
        {
            _containedFsw.Changed -= BufferEvent;
            _onChangedHandler -= value;
        }
    }

    public event FileSystemEventHandler Deleted
    {
        add
        {
            if (_onDeletedHandler == null)
            {
                _containedFsw.Deleted += BufferEvent;
                _onDeletedHandler += value;
            }
        }
        remove
        {
            _containedFsw.Deleted -= BufferEvent;
            _onDeletedHandler -= value;
        }
    }

    public event RenamedEventHandler Renamed
    {
        add
        {
            if (_onRenamedHandler == null)
                _containedFsw.Renamed += BufferEvent;
            _onRenamedHandler += value;
        }
        remove
        {
            _containedFsw.Renamed -= BufferEvent;
            _onRenamedHandler -= value;
        }
    }

    private void BufferEvent(object _, FileSystemEventArgs e)
    {
        if (!_fileSystemEventBuffer.TryAdd(e))
        {
            var ex = new EventQueueOverflowException($"Event queue size {_fileSystemEventBuffer.BoundedCapacity} events exceeded.");
            InvokeHandler(_onErrorHandler, new ErrorEventArgs(ex));
        }
    }

    private void StopRaisingBufferedEvents(object _ = null, EventArgs __ = null)
    {
        _cancellationTokenSource?.Cancel();
        _fileSystemEventBuffer = new BlockingCollection<FileSystemEventArgs>(EventQueueCapacity);
    }

    public event ErrorEventHandler Error
    {
        add
        {
            if (_onErrorHandler == null)
                _containedFsw.Error += BufferingFileSystemWatcher_Error;
            _onErrorHandler += value;
        }
        remove
        {
            if (_onErrorHandler == null)
                _containedFsw.Error -= BufferingFileSystemWatcher_Error;
            _onErrorHandler -= value;
        }
    }

    private void BufferingFileSystemWatcher_Error(object sender, ErrorEventArgs e)
    {
        InvokeHandler(_onErrorHandler, e);
    }
    #endregion

    private void RaiseBufferedEventsUntilCancelled()
    {
        _ = Task.Run(() =>
        {
            try
            {
                if (_onExistedHandler != null || _onAllChangesHandler != null)
                    NotifyExistingFiles();

                foreach (
                    FileSystemEventArgs e in
                    _fileSystemEventBuffer.GetConsumingEnumerable(_cancellationTokenSource.Token))
                {
                    if (_onAllChangesHandler != null)
                        InvokeHandler(_onAllChangesHandler, e);
                    else
                    {
                        switch (e.ChangeType)
                        {
                            case WatcherChangeTypes.Created:
                                InvokeHandler(_onCreatedHandler, e);
                                break;
                            case WatcherChangeTypes.Changed:
                                InvokeHandler(_onChangedHandler, e);
                                break;
                            case WatcherChangeTypes.Deleted:
                                InvokeHandler(_onDeletedHandler, e);
                                break;
                            case WatcherChangeTypes.Renamed:
                                InvokeHandler(_onRenamedHandler, e as RenamedEventArgs);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BufferingFileSystemWatcher_Error(this, new ErrorEventArgs(ex));
            }
        });
    }

    private void NotifyExistingFiles()
    {
        var directoryInfo = new DirectoryInfo(Path);
        var searchOption = IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var existingFiles = GetAllFilters().SelectMany(x => directoryInfo.GetFiles(x, searchOption));

        if (OrderByOldestFirst)
            existingFiles = existingFiles.OrderBy(x => x.LastWriteTime);

        foreach (var fileInfo in existingFiles)
        {
            InvokeHandler(_onExistedHandler, new FileSystemEventArgs(WatcherChangeTypes.All, Path, fileInfo.Name));
            InvokeHandler(_onAllChangesHandler, new FileSystemEventArgs(WatcherChangeTypes.All, Path, fileInfo.Name));
        }
    }

    public IEnumerable<string> GetAllFilters()
    {
        var allFilters = Filters?.ToList() ?? new List<string>();

        if(!string.IsNullOrEmpty(Filter))
            allFilters.Add(Filter);

        if(!allFilters.Any())
            throw new ArgumentNullException("Filter(s) are not set.");

        return allFilters;
    }

    #region InvokeHandlers
    //Automatically raise event in calling thread when _fsw.SynchronizingObject is set. Ex: When used as a component in Win Forms.
    private void InvokeHandler(FileSystemEventHandler eventHandler, FileSystemEventArgs e)
    {
        if (eventHandler != null)
        {
            if (_containedFsw.SynchronizingObject != null && _containedFsw.SynchronizingObject.InvokeRequired)
                _containedFsw.SynchronizingObject.BeginInvoke(eventHandler, new object[] { this, e });
            else
                eventHandler(this, e);
        }
    }
    private void InvokeHandler(RenamedEventHandler eventHandler, RenamedEventArgs e)
    {
        if (eventHandler != null)
        {
            if (_containedFsw.SynchronizingObject != null && _containedFsw.SynchronizingObject.InvokeRequired)
                _containedFsw.SynchronizingObject.BeginInvoke(eventHandler, new object[] { this, e });
            else
                eventHandler(this, e);
        }
    }
    private void InvokeHandler(ErrorEventHandler eventHandler, ErrorEventArgs e)
    {
        if (eventHandler != null)
        {
            if (_containedFsw.SynchronizingObject != null && _containedFsw.SynchronizingObject.InvokeRequired)
                _containedFsw.SynchronizingObject.BeginInvoke(eventHandler, new object[] { this, e });
            else
                eventHandler(this, e);
        }
    }
    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationTokenSource?.Cancel();
            _containedFsw?.Dispose();
        }
        base.Dispose(disposing);
    }
}
