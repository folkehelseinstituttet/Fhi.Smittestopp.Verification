using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using Fhi.Smittestopp.Verification.Server.Credentials;
using Fhi.Smittestopp.Verification.Tests.TestUtils;

using FluentAssertions;

using Moq;
using Moq.AutoMock;

using NUnit.Framework;

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Fhi.Smittestopp.Verification.Tests.Domain.AnonymousTokens
{
    [TestFixture]
    public class PrivateKeyStoreTests
    {
        [Test]
        public async Task GetAsync_GivenAnECCertificate_ReturnsExpectedPrivateKey()
        {
            //Arrange
            var automocker = new AutoMocker();

            var testCertificate = CertUtils.GenerateTestCert();

            automocker
                .Setup<IAnonymousTokensCertLocator, Task<X509Certificate2>>(x => x.GetCertificateAsync())
                .ReturnsAsync(testCertificate);

            var target = automocker.CreateInstance<PrivateKeyStore>();

            //Act
            var result = await target.GetAsync();

            //Assert
            result.Should().NotBeNull();
        }

        /// <summary>
        /// Recommend following this guide for creating the certificate and importing the PFX-file to local machine certificate store: https://gist.github.com/marta-krzyk-dev/83168c9a8e985e5b3b1b14a98b533b9c
        /// </summary>
        [Ignore("Requires a certificate installed on local machine")]
        [Test]
        public async Task GetAsync_GivenAnECCertificateFromLocalMachine_ReturnsExpectedPrivateKey()
        {
            //Arrange
            var automocker = new AutoMocker();

            var certificateLocator = new LocalCertificateLocator();
            var certificate = certificateLocator.GetCertificate("<insert your certificate's thumbprint here>").ValueOr(() => null);

            automocker
                .Setup<IAnonymousTokensCertLocator, Task<X509Certificate2>>(x => x.GetCertificateAsync())
                .ReturnsAsync(certificate);

            var target = automocker.CreateInstance<PrivateKeyStore>();

            //Act
            var result = await target.GetAsync();

            //Assert
            result.Should().NotBeNull();
        }
    }
}
