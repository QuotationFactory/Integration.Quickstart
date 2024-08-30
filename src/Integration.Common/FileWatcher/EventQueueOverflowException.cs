namespace Integration.Common.FileWatcher;

class EventQueueOverflowException : Exception
{
    public EventQueueOverflowException()
        : base() { }

    public EventQueueOverflowException(string message)
        : base(message) { }
}