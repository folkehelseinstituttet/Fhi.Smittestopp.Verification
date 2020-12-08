using AnonymousTokens.Core.Services;

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

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
            var certificate = await _certLocator.GetCertificateAsync();

            var convertedCertificate = DotNetUtilities.FromX509Certificate(certificate);
            return (ECPublicKeyParameters)convertedCertificate.GetPublicKey();
        }
    }
}
