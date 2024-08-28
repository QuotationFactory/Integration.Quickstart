using Integration.Common.Classes;
using Integration.Common.Extensions;
using MediatR;
using MetalHeaven.Agent.Shared.External.Helpers;
using MetalHeaven.Agent.Shared.External.Interfaces;
using Microsoft.Extensions.Options;

namespace Integration.Common.Requests
{
    public class SendMessageWithAgent : IRequest
    {
        public class Request : IRequest<Response>
        {
            public Request(IAgentMessage message, string integrationName)
            {
                Message = message;
                IntegrationName = integrationName;
            }

            public IAgentMessage Message { get; }
            public string IntegrationName { get; }
        }

        public class Response
        {
        }

        public class Handler : IRequestHandler<Request, Response>
        {
            private readonly AgentSettings _settings;

            public Handler(IOptions<AgentSettings> options)
            {
                _settings = options.Value;
            }

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                // define temp file path
                var tempFilePath = Path.GetTempFileName();

                // convert message to json
                var json = request.Message.ToJson();

                // write json to temp file
                await File.WriteAllTextAsync(tempFilePath, json, cancellationToken).ConfigureAwait(false);

                // define agent input directory
                var agentInputDirectory = _settings.GetOrCreateAgentInputDirectory(request.IntegrationName, true);

                // define new path to move temp file to
                var destinationFilePath = Path.Combine(agentInputDirectory, $"{Guid.NewGuid()}.json");

                // move temp file to agent input directory
                File.Move(tempFilePath, destinationFilePath);

                // return result
                return new Response();
            }
        }
    }
}
