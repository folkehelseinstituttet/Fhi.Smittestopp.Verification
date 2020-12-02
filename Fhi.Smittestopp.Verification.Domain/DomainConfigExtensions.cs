using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using Fhi.Smittestopp.Verification.Domain.Factories;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Domain.Users;
using Fhi.Smittestopp.Verification.Domain.Verifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Smittestopp.Verification.Domain
{
    public static class DomainConfigExtensions
    {
        public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration config)
        {
            return services
                .Configure<VerifyIdentifiedUser.Config>(config.GetSection("verification"))
                .Configure<VerificationLimitConfig>(config.GetSection("verificationLimit"))
                .Configure<AnonymousTokensConfig>(config.GetSection("anonymousTokens"))
                .AddSingleton<IAnonymousTokensCertLocator, AnonymousTokensCertLocator>()
                .AddTransient<IVerificationLimit, VerificationLimit>()
                .Configure<OneWayPseudonymFactory.Config>(config.GetSection("pseudonyms"))
                .AddTransient<IPseudonymFactory, OneWayPseudonymFactory>(); ;
        }
    }
}
