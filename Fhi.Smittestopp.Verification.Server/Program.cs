using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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
                });

    }

    public static class ProgramSetupExtensions
    {
        public static LoggerConfiguration Configure(this LoggerConfiguration loggerConfig, IConfiguration config)
        {
            loggerConfig
                .Enrich.FromLogContext()
                .WriteTo.Console();

            var logAnalyticsConfig = config.GetSection("logAnalytics").Get<LogAnalyticsConfig>();
            if (logAnalyticsConfig.Enabled)
            {
                loggerConfig
                    .WriteTo.AzureAnalytics(logAnalyticsConfig.WorkspaceId, logAnalyticsConfig.PrimaryKey, logName: "VerificationLog");
            }

            return loggerConfig;
        }
    }

    public class LogAnalyticsConfig
    {
        public bool Enabled { get; set; }
        public string WorkspaceId { get; set; }
        public string PrimaryKey { get; set; }
    }
}