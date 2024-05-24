using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using MetalHeaven.Agent.Shared.External.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Rhodium24.Host.Features.AgentOutputFile
{
    public class GraphConnector
    {
        private readonly GraphAgentSettings _options;
        private readonly ILogger<AgentOutputFileCreatedHandler> _logger;
        private static GraphServiceClient _graphServiceClient = new GraphServiceClient(new DefaultAzureCredential());
        private static string _driveId;
        private static string _driveWebUrl;
        private static string _targetFolder;

        public GraphConnector(IOptions<GraphAgentSettings> options, ILogger<AgentOutputFileCreatedHandler> logger)
        {
            _options = options.Value;
            _logger = logger;
            var scopes = new[] { "https://graph.microsoft.com/.default" };
            var clientOptions = new ClientSecretCredentialOptions{AuthorityHost = AzureAuthorityHosts.AzurePublicCloud};
            var clientSecretCredential = new ClientSecretCredential(_options.TenantId,_options.ClientId,_options.ClientSecret,clientOptions);

            _graphServiceClient = new GraphServiceClient(clientSecretCredential, scopes);
            _driveId = _options.DriveId;
            _targetFolder = _options.TargetFolder;

            var drive = _graphServiceClient.Drives[_driveId].GetAsync().GetAwaiter().GetResult();
            _driveWebUrl = drive?.WebUrl;
            _logger.LogInformation("GraphClient.Drive.WebUrl: {1}", _driveWebUrl);
        }

        public async Task UploadFileSharePointOnline(string filePath)
        {
            var targetFilePath = _targetFolder + "/" + Path.GetFileName(filePath);
            using (var stream = File.OpenRead(filePath))
            {
                var driveItemUpload = await _graphServiceClient
                    .Drives[_driveId]
                    .Root
                    .ItemWithPath(targetFilePath)
                    .Content
                    .PutAsync(stream);
            }
            _logger.LogInformation("Uploaded {1} to {2}", targetFilePath, _driveWebUrl);
        }
    }
}
