using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Server.Credentials;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Domain.AnonymousTokens
{
    [TestFixture]
    public class AnonymousTokenMasterKeyLoaderTests
    {
        [Test]
        public void LoadMasterKeyBytes_MasterKeyCertNotFound_ThrowsException()
        {
            var options = new AnonymousTokensConfig
            {
                MasterKeyCertId = "key-id"
            };

            var automocker = new AutoMocker();

            automocker.SetupOptions(options);

            automocker
                .Setup<ICertificateLocator, Task<Option<X509Certificate2>>>(x => x.GetCertificateAsync("key-id"))
                .ReturnsAsync(Option.None<X509Certificate2>());

            var target = automocker.CreateInstance<AnonymousTokenMasterKeyLoader>();

            Assert.ThrowsAsync<AnonymousTokenMasterKeyLoaderException>(() => target.LoadMasterKeyBytes());
        }

        [Test]
        public async Task GetMasterKeyCertificate_GivenEccCertificate_ReturnsPrivateKeyBytes()
        {
            var options = new AnonymousTokensConfig
            {
                MasterKeyCertId = "key-id"
            };

            var eccCert = CertUtils.GenerateTestEccCert();

            var automocker = new AutoMocker();

            automocker.SetupOptions(options);

            automocker
                .Setup<ICertificateLocator, Task<Option<X509Certificate2>>>(x => x.GetCertificateAsync("key-id"))
                .ReturnsAsync(eccCert.Some);

            var target = automocker.CreateInstance<AnonymousTokenMasterKeyLoader>();

            var result = await target.LoadMasterKeyBytes();

            result.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task GetMasterKeyCertificate_GivenRsaCertificate_ReturnsPrivateKeyBytes()
        {
            var options = new AnonymousTokensConfig
            {
                MasterKeyCertId = "key-id"
            };

            var eccCert = CertUtils.GenerateTestRsaCert();

            var automocker = new AutoMocker();

            automocker.SetupOptions(options);

            automocker
                .Setup<ICertificateLocator, Task<Option<X509Certificate2>>>(x => x.GetCertificateAsync("key-id"))
                .ReturnsAsync(eccCert.Some);

            var target = automocker.CreateInstance<AnonymousTokenMasterKeyLoader>();

            var result = await target.LoadMasterKeyBytes();

            result.Should().NotBeNullOrEmpty();
        }

        [Test]
        [Ignore("Only run manually for a thumbprint valid in your environment.")]
        public async Task GetMasterKeyCertificate_GivenLocalCertificate_ReturnsPrivateKeyBytes()
        {
            var automocker = new AutoMocker();

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    MasterKeyCertId = "F9656C4BFC04586DC844D423148F655088168109"
                });

            automocker.Use<ICertificateLocator>(new LocalCertificateLocator());

            var target = automocker.CreateInstance<AnonymousTokenMasterKeyLoader>();

            // Some certificates seems to produce the following error:
            // The CNG key handle being opened was detected to be ephemeral, but the EphemeralKey open option was not specified. (Parameter 'keyHandleOpenOptions')
            var result = await target.LoadMasterKeyBytes();

            result.Should().NotBeNullOrEmpty();
        }
    }
}
