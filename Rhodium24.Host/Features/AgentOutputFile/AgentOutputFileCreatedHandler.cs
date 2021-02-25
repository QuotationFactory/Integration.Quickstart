using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Versioned.ExternalDataContracts;
using Versioned.ExternalDataContracts.Contracts.Project;

namespace Rhodium24.Host.Features.AgentOutputFile
{
    public class AgentOutputFileCreatedHandler : INotificationHandler<AgentOutputFileCreated>
    {
        private readonly ILogger<AgentOutputFileCreatedHandler> _logger;

        public AgentOutputFileCreatedHandler(ILogger<AgentOutputFileCreatedHandler> logger)
        {
            _logger = logger;
        }
        
        public async Task Handle(AgentOutputFileCreated notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("File created: {filePath}", notification.FilePath);
            
            // default file creation timeout
            await Task.Delay(500, cancellationToken);

            // define json serializer settings
            var settings = new JsonSerializerSettings();
            settings.SetJsonSettings();
            settings.AddJsonConverters();
            settings.SerializationBinder = new CrossPlatformTypeBinder();
            
            // read all text from file that is created
            var json = await File.ReadAllTextAsync(notification.FilePath, cancellationToken);
            
            // convert json to project object
            var project = JsonConvert.DeserializeObject<ProjectV1>(json, settings);

            _logger.LogInformation("Project deserialized succesfully, project id: {id}", project.Id);
        }
    }
}