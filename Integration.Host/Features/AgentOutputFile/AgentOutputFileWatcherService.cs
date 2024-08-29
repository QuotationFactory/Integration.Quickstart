using System;
using System.IO;
using Integration.Common.Classes;
using Integration.Common.Extensions;
using Integration.Common.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integration.Host.Features.AgentOutputFile;

/// <summary>
/// Service that watches on the output directory of the agent for *.json files
/// Publishes an AgentOutputFileCreated notification if a file is created
/// </summary>
public class AgentOutputFileWatcherService : FileWatcherService
{
    private readonly IMediator _mediator;
    private readonly ILogger<AgentOutputFileWatcherService> _logger;

    public AgentOutputFileWatcherService(IMediator mediator, IOptions<IntegrationSettings> options, ILogger<AgentOutputFileWatcherService> logger)
    {
        _mediator = mediator;
        _logger = logger;

        // add file watcher to the agent output directory
        AddFileWatcher(options.Value.GetOrCreateAgentOutputDirectory(createIfNotExists: true), "*.json");
    }

    protected override void OnAllChanges(object sender, FileSystemEventArgs e)
    {
        try
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    _mediator.Publish(new AgentOutputFileCreated(e.FullPath)).ConfigureAwait(false).GetAwaiter().GetResult();
                    break;
                case WatcherChangeTypes.Deleted:
                    break;
                case WatcherChangeTypes.Changed:
                    break;
                case WatcherChangeTypes.Renamed:
                    break;
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
}
