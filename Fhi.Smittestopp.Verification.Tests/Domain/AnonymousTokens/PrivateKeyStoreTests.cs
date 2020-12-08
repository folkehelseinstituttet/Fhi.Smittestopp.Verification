using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
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
        public async Task GetAsync_GivenACertificate_ReturnsExpectedPrivateKey()
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
    }
}
