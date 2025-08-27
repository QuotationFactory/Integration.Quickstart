using System;
using System.IO;
using System.Threading;
using Integration.Common.FileWatcher;
using Integration.Host.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integration.Host.Features.FileOrchestrator;

/// <summary>
/// Service that watches on the output directory of the edge connector for *.json files
/// Publishes an OutputFileCreated notification if a file is created
/// </summary>
public class OutputFileWatcherService : FileWatcherService
{
    private readonly IMediator _mediator;
    private readonly ILogger<OutputFileWatcherService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public OutputFileWatcherService(IMediator mediator, IOptions<IntegrationSettings> options, ILogger<OutputFileWatcherService> logger)
    {
        _mediator = mediator;
        _logger = logger;

        if (options.Value.NumberOfConcurrentTasks > 1)
        {
            _semaphore = new(options.Value.NumberOfConcurrentTasks, options.Value.NumberOfConcurrentTasks);
            _logger.LogInformation("Semaphore initialized with {NumberOfConcurrentTasks} concurrent tasks", options.Value.NumberOfConcurrentTasks);
        }

        // add file watcher to the output directory
        AddFileWatcher(options.Value.GetOrCreateOutputDirectory(createIfNotExists: true), "*.json");
    }

    protected override void OnAllChanges(object sender, FileSystemEventArgs e)
    {
        bool? isDone = null;
        try
        {
            _semaphore.Wait();
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    _mediator.Publish(new OutputFileOrchestrator.OutputFileCreated(e.FullPath)).ConfigureAwait(false).GetAwaiter().GetResult();
                    isDone = true;
                    _logger.LogInformation("Successfully processed {Event} for file {FilePath}", e.ChangeType, e.FullPath);
                    break;
                case WatcherChangeTypes.Deleted:
                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.Renamed:
                case WatcherChangeTypes.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (Exception ex)
        {
            isDone = false;
            _logger.LogError(ex, "Error while processing {Event} for file {FilePath}", e.ChangeType, e.FullPath);
        }
        finally
        {
            MoveHandledFile(e.FullPath, isDone);
            _semaphore.Release();
        }
    }

    protected override void OnExistingFile(object sender, FileSystemEventArgs e)
    {
        bool? isDone = null;
        try
        {
            _semaphore.Wait();
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                case WatcherChangeTypes.Deleted:
                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.Renamed:
                    break;
                case WatcherChangeTypes.All:
                    _mediator.Publish(new OutputFileOrchestrator.OutputFileCreated(e.FullPath)).ConfigureAwait(false).GetAwaiter()
                        .GetResult();
                    isDone = true;
                    _logger.LogInformation("Successfully processed {Event} for file {FilePath}", e.ChangeType, e.FullPath);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (Exception ex)
        {
            isDone = false;
            _logger.LogError(ex, "Error while processing {Event} for file {FilePath}", e.ChangeType, e.FullPath);
        }
        finally
        {
            MoveHandledFile(e.FullPath, isDone);
            _semaphore.Release();
        }
    }

    private void MoveHandledFile(string filePath, bool? isDone)
    {
        switch (isDone)
        {
            case true:
                {
                    // move file to done directory
                    var destinationFilePath = Path.Combine(Path.GetDirectoryName(filePath)?? string.Empty, "done");
                    filePath.MoveFileToDirectory(destinationFilePath);
                    // also move the file with the same name but .zip in the input directory to done directory
                    var zipFilePath = Path.ChangeExtension(filePath, ".zip");
                    if (File.Exists(zipFilePath))
                    {
                        var zipDestinationFilePath = Path.Combine(Path.GetDirectoryName(zipFilePath)?? string.Empty, "done");
                        zipFilePath.MoveFileToDirectory(zipDestinationFilePath);
                    }

                    break;
                }
            case false:
                {
                    // move file to error directory
                    var destinationFilePath = Path.Combine(Path.GetDirectoryName(filePath)?? string.Empty, "error");
                    filePath.MoveFileToDirectory(destinationFilePath);
                    // also move the file with the same name but .zip in the input directory to error directory
                    var zipFilePath = Path.ChangeExtension(filePath, ".zip");
                    if (File.Exists(zipFilePath))
                    {
                        var zipDestinationFilePath = Path.Combine(Path.GetDirectoryName(zipFilePath)?? string.Empty, "error");
                        zipFilePath.MoveFileToDirectory(zipDestinationFilePath);
                    }

                    break;
                }
            case null:
                {
                    // do nothing, file is not handled
                    break;
                }
        }
    }
}
