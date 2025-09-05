using MediatR;
using MetalHeaven.Agent.Shared.External.Interfaces;

namespace Integration.Host.Features.FileOrchestrator;

public static class AgentRequest
{
    public static IRequest<IAgentMessage> Create(IAgentMessage message)
    {
        var messageType = message.GetType();
        var type = typeof(AgentRequest<>).MakeGenericType(messageType);
        var result = Activator.CreateInstance(type, [message]) as IRequest<IAgentMessage>;
        ArgumentNullException.ThrowIfNull(result);
        return result;
    }
}

public sealed class AgentRequest<T> : IRequest<IAgentMessage>
    where T : IAgentMessage
{
    public T Message { get; }

    public AgentRequest(T message)
    {
        Message = message;
    }
}
