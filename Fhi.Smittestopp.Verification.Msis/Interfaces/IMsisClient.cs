using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Msis.Models;

namespace Fhi.Smittestopp.Verification.Msis.Interfaces
{
    public interface IMsisClient
    {
        Task<Covid19Status> GetCovid19Status(string nationalId);
        Task<bool> GetMsisOnlineStatus();
    }
}
