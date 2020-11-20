using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fhi.Smittestopp.Verification.Persistence.Repositories
{
    public class VerificationRecordsRepository : IVerificationRecordsRepository
    {
        private readonly VerificationDbContext _dbContext;

        public VerificationRecordsRepository(VerificationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveNewRecord(VerificationRecord record)
        {
            _dbContext.VerificationRecords.Add(new VerificationRecordEntity
            {
                Pseudonym = record.Pseudonym,
                VerifiedAtTime = record.VerifiedAtTime
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<VerificationRecord>> RetrieveRecordsForPseudonym(string pseudonym, DateTime cutoff)
        {
            var recordsCutoff = cutoff.ToUniversalTime();
            var entities = await _dbContext.VerificationRecords
                .Where(x => x.Pseudonym == pseudonym && x.VerifiedAtTime > recordsCutoff)
                .ToListAsync();

            return entities.Select(x => new VerificationRecord(x.Pseudonym, x.VerifiedAtTime.UtcDateTime));
        }

        public async Task<int> DeleteExpiredRecords(DateTime cutoff)
        {
            var recordsCutoff = cutoff.ToUniversalTime();
            var entities = await _dbContext.VerificationRecords
                .Where(x => x.VerifiedAtTime <= recordsCutoff)
                .ToListAsync();

            _dbContext.VerificationRecords.RemoveRange(entities);

            await _dbContext.SaveChangesAsync();

            return entities.Count;
        }
    }
}
