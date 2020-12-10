using AnonymousTokens.Core.Services;

using Org.BouncyCastle.Math;

using System;
using System.Security.Cryptography.X509Certificates;
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
            var certificate = await _certLocator.GetCertificateAsync();

            if (certificate.HasPrivateKey)
            {
                var privateKey = certificate.GetECDsaPrivateKey() ??
                                 throw new Exception($"Provided certificate ({certificate.Thumbprint}) does not have an ECDsa private key.");
                var privateKeyParameters = privateKey.ExportParameters(true);

                return new BigInteger(privateKeyParameters.D);
            }

            throw new Exception("Failed to load private key. Certificate " + certificate.Thumbprint + " did not contain a private key");
        }
    }
}
