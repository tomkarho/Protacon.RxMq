using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Protacon.RxMq.AzureServiceBus;
using Protacon.RxMq.AzureServiceBus.Topic;

namespace Protacon.RxMq.ConsoleExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .AddJsonFile("appsettings.json")
                .Build();
            var host = CreateHostBuilder(args, configuration).Build();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args, IConfigurationRoot configurationRoot) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddLogging(builder =>
                {
                    builder.AddApplicationInsights(configurationRoot["ApplicationInsights:InstrumentationKey"]);
                });

                services.AddApplicationInsightsTelemetryWorkerService();

                services.AddSingleton<ITelemetryModule, FileDiagnosticsTelemetryModule>();
                services.AddAzureBusTopics(settings =>
                {
                    configurationRoot.Bind("MessagingSettings", settings);
                });
                services.AddTransient<AzureBusTopicManagement>();

                services.AddHostedService<MessageService>();
            })
            .ConfigureLogging(builder =>
            {
                builder.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Information);
            });
    }
}
