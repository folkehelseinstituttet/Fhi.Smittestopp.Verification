using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public interface IAnonymousTokensCertLocator : ICachedCertLocator
    {
    }

    public class AnonymousTokensCertLocator : CachedCertLocator, IAnonymousTokensCertLocator
    {
        public AnonymousTokensCertLocator(IOptions<AnonymousTokensConfig> config, ICertificateLocator certLocator, IMemoryCache cache)
        : base(config.Value.CertId, nameof(AnonymousTokensConfig), certLocator, cache)
        {
        }
    }
}
