using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Server.Authentication;
using FluentAssertions;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Server.Authentication
{
    [TestFixture]
    public class IdentityServerSelfAuthSchemeTests
    {
        [Test]
        public async Task HandleAuthenticateAsync_GivenNoAuthHeader_ReturnsNotAuthenticated()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Headers = {
                    {
                        "Not-Auth", "not-auth-key"
                    }}
                }
            };

            var options = new IdentityServerSelfAuthScheme.ApiKeyOptions();

            var automocker = new AutoMocker();

            automocker.Use<UrlEncoder>(new UrlTestEncoder());
            automocker
                .Setup<IOptionsMonitor<IdentityServerSelfAuthScheme.ApiKeyOptions>, IdentityServerSelfAuthScheme.ApiKeyOptions>(
                    x => x.Get(IdentityServerSelfAuthScheme.Scheme))
                .Returns(options);
            automocker
                .Setup<ILoggerFactory, ILogger>(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(Mock.Of<ILogger>);

            var target = automocker.CreateInstance<IdentityServerSelfAuthScheme.AuthenticationHandler>();

            await target.InitializeAsync(new AuthenticationScheme(IdentityServerSelfAuthScheme.Scheme, null, typeof(IdentityServerSelfAuthScheme.AuthenticationHandler)), httpContext);

            var result = await target.AuthenticateAsync();

            result.Succeeded.Should().BeFalse();
        }

        [Test]
        public async Task HandleAuthenticateAsync_GivenInvalidAuthHeader_ReturnsNotAuthenticated()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Headers = {
                    {
                        IdentityServerSelfAuthScheme.AuthorizationHeader, "not-valid-auth-header-value"
                    }}
                }
            };

            var options = new IdentityServerSelfAuthScheme.ApiKeyOptions();

            var automocker = new AutoMocker();

            automocker.Use<UrlEncoder>(new UrlTestEncoder());
            automocker
                .Setup<IOptionsMonitor<IdentityServerSelfAuthScheme.ApiKeyOptions>, IdentityServerSelfAuthScheme.ApiKeyOptions>(
                    x => x.Get(IdentityServerSelfAuthScheme.Scheme))
                .Returns(options);
            automocker
                .Setup<ILoggerFactory, ILogger>(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(Mock.Of<ILogger>);

            var target = automocker.CreateInstance<IdentityServerSelfAuthScheme.AuthenticationHandler>();

            await target.InitializeAsync(new AuthenticationScheme(IdentityServerSelfAuthScheme.Scheme, null, typeof(IdentityServerSelfAuthScheme.AuthenticationHandler)), httpContext);

            var result = await target.AuthenticateAsync();

            result.Succeeded.Should().BeFalse();
        }

        [Test]
        public async Task HandleAuthenticateAsync_GivenInvalidAuthScheme_ReturnsNotAuthenticated()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Headers = {
                    {
                        IdentityServerSelfAuthScheme.AuthorizationHeader, "NotBearerScheme 123"
                    }}
                }
            };

            var options = new IdentityServerSelfAuthScheme.ApiKeyOptions();

            var automocker = new AutoMocker();

            automocker.Use<UrlEncoder>(new UrlTestEncoder());
            automocker
                .Setup<IOptionsMonitor<IdentityServerSelfAuthScheme.ApiKeyOptions>, IdentityServerSelfAuthScheme.ApiKeyOptions>(
                    x => x.Get(IdentityServerSelfAuthScheme.Scheme))
                .Returns(options);
            automocker
                .Setup<ILoggerFactory, ILogger>(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(Mock.Of<ILogger>);

            var target = automocker.CreateInstance<IdentityServerSelfAuthScheme.AuthenticationHandler>();

            await target.InitializeAsync(new AuthenticationScheme(IdentityServerSelfAuthScheme.Scheme, null, typeof(IdentityServerSelfAuthScheme.AuthenticationHandler)), httpContext);

            var result = await target.AuthenticateAsync();

            result.Succeeded.Should().BeFalse();
        }

        [Test]
        public async Task HandleAuthenticateAsync_GivenInvalidToken_ReturnsNotAuthenticated()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Headers = {
                    {
                        IdentityServerSelfAuthScheme.AuthorizationHeader, "Bearer not-valid-token"
                    }}
                }
            };

            var options = new IdentityServerSelfAuthScheme.ApiKeyOptions();

            var automocker = new AutoMocker();

            automocker.Use<UrlEncoder>(new UrlTestEncoder());
            automocker
                .Setup<IOptionsMonitor<IdentityServerSelfAuthScheme.ApiKeyOptions>, IdentityServerSelfAuthScheme.ApiKeyOptions>(
                    x => x.Get(IdentityServerSelfAuthScheme.Scheme))
                .Returns(options);
            automocker
                .Setup<ILoggerFactory, ILogger>(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(Mock.Of<ILogger>);

            automocker
                .Setup<ITokenValidator, Task<TokenValidationResult>>(x =>
                    x.ValidateAccessTokenAsync("not-valid-token", null))
                .ReturnsAsync(new TokenValidationResult
                {
                    IsError = true
                });

            var target = automocker.CreateInstance<IdentityServerSelfAuthScheme.AuthenticationHandler>();

            await target.InitializeAsync(new AuthenticationScheme(IdentityServerSelfAuthScheme.Scheme, null, typeof(IdentityServerSelfAuthScheme.AuthenticationHandler)), httpContext);

            var result = await target.AuthenticateAsync();

            result.Succeeded.Should().BeFalse();
        }

        [Test]
        public async Task HandleAuthenticateAsync_GivenValidToken_ReturnsAuthenticated()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Headers = {
                    {
                        IdentityServerSelfAuthScheme.AuthorizationHeader, "Bearer a-valid-token"
                    }}
                }
            };

            var claims = new[] {new Claim("some-claim", "some-value")};

            var options = new IdentityServerSelfAuthScheme.ApiKeyOptions();

            var automocker = new AutoMocker();

            automocker.Use<UrlEncoder>(new UrlTestEncoder());
            automocker
                .Setup<IOptionsMonitor<IdentityServerSelfAuthScheme.ApiKeyOptions>, IdentityServerSelfAuthScheme.ApiKeyOptions>(
                    x => x.Get(IdentityServerSelfAuthScheme.Scheme))
                .Returns(options);
            automocker
                .Setup<ILoggerFactory, ILogger>(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(Mock.Of<ILogger>);

            automocker
                .Setup<ITokenValidator, Task<TokenValidationResult>>(x =>
                    x.ValidateAccessTokenAsync("a-valid-token", null))
                .ReturnsAsync(new TokenValidationResult
                {
                    IsError = false,
                    Claims = claims
                });

            var target = automocker.CreateInstance<IdentityServerSelfAuthScheme.AuthenticationHandler>();

            await target.InitializeAsync(new AuthenticationScheme(IdentityServerSelfAuthScheme.Scheme, null, typeof(IdentityServerSelfAuthScheme.AuthenticationHandler)), httpContext);

            var result = await target.AuthenticateAsync();

            result.Succeeded.Should().BeTrue();
            result.Principal.Claims.Should().Contain(c => c.Type == "some-claim" && c.Value == "some-value");
        }
    }
}
