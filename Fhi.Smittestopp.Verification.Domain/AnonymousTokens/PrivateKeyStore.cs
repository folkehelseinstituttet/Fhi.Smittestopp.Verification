using AnonymousTokens.Core.Services;

using Org.BouncyCastle.Math;

using System.Threading.Tasks;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public class PrivateKeyStore : IPrivateKeyStore
    {
        private readonly IAnonymousTokensCertLocator _certLocator;

        public PrivateKeyStore(IAnonymousTokensCertLocator certLocator)
        {
            _certLocator = certLocator;
        }

        public async Task<BigInteger> GetAsync()
        {
            var cert = await _certLocator.GetCertificateAsync();

            // HWM 08.12 
            // TODO: convert cert to BigInteger

            return null;
        }
    }
}
