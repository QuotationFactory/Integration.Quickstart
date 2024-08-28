using MediatR;

namespace Rhodium24.Host.Features.AgentOutputFile
{
    public class AgentOutputFileCreated : INotification
    {
        public string FilePath { get; }

        public AgentOutputFileCreated(string filePath)
        {
            FilePath = filePath;
        }
    }
}