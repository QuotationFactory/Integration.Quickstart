using MetalHeaven.Agent.Shared.External.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Integration.Common.Classes
{
    public abstract class BaseIntegration : IIntegration
    {
        public IOptions<AgentSettings> Options { get; }

        public BaseIntegration(IOptions<AgentSettings> options)
        {
            Options = options;
        }

        public abstract Guid Id { get; set; }
        public abstract string Name { get; set; }
        public abstract void RegisterServices(IServiceCollection serviceCollection);
        public abstract Task StartAsync(CancellationToken cancellationToken);
        public abstract Task StopAsync(CancellationToken cancellationToken);
    }
}
