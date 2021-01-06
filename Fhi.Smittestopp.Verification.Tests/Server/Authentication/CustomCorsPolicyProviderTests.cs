using System;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Fhi.Smittestopp.Verification.Server.Authentication;
using FluentAssertions;
using FluentAssertions.Execution;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Server.Authentication
{
    [TestFixture]
    public class CustomCorsPolicyProviderTests
    {
        [Test]
        public async Task GetPolicyAsync_GivenUnknownPolicyName_ReturnsNull()
        {
            var httpContext = new DefaultHttpContext();

            var automocker = new AutoMocker();

            var target = automocker.CreateInstance<CustomCorsPolicyProvider>();

            var result = await target.GetPolicyAsync(httpContext, "unknown-policy");

            result.Should().BeNull();
        }

        [Test]
        public async Task GetPolicyAsync_GivenKnownPolicyNameNoOriginHeader_ReturnsNull()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Headers = {
                    {
                        "Not-Origin", "not-origin-value"
                    }}
                }
            };

            var automocker = new AutoMocker();

            var target = automocker.CreateInstance<CustomCorsPolicyProvider>();

            var result = await target.GetPolicyAsync(httpContext, CorsPolicies.AnonymousTokens);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetPolicyAsync_GivenKnownPolicyDisallowedOrigin_ReturnsNull()
        {
            var automocker = new AutoMocker();

            automocker
                .Setup<ICorsPolicyService, Task<bool>>(x => x.IsOriginAllowedAsync("http://not.allowed.com"))
                .ReturnsAsync(false);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns<Type>(t => automocker.Get(t));

            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Headers = {
                    {
                        "Origin", "http://not.allowed.com"
                    }}
                },
                RequestServices = serviceProviderMock.Object
            };

            var target = automocker.CreateInstance<CustomCorsPolicyProvider>();

            var result = await target.GetPolicyAsync(httpContext, CorsPolicies.AnonymousTokens);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetPolicyAsync_GivenKnownPolicyAllowedOrigin_ReturnsAllowAllPolicy()
        {
            var automocker = new AutoMocker();

            automocker
                .Setup<ICorsPolicyService, Task<bool>>(x => x.IsOriginAllowedAsync("http://is.allowed.com"))
                .ReturnsAsync(true);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns<Type>(t => automocker.Get(t));

            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Headers = {
                    {
                        "Origin", "http://is.allowed.com"
                    }}
                },
                RequestServices = serviceProviderMock.Object
            };

            var target = automocker.CreateInstance<CustomCorsPolicyProvider>();

            var result = await target.GetPolicyAsync(httpContext, CorsPolicies.AnonymousTokens);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.AllowAnyHeader.Should().BeTrue();
                result.AllowAnyMethod.Should().BeTrue();
                result.Origins.Should().HaveCount(1).And.Contain("http://is.allowed.com");
            }
        }
    }
}
