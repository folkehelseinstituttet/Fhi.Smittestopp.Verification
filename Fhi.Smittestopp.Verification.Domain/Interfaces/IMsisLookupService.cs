using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Models;
using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Interfaces
{
    public interface IMsisLookupService
    {
        Task<Option<PositiveTestResult>> FindPositiveTestResult(string nationalId);
    }
}
