using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Smittestopp.Verification.Persistence
{
    public static class PersistenceConfigExtentions
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<VerificationDbContext>(options =>
            {
                if (connectionString == "in-memory")
                {
                    options.UseInMemoryDatabase(nameof(VerificationDbContext));
                }
                else
                {
                    options.UseSqlServer(connectionString);
                }
            });

            return services.AddTransient<IVerificationRecordsRepository, VerificationRecordsRepository>();
        }

        public static IHealthChecksBuilder AddDbHealthCheck(this IHealthChecksBuilder hcBuilder, string connectionString)
        {
            // Add SQL-server check, unless using in-memory DB
            if (connectionString != "in-memory")
            {
                hcBuilder.AddSqlServer(connectionString);
            }
            return hcBuilder;
        }
    }
}
