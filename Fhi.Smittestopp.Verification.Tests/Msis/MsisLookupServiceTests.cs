using System;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Msis;
using Fhi.Smittestopp.Verification.Msis.Interfaces;
using Fhi.Smittestopp.Verification.Msis.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Msis
{
    [TestFixture]
    public class MsisLookupServiceTests
    {
        [Test]
        public async Task FindPositiveTestResult_ResponseNoPositiveTest_ReturnsNone()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IMsisClient, Task<Covid19Status>>(x => x.GetCovid19Status("01019012345"))
                .ReturnsAsync(new Covid19Status
                {
                    HarPositivCovid19Prove = false
                });

            var target = automocker.CreateInstance<MsisLookupService>();

            var result = await target.FindPositiveTestResult("01019012345");

            result.Should().Be(Option.None<PositiveTestResult>());
        }

        [Test]
        public async Task FindPositiveTestResult_ResponseHasPositiveTest_ReturnsPositiveTestResult()
        {
            var automocker = new AutoMocker();

            var positiveTestData = DateTime.UtcNow.AddDays(-2);

            automocker.Setup<IMsisClient, Task<Covid19Status>>(x => x.GetCovid19Status("01019012345"))
                .ReturnsAsync(new Covid19Status
                {
                    HarPositivCovid19Prove = true,
                    Provedato = positiveTestData
                });

            var target = automocker.CreateInstance<MsisLookupService>();

            var result = await target.FindPositiveTestResult("01019012345");

            using (new AssertionScope())
            {
                result.HasValue.Should().BeTrue();
                result.MatchSome(x => x.PositiveTestDate.Should().Be(positiveTestData.Some()));
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task CheckIsOnline_GivenResponseFromClient_ReturnsSameResult(bool isOnline)
        {
            var automocker = new AutoMocker();

            automocker.Setup<IMsisClient, Task<bool>>(x => x.GetMsisOnlineStatus())
                .ReturnsAsync(isOnline);

            var target = automocker.CreateInstance<MsisLookupService>();

            var result = await target.CheckIsOnline();

            result.Should().Be(isOnline);
        }
    }
}
