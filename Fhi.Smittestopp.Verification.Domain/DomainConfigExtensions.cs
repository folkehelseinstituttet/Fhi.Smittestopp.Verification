using Fhi.Smittestopp.Verification.Domain.Factories;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Smittestopp.Verification.Domain
{
    public static class DomainConfigExtensions
    {
        public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration config)
        {
            return services
                .Configure<VerificationLimitConfig>(config.GetSection("verificationLimit"))
                .AddTransient<IVerificationLimit, VerificationLimit>()
                .Configure<OneWayPseudonymFactory.Config>(config.GetSection("pseudonyms"))
                .AddTransient<IPseudonymFactory, OneWayPseudonymFactory>(); ;
        }
    }
}
