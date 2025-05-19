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
using MetalHeaven.Agent.Shared.External.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Versioned.ExternalDataContracts;
using Versioned.ExternalDataContracts.Contracts.Project;

namespace Integration.Host.Features.Project;

public static class ProjectFiles
{
    public class ProjectFileCreated(string filePath) : INotification
    {
        public string FilePath { get; } = filePath;
    }
    // ReSharper disable once UnusedType.Global
    public class NotificationHandler : INotificationHandler<ProjectFileCreated>
    {
        private readonly ILogger<NotificationHandler> _logger;
        private readonly IAgentMessageSerializationHelper _agentMessageSerializationHelper;
        private readonly IntegrationSettings _integrationSettings;
        private static readonly Random s_random = new();

        public NotificationHandler(
            ILogger<NotificationHandler> logger,
            IAgentMessageSerializationHelper agentMessageSerializationHelper,
            IOptions<IntegrationSettings> options)
        {
            _logger = logger;
            _agentMessageSerializationHelper = agentMessageSerializationHelper;
            _integrationSettings = options.Value;
        }

        public async Task Handle(ProjectFileCreated notification, CancellationToken cancellationToken)
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
                {
                    _logger.LogInformation("Found file \'{FileName}\' in zipfile, size {FileBytesLength}", fileName, fileBytes.Length);
                }

                // define json serializer settings
                var settings = new JsonSerializerSettings();
                settings.SetJsonSettings();
                settings.AddJsonConverters();
                settings.SerializationBinder = new CrossPlatformTypeBinder();

                // read all text from file that is created
                var json = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);

                // convert json to project object
                var project = JsonConvert.DeserializeObject<ProjectV1>(json, settings);

                _logger.LogInformation("Project deserialized succesfully, project id: {id}", project.Id);

                // optional response if your using this to export to ERP.
                var response = new ExportToErpResponse
                {
                    Source = "Integration name",
                    Succeed = s_random.NextDouble() >= 0.5,
                    ExternalUrl = "https://www.google.nl", //Optional url to open the imported entity from Rhodium24
                    ProjectId = project.Id,
                    EventLogs = new List<EventLog>
                    {
                        new()
                        {
                            DateTime = DateTime.UtcNow,
                            Level = EventLogLevel.Information,
                            Message = "This is some random information",
                            ProjectId = project.Id,
                        }
                    },
                    AssemblyImportResults = project.BoM.Assemblies.Select(assembly => new ExportToErpAssemblyResponse
                    {
                        Succeed = s_random.NextDouble() >= 0.5,
                        AssemblyId = assembly.Id, // specific assembly id
                        ExternalUrl = "", // Optional url to open the imported entity from Rhodium24
                        EventLogs = new List<EventLog>
                        {
                            new()
                            {
                                DateTime = DateTime.UtcNow,
                                Level = EventLogLevel.Information,
                                Message = "This is some random information",
                                ProjectId = project.Id,
                                AssemblyId = assembly.Id
                            }
                        },
                    }),
                    PartTypeResults = project.BoM.PartList.Select(partType => new ExportToErpPartTypeResponse
                    {
                        Succeed = s_random.NextDouble() >= 0.5,
                        PartTypeId = partType.Id, // specific assembly id
                        ExternalUrl = "", // Optional url to open the imported entity from Rhodium24
                        EventLogs = new List<EventLog>
                        {
                            new()
                            {
                                DateTime = DateTime.UtcNow,
                                Level = EventLogLevel.Information,
                                Message = "This is some random information",
                                ProjectId = project.Id,
                                PartTypeId = partType.Id
                            }
                        },
                    })
                };

                var responseJson = _agentMessageSerializationHelper.ToJson(response);

                // get temp file path
                var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

                // save json to temp file
                await File.WriteAllTextAsync(tempFile, responseJson, cancellationToken);

                // move file to input directory
                _integrationSettings.MoveFileToInput(tempFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while handling project files");
            }

        }
    }
}

