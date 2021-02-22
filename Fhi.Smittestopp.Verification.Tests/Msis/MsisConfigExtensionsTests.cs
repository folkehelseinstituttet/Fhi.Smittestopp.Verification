using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Msis;
using Fhi.Smittestopp.Verification.Msis.Interfaces;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Msis
{
    [TestFixture]
    public class MsisConfigExtensionsTests
    {
        [Test]
        public void AddMsisLookup_GivenMockActive_AddsMockClientToCollection()
        {
            // Arrange
            var config = new MsisConfig
            {
                Mock = true
            };

            var serviceCollection = new ServiceCollection();

            // Act
            serviceCollection.AddMsisLookup(config);

            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var msisClient = serviceProvider.GetRequiredService<IMsisClient>();
            msisClient.Should().BeOfType<MockMsisClient>();
        }

        [Test]
        public void AddMsisLookup_GivenMockDisabled_AddsRealClientToCollection()
        {
            // Arrange
            var config = new MsisConfig
            {
                Mock = false,
                CertId = "some-cert",
                BaseUrl = "https://msis.api.com/api/v1/"
            };

            var certificate = CertUtils.GenerateTestEccCert();

            var certLocator = new Mock<ICertificateLocator>();

            certLocator
                .Setup(x => x.GetCertificate("some-cert"))
                .Returns(certificate.Some());

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddMemoryCache();
            serviceCollection.AddSingleton(certLocator.Object);

            // Act
            serviceCollection.AddMsisLookup(config);

            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var msisClient = serviceProvider.GetRequiredService<IMsisClient>();
            msisClient.Should().BeOfType<MsisClient>();
            certLocator.Verify();
        }
    }
}
