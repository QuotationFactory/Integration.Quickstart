using MediatR;
using MetalHeaven.Agent.Shared.External.Interfaces;

namespace Integration.Host.Features.FileOrchestrator;

public interface IAgentRequestHandler<T> : IRequestHandler<AgentRequest<T>, IAgentMessage> where T : IAgentMessage;
