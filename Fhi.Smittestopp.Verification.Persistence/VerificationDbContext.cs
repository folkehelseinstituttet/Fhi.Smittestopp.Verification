using Fhi.Smittestopp.Verification.Persistence.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fhi.Smittestopp.Verification.Persistence
{
    public class VerificationDbContext : DbContext, IDataProtectionKeyContext
    {
        public VerificationDbContext(DbContextOptions<VerificationDbContext> options) : base(options)
        {
        }

        public DbSet<VerificationRecordEntity> VerificationRecords { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    }
}
