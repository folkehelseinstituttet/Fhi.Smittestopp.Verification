using System;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Smittestopp.Verification.Persistence
{
    public static class PersistenceConfigExtentions
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<VerificationDbContext>(options => options.UseSqlOrInMemory(connectionString));

            return services.AddTransient<IVerificationRecordsRepository, VerificationRecordsRepository>();
        }

        public static DbContextOptionsBuilder UseSqlOrInMemory(this DbContextOptionsBuilder options, string connectionString, Action<SqlServerDbContextOptionsBuilder> optionsAction = null)
        {
            if (connectionString == "in-memory")
            {
                options.UseInMemoryDatabase(nameof(VerificationDbContext));
            }
            else
            {
                options.UseSqlServer(connectionString, optionsAction);
            }

            return options;
        }
    }
}
