using System;
using System.IO;
using MediatR;
using MetalHeaven.Integration.Shared.Classes;
using MetalHeaven.Integration.Shared.Extensions;
using MetalHeaven.Integration.Shared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Rhodium24.Host.Features.AgentOutputFile
{
    /// <summary>
    /// Service that watches on the output directory of the agent for *.json files
    /// Publishes an AgentOutputFileCreated notification if a file is created
    /// </summary>
    public class AgentOutputFileWatcherService : FileWatcherService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AgentOutputFileWatcherService> _logger;

        public AgentOutputFileWatcherService(IMediator mediator, IOptions<GraphAgentSettings> options, ILogger<AgentOutputFileWatcherService> logger) : base(options)
        {
            _mediator = mediator;
            _logger = logger;

            // add file watcher to the agent output directory
            AddFileWatcher(options.Value.GetOutputDirectory(), "*.json");
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
}
