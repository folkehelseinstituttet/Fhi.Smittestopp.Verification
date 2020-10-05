using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Server;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests
{
    public class ServerSetupTests
    {
        private WebApplicationFactory<Startup> _factory;

        [SetUp]
        public void Setup()
        {
            _factory = new WebApplicationFactory<Startup>();
        }

        [Test]
        public async Task Server_GivenOpenIdConfigRequest_ReturnsOpenIdConfig()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(".well-known/openid-configuration");

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            response.Content.Headers.ContentType.ToString().Should().Be("application/json; charset=UTF-8");
        }
    }
}