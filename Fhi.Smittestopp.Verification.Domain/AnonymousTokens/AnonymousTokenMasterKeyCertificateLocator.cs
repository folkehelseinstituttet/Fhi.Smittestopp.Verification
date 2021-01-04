using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public interface IAnonymousTokenMasterKeyCertificateLocator
    {
        Task<X509Certificate2> GetMasterKeyCertificate();
    }

    public class AnonymousTokenMasterKeyCertificateLocator : CachedCertLocator, IAnonymousTokenMasterKeyCertificateLocator
    {
        public AnonymousTokenMasterKeyCertificateLocator(IOptions<AnonymousTokensConfig> config, ICertificateLocator certLocator, IMemoryCache cache)
            : base(config.Value.MasterKeyCertId, nameof(AnonymousTokenMasterKeyCertificateLocator), certLocator, cache)
        {
        }

        public Task<X509Certificate2> GetMasterKeyCertificate()
        {
            return GetCertificateAsync();
        }
    }
}