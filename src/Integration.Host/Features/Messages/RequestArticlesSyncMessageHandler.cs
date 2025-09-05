using Integration.Host.Configuration;
using Integration.Host.Features.FileOrchestrator;
using MetalHeaven.Agent.Shared.External.Interfaces;
using MetalHeaven.Agent.Shared.External.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Versioned.ExternalDataContracts.Contracts.Article;

namespace Integration.Host.Features.Messages;

public class RequestArticlesSyncMessageHandler : IAgentRequestHandler<RequestArticlesSyncMessage>
{
    private readonly ILogger<RequestArticlesSyncMessageHandler> _logger;
    private readonly IntegrationSettings _integrationSettings;
    public RequestArticlesSyncMessageHandler(ILogger<RequestArticlesSyncMessageHandler> logger, IOptions<IntegrationSettings> options)
    {
        _logger = logger;
        _integrationSettings = options.Value;

    }
    public Task<IAgentMessage> Handle(AgentRequest<RequestArticlesSyncMessage> request, CancellationToken cancellationToken)
    {
        if (!_integrationSettings.EnableArticleSyncMessages)
        {
            throw new NotImplementedException();
        }

        var msg = request.Message;

        // implement business logic here

        // create  articlesSyncMessageResponse message
        // this is a sample response, you should replace it with actual data from your business logic

        #region Example Response
        var result = new RequestArticlesSyncMessageResponse
        {
            Articles =
            [
                new AgentArticleImportRequest
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
                // Item with scaled price
                new AgentArticleImportRequest
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
                    ScalePrices = { new ScalePrice { Price = 200, Quantity = 0 }, new ScalePrice { Price = 100, Quantity = 50 } }
                }
            ],
            EventLogs = new List<EventLog>
            {
                new() { DateTime = DateTime.UtcNow, Level = EventLogLevel.Information, Message = "This is some random information" }
            }
        };
        #endregion

        _logger.LogInformation("Articles sync message handler is enabled, processing message...");
        return Task.FromResult<IAgentMessage>(result);
    }
}


