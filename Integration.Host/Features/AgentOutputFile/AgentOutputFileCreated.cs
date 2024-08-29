using MediatR;

namespace Integration.Host.Features.AgentOutputFile;

public class AgentOutputFileCreated : INotification
{
    public string FilePath { get; }

    public AgentOutputFileCreated(string filePath)
    {
        FilePath = filePath;
    }
}
