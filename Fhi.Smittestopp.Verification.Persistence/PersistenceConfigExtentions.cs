using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Smittestopp.Verification.Persistence
{
    public static class PersistenceConfigExtentions
    {
        public static IServiceCollection AddMockPersistence(this IServiceCollection services)
        {
            return services.AddTransient<IVerificationRecordsRepository, MockVerificationRecordsRepository>();
        }
    }
}
