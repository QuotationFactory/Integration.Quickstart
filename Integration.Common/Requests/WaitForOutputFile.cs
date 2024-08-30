using Integration.Common.Classes;
using Integration.Common.Extensions;
using MediatR;
using Microsoft.Extensions.Options;

namespace Integration.Common.Requests;

public class WaitForOutputFile : IRequest
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
        private readonly IntegrationSettings _settings;

        public Handler(IOptions<IntegrationSettings> options)
        {
            _settings = options.Value;
        }

        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            // define output directory
            var outputDirectory = _settings.GetOrCreateOutputDirectory(request.IntegrationName, true);

            // loop while cancel token is not cancelled and no files are found
            while (!cancellationToken.IsCancellationRequested)
            {
                // search files in output directory
                var files = Directory.GetFiles(outputDirectory, request.FileName);

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
