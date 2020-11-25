using System;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Msis;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Msis
{
    [TestFixture]
    public class MsisHealthCheckTests
    {
        [Test]
        public async Task CheckHealthAsync_GivenMsisOnline_ReturnsHealthy()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IMsisLookupService, Task<bool>>(x => x.CheckIsOnline())
                .ReturnsAsync(true);

            var target = automocker.CreateInstance<MsisHealthCheck>();

            var result = await target.CheckHealthAsync(new HealthCheckContext());

            result.Status.Should().Be(HealthStatus.Healthy);
        }


        [Test]
        public async Task CheckHealthAsync_GivenMsisNotOnline_ReturnsUnhealthy()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IMsisLookupService, Task<bool>>(x => x.CheckIsOnline())
                .ReturnsAsync(false);

            var target = automocker.CreateInstance<MsisHealthCheck>();

            var result = await target.CheckHealthAsync(new HealthCheckContext());

            result.Status.Should().Be(HealthStatus.Unhealthy);
        }


        [Test]
        public async Task CheckHealthAsync_WhenRequestFails_ReturnsUnhealthy()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IMsisLookupService, Task<bool>>(x => x.CheckIsOnline())
                .ThrowsAsync(new Exception("failed"));

            var target = automocker.CreateInstance<MsisHealthCheck>();

            var result = await target.CheckHealthAsync(new HealthCheckContext());

            result.Status.Should().Be(HealthStatus.Unhealthy);
        }
    }
}
