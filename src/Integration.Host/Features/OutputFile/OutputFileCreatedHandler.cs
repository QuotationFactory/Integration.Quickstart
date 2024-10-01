using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Integration.Common;
using Integration.Common.Serialization;
using Integration.Host.Configuration;
using MediatR;
using MetalHeaven.Agent.Shared.External.Interfaces;
using MetalHeaven.Agent.Shared.External.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Versioned.ExternalDataContracts;
using Versioned.ExternalDataContracts.Contracts.AddressBook;
using Versioned.ExternalDataContracts.Contracts.Article;
using Versioned.ExternalDataContracts.Contracts.Project;
using Versioned.ExternalDataContracts.Enums;

namespace Integration.Host.Features.OutputFile;

public class OutputFileCreatedHandler : INotificationHandler<OutputFileCreated>
{
    private readonly IntegrationSettings _options;
    private readonly IAgentMessageSerializationHelper _agentMessageSerializationHelper;
    private readonly ILogger<OutputFileCreatedHandler> _logger;
    private static Random _random = new Random();

    public OutputFileCreatedHandler(IOptions<IntegrationSettings> options,
        IAgentMessageSerializationHelper agentMessageSerializationHelper, ILogger<OutputFileCreatedHandler> logger)
    {
        _options = options.Value;
        _agentMessageSerializationHelper = agentMessageSerializationHelper;
        _logger = logger;
    }

    public async Task Handle(OutputFileCreated notification, CancellationToken cancellationToken)
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

        await HandleMessage(jsonFilePath);
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
                Succeed = _random.NextDouble() >= 0.5,
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
                    Succeed = _random.NextDouble() >= 0.5,
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
                    Succeed = _random.NextDouble() >= 0.5,
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
            await File.WriteAllTextAsync(tempFile, responseJson);

            // move file to input directory
            _options.MoveFileToInput(tempFile);
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

    private async Task HandleMessage(string jsonFilePath)
    {
        try
        {
            // read json from file
            var fileContent = await File.ReadAllTextAsync(jsonFilePath);

            // convert json to message
            var message = _agentMessageSerializationHelper.FromJson(fileContent);

            _logger.LogInformation("Processing message type: '{Type}'", message.MessageType);

            IAgentMessage messageResponse;

            // process message
            switch (message)
            {
                //process addressBookSyncRequest
                case RequestAddressBookSyncMessage addressBookSyncRequest:

                    // implement business logic here

                    // create addressBookSyncRequestResponse message
                    messageResponse = new RequestAddressBookSyncMessageResponse
                    {
                        Relations = new AgentRelationImportRequest[]
                        {
                            new()
                            {
                                Id = 1,
                                Code = "Debtor Code",
                                CompanyName = "Quotation Factory B.V.",
                                Email = "info@quotationfactory.com",
                                Phone = "+31(0)850047332",
                                Website = "https://www.quotationfactory.com",
                                PostalStreet = "Aalsterweg",
                                PostalHouseNumber = "262",
                                //PostalHouseNumberAddition = "",
                                PostalCity = "Eindhoven",
                                PostalZipCode = "5644RK",
                                PostalStateOrProvince = "Noord-Brabant",
                                PostalCountryCode = "NL",
                                PostalCountryName = "Netherlands",
                                LanguageCode = "",
                                SegmentName = "A",
                                Tags = Array.Empty<string>(),
                                VatNumber = "",
                                // This is the VAT rate in percentage from 0 to 100
                                VatRatio = 21.0
                                // CoCNumber = "",
                                // CoCCountryCode = "NL",
                                // CoCCountryName = "Netherlands"
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

                //process addressBookSyncRequest
                case RequestArticlesSyncMessage articlesSyncRequest:
                    // implement business logic here

                    // create ArticleSyncRequestResponse message

                    messageResponse = new RequestArticlesSyncMessageResponse
                    {
                        Articles = new AgentArticleImportRequest[]
                        {
                            new()
                            {
                                Id = 1,
                                Code = "ITEM1",
                                Description = "Item with single price",
                                Price = 123.45m,
                                CurrencyIsoCode = "EUR",
                                Quantity = 1,
                                /// <summary>
                                /// Codes used in the integrations can be found in Recommendation No. 20: Codes for Units of measure used in international trade van de UNECE.
                                /// https://unece.org/trade/uncefact/cl-recommendations
                                /// https://unece.org/sites/default/files/2023-10/rec20_Rev7e_2010.zip
                                /// see: rec20_Rev7e_2010.xls
                                /// see: rec20_Rev7e_2010.pdf
                                /// </summary>
                                UnitIsoCode = "C62", // one piece
                                HideInPortal = false // null / true / false
                            },
                            new()
                            {
                                Id = 2,
                                Code = "ITEM2",
                                Description = "Item with scaled price",
                                // Price = 123.45m,
                                CurrencyIsoCode = "EUR",
                                Quantity = 1,
                                UnitIsoCode = "C62", // one piece
                                HideInPortal = false, // null / true / false
                                /// <summary>
                                /// Codes used in the integrations can be found in Recommendation No. 20: Codes for Units of measure used in international trade van de UNECE.
                                /// https://unece.org/trade/uncefact/cl-recommendations
                                /// https://unece.org/sites/default/files/2023-10/rec20_Rev7e_2010.zip
                                /// see: rec20_Rev7e_2010.xls
                                /// see: rec20_Rev7e_2010.pdf
                                /// </summary>
                                ScalePriceUnitIsoCode = "C62", // one piece
                                ScalePrices =
                                {
                                    new ScalePrice
                                    {
                                        Price = 200,
                                        Quantity = 0
                                    },
                                    new ScalePrice
                                    {
                                        Price = 100,
                                        Quantity = 50
                                    }
                                }
                            },
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
                    {
                        messageResponse = new RequestManufacturabilityCheckOfPartTypeMessageResponse
                        {
                            ProjectId = manufacturabilityCheck.ProjectId,
                            PartTypeId = manufacturabilityCheck.PartType.Id,
                            IsManufacturable = _random.NextDouble() >= 0.5,
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
                            },
                            Documents = new[]
                            {
                                new ResponseDocument()
                                {
                                    DocumentName = "test3 - Copy (2).CBBatchResult",
                                    DocumentContent =
                                        "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iSVNPLTg4NTktMSIgPz4NCjxGSUxFUyBTb3VyY2U9IkNBRE1BTi1CIiBWZXJzaW9uPSI4LjcuMi4wOTEiIERhdGU9IjIzLTA4LTIwMjEgMTM6MjQ6MzYiIEFwcD0iQ0FETUFOLUIiPg0KICAgIDxGSUxFPg0KICAgICAgICA8RmlsZU5hbWU+QzpcVGVtcFxtaFxpbnB1dFxzdGVwc1xiYXMtdGVzdC0zX3JvdzNfMXhfNDAwMTAwXzE3LnN0ZXA8L0ZpbGVOYW1lPg0KICAgICAgICA8TG9hZGluZ0ZpbGU+RG9uZTwvTG9hZGluZ0ZpbGU+DQogICAgICAgIDxUb29sc1Byb3ZpZGVkPk5vPC9Ub29sc1Byb3ZpZGVkPg0KICAgICAgICA8TWF0ZXJpYWxQcm92aWRlZD5ZZXM8L01hdGVyaWFsUHJvdmlkZWQ+DQogICAgICAgIDxUaGlja25lc3NQcm92aWRlZD5ZZXM8L1RoaWNrbmVzc1Byb3ZpZGVkPg0KICAgICAgICA8UEFSVElORk8+DQogICAgICAgICAgICA8REJQYXJ0TmFtZT5iYXMtdGVzdC0zX3JvdzNfMXhfNDAwMTAwXzE3PC9EQlBhcnROYW1lPg0KICAgICAgICAgICAgPERCUGFydFJldj4xPC9EQlBhcnRSZXY+DQogICAgICAgICAgICA8REJQYXJ0U3ViUmV2PjA8L0RCUGFydFN1YlJldj4NCiAgICAgICAgICAgIDxUaGlja25lc3M+ODwvVGhpY2tuZXNzPg0KICAgICAgICAgICAgPE1hdGVyaWFsPlMgMjM1PC9NYXRlcmlhbD4NCiAgICAgICAgICAgIDxRdWFudGl0eT4wPC9RdWFudGl0eT4NCiAgICAgICAgICAgIDxTdG9jaz4wPC9TdG9jaz4NCiAgICAgICAgICAgIDxDdXN0b21lcj5SaG9kaXVtMjQgQ0FETUFOLUIgQWdlbnQ8L0N1c3RvbWVyPg0KICAgICAgICAgICAgPE9yZGVyPjVjNzBkNjQ5LWEzZTMtNDA5Yy1hMTA5LWZjNGM0NWRlMWY4MTwvT3JkZXI+DQogICAgICAgICAgICA8RGVsaXZlcj48L0RlbGl2ZXI+DQogICAgICAgICAgICA8RG9zc2llcj48L0Rvc3NpZXI+DQogICAgICAgICAgICA8RGVzaWduTnVtYmVyPjwvRGVzaWduTnVtYmVyPg0KICAgICAgICAgICAgPEluZGV4PjwvSW5kZXg+DQogICAgICAgICAgICA8UmVmZXJlbmNlPmI4ZmExMzY4LTI1Y2QtNDUzMy05MWRhLTI3NWMxMjI1MDczZDwvUmVmZXJlbmNlPg0KICAgICAgICAgICAgPEJ1bGxldGluPjwvQnVsbGV0aW4+DQogICAgICAgICAgICA8U3ltbWV0cnk+PC9TeW1tZXRyeT4NCiAgICAgICAgICAgIDxSb3RhdGlvbj48L1JvdGF0aW9uPg0KICAgICAgICA8L1BBUlRJTkZPPg0KICAgICAgICA8R2VvbWV0cnk+DQogICAgICAgICAgICA8TGluZXM+MTI8L0xpbmVzPg0KICAgICAgICAgICAgPEFyY3NDaXJjbGVzPjU8L0FyY3NDaXJjbGVzPg0KICAgICAgICAgICAgPEZhY2VzPjI8L0ZhY2VzPg0KICAgICAgICAgICAgPE1pbkJlbmRMZW5ndGg+NzE3LjUwMTkwOTQ0ODgxOTwvTWluQmVuZExlbmd0aD4NCiAgICAgICAgICAgIDxNYXhCZW5kTGVuZ3RoPjcxNy41MDE5MDk0NDg4MTk8L01heEJlbmRMZW5ndGg+DQogICAgICAgICAgICA8TWluTWF0ZXJpYWxMZW5ndGg+NzE3LjUwMTkwOTQ0ODgxOTwvTWluTWF0ZXJpYWxMZW5ndGg+DQogICAgICAgICAgICA8TWF4TWF0ZXJpYWxMZW5ndGg+NzE3LjUwMTkwOTQ0ODgxOTwvTWF4TWF0ZXJpYWxMZW5ndGg+DQogICAgICAgIDwvR2VvbWV0cnk+DQogICAgICAgIDxBZGREb2N1bWVudGF0aW9uPkRvbmU8L0FkZERvY3VtZW50YXRpb24+DQogICAgICAgIDxUb29sc0ZvdW5kPkRvbmU8L1Rvb2xzRm91bmQ+DQogICAgICAgIDxDQU1MSVNUPg0KICAgICAgICAgICAgPENBTSBJRD0iNkE3MkQ0NUYtMzVFMi00MkM3LTg2QUUtNDk0M0VBRUZBOEE5IiBOYW1lPSJiYXMtdGVzdC0zX3JvdzNfMXhfNDAwMTAwXzE3Ij4NCiAgICAgICAgICAgICAgICA8Q0FNUGFydD5Eb25lPC9DQU1QYXJ0Pg0KICAgICAgICAgICAgICAgIDxDb250YWluc0hvbGVzVG9vTmVhclRvQmVuZD5ObzwvQ29udGFpbnNIb2xlc1Rvb05lYXJUb0JlbmQ+DQogICAgICAgICAgICAgICAgPEZpbGVUT0w+Tm90IGRvbmU8L0ZpbGVUT0w+DQogICAgICAgICAgICAgICAgPENvbnRvdXJEWEZJbkRCPkRvbmU8L0NvbnRvdXJEWEZJbkRCPg0KICAgICAgICAgICAgICAgIDxGaWxlQ29udG91ckRYRj5Eb25lPC9GaWxlQ29udG91ckRYRj4NCiAgICAgICAgICAgICAgICA8RmlsZUNvbnRvdXJEWEZGaWxlTmFtZT5DOlxVc2Vyc1xCYXNcU291cmNlXG1ldGFsLWhlYXZlblxjYWRtYW4tYiBpbnRlZ3JhdGllXHRlc3RcYmFzLXRlc3QtNFxiYXMtdGVzdC0zX3JvdzNfMXhfNDAwMTAwXzE3LmR4ZjwvRmlsZUNvbnRvdXJEWEZGaWxlTmFtZT4NCiAgICAgICAgICAgICAgICA8U2VuZFRvTGFudGVrPk5vdCBkb25lPC9TZW5kVG9MYW50ZWs+DQogICAgICAgICAgICAgICAgPE1BQ0hJTkVTPg0KICAgICAgICAgICAgICAgICAgICA8TUFDSElORSBKVElEPSJCNzFBQkI5My0wMzQxLTQ5ODEtODc5Ri0wM0Q1NjhFRDIyODAiIE1hY2hpbmVJRD0iMzc4ODkiPg0KICAgICAgICAgICAgICAgICAgICAgICAgPENPTExJU0lPTklORk8gLz4NCiAgICAgICAgICAgICAgICAgICAgICAgIDxOdW1iZXJPZlNvbHV0aW9ucz4xPC9OdW1iZXJPZlNvbHV0aW9ucz4NCiAgICAgICAgICAgICAgICAgICAgICAgIDxCZW5kaW5nU29sdXRpb24+RG9uZTwvQmVuZGluZ1NvbHV0aW9uPg0KICAgICAgICAgICAgICAgICAgICAgICAgPFRvb2xTdGF0aW9uaW5nPkRvbmU8L1Rvb2xTdGF0aW9uaW5nPg0KICAgICAgICAgICAgICAgICAgICAgICAgPFRvb2xTZWdtZW50YXRpb24+RmFpbGVkPC9Ub29sU2VnbWVudGF0aW9uPg0KICAgICAgICAgICAgICAgICAgICAgICAgPFVzZWRQdW5jaEhvbGRlcnMgLz4NCiAgICAgICAgICAgICAgICAgICAgICAgIDxVc2VkUHVuY2hlcz4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICA8UHVuY2g+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIDxJRD5SYWRpdXMgZ2VyZWVkc2NoYXAtUjEwPC9JRD4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPFJldj4xPC9SZXY+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIDxTdWJSZXY+MDwvU3ViUmV2Pg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8VHVybmVkPjA8L1R1cm5lZD4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICA8L1B1bmNoPg0KICAgICAgICAgICAgICAgICAgICAgICAgPC9Vc2VkUHVuY2hlcz4NCiAgICAgICAgICAgICAgICAgICAgICAgIDxVc2VkRGllcz4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICA8RGllPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8SUQ+VjYwXzc4XzkwX0cxNTQ1MzI4PC9JRD4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPFJldj4xPC9SZXY+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIDxTdWJSZXY+MDwvU3ViUmV2Pg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8VHVybmVkPjA8L1R1cm5lZD4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICA8L0RpZT4NCiAgICAgICAgICAgICAgICAgICAgICAgIDwvVXNlZERpZXM+DQogICAgICAgICAgICAgICAgICAgICAgICA8VXNlZERpZUhvbGRlcnMgLz4NCiAgICAgICAgICAgICAgICAgICAgICAgIDxCb3VuZGluZ0JveD4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICA8SGVpZ2h0PjU4Ny42NDM1MzE3NTgzMTg8L0hlaWdodD4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICA8V2lkdGg+NjQ4LjE2NDkyOTg3MjE8L1dpZHRoPg0KICAgICAgICAgICAgICAgICAgICAgICAgPC9Cb3VuZGluZ0JveD4NCiAgICAgICAgICAgICAgICAgICAgICAgIDxNYWNoaW5lRml0PkRvbmU8L01hY2hpbmVGaXQ+DQogICAgICAgICAgICAgICAgICAgICAgICA8VGVjaERCPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgIDxCZW5kIElEPSIyIj4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPEJlbmRBbmdsZURlZz4xMDcuMzk8L0JlbmRBbmdsZURlZz4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPEJlbmRMZW5ndGg+NzE3LjUwMTkwOTQ0ODYyNDwvQmVuZExlbmd0aD4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPFB1bmNoSG9sZGVyPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPElEPjwvSUQ+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8UmV2Pi0xPC9SZXY+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8U3ViUmV2Pi0xPC9TdWJSZXY+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8VHVybmVkPjA8L1R1cm5lZD4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPC9QdW5jaEhvbGRlcj4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPFB1bmNoPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPElEPlJhZGl1cyBnZXJlZWRzY2hhcC1SMTA8L0lEPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPFJldj4xPC9SZXY+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8U3ViUmV2PjA8L1N1YlJldj4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIDxUdXJuZWQ+MDwvVHVybmVkPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8L1B1bmNoPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8RGllPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPElEPlY2MF83OF85MF9HMTU0NTMyODwvSUQ+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8UmV2PjE8L1Jldj4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIDxTdWJSZXY+MDwvU3ViUmV2Pg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPFR1cm5lZD4wPC9UdXJuZWQ+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIDwvRGllPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8RGllSG9sZGVyPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPElEPjwvSUQ+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8UmV2Pi0xPC9SZXY+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8U3ViUmV2Pi0xPC9TdWJSZXY+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8VHVybmVkPjA8L1R1cm5lZD4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPC9EaWVIb2xkZXI+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIDxCQT4xMC4wNzgzNjk4NzYxODwvQkE+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIDxSaT4xMS40MjQ2NDU1NjAxMTQ8L1JpPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgIDwvQmVuZD4NCiAgICAgICAgICAgICAgICAgICAgICAgIDwvVGVjaERCPg0KICAgICAgICAgICAgICAgICAgICAgICAgPFZhbGlkTkM+Tm8NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICA8UkVBU09OIFRZUEU9IkVycm9yIj4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPERlc2NyaXB0aW9uPkludmFsaWQgTkM8L0Rlc2NyaXB0aW9uPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8U1VCUFJPQkxFTSBUWVBFPSJFcnJvciI+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8U291cmNlPlBCUHJvZ3JhbTwvU291cmNlPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPElEPkludmFsaWRTZXF1ZW5jZTwvSUQ+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8RGVmYXVsdD48L0RlZmF1bHQ+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8RGVzY3JpcHRpb24+T25nZWxkaWdlIHNlcXVlbnRpZSAoMSk8L0Rlc2NyaXB0aW9uPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPFNVQlBST0JMRU0gVFlQRT0iRXJyb3IiPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIDxTb3VyY2U+UEJQcm9ncmFtPC9Tb3VyY2U+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPElEPlZhbHVlTGVzc1RoYW5NaW48L0lEPg0KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIDxEZWZhdWx0PjwvRGVmYXVsdD4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8RGVzY3JpcHRpb24+WTogV2FhcmRlICg3OC45MikgJmx0OyBNaW4gKDgxLjAwKTwvRGVzY3JpcHRpb24+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8L1NVQlBST0JMRU0+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIDwvU1VCUFJPQkxFTT4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICA8L1JFQVNPTj4NCiAgICAgICAgICAgICAgICAgICAgICAgIDwvVmFsaWROQz4NCiAgICAgICAgICAgICAgICAgICAgICAgIDxWYWxpZEZvcmNlPlllcw0KICAgICAgICAgICAgICAgICAgICAgICAgICAgIDxGb3JjZT40NTYuNDQ0ODwvRm9yY2U+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgPEZvcmNlTGVuZ3RoPjYzNi4xNjwvRm9yY2VMZW5ndGg+DQogICAgICAgICAgICAgICAgICAgICAgICA8L1ZhbGlkRm9yY2U+DQogICAgICAgICAgICAgICAgICAgICAgICA8VHVybnM+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgPFRVUk4gSUQ9Ii0iPjE8L1RVUk4+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgPFRVUk4gSUQ9IlgiPjA8L1RVUk4+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgPFRVUk4gSUQ9IlkiPjA8L1RVUk4+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgPFRVUk4gSUQ9IloiPjA8L1RVUk4+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgPFRVUk4gSUQ9Ilk5MEwiPjA8L1RVUk4+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgPFRVUk4gSUQ9Ilk5MFIiPjA8L1RVUk4+DQogICAgICAgICAgICAgICAgICAgICAgICAgICAgPFRVUk4gSUQ9Ilk5MEwrWiI+MDwvVFVSTj4NCiAgICAgICAgICAgICAgICAgICAgICAgICAgICA8VFVSTiBJRD0iWTkwUitaIj4wPC9UVVJOPg0KICAgICAgICAgICAgICAgICAgICAgICAgPC9UdXJucz4NCiAgICAgICAgICAgICAgICAgICAgICAgIDxFc3RpbWF0ZWRQcmVTZXR1cFRpbWU+MjYuNTQ8L0VzdGltYXRlZFByZVNldHVwVGltZT4NCiAgICAgICAgICAgICAgICAgICAgICAgIDxFc3RpbWF0ZWRQcm9kdWN0aW9uVGltZT4zMDwvRXN0aW1hdGVkUHJvZHVjdGlvblRpbWU+DQogICAgICAgICAgICAgICAgICAgICAgICA8RXN0aW1hdGVkUG9zdFNldHVwVGltZT4yMC40NDwvRXN0aW1hdGVkUG9zdFNldHVwVGltZT4NCiAgICAgICAgICAgICAgICAgICAgICAgIDxDT0xMSVNJT04zRElORk8gLz4NCiAgICAgICAgICAgICAgICAgICAgICAgIDxDb2xsaXNpb25zM0Q+Tm88L0NvbGxpc2lvbnMzRD4NCiAgICAgICAgICAgICAgICAgICAgICAgIDxQREZFeHBvcnQ+RG9uZTwvUERGRXhwb3J0Pg0KICAgICAgICAgICAgICAgICAgICAgICAgPFByaW50UmVwb3J0Pk5vdCBkb25lPC9QcmludFJlcG9ydD4NCiAgICAgICAgICAgICAgICAgICAgICAgIDxJbmNsdWRlRGVmYXVsdFBERlJlcG9ydD5Ob3QgZG9uZTwvSW5jbHVkZURlZmF1bHRQREZSZXBvcnQ+DQogICAgICAgICAgICAgICAgICAgICAgICA8RXhwb3J0RlhNRmlsZT5Ob3QgZG9uZTwvRXhwb3J0RlhNRmlsZT4NCiAgICAgICAgICAgICAgICAgICAgICAgIDxBc3NvY2lhdGVSb2JvdFByb2dyYW0+Tm90IGRvbmU8L0Fzc29jaWF0ZVJvYm90UHJvZ3JhbT4NCiAgICAgICAgICAgICAgICAgICAgPC9NQUNISU5FPg0KICAgICAgICAgICAgICAgIDwvTUFDSElORVM+DQogICAgICAgICAgICA8L0NBTT4NCiAgICAgICAgPC9DQU1MSVNUPg0KICAgICAgICA8RXhwb3J0UFJKPkRvbmU8L0V4cG9ydFBSSj4NCiAgICAgICAgPEV4cG9ydE9TTT5Ob3QgZG9uZTwvRXhwb3J0T1NNPg0KICAgICAgICA8U2F2ZUluREI+Tm90IGRvbmU8L1NhdmVJbkRCPg0KICAgICAgICA8QkFUQ0hTVEFUSVNUSUNTPg0KICAgICAgICAgICAgPFN0YXJ0VGltZT4yMy8wOC8yMDIxIDEzOjI0OjM2PC9TdGFydFRpbWU+DQogICAgICAgICAgICA8RW5kVGltZT4yMy8wOC8yMDIxIDEzOjI0OjQ2PC9FbmRUaW1lPg0KICAgICAgICAgICAgPER1cmF0aW9uPjAwOjAwOjEwPC9EdXJhdGlvbj4NCiAgICAgICAgPC9CQVRDSFNUQVRJU1RJQ1M+DQogICAgPC9GSUxFPg0KPC9GSUxFUz4NCg=="
                                },
                                new ResponseDocument()
                                {
                                    DocumentName = "bas-test-3_row3_1x_400100_17.dxf",
                                    DocumentContent =
                                        "OTk5DQpDQURNQU4tQiBWOC43LjIuMDkxDQo5OTkNCkdSRg0KMA0KU0VDVElPTg0KMg0KSEVBREVSDQo5DQokRVhUTUlODQoxMA0KLTUwOC4zMzYzOTMNCjIwDQotNDMuMjYwNDMxDQo5DQokRVhUTUFYDQoxMA0KMTM5LjgyODUzNw0KMjANCjU0NC4zODMxMDENCjkNCiRMSU1NSU4NCjEwDQotNTQwLjc0NDYzOQ0KMjANCi03NS42Njg2NzcNCjkNCiRMSU1NQVgNCjEwDQoxNjkuMjEwNzE0DQoyMA0KNTczLjc2NTI3OA0KOQ0KJFJFR0VOTU9ERQ0KNzANCjENCjkNCiRMVFNDQUxFDQo0MA0KNjEuNzkwNDIzDQo5DQokQ0xBWUVSDQo4DQowDQo5DQokRElNU0NBTEUNCjQwDQoxLjANCjkNCiRDT09SRFMNCjcwDQoxDQowDQpFTkRTRUMNCjANClNFQ1RJT04NCjINClRBQkxFUw0KMA0KVEFCTEUNCjINCkxUWVBFDQo3MA0KMQ0KMA0KTFRZUEUNCjINCkNPTlRJTlVPVVMNCjcwDQo2NA0KMw0KU29saWQgbGluZQ0KNzINCjY1DQo3Mw0KMA0KNDANCjAuMA0KMA0KRU5EVEFCDQowDQpUQUJMRQ0KMg0KTEFZRVINCjcwDQoxDQowDQpMQVlFUg0KMg0KMA0KNzANCjANCjYyDQo3DQo2DQpDT05USU5VT1VTDQowDQpFTkRUQUINCjANCkVORFNFQw0KMA0KU0VDVElPTg0KMg0KRU5USVRJRVMNCjANCkxJTkUNCjgNCkdFT01FVFJZLUNVVFRJTkcNCjYNCkNPTlRJTlVPVVMNCjYyDQo3DQoxMA0KMC4wMDcxNTINCjIwDQowLjk5OTk3NA0KMTENCjEzOS44Mjg1MzcNCjIxDQotMC4wMDAwMDANCjANCkxJTkUNCjgNCkdFT01FVFJZLUNVVFRJTkcNCjYNCkNPTlRJTlVPVVMNCjYyDQo3DQoxMA0KMTM5LjgyODUzNw0KMjANCi0wLjAwMDAwMA0KMTENCi0yMTAuNTQ3Njk0DQoyMQ0KNTM2LjkwODU1MQ0KMA0KTElORQ0KOA0KR0VPTUVUUlktQ1VUVElORw0KNg0KQ09OVElOVU9VUw0KNjINCjcNCjEwDQotMjEwLjU0NzY5NA0KMjANCjUzNi45MDg1NTENCjExDQotNDgwLjM0MzIwMw0KMjENCjUzOC44MzgwNzYNCjANCkxJTkUNCjgNCkdFT01FVFJZLUNVVFRJTkcNCjYNCkNPTlRJTlVPVVMNCjYyDQo3DQoxMA0KLTMwLjI4MjU4Mg0KMjANCi00My4yNjA0MzENCjExDQotNTA4LjMzNjM5Mw0KMjENCjQ5MS43ODQ5MzYNCjANCkxJTkUNCjgNCkdFT01FVFJZLUNVVFRJTkcNCjYNCkNPTlRJTlVPVVMNCjYyDQo3DQoxMA0KLTQ4MS45MDMyNTgNCjIwDQo1MzcuNjI0MzE2DQoxMQ0KLTUwOC4zMzYzOTMNCjIxDQo0OTEuNzg0OTM2DQowDQpMSU5FDQo4DQpHRU9NRVRSWS1DVVRUSU5HDQo2DQpDT05USU5VT1VTDQo2Mg0KNw0KMTANCi0yLjQ4OTM4Nw0KMjANCjMuNzk0MTQwDQoxMQ0KMC4wMDcxNTINCjIxDQowLjk5OTk3NA0KMA0KTElORQ0KOA0KR0VPTUVUUlktQ1VUVElORw0KNg0KQ09OVElOVU9VUw0KNjINCjcNCjEwDQotMzAuMjgyNTgyDQoyMA0KLTQzLjI2MDQzMQ0KMTENCi0zLjg0OTQ0OA0KMjENCjIuNTc4OTUwDQowDQpMSU5FDQo4DQpHRU9NRVRSWS1DVVRUSU5HDQo2DQpDT05USU5VT1VTDQo2Mg0KNw0KMTANCi00NzQuMzM4NzIwDQoyMA0KNTQ0LjM4MzEwMQ0KMTENCi00ODEuOTAzMjU4DQoyMQ0KNTM3LjYyNDMxNg0KMA0KTElORQ0KOA0KR0VPTUVUUlktQ1VUVElORw0KNg0KQ09OVElOVU9VUw0KNjINCjcNCjEwDQotMi40ODkzODcNCjIwDQozLjc5NDE0MA0KMTENCi0zLjg0OTQ0OA0KMjENCjIuNTc4OTUwDQowDQpDSVJDTEUNCjgNCkdFT01FVFJZLUNVVFRJTkcNCjYNCkNPTlRJTlVPVVMNCjYyDQo3DQoxMA0KOTAuMTkxNjA3DQoyMA0KMjcuNzU1Njk1DQo0MA0KMi40NTg1MDMNCjANCkNJUkNMRQ0KOA0KR0VPTUVUUlktQ1VUVElORw0KNg0KQ09OVElOVU9VUw0KNjINCjcNCjEwDQotNDI1Ljg5Nzk1OA0KMjANCjUwOC40NDc5MjcNCjQwDQoyLjQ1ODUwMA0KMA0KQ0lSQ0xFDQo4DQpHRU9NRVRSWS1DVVRUSU5HDQo2DQpDT05USU5VT1VTDQo2Mg0KNw0KMTANCi0yMjYuODY4Nzc4DQoyMA0KNTA3LjAyNDUwOQ0KNDANCjIuNDU4NTAwDQowDQpDSVJDTEUNCjgNCkdFT01FVFJZLUNVVFRJTkcNCjYNCkNPTlRJTlVPVVMNCjYyDQo3DQoxMA0KOS4wODE1NjINCjIwDQoyOC4zNjU3NzgNCjQwDQoyLjQ1ODUwMA0KMA0KQ0lSQ0xFDQo4DQpHRU9NRVRSWS1DVVRUSU5HDQo2DQpDT05USU5VT1VTDQo2Mg0KNw0KMTANCi0zOC4zNzIxMjYNCjIwDQowLjc2NDA0Ng0KNDANCjIuNDU4NTAwDQowDQpFTkRTRUMNCjANCkVPRg0K"
                                }
                            }
                        };
                    }
                    break;
                case RequestProductionTimeEstimationOfPartTypeMessage productionTimeEstimation:
                    messageResponse = new RequestProductionTimeEstimationOfPartTypeMessageResponse
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
                    messageResponse = new RequestAdditionalCostsOfPartTypeMessageResponse
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
                case ProjectStatusChangedMessage projectStatusChanged:
                    {
                        //optional ChangeProjectStatusMessage
                        messageResponse = new ChangeProjectStatusMessage()
                        {
                            ProjectId = projectStatusChanged.ProjectId,
                            ProjectState = ProjectStatesV1.Produced
                            //examples
                            // ProjectStatesV1.Ordered = 6,
                            // ProjectStatesV1.Producing = 7,
                            // ProjectStatesV1.Produced = 8,
                            // ProjectStatesV1.Packaging = 10,
                            // ProjectStatesV1.Packaged = 11,
                            // ProjectStatesV1.Delivering = 12,
                            // ProjectStatesV1.Delivered = 13,
                            // ProjectStatesV1.Cancelled = 14,

                            // ProjectStatesV1.Defining = 1,
                            // ProjectStatesV1.Requested = 2,
                            // ProjectStatesV1.Quoting = 3,
                            // ProjectStatesV1.Quoted = 4,
                            // ProjectStatesV1.Negotiating = 5,

                        };
                        break;
                    }
                case ChangeProjectOrderNumberMessage changeProjectOrderNumber:
                    {
                        //optional ChangeProjectOrderNumberMessage
                        messageResponse = new ChangeProjectOrderNumberMessage()
                        {
                            ProjectId = changeProjectOrderNumber.ProjectId,
                            OrderNumber = "2024123456789"
                        };
                        break;
                    }
                default:
                    throw new Exception($"Cannot process message {message.MessageType}");
            }


            // convert response message to json
            var json = _agentMessageSerializationHelper.ToJson(messageResponse);

            // get temp file path
            var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

            // save json to temp file
            await File.WriteAllTextAsync(tempFile, json);

            // move file to input directory
            _options.MoveFileToInput(tempFile);


            _logger.LogInformation("message file successfully processed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured while handling message");
        }
    }
}
