using Integration.Host.Configuration;
using Integration.Host.Features.FileOrchestrator;
using MetalHeaven.Agent.Shared.External.Interfaces;
using MetalHeaven.Agent.Shared.External.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integration.Host.Features.Messages;

public class RequestSellingBuyingPartyArticleMessageHandler : IAgentRequestHandler<RequestSellingBuyingPartyArticleMessage>
{
    private readonly ILogger<RequestSellingBuyingPartyArticleMessageHandler> _logger;
    private readonly IntegrationSettings _integrationSettings;
    public RequestSellingBuyingPartyArticleMessageHandler(ILogger<RequestSellingBuyingPartyArticleMessageHandler> logger, IOptions<IntegrationSettings> options)
    {
        _logger = logger;
        _integrationSettings = options.Value;
    }
    public Task<IAgentMessage> Handle(AgentRequest<RequestSellingBuyingPartyArticleMessage> request, CancellationToken cancellationToken)
    {
        if (!_integrationSettings.EnableSellingBuyingPartyArticleMessages)
        {
            throw new NotImplementedException();
        }

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

       _logger.LogInformation("Selling buying party article message handler is enabled, processing message...");
       return Task.FromResult<IAgentMessage>(result);
    }
}
