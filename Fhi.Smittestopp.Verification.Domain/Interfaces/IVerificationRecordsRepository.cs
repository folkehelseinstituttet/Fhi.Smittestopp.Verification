using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Models;

namespace Fhi.Smittestopp.Verification.Domain.Interfaces
{
    public interface IVerificationRecordsRepository
    {
        Task SaveNewRecord(VerificationRecord record);
        Task<IEnumerable<VerificationRecord>> RetrieveRecordsForPseudonym(string pseudonym, DateTime cutoff);
        Task<int> DeleteExpiredRecords(DateTime cutoff);
    }
}
