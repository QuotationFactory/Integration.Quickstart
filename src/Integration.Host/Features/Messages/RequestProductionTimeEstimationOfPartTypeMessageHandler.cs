using Integration.Host.Configuration;
using Integration.Host.Features.FileOrchestrator;
using MetalHeaven.Agent.Shared.External.Interfaces;
using MetalHeaven.Agent.Shared.External.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integration.Host.Features.Messages;

public class RequestProductionTimeEstimationOfPartTypeMessageHandler : IAgentRequestHandler<RequestProductionTimeEstimationOfPartTypeMessage>
{
    private readonly ILogger<RequestProductionTimeEstimationOfPartTypeMessageHandler> _logger;
    private readonly IntegrationSettings _integrationSettings;
    public RequestProductionTimeEstimationOfPartTypeMessageHandler(ILogger<RequestProductionTimeEstimationOfPartTypeMessageHandler> logger, IOptions<IntegrationSettings> options)
    {
        _logger = logger;
        _integrationSettings = options.Value;

    }
    public Task<IAgentMessage> Handle(AgentRequest<RequestProductionTimeEstimationOfPartTypeMessage> request, CancellationToken cancellationToken)
    {
        if (!_integrationSettings.EnableProductionTimeEstimationOfPartTypeMessages)
        {
            throw new NotImplementedException();
        }

        var msg = request.Message;

        // implement business logic here

        //create a ProductionTimeEstimationOfPartTypeMessageReponse message
        // this is a sample response, you should replace it with actual data from your business logic
        #region Example Response

        var result = new RequestProductionTimeEstimationOfPartTypeMessageResponse
        {
            ProjectId = msg.ProjectId,
            PartTypeId = msg.PartType.Id,
            WorkingStepKey = msg.WorkingStepKey,
            EstimatedProductionTimeMs = (long)TimeSpan.FromMinutes(12).TotalMilliseconds,
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

        _logger.LogInformation("Production time estimation message handler is enabled, processing message...");
        return Task.FromResult<IAgentMessage>(result);
    }
}
