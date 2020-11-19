using System;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fhi.Smittestopp.Verification.Msis
{
    public class MsisHealthCheck : IHealthCheck
    {
        private readonly IMsisLookupService _msisLookupService;

        public MsisHealthCheck(IMsisLookupService msisLookupService)
        {
            _msisLookupService = msisLookupService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                if (await _msisLookupService.CheckIsOnline())
                {
                    return HealthCheckResult.Healthy("MSIS-gateway responded that MSIS is online.");
                }
                else
                {
                    return HealthCheckResult.Unhealthy("MSIS-gateway responded that MSIS is not online.");
                }
            }
            catch
            {
                return HealthCheckResult.Unhealthy("Failed to perform request against MSIS-gateway.");
            }
        }
    }
}
