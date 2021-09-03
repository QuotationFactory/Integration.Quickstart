using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using MediatR;
using MetalHeaven.Agent.Shared.External.Interfaces;
using MetalHeaven.Agent.Shared.External.Messages;
using MetalHeaven.Integration.Shared.Classes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Versioned.ExternalDataContracts;
using Versioned.ExternalDataContracts.Contracts.AddressBook;
using Versioned.ExternalDataContracts.Contracts.Article;
using Versioned.ExternalDataContracts.Contracts.Project;

namespace Rhodium24.Host.Features.AgentOutputFile
{
    public class AgentOutputFileCreatedHandler : INotificationHandler<AgentOutputFileCreated>
    {
        private readonly AgentSettings _options;
        private readonly IAgentMessageSerializationHelper _agentMessageSerializationHelper;
        private readonly ILogger<AgentOutputFileCreatedHandler> _logger;

        public AgentOutputFileCreatedHandler(IOptions<AgentSettings> options, IAgentMessageSerializationHelper agentMessageSerializationHelper, ILogger<AgentOutputFileCreatedHandler> logger)
        {
            _options = options.Value;
            _agentMessageSerializationHelper = agentMessageSerializationHelper;
            _logger = logger;
        }

        public async Task Handle(AgentOutputFileCreated notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("File created: {filePath}", notification.FilePath);

            // default file creation timeout
            await Task.Delay(500, cancellationToken);

            // define file paths
            var jsonFilePath = notification.FilePath;
            var zipFilePath = Path.ChangeExtension(notification.FilePath, ".zip");

            // check if json & zip file exists it means that a project has been exported
            if (File.Exists(jsonFilePath) && File.Exists(zipFilePath))
            {
                await HandleProjectFiles(jsonFilePath, zipFilePath);
                return;
            }

            await HandleAgentMessage(jsonFilePath);
        }

        private async Task HandleProjectFiles(string jsonFilePath, string zipFilePath)
        {
            try
            {
                // read zip content
                var zipContent = await ReadProjectZipFileAsync(zipFilePath);

                // log zip content
                foreach (var (fileName, fileBytes) in zipContent)
                    _logger.LogInformation($"Found file '{fileName}' in zipfile, size {fileBytes.Length}.");

                // define json serializer settings
                var settings = new JsonSerializerSettings();
                settings.SetJsonSettings();
                settings.AddJsonConverters();
                settings.SerializationBinder = new CrossPlatformTypeBinder();

                // read all text from file that is created
                var json = await File.ReadAllTextAsync(jsonFilePath);

                // convert json to project object
                var project = JsonConvert.DeserializeObject<ProjectV1>(json, settings);

                _logger.LogInformation("Project deserialized succesfully, project id: {id}", project.Id);

                // optional response if your using this to export to ERP.
                var response = new ExportToErpResponse
                {
                    Source = "Integration name",
                    Succeed = true,
                    ExternalUrl = "", //Optional url to open the imported entity from Rhodium24
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
                    AssemblyImportResults = new List<ExportToErpAssemblyResponse>
                    {
                        new()
                        {
                            Succeed = false,
                            AssemblyId = Guid.Empty, // specific assembly id
                            ExternalUrl = "", // Optional url to open the imported entity from Rhodium24
                            EventLogs = new List<EventLog>
                            {
                                new()
                                {
                                    DateTime = DateTime.UtcNow,
                                    Level = EventLogLevel.Information,
                                    Message = "This is some random information",
                                    ProjectId = project.Id,
                                    AssemblyId = Guid.Empty // specific assembly id
                                }
                            },
                        }
                    },
                    PartTypeResults = new List<ExportToErpPartTypeResponse>
                    {
                        new()
                        {
                            Succeed = false,
                            PartTypeId = Guid.Empty, // specific part type id
                            ExternalUrl = "", // Optional url to open the imported entity from Rhodium24
                            EventLogs = new List<EventLog>
                            {
                                new()
                                {
                                    DateTime = DateTime.UtcNow,
                                    Level = EventLogLevel.Information,
                                    Message = "This is some random information",
                                    ProjectId = project.Id,
                                    PartTypeId = Guid.Empty // specific part type id
                                }
                            },
                        }
                    }
                };

                var responseJson = _agentMessageSerializationHelper.ToJson(response);

                // get temp file path
                var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

                // save json to temp file
                await File.WriteAllTextAsync(tempFile, responseJson);

                // move file to agent input directory
                _options.MoveFileToAgentInput(tempFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while handling project files");
            }
        }

        private static async Task<Dictionary<string, byte[]>> ReadProjectZipFileAsync(string zipFilePath)
        {
            await using var fileStream = File.OpenRead(zipFilePath);
            using var zipFile = new ZipFile(fileStream);

            var zipFiles = new Dictionary<string, byte[]>();

            foreach (ZipEntry zipEntry in zipFile)
            {
                // ignore directories
                if (!zipEntry.IsFile) continue;

                var fileName = zipEntry.Name.Contains("/") ? zipEntry.Name.Substring(zipEntry.Name.LastIndexOf('/') + 1) : zipEntry.Name;

                if (zipFiles.ContainsKey(fileName))
                    continue;

                var zipStream = zipFile.GetInputStream(zipEntry);
                await using var ms = new MemoryStream();
                await zipStream.CopyToAsync(ms);
                zipFiles.Add(fileName, ms.ToArray());
            }

            return zipFiles;
        }

        private async Task HandleAgentMessage(string jsonFilePath)
        {
            try
            {
                // read json from file
                var fileContent = await File.ReadAllTextAsync(jsonFilePath);

                _logger.LogInformation("Start file process as agent message");

                // convert json to agent message
                var agentMessage = _agentMessageSerializationHelper.FromJson(fileContent);

                IAgentMessage agentMessageResponse;

                // process agent message
                switch (agentMessage)
                {
                    case RequestAddressBookSyncMessage addressBookSync:
                        agentMessageResponse = new RequestAddressBookSyncMessageResponse
                        {
                            Relations = new AgentRelationImportRequest[]
                            {
                                new()
                                {
                                    Id = 1,
                                    Code = "MH24",
                                    // etc.
                                }
                            },
                            EventLogs = new List<EventLog>
                            {
                                new()
                                {
                                    DateTime = DateTime.UtcNow,
                                    Level = EventLogLevel.Information,
                                    Message = "This is some random information"
                                }
                            }
                        };
                        break;
                    case RequestArticlesSyncMessage articlesSync:
                        agentMessageResponse = new RequestArticlesSyncMessageResponse
                        {
                            Articles = new AgentArticleImportRequest[]
                            {
                                new()
                                {
                                    Id = 1,
                                    Code = "ITEM24",
                                    // etc.
                                }
                            },
                            EventLogs = new List<EventLog>
                            {
                                new()
                                {
                                    DateTime = DateTime.UtcNow,
                                    Level = EventLogLevel.Information,
                                    Message = "This is some random information"
                                }
                            }
                        };
                        break;
                    case RequestManufacturabilityCheckOfPartTypeMessage manufacturabilityCheck:
                        agentMessageResponse = new RequestManufacturabilityCheckOfPartTypeMessageResponse
                        {
                            ProjectId = manufacturabilityCheck.ProjectId,
                            PartTypeId = manufacturabilityCheck.PartType.Id,
                            IsManufacturable = true,
                            WorkingStepKey = manufacturabilityCheck.WorkingStepKey,
                            EventLogs = new List<EventLog>
                            {
                                new()
                                {
                                    DateTime = DateTime.UtcNow,
                                    Level = EventLogLevel.Information,
                                    Message = "This is some random information",
                                    ProjectId = manufacturabilityCheck.ProjectId,
                                    PartTypeId = manufacturabilityCheck.PartType.Id
                                }
                            }
                        };
                        break;
                    case RequestProductionTimeEstimationOfPartTypeMessage productionTimeEstimation:
                        agentMessageResponse = new RequestProductionTimeEstimationOfPartTypeMessageResponse
                        {
                            ProjectId = productionTimeEstimation.ProjectId,
                            PartTypeId = productionTimeEstimation.PartType.Id,
                            WorkingStepKey = productionTimeEstimation.WorkingStepKey,
                            EstimatedProductionTimeMs = (long)TimeSpan.FromMinutes(12).TotalMilliseconds,
                            EventLogs = new List<EventLog>
                            {
                                new()
                                {
                                    DateTime = DateTime.UtcNow,
                                    Level = EventLogLevel.Information,
                                    Message = "This is some random information",
                                    ProjectId = productionTimeEstimation.ProjectId,
                                    PartTypeId = productionTimeEstimation.PartType.Id
                                }
                            }
                        };
                        break;
                    case RequestAdditionalCostsOfPartTypeMessage additionalCosts:
                        agentMessageResponse = new RequestAdditionalCostsOfPartTypeMessageResponse
                        {
                            ProjectId = additionalCosts.ProjectId,
                            PartTypeId = additionalCosts.PartType.Id,
                            WorkingStepKey = additionalCosts.WorkingStepKey,
                            AdditionalCosts = 12.50m,
                            EventLogs = new List<EventLog>
                            {
                                new()
                                {
                                    DateTime = DateTime.UtcNow,
                                    Level = EventLogLevel.Information,
                                    Message = "This is some random information",
                                    ProjectId = additionalCosts.ProjectId,
                                    PartTypeId = additionalCosts.PartType.Id
                                }
                            }
                        };
                        break;
                    default:
                        throw new Exception($"Cannot process agent message {agentMessage.MessageType}");
                }

                // convert agent response message to json
                var json = _agentMessageSerializationHelper.ToJson(agentMessageResponse);

                // get temp file path
                var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

                // save json to temp file
                await File.WriteAllTextAsync(tempFile, json);

                // move file to agent input directory
                _options.MoveFileToAgentInput(tempFile);

                _logger.LogInformation("Agent message file successfully processed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while handling agent message");
            }
        }
    }
}
