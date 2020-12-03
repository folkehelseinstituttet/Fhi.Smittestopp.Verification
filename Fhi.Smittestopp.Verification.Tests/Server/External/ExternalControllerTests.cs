using System;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Server.Account.ViewModels;
using Fhi.Smittestopp.Verification.Server.ExternalController;
using Fhi.Smittestopp.Verification.Server.ExternalController.Models;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using FluentAssertions;
using IdentityServer4;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Server.External
{
    [TestFixture]
    public class ExternalControllerTests
    {
        [Test]
        public void Challenge_RequestRejected_ThrowsError()
        {
            //Arrange
            var automocker = new AutoMocker();

            automocker.Setup<IExternalService, Option<ExtChallengeResult, string>>(x => x.CreateChallenge(It.Is<ExtChallengeRequest>(r =>
                r.Scheme == "some-scheme" && r.ReturnUrl == "/return/me/here".Some())))
                .Returns(Option.None<ExtChallengeResult, string>("rejected!"));

            var target = automocker.CreateInstance<ExternalController>().AddTestControllerContext();

            //Act / Assert
            Assert.Throws<Exception>(() => target.Challenge("some-scheme", "/return/me/here"));
        }

        [Test]
        public void Challenge_RequestAccepted_ReturnsChallengeResult()
        {
            //Arrange
            var automocker = new AutoMocker();

            automocker.Setup<IExternalService, Option<ExtChallengeResult, string>>(x => x.CreateChallenge(It.Is<ExtChallengeRequest>(r =>
                    r.Scheme == "some-scheme" && r.ReturnUrl == "/return/me/here".Some())))
                .Returns(new ExtChallengeResult
                {
                    TrustedReturnUrl = "/return/me/here",
                    Scheme = "some-scheme"
                }.Some<ExtChallengeResult, string>());

            var target = automocker.CreateInstance<ExternalController>().AddTestControllerContext();

            //Act
            var result = target.Challenge("some-scheme", "/return/me/here");

            //Assert
            result.Should().BeOfType<ChallengeResult>();
            var challenge = (ChallengeResult) result;
            challenge.Properties.Items.Should().Contain("returnUrl", "/return/me/here")
                .And.Contain("scheme", "some-scheme");
        }

        [Test]
        public void Callback_AuthResultRejected_ThrowsException()
        {
            //Arrange
            var automocker = new AutoMocker();

            automocker.Setup<IExternalService, Task<Option<ExtAuthenticationResult, string>>>(x =>
                    x.ProcessExternalAuthentication(It.IsAny<AuthenticateResult>()))
                .ReturnsAsync(Option.None<ExtAuthenticationResult, string>("Rejected."));

            var target = automocker.CreateInstance<ExternalController>().AddTestControllerContext(automocker);

            //Act / Assert
            Assert.ThrowsAsync<Exception>(() => target.Callback());
        }

        [Test]
        public async Task Callback_ValidAuthResultNotNativeClient_ReturnsDefaultRedirectResult()
        {
            //Arrange
            var automocker = new AutoMocker();

            automocker.Setup<IExternalService, Task<Option<ExtAuthenticationResult, string>>>(x =>
                    x.ProcessExternalAuthentication(It.IsAny<AuthenticateResult>()))
                .ReturnsAsync(new ExtAuthenticationResult
                {
                    ReturnUrl = "~/",
                    IsUser = new IdentityServerUser("user-1"),
                    ExternalIdToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c".Some(),
                    UseNativeClientRedirect = false

                }.Some<ExtAuthenticationResult, string>());
            var target = automocker.CreateInstance<ExternalController>().AddTestControllerContext(automocker);

            //Act
            var result = await target.Callback();

            //Assert
            result.Should().BeOfType<RedirectResult>();
            var rResult = (RedirectResult)result;
            rResult.Url.Should().Be("~/");
        }

        [Test]
        public async Task Callback_ValidAuthResultNativeClient_ReturnsNativeRedirectResult()
        {
            //Arrange
            var automocker = new AutoMocker();

            automocker.Setup<IExternalService, Task<Option<ExtAuthenticationResult, string>>>(x =>
                    x.ProcessExternalAuthentication(It.IsAny<AuthenticateResult>()))
                .ReturnsAsync(new ExtAuthenticationResult
                {
                    ReturnUrl = "native-scheme://return-here",
                    IsUser = new IdentityServerUser("user-1"),
                    ExternalIdToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c".Some(),
                    UseNativeClientRedirect = true

                }.Some<ExtAuthenticationResult, string>());
            var target = automocker.CreateInstance<ExternalController>().AddTestControllerContext(automocker);

            //Act
            var result = await target.Callback();

            //Assert
            result.Should().BeOfType<ViewResult>();
            var vResult = (ViewResult)result;
            vResult.ViewName.Should().Be("Redirect");
            vResult.Model.Should().BeOfType<RedirectViewModel>();
            ((RedirectViewModel)vResult.Model).RedirectUrl.Should().Be("native-scheme://return-here");
        }
    }
}
