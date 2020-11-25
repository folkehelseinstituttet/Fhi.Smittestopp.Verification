using System;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Fhi.Smittestopp.Verification.Msis
{
    public class MsisHealthCheck : IHealthCheck
    {
        private readonly IMsisLookupService _msisLookupService;
        private readonly ILogger<MsisHealthCheck> _logger;

        public MsisHealthCheck(IMsisLookupService msisLookupService, ILogger<MsisHealthCheck> logger)
        {
            _msisLookupService = msisLookupService;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                if (await _msisLookupService.CheckIsOnline())
                {
                    _logger.LogInformation("MSIS-gateway responded that MSIS is online.");
                    return HealthCheckResult.Healthy("MSIS-gateway responded that MSIS is online.");
                }
                else
                {
                    _logger.LogError("MSIS-gateway responded that MSIS is not online.");
                    return HealthCheckResult.Unhealthy("MSIS-gateway responded that MSIS is not online.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Request to check if MSIS is online failed.");
                return HealthCheckResult.Unhealthy("Failed to perform request against MSIS-gateway.");
            }
        }
    }
}
