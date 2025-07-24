using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Integration.Host.Configuration;
using Integration.Host.Features.Project;
using Integration.Host.Features.SFTP;
using MediatR;
using MetalHeaven.Agent.Shared.External.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Integration.Host.Features.FileOrchestrator;

public static class OutputFileOrchestrator
{
    public class OutputFileCreated(string filePath) : INotification
    {
        public string FilePath { get; } = filePath;
    }

    // ReSharper disable once UnusedType.Global
    public class NotificationHandler : INotificationHandler<OutputFileCreated>
    {
        private readonly IntegrationSettings _integrationSettings;
        private readonly IAgentMessageSerializationHelper _agentMessageSerializationHelper;
        private readonly ILogger<NotificationHandler> _logger;
        private readonly IMediator _mediator;

        public NotificationHandler(
            IOptions<IntegrationSettings> options,
            IAgentMessageSerializationHelper agentMessageSerializationHelper,
            ILogger<NotificationHandler> logger,
            IMediator mediator)
        {
            _integrationSettings = options.Value;
            _agentMessageSerializationHelper = agentMessageSerializationHelper;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task Handle(OutputFileCreated notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("File created: {FilePath}", notification.FilePath);

            // check if the file is accessible
            await CheckSafeFileAccessAsync(notification.FilePath);

            _logger.LogInformation("File is accessible: {FilePath}", notification.FilePath);
            // check if sftp upload is enabled
            // if sftp upload is enabled, do not process the file
            // this needs to be refactored
            if (_integrationSettings.EnableSftpUpload)
            {
                await _mediator.Publish(new SftpFileUpload.Upload(notification.FilePath), cancellationToken);
                return;
            }

            if (UseProjectFileHandle(notification.FilePath))
            {
                //BE CAREFULL HERE Both scenario's cannot be supported concurrent.
                // This is an example handling ProjectFiles
                await _mediator.Publish(new ProjectFiles.ProjectFileCreated(notification.FilePath), cancellationToken);
                // This is an example handling Project and explains how to use the time registration feedback.
                await _mediator.Publish(new TimeRegistration.ProjectFileCreatedReturnTimeRegistrationExport(notification.FilePath), cancellationToken);
                return;
            }

            if (TryIsIAgentMessage(notification.FilePath, out var agentMessage))
            {
                await HandleMessage(agentMessage);
                return;
            }

            _logger.LogWarning("File '{FilePath}' is not a valid agent message or project file", notification.FilePath);

        }

        private async Task CheckSafeFileAccessAsync(string filePath)
        {
            var info = new FileInfo(filePath);

            // https://learn.microsoft.com/en-us/dotnet/standard/io/handling-io-errors
            await Policy
                .Handle<IOException>(ex => (ex.HResult & 0x0000FFFF) == 32) // Retry 'sharing violation' (file is in use)
                .Or<UnauthorizedAccessException>()
                .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(5), (ex, retry, _) =>
                {
                    if (retry > 24)
                    {
                        _logger.LogError(ex, "File {FilePath} cannot be read retry runs for {Retry} time(s): {Message}", filePath, retry, ex.Message);
                    }
                    else
                    {
                        _logger.LogDebug("File {FilePath} cannot be read retry runs for {Retry} time(s): {Message}", filePath, retry, ex.Message);
                    }
                })
                .ExecuteAsync(() =>
                {
                    using var stream = info.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                    return Task.CompletedTask;
                });
        }

        private bool TryIsIAgentMessage(string notificationFilePath, [NotNullWhen(true)] out IAgentMessage message)
        {
            // read json from file
            var fileContent = File.ReadAllText(notificationFilePath);

            // convert json to message
            message = _agentMessageSerializationHelper.FromJson(fileContent);

            _logger.LogInformation("Processing message type: '{Type}'", message.MessageType);

            return message is not null;

        }

        private bool UseProjectFileHandle(string filePath)
        {
            // define file paths
            var jsonFilePath = filePath;
            var zipFilePath = Path.ChangeExtension(filePath, ".zip");

            return File.Exists(jsonFilePath) && File.Exists(zipFilePath);
        }

        private async Task HandleMessage(IAgentMessage message)
        {
            try
            {
                var request = AgentRequest.Create(message);
                // send request to mediator that will handle the message
                // this will call the appropriate request handler based on the message type
                // RequestAddressBookSyncMessageHandler.cs
                //
                var result = await _mediator.Send(request);
                // convert response message to json
                var json = _agentMessageSerializationHelper.ToJson(result);

                // get temp file path
                var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

                // save json to temp file
                await File.WriteAllTextAsync(tempFile, json);

                // move file to input directory
                _integrationSettings.MoveFileToInput(tempFile);

                _logger.LogInformation("message file successfully processed");
            }
            catch (NotImplementedException)
            {
                // do not log NotImplementedException, this is expected when the message is not implemented
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
    }
}
