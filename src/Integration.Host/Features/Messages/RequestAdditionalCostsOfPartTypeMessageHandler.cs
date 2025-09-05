using Integration.Host.Configuration;
using Integration.Host.Features.FileOrchestrator;
using MetalHeaven.Agent.Shared.External.Interfaces;
using MetalHeaven.Agent.Shared.External.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integration.Host.Features.Messages;

public class RequestAdditionalCostsOfPartTypeMessageHandler : IAgentRequestHandler<RequestAdditionalCostsOfPartTypeMessage>
{
    private readonly ILogger<RequestAdditionalCostsOfPartTypeMessageHandler> _logger;
    private readonly IntegrationSettings _integrationSettings;
    public RequestAdditionalCostsOfPartTypeMessageHandler(ILogger<RequestAdditionalCostsOfPartTypeMessageHandler> logger, IOptions<IntegrationSettings> options)
    {
        _logger = logger;
        _integrationSettings = options.Value;
    }
    public Task<IAgentMessage> Handle(AgentRequest<RequestAdditionalCostsOfPartTypeMessage> request, CancellationToken cancellationToken)
    {
        if (!_integrationSettings.EnableAdditionalCostsOfPartTypeMessages)
        {
            throw new NotImplementedException();
        }

        var msg = request.Message;

        // implement business logic here

        // create AdditionalCostsOfPartTypeMessageReponse message
        // this is a sample response, you should replace it with actual data from your business logic

        #region Example Response
        var result = new RequestAdditionalCostsOfPartTypeMessageResponse
        {
            ProjectId = msg.ProjectId,
            PartTypeId = msg.PartType.Id,
            WorkingStepKey = msg.WorkingStepKey,
            AdditionalCosts = 12.50m,
            EventLogs = new List<EventLog>
            {
                new()
                {
                    DateTime = DateTime.UtcNow,
                    Level = EventLogLevel.Information,
                    Message = "This is some random information",
                    ProjectId = msg.ProjectId,
                    PartTypeId = msg.PartType.Id
                }
            }
        };
        #endregion

        _logger.LogInformation("Additional costs message handler is enabled, processing message...");
        return Task.FromResult<IAgentMessage>(result);
    }
}
