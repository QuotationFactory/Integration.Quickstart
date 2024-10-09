using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Host.Configuration;
using Integration.Host.Features.OutputFile;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Identity;
using Microsoft.Graph;

namespace Integration.Host.Features.Graph
{
    public class GraphConnector
    {
        private readonly IntegrationSettings _options;
        private readonly ILogger<OutputFileCreatedHandler> _logger;
        private static GraphServiceClient _graphServiceClient = new GraphServiceClient(new DefaultAzureCredential());
        private static string _driveId;
        private static string _driveWebUrl;
        private static string _targetDirectory;

        public GraphConnector(IOptions<IntegrationSettings> options, ILogger<OutputFileCreatedHandler> logger)
        {
            _options = options.Value;
            _logger = logger;
            var scopes = new[] { "https://graph.microsoft.com/.default" };
            var clientOptions = new ClientSecretCredentialOptions { AuthorityHost = AzureAuthorityHosts.AzurePublicCloud };
            var clientSecretCredential = new ClientSecretCredential(_options.TenantId, _options.ClientId, _options.ClientSecret, clientOptions);

            _graphServiceClient = new GraphServiceClient(clientSecretCredential, scopes);
            _driveId = _options.DriveId;
            _targetDirectory = _options.TargetDirectory;

            var drive = _graphServiceClient.Drives[_driveId].GetAsync().GetAwaiter().GetResult();
            _driveWebUrl = drive?.WebUrl;
            _logger.LogInformation("GraphClient.Drive.WebUrl: {1}", _driveWebUrl);
        }

        public async Task UploadFileSharePointOnline(string filePath)
        {
            var targetFilePath = _targetDirectory + "/" + Path.GetFileName(filePath);
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
