using System.Collections.Generic;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Models;

namespace Fhi.Smittestopp.Verification.Domain.Interfaces
{
    public interface IAnonymousTokenIssueRecordRepository
    {
        Task SaveNewRecord(AnonymousTokenIssueRecord record);
        Task<IEnumerable<AnonymousTokenIssueRecord>> RetrieveRecordsJwtToken(string jwtId);
        Task<int> DeleteExpiredRecords();
    }
}
