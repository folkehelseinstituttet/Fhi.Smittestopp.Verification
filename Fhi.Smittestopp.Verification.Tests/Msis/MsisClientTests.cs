using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Msis;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Msis
{
    [TestFixture]
    public class MsisClientTests
    {
        [TestCase("true", true)]
        [TestCase("false", false)]
        public async Task GetMsisOnlineStatus_ReturnsResultAccordingToResponse(string responseBody, bool expectedResult)
        {
            // ARRANGE
            var handlerMock = new Mock<HttpMessageHandler>()
                .SetupRequest(HttpMethod.Get, "/v1/Msis/erMsisOnline", new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
                });
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://test.com/v1/Msis/"),
            };

            var target = new MsisClient(httpClient);

            // ACT
            var result = await target.GetMsisOnlineStatus();

            // ASSERT
            result.Should().Be(expectedResult);
            handlerMock.Verify();
        }

        [Test]
        public async Task GetCovid19Status_GivenPositiveResponse_ReturnsPositiveResult()
        {
            // ARRANGE
            var handlerMock = new Mock<HttpMessageHandler>()
                .SetupRequest(HttpMethod.Get, "/v1/Msis/covid19status?ident=01019098765", new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"{
                        ""harPositivCovid19Prove"": true,
                        ""provedato"": ""2020-11-19T17:08:59.902Z""
                    }", Encoding.UTF8, "application/json"),
                });
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://test.com/v1/Msis/"),
            };

            var target = new MsisClient(httpClient);

            // ACT
            var result = await target.GetCovid19Status("01019098765");

            // ASSERT
            using (new AssertionScope())
            {
                result.HarPositivCovid19Prove.Should().BeTrue();
                result.Provedato.Should().BeCloseTo(DateTime.Parse("2020-11-19T17:08:59.902Z").ToUniversalTime());
            }
            handlerMock.Verify();
        }

        [Test]
        public async Task GetCovid19Status_GivenNonPositiveResponse_ReturnsNonPositiveResult()
        {
            // ARRANGE
            var handlerMock = new Mock<HttpMessageHandler>()
                .SetupRequest(HttpMethod.Get, "/v1/Msis/covid19status?ident=01019098765", new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"{
                        ""harPositivCovid19Prove"": false,
                        ""provedato"": null
                    }", Encoding.UTF8, "application/json"),
                });
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://test.com/v1/Msis/"),
            };

            var target = new MsisClient(httpClient);

            // ACT
            var result = await target.GetCovid19Status("01019098765");

            // ASSERT
            using (new AssertionScope())
            {
                result.HarPositivCovid19Prove.Should().BeFalse();
                result.Provedato.Should().BeNull();
            }
            handlerMock.Verify();
        }
    }
}
