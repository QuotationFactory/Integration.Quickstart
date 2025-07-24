using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Integration.Host.Features.FileOrchestrator;
using MetalHeaven.Agent.Shared.External.Interfaces;
using MetalHeaven.Agent.Shared.External.Messages;

namespace Integration.Host.Features.Messages;

public class RequestSellingBuyingPartyArticleMessageHandler : IAgentRequestHandler<RequestSellingBuyingPartyArticleMessage>
{
    public Task<IAgentMessage> Handle(AgentRequest<RequestSellingBuyingPartyArticleMessage> request, CancellationToken cancellationToken)
    {
        var msg = request.Message;

        // implement business logic here

        // query the database or any other data source to get the required data
        // for example, let's assume we have a list of requests

        // create SellingBuyingPartyArticleMessageResponse message
        // this is a sample response, you should replace it with actual data from your business logic
        #region Example Response
        var result = new RequestSellingBuyingPartyArticleMessageResponse()
        {
            ProjectId = msg.ProjectId,
            Responses = msg.Requests.Select(r => new SellingBuyingPartyArticleResponse
            {
                BoMItemId = r.BoMItemId,
                BuyingPartyArticleNumber = $"BUYING-ART-{r.BoMItemId}",
                SellingPartyArticleNumber = $"SELLING-ART-{r.BoMItemId}",
            }).ToList()
        };
       #endregion
       throw new NotImplementedException();
       return Task.FromResult<IAgentMessage>(result);
    }
}
