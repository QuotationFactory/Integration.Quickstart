using Integration.Common.Classes;
using Integration.Common.Extensions;
using MediatR;
using Microsoft.Extensions.Options;

namespace Integration.Common.Requests
{
    public class WaitForAgentOutputFile : IRequest
    {
        public class Request : IRequest<string>
        {
            public string FileName { get; }
            public string IntegrationName { get; }

            public Request(string fileName, string integrationName)
            {
                FileName = fileName;
                IntegrationName = integrationName;
            }
        }

        public class Handler : IRequestHandler<Request, string>
        {
            private readonly AgentSettings _settings;

            public Handler(IOptions<AgentSettings> options)
            {
                _settings = options.Value;
            }

            public async Task<string> Handle(Request request, CancellationToken cancellationToken)
            {
                // define agent output directory
                var agentOutputDirectory = _settings.GetOrCreateAgentOutputDirectory(request.IntegrationName, true);

                // loop while cancel token is not cancelled and no files are found
                while (!cancellationToken.IsCancellationRequested)
                {
                    // search files in output directory
                    var files = Directory.GetFiles(agentOutputDirectory, request.FileName);

                    // return response with files if any
                    if (files.Any())
                        return files.First();

                    // delay before retry
                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                }

                // return empty response
                return string.Empty;
            }
        }
    }
}
