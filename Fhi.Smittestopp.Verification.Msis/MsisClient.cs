using System.Net.Http;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Msis.Interfaces;
using Fhi.Smittestopp.Verification.Msis.Models;
using Newtonsoft.Json;

namespace Fhi.Smittestopp.Verification.Msis
{
    public class MsisClient : IMsisClient
    {
        private readonly HttpClient _httpClient;

        public MsisClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Covid19Status> GetCovid19Status(string nationalId)
        {
            var result = await _httpClient.GetAsync("covid19status?ident=" + nationalId);
            result.EnsureSuccessStatusCode();
            var responseJson = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Covid19Status>(responseJson);
        }

        public async Task<bool> GetMsisOnlineStatus()
        {
            var result = await _httpClient.GetAsync("erMsisOnline");
            result.EnsureSuccessStatusCode();
            var responseJson = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<bool>(responseJson);
        }
    }
}
