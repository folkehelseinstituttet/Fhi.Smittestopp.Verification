using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;

namespace Fhi.Smittestopp.Verification.Persistence
{
    public class MockVerificationRecordsRepository : IVerificationRecordsRepository
    {
        private static readonly List<VerificationRecord> Records = new List<VerificationRecord>();

        public Task SaveNewRecord(VerificationRecord record)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<VerificationRecord>> RetrieveRecordsForPseudonym(string pseudonym)
        {
            return Task.FromResult<IEnumerable<VerificationRecord>>(Records.Where(r => r.Pseudonym == pseudonym).ToList());
        }
    }
}
