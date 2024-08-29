using MediatR;

namespace Integration.Common.Notifications;

public class IntegrationOutputFileCreated : INotification
{
    public string FilePath { get; }

    public IntegrationOutputFileCreated(string filePath)
    {
        FilePath = filePath;
    }
}