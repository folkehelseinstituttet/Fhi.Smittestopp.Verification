using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Smittestopp.Verification.Msis
{
    public static class MsisConfigExtensions
    {
        public static IServiceCollection AddMockMsisLookup(this IServiceCollection services)
        {
            return services.AddTransient<IMsisLookupService, MockMsisLookupService>();
        }
    }
}
