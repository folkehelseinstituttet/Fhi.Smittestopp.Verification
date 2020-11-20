using Fhi.Smittestopp.Verification.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fhi.Smittestopp.Verification.Persistence
{
    public class VerificationDbContext : DbContext
    {
        public VerificationDbContext(DbContextOptions<VerificationDbContext> options) : base(options)
        {
        }

        public DbSet<VerificationRecordEntity> VerificationRecords { get; set; }
    }
}
