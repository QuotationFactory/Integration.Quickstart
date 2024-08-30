using MediatR;

namespace Integration.Host.Features.OutputFile;

public class OutputFileCreated : INotification
{
    public string FilePath { get; }

    public OutputFileCreated(string filePath)
    {
        FilePath = filePath;
    }
}
