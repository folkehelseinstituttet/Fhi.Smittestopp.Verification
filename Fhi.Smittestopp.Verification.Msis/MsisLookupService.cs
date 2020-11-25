using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Msis.Interfaces;
using Optional;

namespace Fhi.Smittestopp.Verification.Msis
{
    public class MsisLookupService : IMsisLookupService
    {
        private readonly IMsisClient _msisClient;

        public MsisLookupService(IMsisClient msisClient)
        {
            _msisClient = msisClient;
        }

        public async Task<Option<PositiveTestResult>> FindPositiveTestResult(string nationalId)
        {
            var covid19Status = await _msisClient.GetCovid19Status(nationalId);

            return covid19Status
                .SomeWhen(x => x.HarPositivCovid19Prove)
                .Map(x => new PositiveTestResult
                {
                    PositiveTestDate = x.Provedato.ToOption()
                });
        }

        public Task<bool> CheckIsOnline()
        {
            return _msisClient.GetMsisOnlineStatus();
        }
    }
}
