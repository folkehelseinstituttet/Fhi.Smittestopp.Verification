using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Domain.Users;
using Fhi.Smittestopp.Verification.Server.Account;
using Fhi.Smittestopp.Verification.Server.Account.Models;
using FluentAssertions;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;
using Optional.Unsafe;

namespace Fhi.Smittestopp.Verification.Tests.Server.Account
{
    [TestFixture]
    public class AccountServiceTests
    {
        [Test]
        public async Task GetLoginOptions_GivenExtIdpRequest_ReturnsSingleIdpOptions()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IIdentityServerInteractionService, Task<AuthorizationRequest>>(x =>
                    x.GetAuthorizationContextAsync("/auth/?authRequest=123"))
                .ReturnsAsync(new AuthorizationRequest
                {
                    IdP = "ext-provider"
                });

            automocker.Setup<IAuthenticationSchemeProvider, Task<AuthenticationScheme>>(x =>
                    x.GetSchemeAsync("ext-provider"))
                .ReturnsAsync(new AuthenticationScheme("ext-provider", "An external provider", typeof(DummyAuthenticationHandler)));

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.GetLoginOptions("/auth/?authRequest=123");

            result.IsExternalLoginOnly.Should().BeTrue();
            result.ExternalLoginScheme.Should().Be("ext-provider");
        }


        [Test]
        public async Task GetLoginOptions_GivenLocalIdpRequest_ReturnsLocalLoginNoExtProviders()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IIdentityServerInteractionService, Task<AuthorizationRequest>>(x =>
                    x.GetAuthorizationContextAsync("/auth/?authRequest=123"))
                .ReturnsAsync(new AuthorizationRequest
                {
                    IdP = "local"
                });

            automocker.Setup<IAuthenticationSchemeProvider, Task<AuthenticationScheme>>(x =>
                    x.GetSchemeAsync("local"))
                .ReturnsAsync(new AuthenticationScheme("local", "Local provider", typeof(DummyAuthenticationHandler)));

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.GetLoginOptions("/auth/?authRequest=123");

            result.IsExternalLoginOnly.Should().BeFalse();
            result.EnableLocalLogin.Should().BeTrue();
            result.ExternalProviders.Should().BeNullOrEmpty();
        }

        [Test]
        public async Task GetLoginOptions_GivenNoContext_ReturnsAllValidLoginOptions()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAuthenticationSchemeProvider, Task<IEnumerable<AuthenticationScheme>>>(x => x.GetAllSchemesAsync())
                .ReturnsAsync(new []
                {
                    new AuthenticationScheme("ext-provider-a", "External provider A", typeof(DummyAuthenticationHandler)),
                    new AuthenticationScheme("ext-provider-b", "External provider B", typeof(DummyAuthenticationHandler))
                });

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.GetLoginOptions(null);

            result.IsExternalLoginOnly.Should().BeFalse();
            result.EnableLocalLogin.Should().BeTrue();
            result.ExternalProviders.Should().Contain(p => p.AuthenticationScheme == "ext-provider-a")
                .And.Contain(p => p.AuthenticationScheme == "ext-provider-b");
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetLoginOptions_GivenClientContext_ReturnsOptionsAccordingToClientConfig(bool allowLocalLoginForClient)
        {
            var automocker = new AutoMocker();

            var client = new Client
            {
                ClientId = "a-client",
                Enabled = true,
                EnableLocalLogin = allowLocalLoginForClient,
                IdentityProviderRestrictions = new[]
                {
                    "ext-provider-b"
                }
            };

            automocker.Setup<IIdentityServerInteractionService, Task<AuthorizationRequest>>(x =>
                    x.GetAuthorizationContextAsync("/auth/?authRequest=123"))
                .ReturnsAsync(new AuthorizationRequest
                {
                    Client = client
                });

            automocker.Setup<IClientStore, Task<Client>>(x =>
                    x.FindClientByIdAsync("a-client"))
                .ReturnsAsync(client);

            automocker.Setup<IAuthenticationSchemeProvider, Task<IEnumerable<AuthenticationScheme>>>(x => x.GetAllSchemesAsync())
                .ReturnsAsync(new[]
                {
                    new AuthenticationScheme("ext-provider-a", "External provider A", typeof(DummyAuthenticationHandler)),
                    new AuthenticationScheme("ext-provider-b", "External provider B", typeof(DummyAuthenticationHandler))
                });

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.GetLoginOptions("/auth/?authRequest=123");

            result.EnableLocalLogin.Should().Be(allowLocalLoginForClient);
            result.ExternalProviders.Should().Contain(p => p.AuthenticationScheme == "ext-provider-b")
                .And.NotContain(p => p.AuthenticationScheme == "ext-provider-a");
        }

        [Test]
        public async Task GetLogoutOptions_GivenNoPrincipal_GivesOptionsNoPrompt()
        {
            var automocker = new AutoMocker();

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.GetLogoutOptions("logout-1", Option.None<ClaimsPrincipal>());

            result.ShowLogoutPrompt.Should().BeFalse();
        }

        [Test]
        public async Task GetLogoutOptions_GivenNotAuthenticatedPrincipal_GivesOptionsNoPrompt()
        {
            var automocker = new AutoMocker();

            var identity = new ClaimsIdentity(Enumerable.Empty<Claim>(), null); // IsAuthenticated => authType not null or empty

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.GetLogoutOptions("logout-1", new ClaimsPrincipal(identity).Some());

            result.ShowLogoutPrompt.Should().BeFalse();
        }

        [Test]
        public async Task GetLogoutOptions_GivenPrincipalNoContext_ReturnsPromptAccordingToGlobalConfig()
        {
            var automocker = new AutoMocker();

            var identity = new ClaimsIdentity(Enumerable.Empty<Claim>(), "some-auth"); // IsAuthenticated => authType not null or empty

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.GetLogoutOptions("logout-1", new ClaimsPrincipal(identity).Some());

            result.ShowLogoutPrompt.Should().Be(AccountOptions.ShowLogoutPrompt);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetLogoutOptions_GivenPrincipalAndContext_ReturnsPromptAccordingToContext(bool showPromptForContext)
        {
            var automocker = new AutoMocker();

            automocker.Setup<IIdentityServerInteractionService, Task<LogoutRequest>>(x =>
                    x.GetLogoutContextAsync("logout-1"))
                .ReturnsAsync(new LogoutRequest("dummyUrl", new LogoutMessage())
                {
                    ClientId = showPromptForContext ? null : "a client" // LogoutRequest.ShowSignoutPrompt is based on ClientId
                });

            var identity = new ClaimsIdentity(Enumerable.Empty<Claim>(), "some-auth"); // IsAuthenticated => authType not null or empty

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.GetLogoutOptions("logout-1", new ClaimsPrincipal(identity).Some());

            result.ShowLogoutPrompt.Should().Be(showPromptForContext);
        }

        [Test]
        public void AttemptLocalLogin_GivenInvalidReturnUrl_ThrowsException()
        {
            var automocker = new AutoMocker();

            automocker
                .Setup<IMediator, Task<Option<PinVerifiedUser>>>(m =>
                    m.Send(It.IsAny<CreateFromPinCode.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Option.None<PinVerifiedUser>());

            automocker
                .Setup<IUrlHelper, bool>(m => m.IsLocalUrl(It.IsAny<string>()))
                .Returns(false);

            automocker
                .Setup<IUrlHelperFactory, IUrlHelper>(m => m.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(() => automocker.Get<IUrlHelper>());

            var target = automocker.CreateInstance<AccountService>();

            Assert.ThrowsAsync<Exception>(() => target.AttemptLocalLogin("123456", "http://malicous.returnurl.com/"));
        }

        [Test]
        public async Task AttemptLocalLogin_GivenInvalidPinCode_ReturnsNone()
        {
            var automocker = new AutoMocker();

            automocker
                .Setup<IMediator, Task<Option<PinVerifiedUser>>>(m =>
                    m.Send(It.IsAny<CreateFromPinCode.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Option.None<PinVerifiedUser>());

            automocker
                .Setup<IUrlHelper, bool>(m => m.IsLocalUrl(It.IsAny<string>()))
                .Returns(true);

            automocker
                .Setup<IUrlHelperFactory, IUrlHelper>(m => m.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(() => automocker.Get<IUrlHelper>());

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.AttemptLocalLogin("123456", null);

            result.Should().Be(Option.None<LocalLoginResult, string>(AccountOptions.InvalidCredentialsErrorMessage));
        }

        [Test]
        public async Task AttemptLocalLogin_GivenValidPinCodeNoReturnUrl_ReturnsResult()
        {
            var automocker = new AutoMocker();

            automocker
                .Setup<IMediator, Task<Option<PinVerifiedUser>>>(m =>
                    m.Send(It.IsAny<CreateFromPinCode.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PinVerifiedUser("pseudo-1").Some());

            automocker
                .Setup<IUrlHelper, bool>(m => m.IsLocalUrl(It.IsAny<string>()))
                .Returns(false);

            automocker
                .Setup<IUrlHelperFactory, IUrlHelper>(m => m.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(() => automocker.Get<IUrlHelper>());

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.AttemptLocalLogin("123456", null);

            var loginResult = result.ValueOrFailure();
            loginResult.TrustedReturnUrl.Should().Be(Option.None<string>());
            loginResult.UseNativeClientRedirect.Should().BeFalse();
            loginResult.IsUser.Should().NotBeNull();
        }

        [Test]
        public async Task AttemptLocalLogin_GivenValidPinCodeLocalReturnUrl_ReturnsResult()
        {
            var automocker = new AutoMocker();

            automocker
                .Setup<IMediator, Task<Option<PinVerifiedUser>>>(m =>
                    m.Send(It.IsAny<CreateFromPinCode.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PinVerifiedUser("pseudo-1").Some());

            automocker
                .Setup<IUrlHelper, bool>(m => m.IsLocalUrl(It.IsAny<string>()))
                .Returns(true);

            automocker
                .Setup<IUrlHelperFactory, IUrlHelper>(m => m.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(() => automocker.Get<IUrlHelper>());

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.AttemptLocalLogin("123456", "/");

            var loginResult = result.ValueOrFailure();
            loginResult.TrustedReturnUrl.Should().Be("/".Some());
            loginResult.UseNativeClientRedirect.Should().BeFalse();
            loginResult.IsUser.Should().NotBeNull();
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task AttemptLocalLogin_GivenValidPinCodeClientReturnUrl_ReturnsResult(bool isNativeClient)
        {
            var automocker = new AutoMocker();

            automocker
                .Setup<IMediator, Task<Option<PinVerifiedUser>>>(m =>
                    m.Send(It.IsAny<CreateFromPinCode.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PinVerifiedUser("pseudo-1").Some());

            automocker.Setup<IIdentityServerInteractionService, Task<AuthorizationRequest>>(x =>
                    x.GetAuthorizationContextAsync("/auth/?authRequest=123"))
                .ReturnsAsync(new AuthorizationRequest
                {
                    IdP = "ext-provider",
                    RedirectUri = isNativeClient ? "custom:return" : "http://non.native.url/",
                    Client = new Client
                    {
                        ClientId = "a-client",
                        Enabled = true,
                        IdentityProviderRestrictions = new[]
                        {
                            "ext-provider-b"
                        }
                    }
                });

            automocker
                .Setup<IUrlHelper, bool>(m => m.IsLocalUrl(It.IsAny<string>()))
                .Returns(false);

            automocker
                .Setup<IUrlHelperFactory, IUrlHelper>(m => m.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(() => automocker.Get<IUrlHelper>());

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.AttemptLocalLogin("123456", "/auth/?authRequest=123");

            var loginResult = result.ValueOrFailure();
            loginResult.TrustedReturnUrl.Should().Be("/auth/?authRequest=123".Some());
            loginResult.UseNativeClientRedirect.Should().Be(isNativeClient);
            loginResult.IsUser.Should().NotBeNull();
        }

        [Test]
        public async Task CancelLogin_GivenNoRequestContext_ReturnsEmptyCancelResult()
        {
            var automocker = new AutoMocker();

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.CancelLogin("not-valid-return-url");

            result.TrustedReturnUrl.Should().Be(Option.None<string>());
            result.UseNativeClientRedirect.Should().BeFalse();
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task CancelLogin_GivenRequestContext_ReturnsCancelResultAccordingToClient(bool isNativeClient)
        {
            var automocker = new AutoMocker();

            automocker.Setup<IIdentityServerInteractionService, Task<AuthorizationRequest>>(x =>
                    x.GetAuthorizationContextAsync("/auth/?authRequest=123"))
                .ReturnsAsync(new AuthorizationRequest
                {
                    IdP = "ext-provider",
                    RedirectUri = isNativeClient ? "custom:return" : "http://non.native.url/",
                    Client = new Client
                    {
                        ClientId = "a-client",
                        Enabled = true,
                        IdentityProviderRestrictions = new[]
                        {
                            "ext-provider-b"
                        }
                    }
                });


            var target = automocker.CreateInstance<AccountService>();

            var result = await target.CancelLogin("/auth/?authRequest=123");

            result.TrustedReturnUrl.Should().Be("/auth/?authRequest=123".Some());
            result.UseNativeClientRedirect.Should().Be(isNativeClient);
        }

        [Test]
        public async Task Logout_GivenContext_ReturnsLogoutRequestAccordingToContet()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IIdentityServerInteractionService, Task<LogoutRequest>>(x =>
                    x.GetLogoutContextAsync("logout-1"))
                .ReturnsAsync(new LogoutRequest("dummy", new LogoutMessage())
                {
                    PostLogoutRedirectUri = "post-logout-uri",
                    ClientName = "",
                    ClientId = "client-a",
                    SignOutIFrameUrl = "signout-iframe-uri"
                });


            var target = automocker.CreateInstance<AccountService>();

            var result = await target.LogOut("logout-1", Option.None<ClaimsPrincipal>(), x => Task.FromResult(false));

            result.LogoutId.Should().Be("logout-1");
            result.AutomaticRedirectAfterSignOut.Should().Be(AccountOptions.AutomaticRedirectAfterSignOut);
            result.PostLogoutRedirectUri.Should().Be("post-logout-uri".Some());
            result.ClientName.Should().Be("client-a".Some());
            result.SignOutIframeUrl.Should().Be("signout-iframe-uri".Some());
        }

        [TestCase(IdentityServerConstants.LocalIdentityProvider)]
        [TestCase("ext-scheme")]
        public async Task Logout_GivenUser_RaisesLogoutEvent(string scheme)
        {
            var automocker = new AutoMocker();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(JwtClaimTypes.Subject, "user-a"),
                new Claim(JwtClaimTypes.IdentityProvider, scheme)
            }, scheme));

            var target = automocker.CreateInstance<AccountService>();

            await target.LogOut("logout-1", user.Some(), x => Task.FromResult(false));

            automocker.Verify<IEventService>(x => x.RaiseAsync(It.Is<UserLogoutSuccessEvent>(e => e.SubjectId == "user-a")));
        }

        [Test]
        public async Task Logout_GivenExternalUserSupportsSignout_ReturnsExternalSchemeForLogout()
        {
            var automocker = new AutoMocker();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new []
            {
                new Claim(JwtClaimTypes.Subject, "user-a"),
                new Claim(JwtClaimTypes.IdentityProvider, "ext-scheme")
            }, "ext-scheme"));

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.LogOut("logout-1", user.Some(), x => Task.FromResult(x == "ext-scheme"));

            result.LogoutId.Should().Be("logout-1");
            result.ExternalAuthenticationScheme.Should().Be("ext-scheme");
        }

        [Test]
        public async Task Logout_GivenExternalUserSupportsSignoutNoContext_CreatesNewContext()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(JwtClaimTypes.Subject, "user-a"),
                new Claim(JwtClaimTypes.IdentityProvider, "ext-scheme")
            }, "ext-scheme"));

            var automocker = new AutoMocker();

            automocker.Setup<IIdentityServerInteractionService, Task<string>>(x => x.CreateLogoutContextAsync())
                .ReturnsAsync("new-logout-id");

            var target = automocker.CreateInstance<AccountService>();

            var result = await target.LogOut(null, user.Some(), x => Task.FromResult(x == "ext-scheme"));

            result.LogoutId.Should().Be("new-logout-id");
        }

        private class DummyAuthenticationHandler : IAuthenticationHandler
        {
            public Task<AuthenticateResult> AuthenticateAsync()
            {
                throw new NotImplementedException();
            }

            public Task ChallengeAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task ForbidAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
