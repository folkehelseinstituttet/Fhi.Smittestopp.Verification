using AnonymousTokens.Core.Services;

using Org.BouncyCastle.Crypto.Parameters;

using System.Threading.Tasks;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public class PublicKeyStore : IPublicKeyStore
    {
        private readonly IAnonymousTokensCertLocator _certLocator;

        public PublicKeyStore(IAnonymousTokensCertLocator certLocator)
        {
            _certLocator = certLocator;
        }

        public async Task<ECPublicKeyParameters> GetAsync()
        {
            var cert = await _certLocator.GetCertificateAsync();

            // HWM 08.12 
            // TODO: convert cert to ECPublicKeyParameters    

            return null;
        }
    }
}
