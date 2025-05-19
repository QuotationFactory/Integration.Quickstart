using System;
using System.Collections.Generic;
using System.IO;
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
using Renci.SshNet;
using Versioned.ExternalDataContracts;
using Versioned.ExternalDataContracts.Contracts.Project;

namespace Integration.Host.Features.SFTP;

public static class SftpFileUpload
{
    public class Upload(string filePath) : INotification
    {
        public string FilePath { get; } = filePath;
    }

    public class NotificationHandler : INotificationHandler<Upload>
    {
        private readonly ILogger<NotificationHandler> _logger;
        private readonly IAgentMessageSerializationHelper _agentMessageSerializationHelper;
        private readonly IntegrationSettings _integrationSettings;
        private readonly SftpSettings _sftpSettings;

        public NotificationHandler(
            IOptions<IntegrationSettings> integrationOptions,
            IOptions<SftpSettings> sftpOptions,
            ILogger<NotificationHandler> logger,
            IAgentMessageSerializationHelper agentMessageSerializationHelper)
        {
            _sftpSettings = sftpOptions.Value;
            _integrationSettings = integrationOptions.Value;
            _logger = logger;
            _agentMessageSerializationHelper = agentMessageSerializationHelper;
        }

        public async Task Handle(Upload notification, CancellationToken cancellationToken)
        {

            // default file creation timeout
            await Task.Delay(500, cancellationToken);

            // check is notification.FilePath is null or empty
            if (string.IsNullOrEmpty(notification.FilePath))
            {
                _logger.LogError("File path is null or empty");
                return;
            }

            var projectId = await GetProjectIdAsync(notification.FilePath);
            var processedSuccess = false;

            try
            {

                await UploadFileToSftpAsync(notification.FilePath, cancellationToken);
                await SendErpResultAsync(notification.FilePath, projectId, true);
                processedSuccess = true;
            }
            catch (Exception e)
            {
                var eventLogs = new List<EventLog>()
                {
                    new() { Level = EventLogLevel.Error, ProjectId = projectId, Message = e.Message + " " + e.InnerException?.Message }
                };

                await SendErpResultAsync(notification.FilePath, projectId, false, eventLogs);

                _logger.LogError(e, "Error while processing file {filePath}", notification.FilePath);
            }
            finally
            {
                if (processedSuccess)
                {
                    // Move the file to a processed directory
                    var processedDirectory = Path.Combine(Path.GetDirectoryName(notification.FilePath) ?? string.Empty, "processed");
                    var result = notification.FilePath.MoveFileToDirectory(processedDirectory);
                    _logger.LogInformation("Moved file {filePath} to processed directory {processedDirectory}", notification.FilePath,
                        result);

                    if (_sftpSettings.UploadZipFile)
                    {
                        // Move the file to a processed directory
                        var zipFilePath = Path.ChangeExtension(notification.FilePath, ".zip");
                        var processedDirectoryZip = Path.Combine(Path.GetDirectoryName(zipFilePath) ?? string.Empty, "processed");
                        var resultZip = zipFilePath.MoveFileToDirectory(processedDirectoryZip);
                        _logger.LogInformation("Moved file {zipFilePath} to processed directory {processedDirectoryZip}", zipFilePath,
                            resultZip);
                    }
                }
                else
                {
                    // Move the file to a processed directory
                    var processedDirectory = Path.Combine(Path.GetDirectoryName(notification.FilePath) ?? string.Empty, "error");
                    var result = notification.FilePath.MoveFileToDirectory(processedDirectory);
                    _logger.LogInformation("Moved file {filePath} to error directory {processedDirectory}", notification.FilePath, result);

                    if (_sftpSettings.UploadZipFile)
                    {
                        // Move the file to a processed directory
                        var zipFilePath = Path.ChangeExtension(notification.FilePath, ".zip");
                        var processedDirectoryZip = Path.Combine(Path.GetDirectoryName(zipFilePath) ?? string.Empty, "error");
                        var resultZip = zipFilePath.MoveFileToDirectory(processedDirectoryZip);
                        _logger.LogInformation("Moved file {zipFilePath} to processed directory {processedDirectoryZip}", zipFilePath,
                            resultZip);
                    }
                }
            }
        }

        private async Task UploadFileToSftpAsync(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                // Here we want to implement the logic to upload the file to SFTP server
                // using the SftpSettings provided in the constructor.
                // This is a placeholder for the actual upload logic.
                _logger.LogInformation("Uploading file {filePath} to SFTP server {server} with user {user}", filePath, _sftpSettings.Host,
                    _sftpSettings.Username);
                // Connect to the SFTP server

                using var sftp = new SftpClient(_sftpSettings.Host, _sftpSettings.Port, _sftpSettings.Username, _sftpSettings.Password);
                await sftp.ConnectAsync(cancellationToken);
                await using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    sftp.UploadFile(fileStream, Path.Combine(_sftpSettings.RemoteDirectory, Path.GetFileName(filePath)));
                }

                if (_sftpSettings.UploadZipFile)
                {
                    var zipFilePath = Path.ChangeExtension(filePath, ".zip");
                    await using var fileStream = new FileStream(zipFilePath, FileMode.Open);
                    sftp.UploadFile(fileStream, Path.Combine(_sftpSettings.RemoteDirectory, Path.GetFileName(zipFilePath)));
                }


                sftp.Disconnect();
                // Log the upload completion
                _logger.LogInformation("File {filePath} uploaded to SFTP server {server} with user {user}", filePath, _sftpSettings.Host,
                    _sftpSettings.Username);

            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "unable to upload file {NotificationFilePath} to SFTP server {SftpSettingsHost} with user {SftpSettingsUsername}",
                    filePath, _sftpSettings.Host, _sftpSettings.Username);
                throw;
            }
        }

        private async Task<Guid> GetProjectIdAsync(string notificationFilePath)
        {
            // define json serializer settings
            var settings = new JsonSerializerSettings();
            settings.SetJsonSettings();
            settings.AddJsonConverters();
            settings.SerializationBinder = new CrossPlatformTypeBinder();

            // read all text from file that is created
            var json = await File.ReadAllTextAsync(notificationFilePath);

            // convert json to project object
            var project = JsonConvert.DeserializeObject<ProjectV1>(json, settings);

            return project.Id;
        }

        private async Task SendErpResultAsync(string filePath, Guid projectId, bool success, List<EventLog> eventLogs = null)
        {
            // Here we want to implement the logic to send the result to ERP system
            // This is a placeholder for the actual sending logic.
            _logger.LogInformation("Sending ERP result for file {filePath} with project id {projectId}", filePath, projectId);

            // optional response if your using this to export to ERP.
            var response = new ExportToErpResponse
            {
                Source = "Sftp File Upload", Succeed = success, ProjectId = projectId, EventLogs = eventLogs ?? [],
            };

            var responseJson = _agentMessageSerializationHelper.ToJson(response);

            // get temp file path
            var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

            // save json to temp file
            await File.WriteAllTextAsync(tempFile, responseJson);

            // move file to input directory
            _integrationSettings.MoveFileToInput(tempFile);
        }
    }
}
