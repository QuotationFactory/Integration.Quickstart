using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Integration.Host.Features.FileOrchestrator;
using MetalHeaven.Agent.Shared.External.Interfaces;
using MetalHeaven.Agent.Shared.External.Messages;

namespace Integration.Host.Features.Messages;

public class RequestAdditionalCostsOfPartTypeMessageHandler : IAgentRequestHandler<RequestAdditionalCostsOfPartTypeMessage>
{
    public Task<IAgentMessage> Handle(AgentRequest<RequestAdditionalCostsOfPartTypeMessage> request, CancellationToken cancellationToken)
    {
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

        throw new NotImplementedException();
        return Task.FromResult<IAgentMessage>(result);
    }
}
