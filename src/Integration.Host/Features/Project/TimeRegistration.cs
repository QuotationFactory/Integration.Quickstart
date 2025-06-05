using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Integration.Common.Serialization;
using Integration.Host.Configuration;
using MediatR;
using MetalHeaven.Agent.Shared.External.Interfaces;
using MetalHeaven.Agent.Shared.External.TimeRegistration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Versioned.ExternalDataContracts;
using Versioned.ExternalDataContracts.Contracts.Project;

namespace Integration.Host.Features.Project;

public static class TimeRegistration
{
    public class ProjectFileCreatedReturnTimeRegistrationExport(string filePath) : INotification
    {
        public string FilePath { get; } = filePath;
    }

    // ReSharper disable once UnusedType.Global
    public class NotificationHandler : INotificationHandler<ProjectFileCreatedReturnTimeRegistrationExport>
    {
        private readonly ILogger<NotificationHandler> _logger;
        private readonly IntegrationSettings _integrationSettings;
        private readonly IAgentMessageSerializationHelper _agentMessageSerializationHelper;

        public NotificationHandler(
            ILogger<NotificationHandler> logger,
            IAgentMessageSerializationHelper agentMessageSerializationHelper,
            IOptions<IntegrationSettings> options)
        {
            _logger = logger;
            _agentMessageSerializationHelper = agentMessageSerializationHelper;
            _integrationSettings = options.Value;
        }

        public async Task Handle(ProjectFileCreatedReturnTimeRegistrationExport notification, CancellationToken cancellationToken)
        {
            // define file paths
            var jsonFilePath = notification.FilePath;
            var zipFilePath = Path.ChangeExtension(notification.FilePath, ".zip");
            try
            {
                // read zip content
                var zipContent = await ProjectZipFileHelper.ReadProjectZipFileAsync(zipFilePath);

                // log zip content
                foreach (var (fileName, fileBytes) in zipContent)
                    _logger.LogInformation($"Found file '{fileName}' in zipfile, size {fileBytes.Length}.");

                // define json serializer settings
                var settings = new JsonSerializerSettings();
                settings.SetJsonSettings();
                settings.AddJsonConverters();
                settings.SerializationBinder = new CrossPlatformTypeBinder();

                // read all text from file that is created
                var json = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);

                // convert json to project object
                var project = JsonConvert.DeserializeObject<ProjectV1>(json, settings);

                _logger.LogInformation("Project deserialized successfully, project id: {id}", project.Id);

                // optional response if your using this to export to ERP.
                // this is an example to simulate the TimeRegistrationExport with Random productionTimeInSeconds
                var response = new AgentTimeRegistrationExport
                {
                    Records = project.BoM.PartList.SelectMany(partType => partType.Activities.Where(z => z.Resource?.ResourceId is not null)
                        .SelectMany(activity =>
                        {
                            var simulatedTimeInSeconds = Random.Shared.Next(30, 180);
                            var measuredProductionTimeInSeconds = Random.Shared.Next(60, 360);

                            return new List<AgentTimeRegistrationExportRecord>
                            {
                                new(partType.Id, activity.WorkingStepType, measuredProductionTimeInSeconds,
                                    partType.Financial.TotalProjectQuantity, AgentTimeRegistrationSource.Production, project.Id,
                                    activity.Resource?.ResourceId),
                                new(partType.Id, activity.WorkingStepType, simulatedTimeInSeconds, partType.Financial.TotalProjectQuantity,
                                    AgentTimeRegistrationSource.CAM, project.Id, activity.Resource?.ResourceId),
                            };
                        })).ToList()
                };

                var responseJson = _agentMessageSerializationHelper.ToJson(response);

                // get temp file path
                var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

                // save json to temp file
                await File.WriteAllTextAsync(tempFile, responseJson, cancellationToken);

                // move file to input directory
                _integrationSettings.MoveFileToInput(tempFile);

                _logger.LogInformation("'{Count}' Generated random time registration export", response.Records.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while handling project files");
            }
        }
    }
}
