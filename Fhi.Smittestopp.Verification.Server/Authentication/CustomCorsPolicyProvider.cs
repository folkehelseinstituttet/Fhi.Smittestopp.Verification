using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Constants;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fhi.Smittestopp.Verification.Server.Authentication
{
    /// <summary>
    /// Custom Cors Policy Provider used to add any policies needed in addition to the ones already provided by identity server
    /// </summary>
    public class CustomCorsPolicyProvider : ICorsPolicyProvider
    {
        private readonly ILogger<CustomCorsPolicyProvider> _logger;

        public CustomCorsPolicyProvider(ILogger<CustomCorsPolicyProvider> logger)
        {
            _logger = logger;
        }

        public Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
        {
            if (policyName == CorsPolicies.AnonymousTokens)
            {
                return ProcessAnonymousTokensPolicyAsync(context);
            }

            return Task.FromResult((CorsPolicy)null);
        }

        private async Task<CorsPolicy> ProcessAnonymousTokensPolicyAsync(HttpContext context)
        {
            var origin = context.Request.GetCorsOrigin();
            if (origin == null)
            {
                return null;
            }

            // Provides access to client cors registrations in identity server
            var corsPolicyService = context.RequestServices.GetRequiredService<ICorsPolicyService>();

            if (!await corsPolicyService.IsOriginAllowedAsync(origin))
            {
                _logger.LogDebug("CorsPolicyService rejected origin: {origin} for policy {policy}", origin, CorsPolicies.AnonymousTokens);
                return null;
            }

            _logger.LogDebug("CorsPolicyService allowed origin: {origin} for policy {policy}", origin, CorsPolicies.AnonymousTokens);
            return new CorsPolicyBuilder()
                .WithOrigins(origin)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .Build();
        }
    }
}
