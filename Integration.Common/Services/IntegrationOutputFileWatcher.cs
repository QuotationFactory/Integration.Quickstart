using Integration.Common.Classes;
using Integration.Common.Extensions;
using Integration.Common.Notifications;
using MediatR;
using Microsoft.Extensions.Options;

namespace Integration.Common.Services
{
    /// <summary>
    /// Background service that watches the integration output directory for new created files
    /// and publish new IntegrationOutputFileCreated notification when new files are created
    /// </summary>
    public class IntegrationOutputFileWatcher : FileWatcherService
    {
        private readonly IMediator _mediator;

        public IntegrationOutputFileWatcher(IMediator mediator, BaseIntegration integration, IOptions<AgentSettings> options)
        {
            _mediator = mediator;
            var integrationOutputDirectory = options.Value.GetOrCreateAgentOutputDirectory(integration.Name, true);

            AddFileWatcher(integrationOutputDirectory, "*.*");
        }

        protected override void OnAllChanges(object sender, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    _mediator.Publish(new IntegrationOutputFileCreated(e.FullPath)).ConfigureAwait(false).GetAwaiter().GetResult();
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
    }
}
