using Fhi.Smittestopp.Verification.Server.BackgroundServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Fhi.Smittestopp.Verification.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration.Configure(hostingContext.Configuration))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Adding hostedservices here to ensure DB-migrations are executed first
                    services.AddEnabledBackgroundServices(hostContext.Configuration);
                });

    }

    public static class ProgramSetupExtensions
    {
        public static LoggerConfiguration Configure(this LoggerConfiguration loggerConfig, IConfiguration config)
        {
            loggerConfig
                .Enrich.FromLogContext()
                .WriteTo.Console();

            var fileLogConfig = config.GetSection("logFile").Get<LogFileConfig>();
            if (fileLogConfig.Enabled)
            {
                loggerConfig
                    .WriteTo.File(fileLogConfig.Filename, rollingInterval: RollingInterval.Day);
            }

            var logAnalyticsConfig = config.GetSection("logAnalytics").Get<LogAnalyticsConfig>();
            if (logAnalyticsConfig.Enabled)
            {
                loggerConfig
                    .WriteTo.AzureAnalytics(logAnalyticsConfig.WorkspaceId, logAnalyticsConfig.PrimaryKey, logName: "VerificationLog");
            }

            return loggerConfig;
        }

        public static IServiceCollection AddEnabledBackgroundServices(this IServiceCollection services, IConfiguration config)
        {
            // Adding hostedservices here to ensure DB-migrations are executed first
            var cleanupTaskConfig = config.GetSection("cleanupTask");
            if (cleanupTaskConfig["enabled"] == "True")
            {
                services.Configure<DeleteExpiredDataBackgroundService.Config>(cleanupTaskConfig);
                services.AddHostedService<DeleteExpiredDataBackgroundService>();
            }

            return services;
        }
    }

    public class LogAnalyticsConfig
    {
        public bool Enabled { get; set; }
        public string WorkspaceId { get; set; }
        public string PrimaryKey { get; set; }
    }

    public class LogFileConfig
    {
        public bool Enabled { get; set; }
        public string Filename { get; set; } = "log.txt";
    }
}