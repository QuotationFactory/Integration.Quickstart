using System.Threading.Tasks;
using MetalHeaven.Agent.Shared.External.Interfaces;
using MetalHeaven.Agent.Shared.External.Messages;
using Versioned.ExternalDataContracts.Contracts.AddressBook;

namespace Integration.Host.Features.Messages;

public static class RequestAutoInviteMessageResponseExample
{
    public static Task<IAgentMessage> Example()
    {
        // This method is a placeholder for AutoInviteResponse messages.

        // implement business logic here

        // This message is a optional message and is meant to send an invite for the portal

        // this is a sample response, you should replace it with actual data from your business logic


        #region Example Response
        var result = new RequestAutoInviteMessageResponse()
        {
            AutoInviteImportRequests =
            [
                new AgentAutoInviteImportRequest()
                {
                    Id = 1,
                    Code = "Debtor Code", // this should be an existing code
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "user@example.com"
                }
            ]
        };

        #endregion
        return Task.FromResult<IAgentMessage>(result);
    }
}
