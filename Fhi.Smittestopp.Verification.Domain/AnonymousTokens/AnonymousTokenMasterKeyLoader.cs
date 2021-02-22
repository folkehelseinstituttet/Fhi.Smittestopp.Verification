using System;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public interface IAnonymousTokenMasterKeyLoader
    {
        Task<byte[]> LoadMasterKeyBytes();
    }

    public class AnonymousTokenMasterKeyLoader : IAnonymousTokenMasterKeyLoader
    {
        private readonly AnonymousTokensConfig _config;
        private readonly ICertificateLocator _certLocator;
        private readonly CertificatePrivateKeyBytesLoader _keyBytesLoader;

        public AnonymousTokenMasterKeyLoader(IOptions<AnonymousTokensConfig> config, ICertificateLocator certLocator)
        {
            _config = config.Value;
            _certLocator = certLocator;
            _keyBytesLoader = new CertificatePrivateKeyBytesLoader();
        }

        public async Task<byte[]> LoadMasterKeyBytes()
        {
            var certificate = (await _certLocator.GetCertificateAsync(_config.MasterKeyCertId))
                .ValueOr(() => throw new AnonymousTokenMasterKeyLoaderException("Unable to locate master key certificate for thumbprint: " + _config.MasterKeyCertId));

            return _keyBytesLoader.ExtractPrivateKeyBytes(certificate);
        }
    }

    public class AnonymousTokenMasterKeyLoaderException : Exception
    {
        public AnonymousTokenMasterKeyLoaderException(string message) : base(message)
        {

        }
    }
}
