using MediatR;
using MetalHeaven.Agent.Shared.Entities.Classes;

namespace Integration.Common.Requests
{
    public class RequestIntegrationSettings : IRequest
    {
        public class Request : IRequest<Response>
        {
            public Request(Guid integrationId, string integrationName)
            {
                IntegrationId = integrationId;
                IntegrationName = integrationName;
            }

            public Guid IntegrationId { get; }
            public string IntegrationName { get; }
        }

        public class Response
        {
            public string SettingsFilePath { get; }

            public Response(string settingsFilePath)
            {
                SettingsFilePath = settingsFilePath;
            }
        }

        public class Handler : IRequestHandler<Request, Response>
        {
            private readonly IMediator _mediator;

            public Handler(IMediator mediator)
            {
                _mediator = mediator;
            }

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                var message = new RequestIntegrationSettingsMessage
                {
                    IntegrationId = request.IntegrationId
                };

                // sent message with agent
                await _mediator
                    .Send(new SendMessageWithAgent.Request(message, request.IntegrationName), cancellationToken)
                    .ConfigureAwait(false);

                // wait for response
                var file = await _mediator.Send(new WaitForAgentOutputFile.Request("Settings.json", request.IntegrationName),
                    cancellationToken).ConfigureAwait(false);

                // throw error if there are no files found
                if (string.IsNullOrEmpty(file))
                    throw new Exception("Settings file not provided by Agent");

                return new Response(file);
            }
        }
    }
}
