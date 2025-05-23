﻿using System;
using System.IO;
using Integration.Common.FileWatcher;
using Integration.Host.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integration.Host.Features.OutputFile;

/// <summary>
/// Service that watches on the output directory of the edge connector for *.json files
/// Publishes an OutputFileCreated notification if a file is created
/// </summary>
public class OutputFileWatcherService : FileWatcherService
{
    private readonly IMediator _mediator;
    private readonly ILogger<OutputFileWatcherService> _logger;

    public OutputFileWatcherService(IMediator mediator, IOptions<IntegrationSettings> options, ILogger<OutputFileWatcherService> logger)
    {
        _mediator = mediator;
        _logger = logger;

        // add file watcher to the output directory
        AddFileWatcher(options.Value.GetOrCreateOutputDirectory(createIfNotExists: true), "*.json");
    }

    protected override void OnAllChanges(object sender, FileSystemEventArgs e)
    {
        try
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    _mediator.Publish(new OutputFileCreated(e.FullPath)).ConfigureAwait(false).GetAwaiter().GetResult();
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
            _logger.LogError(ex, "Error while processing {event} for file {filePath}", e.ChangeType, e.FullPath);
        }
    }

    protected override void OnExistingFile(object sender, FileSystemEventArgs e)
    {
        try
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                case WatcherChangeTypes.Deleted:
                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.Renamed:
                    break;
                case WatcherChangeTypes.All:
                    _mediator.Publish(new OutputFileCreated(e.FullPath)).ConfigureAwait(false).GetAwaiter().GetResult();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing {event} for file {filePath}", e.ChangeType, e.FullPath);
        }
    }
}
