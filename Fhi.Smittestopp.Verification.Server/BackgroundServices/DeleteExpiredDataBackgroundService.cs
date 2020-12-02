using System;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.DataCleanup;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fhi.Smittestopp.Verification.Server.BackgroundServices
{
    public class DeleteExpiredDataBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DeleteExpiredDataBackgroundService> _logger;
        private readonly Config _config;

        public DeleteExpiredDataBackgroundService(IServiceProvider services, ILogger<DeleteExpiredDataBackgroundService> logger, IOptions<Config> config)
        {
            _services = services;
            _logger = logger;
            _config = config.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting background service..");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting data cleanup task...");
                    using (var scope = _services.CreateScope())
                    {
                        var mediator = scope.ServiceProvider.GetService<IMediator>();
                        await mediator.Send(new DeleteExpiredData.Command(), stoppingToken);
                    }

                    _logger.LogInformation("Data cleanup task completed. Waiting {runInterval} until next run.",
                        _config.RunInterval);
                    await Task.Delay(_config.RunInterval, stoppingToken);
                }
                catch (Exception e)
                {
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogError(e, 
                            "Unexpected error encountered while performing data cleanup task. Waiting {runInterval} until next run.",
                            _config.RunInterval);
                        try
                        {
                            await Task.Delay(_config.RunInterval, stoppingToken);
                        }
                        catch
                        {
                            // ignore -> stoppingtoken has been triggered
                        }
                    }
                }
            }
            _logger.LogInformation("Stopping background service.");
        }

        public class Config
        {
            public bool Enabled { get; set; }
            public TimeSpan RunInterval { get; set; } = TimeSpan.FromHours(1);
        }
    }
}
