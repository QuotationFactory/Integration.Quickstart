using System;
using Integration.Host.Configuration;
using Integration.Host.Features.OutputFile;
using Integration.Host.Features.SFTP;
using MetalHeaven.Agent.Shared.External.Classes;
using MetalHeaven.Agent.Shared.External.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Integration.Host;

public static class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs\\log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting host");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .UseWindowsService()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                if (context.HostingEnvironment.IsDevelopment())
                {
                    config.AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true);
                }
            })
            .ConfigureServices((hostContext, services) =>
            {
                // register message serialization helper
                services.AddTransient<IAgentMessageSerializationHelper, ExternalAgentMessageSerializationHelper>();
                // we have options defined in the appsettings.json file but also in appsettings.{env}.json

                // register settings
                services.AddOptions<IntegrationSettings>()
                    .Bind(hostContext.Configuration.GetSection("IntegrationSettings"))
                    .ValidateDataAnnotations();
                services.AddOptions<SftpSettings>()
                    .Bind(hostContext.Configuration.GetSection("SftpSettings"))
                    .ValidateDataAnnotations();

                // register output file watcher service
                services.AddHostedService<OutputFileWatcherService>();

                // register MediatR with current assembly
                services.AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(typeof(OutputFileWatcherService).Assembly);
                });
            });
}
