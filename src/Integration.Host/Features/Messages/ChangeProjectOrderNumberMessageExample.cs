using System;
using System.Threading.Tasks;
using MetalHeaven.Agent.Shared.External.Interfaces;
using MetalHeaven.Agent.Shared.External.Messages;

namespace Integration.Host.Features.Messages;

public static class ChangeProjectOrderNumberMessageExample
{
    public static Task<IAgentMessage> Example()
    {
        // This method is a placeholder for handling project order number change messages.

        // implement business logic here

        // This message is a message if needed and updates the orderNumber of projects when the order number changes or is set.
        // for example, you might want to update  the orderNumber in quotation factory based on the order number output from the erp system.

        // this is a sample response, you should replace it with actual data from your business logic
        // it's not possible to revert to a previous state
        // if you want to revert a project to a previous state, you need to do this manually
        #region Example Response
        var result = new ChangeProjectOrderNumberMessage()
        {
            ProjectId =  Guid.NewGuid(), // replace with actual project id
            OrderNumber = "2024123456789"
        };

        #endregion

        return Task.FromResult<IAgentMessage>(result);
    }
}
