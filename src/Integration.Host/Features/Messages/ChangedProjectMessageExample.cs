using System;
using System.Threading.Tasks;
using MetalHeaven.Agent.Shared.External.Interfaces;
using MetalHeaven.Agent.Shared.External.Messages;
using Versioned.ExternalDataContracts.Enums;

namespace Integration.Host.Features.Messages;

public static class ChangeProjectStatusMessageExample
{
    public static Task<IAgentMessage> Example()
    {
        // This method is a placeholder for handling project status change messages.

        // implement business logic here

        // This message is a optional message and is meant to change the status of projects when other systems have changed the status.
        // for example, you might want to update  the status in quotation factory based on changes you erp system has made.

        // this is a sample response, you should replace it with actual data from your business logic

        // notes:
        // * it's not possible to revert to a previous state
        //   if you want to revert a project to a previous state, you need to do this manually

        #region Example Response
        var result = new ChangeProjectStatusMessage()
        {
            // this is the project id of the project that has changed its status
            ProjectId = Guid.NewGuid(), // replace with actual project id
            // this is the new status of the project
            ProjectState = ProjectStatesV1.Producing
            //examples
            // ProjectStatesV1.Ordered = 6,
            // ProjectStatesV1.Producing = 7,
            // ProjectStatesV1.Produced = 8,
            // ProjectStatesV1.Packaging = 10,
            // ProjectStatesV1.Packaged = 11,
            // ProjectStatesV1.Delivering = 12,
            // ProjectStatesV1.Delivered = 13,
            // ProjectStatesV1.Cancelled = 14,

            // ProjectStatesV1.Defining = 1,
            // ProjectStatesV1.Requested = 2,
            // ProjectStatesV1.Quoting = 3,
            // ProjectStatesV1.Quoted = 4,
            // ProjectStatesV1.Negotiating = 5,
        };

        #endregion

        throw new NotImplementedException();
        return Task.FromResult<IAgentMessage>(result);
    }
}
