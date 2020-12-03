using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Domain.Users;
using Fhi.Smittestopp.Verification.Server;
using Fhi.Smittestopp.Verification.Server.ExternalController;
using Fhi.Smittestopp.Verification.Server.ExternalController.Models;
using FluentAssertions;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;
using Optional.Unsafe;

namespace Fhi.Smittestopp.Verification.Tests.Server.External
{
    [TestFixture]
    public class ExternalServiceTests
    {
        [Test]
        public void CreateChallenge_InvalidReturnUrl_RejectsRequest()
        {
            //Arrange
            var automocker = new AutoMocker();

            var urlHelper = new Mock<IUrlHelper>();

            urlHelper.Setup(x => x.IsLocalUrl("http://mal.icio.us/return/url")).Returns(false);

            automocker
                .Setup<IUrlHelperFactory, IUrlHelper>(x => x.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            automocker
                .Setup<IIdentityServerInteractionService, bool>(x => x.IsValidReturnUrl("http://mal.icio.us/return/url"))
                .Returns(false);

            var target = automocker.CreateInstance<ExternalService>();

            //Act
            var result = target.CreateChallenge(new ExtChallengeRequest
            {
                Scheme = "some-scheme",
                ReturnUrl = "http://mal.icio.us/return/url".Some()
            });

            //Assert
            result.Should().Be(Option.None<ExtChallengeResult, string>("invalid return URL"));
        }

        [Test]
        public void CreateChallenge_NoReturnUrl_RedirectsToRoot()
        {
            //Arrange
            var automocker = new AutoMocker();

            var urlHelper = new Mock<IUrlHelper>();

            urlHelper.Setup(x => x.IsLocalUrl("~/")).Returns(true);

            automocker
                .Setup<IUrlHelperFactory, IUrlHelper>(x => x.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            automocker
                .Setup<IIdentityServerInteractionService, bool>(x => x.IsValidReturnUrl("~/"))
                .Returns(false);

            var target = automocker.CreateInstance<ExternalService>();

            //Act
            var result = target.CreateChallenge(new ExtChallengeRequest
            {
                Scheme = "some-scheme",
                ReturnUrl = Option.None<string>()
            });

            //Assert
            result.HasValue.Should().BeTrue();
            var innerResult = result.ValueOrFailure();
            innerResult.TrustedReturnUrl.Should().Be("~/");
            innerResult.Scheme.Should().Be("some-scheme");
        }

        [Test]
        public void CreateChallenge_ValidExternalRedirect_RedirectsToGivenUrl()
        {
            //Arrange
            var automocker = new AutoMocker();

            var urlHelper = new Mock<IUrlHelper>();

            urlHelper.Setup(x => x.IsLocalUrl("https://totally.legit.return/url/")).Returns(false);

            automocker
                .Setup<IUrlHelperFactory, IUrlHelper>(x => x.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            automocker
                .Setup<IIdentityServerInteractionService, bool>(x => x.IsValidReturnUrl("https://totally.legit.return/url/"))
                .Returns(true);

            var target = automocker.CreateInstance<ExternalService>();

            //Act
            var result = target.CreateChallenge(new ExtChallengeRequest
            {
                Scheme = "some-scheme",
                ReturnUrl = "https://totally.legit.return/url/".Some()
            });

            //Assert
            result.HasValue.Should().BeTrue();
            var innerResult = result.ValueOrFailure();
            innerResult.TrustedReturnUrl.Should().Be("https://totally.legit.return/url/");
            innerResult.Scheme.Should().Be("some-scheme");
        }


        [Test]
        public void CreateChallenge_ValidLocalRedirect_RedirectsToGivenUrl()
        {
            //Arrange
            var automocker = new AutoMocker();

            var urlHelper = new Mock<IUrlHelper>();

            urlHelper.Setup(x => x.IsLocalUrl("~/local/url")).Returns(true);

            automocker
                .Setup<IUrlHelperFactory, IUrlHelper>(x => x.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            automocker
                .Setup<IIdentityServerInteractionService, bool>(x => x.IsValidReturnUrl("~/local/url"))
                .Returns(false);

            var target = automocker.CreateInstance<ExternalService>();

            //Act
            var result = target.CreateChallenge(new ExtChallengeRequest
            {
                Scheme = "some-scheme",
                ReturnUrl = "~/local/url".Some()
            });

            //Assert
            result.HasValue.Should().BeTrue();
            var innerResult = result.ValueOrFailure();
            innerResult.TrustedReturnUrl.Should().Be("~/local/url");
            innerResult.Scheme.Should().Be("some-scheme");
        }

        [Test]
        public async Task ProcessExternalAuthentication_MissingAuthResult_ReturnsRejectResult()
        {
            //Arrange
            var automocker = new AutoMocker();

            var target = automocker.CreateInstance<ExternalService>();

            //Act
            var result = await target.ProcessExternalAuthentication(null);

            //Assert
            result.Should().Be(Option.None<ExtAuthenticationResult, string>("External authentication error"));
        }

        [Test]
        public async Task ProcessExternalAuthentication_FailedResult_ReturnsRejectResult()
        {
            //Arrange
            var automocker = new AutoMocker();

            var failedAuthResult = AuthenticateResult.Fail("test");

            var target = automocker.CreateInstance<ExternalService>();

            //Act
            var result = await target.ProcessExternalAuthentication(failedAuthResult);

            //Assert
            result.Should().Be(Option.None<ExtAuthenticationResult, string>("External authentication error"));
        }

        [Test]
        public async Task ProcessExternalAuthentication_SuccessfulResult_ReturnsAuthResultForCreatedInternalUser()
        {
            //Arrange
            var claims = new []
            {
                new Claim("ident", "08089403198"), 
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var authProps = new AuthenticationProperties
            {
                Items =
                {
                    {"scheme", "ext-scheme"},
                    {"returnUrl", "http://return.me.here/please"}
                }
            };
            var authTicket = new AuthenticationTicket(user, authProps, "ext-scheme");
            var successfulAuthResult = AuthenticateResult.Success(authTicket);

            var newInternalUser = new IdentifiedUser("08089403198", "pseudo-1");

            var automocker = new AutoMocker();

            automocker
                .Setup<IOptions<InteractionConfig>, InteractionConfig>(x => x.Value)
                .Returns(new InteractionConfig
                {
                    RequireAuthorizationRequest = false
                });

            automocker.Setup<IMediator, Task<IdentifiedUser>>(x =>
                x.Send(It.Is<CreateFromExternalAuthentication.Command>(c => 
                    c.Provider == "ext-scheme" &&
                    claims.All(c1 => c.ExternalClaims.Any(c2 => c2.Type == c1.Type))
                ), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newInternalUser);

            var target = automocker.CreateInstance<ExternalService>();

            //Act
            var result = await target.ProcessExternalAuthentication(successfulAuthResult);

            //Assert
            result.HasValue.Should().BeTrue();
            var innerResult = result.ValueOrFailure();
            innerResult.IsUser.SubjectId.Should().Be(newInternalUser.Id.ToString());
            innerResult.UseNativeClientRedirect.Should().BeFalse();
        }

        [Test]
        public async Task ProcessExternalAuthentication_SuccessfulResultNativeClientContext_ReturnsResultWithNativeRedirect()
        {
            //Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[0]));
            var authProps = new AuthenticationProperties
            {
                Items =
                {
                    {"scheme", "ext-scheme"},
                    {"returnUrl", "~/authorize?requestId=123"}
                }
            };
            var authTicket = new AuthenticationTicket(user, authProps, "ext-scheme");
            var successfulAuthResult = AuthenticateResult.Success(authTicket);

            var newInternalUser = new IdentifiedUser("08089403198", "pseudo-1");

            var automocker = new AutoMocker();

            automocker.Setup<IMediator, Task<IdentifiedUser>>(x =>
                    x.Send(It.IsAny<CreateFromExternalAuthentication.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newInternalUser);

            automocker
                .Setup<IOptions<InteractionConfig>, InteractionConfig>(x => x.Value)
                .Returns(new InteractionConfig
                {
                    RequireAuthorizationRequest = false
                });

            automocker.Setup<IIdentityServerInteractionService, Task<AuthorizationRequest>>(x =>
                    x.GetAuthorizationContextAsync("~/authorize?requestId=123"))
                .ReturnsAsync(new AuthorizationRequest
                {
                    RedirectUri = "native-scheme://some-path",
                    Client = new Client
                    {
                        ClientId = "client-a"
                    }
                });

            var target = automocker.CreateInstance<ExternalService>();

            //Act
            var result = await target.ProcessExternalAuthentication(successfulAuthResult);

            //Assert
            result.HasValue.Should().BeTrue();
            var innerResult = result.ValueOrFailure();
            innerResult.UseNativeClientRedirect.Should().BeTrue();
        }

        [Test]
        public async Task ProcessExternalAuthentication_SuccessfulResultWithExtInfo_AddsRelevantInfoAsClaims()
        {
            //Arrange
            var idToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            var sessionId = "session-1";
            var claims = new[]
            {
                new Claim(JwtClaimTypes.SessionId, sessionId)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var authProps = new AuthenticationProperties
            {
                Items =
                {
                    {"scheme", "ext-scheme"},
                    {"returnUrl", "~/authorize?requestId=123"},
                    {".Token.id_token", idToken}
                }
            };
            var authTicket = new AuthenticationTicket(user, authProps, "ext-scheme");
            var successfulAuthResult = AuthenticateResult.Success(authTicket);

            var newInternalUser = new IdentifiedUser("08089403198", "pseudo-1");

            var automocker = new AutoMocker();

            automocker.Setup<IMediator, Task<IdentifiedUser>>(x =>
                    x.Send(It.IsAny<CreateFromExternalAuthentication.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newInternalUser);

            automocker
                .Setup<IOptions<InteractionConfig>, InteractionConfig>(x => x.Value)
                .Returns(new InteractionConfig
                {
                    RequireAuthorizationRequest = false
                });

            automocker.Setup<IIdentityServerInteractionService, Task<AuthorizationRequest>>(x =>
                    x.GetAuthorizationContextAsync("~/authorize?requestId=123"))
                .ReturnsAsync(new AuthorizationRequest
                {
                    RedirectUri = "native-scheme://some-path",
                    Client = new Client
                    {
                        ClientId = "client-a"
                    }
                });

            var target = automocker.CreateInstance<ExternalService>();

            //Act
            var result = await target.ProcessExternalAuthentication(successfulAuthResult);

            //Assert
            result.HasValue.Should().BeTrue();
            var innerResult = result.ValueOrFailure();
            innerResult.ExternalIdToken.Should().Be(idToken.Some());
            innerResult.IsUser.AdditionalClaims.Should()
                .Contain(c => c.Type == JwtClaimTypes.SessionId && c.Value == sessionId);
        }

        [Test]
        public async Task ProcessExternalAuthentication_AuthRequestRequiredButNotAvailable_ReturnsErrorResult()
        {
            //Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[0]));
            var authProps = new AuthenticationProperties
            {
                Items =
                {
                    {"scheme", "ext-scheme"},
                    {"returnUrl", "~/authorize?requestId=123"}
                }
            };
            var authTicket = new AuthenticationTicket(user, authProps, "ext-scheme");
            var successfulAuthResult = AuthenticateResult.Success(authTicket);

            var newInternalUser = new IdentifiedUser("08089403198", "pseudo-1");

            var automocker = new AutoMocker();

            automocker.Setup<IMediator, Task<IdentifiedUser>>(x =>
                    x.Send(It.IsAny<CreateFromExternalAuthentication.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newInternalUser);

            automocker
                .Setup<IOptions<InteractionConfig>, InteractionConfig>(x => x.Value)
                .Returns(new InteractionConfig
                {
                    RequireAuthorizationRequest = true
                });

            automocker.Setup<IIdentityServerInteractionService, Task<AuthorizationRequest>>(x =>
                    x.GetAuthorizationContextAsync("~/authorize?requestId=123"))
                .ReturnsAsync((AuthorizationRequest)null);

            var target = automocker.CreateInstance<ExternalService>();

            //Act
            var result = await target.ProcessExternalAuthentication(successfulAuthResult);

            //Assert
            result.Should().Be(Option.None<ExtAuthenticationResult, string>("A valid authorization request is required for login."));
        }
    }
}
