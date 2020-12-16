using AnonymousTokens.Core.Services;

using Microsoft.Extensions.Options;

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public class PrivateKeyStore : IPrivateKeyStore
    {
        private readonly IAnonymousTokensCertLocator _certLocator;
        private readonly string _password;

        public PrivateKeyStore(IOptions<AnonymousTokensConfig> config, IAnonymousTokensCertLocator certLocator)
        {
            _certLocator = certLocator;

            _password = string.IsNullOrEmpty(config.Value.CertPassword) ? string.Empty : config.Value.CertPassword;
        }

        public async Task<BigInteger> GetAsync()
        {
            var certificate = await _certLocator.GetCertificateAsync();

            byte[] rawdata = certificate.Export(X509ContentType.Pfx);
            using (var memStream = new MemoryStream(rawdata))
            {
                var pkcs12Store = new Pkcs12Store(memStream, _password.ToCharArray());
                foreach (var alias in pkcs12Store.Aliases)
                {
                    if (pkcs12Store.IsKeyEntry((string)alias))
                    {
                        var privateKeyParameters = (ECPrivateKeyParameters)pkcs12Store.GetKey(alias.ToString()).Key;

                        return privateKeyParameters.D;
                    }
                }

                throw new Exception("Failed to load private key. Certificate " + certificate.Thumbprint + " did not contain a private key");
            }
        }
    }
}
