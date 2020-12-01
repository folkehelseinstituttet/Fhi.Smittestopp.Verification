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
    public class AnonymousTokenIssueRecordRepository : IAnonymousTokenIssueRecordRepository
    {
        private readonly VerificationDbContext _dbContext;

        public AnonymousTokenIssueRecordRepository(VerificationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveNewRecord(AnonymousTokenIssueRecord record)
        {
            _dbContext.AnonymousTokenIssueRecords.Add(new AnonymousTokenIssueRecordEntity
            {
                JwtTokenId = record.JwtTokenId,
                JwtTokenExpiry = record.JwtTokenExpiry.ToUniversalTime()
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<AnonymousTokenIssueRecord>> RetrieveRecordsJwtToken(string jwtId)
        {
            var entities = await _dbContext.AnonymousTokenIssueRecords
                .Where(x => x.JwtTokenId == jwtId)
                .ToListAsync();

            return entities.Select(x => new AnonymousTokenIssueRecord(x.JwtTokenId, x.JwtTokenExpiry.UtcDateTime));
        }

        public async Task<int> DeleteExpiredRecords()
        {
            var recordsCutoff = DateTimeOffset.Now;
            var entities = await _dbContext.VerificationRecords
                .Where(x => x.VerifiedAtTime <= recordsCutoff)
                .ToListAsync();

            _dbContext.VerificationRecords.RemoveRange(entities);

            await _dbContext.SaveChangesAsync();

            return entities.Count;
        }
    }
}
