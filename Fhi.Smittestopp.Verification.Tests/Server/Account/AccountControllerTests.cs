using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Server.Account;
using Fhi.Smittestopp.Verification.Server.Account.Models;
using Fhi.Smittestopp.Verification.Server.Account.ViewModels;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using FluentAssertions;
using IdentityServer4;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Server.Account
{
    [TestFixture]
    public class AccountControllerTests
    {
        [Test]
        public async Task Login_SingleExternalLoginOnly_ReturnsRedirectToExtLoginResult()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<Option<LoginOptions, string>>>(x =>
                    x.GetLoginOptions("/auth/?authRequest=123"))
                .ReturnsAsync(new LoginOptions
                {
                    EnableLocalLogin = false,
                    ExternalProviders = new []
                    {
                        new ExternalProvider
                        {
                            AuthenticationScheme = "ext-provider",
                            DisplayName = "An external provider"
                        }
                    }
                }.Some<LoginOptions, string>());

            var target = automocker.CreateInstance<AccountController>();

            var result = await target.Login("/auth/?authRequest=123");

            result.Should().BeOfType<RedirectToActionResult>();
            var redirResult = (RedirectToActionResult) result;
            redirResult.ControllerName.Should().Be("External");
            redirResult.ActionName.Should().Be("Challenge");
            redirResult.ActionName.Should().Be("Challenge");
            redirResult.RouteValues.Should()
                .Contain(new KeyValuePair<string, object>("returnUrl", "/auth/?authRequest=123"))
                .And.Contain(new KeyValuePair<string, object>("scheme", "ext-provider"));
        }

        [Test]
        public async Task Login_LocalAndExternal_ReturnsViewResult()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<Option<LoginOptions, string>>>(x =>
                    x.GetLoginOptions("/auth/?authRequest=123"))
                .ReturnsAsync(new LoginOptions
                {
                    EnableLocalLogin = true,
                    ExternalProviders = new[]
                    {
                        new ExternalProvider
                        {
                            AuthenticationScheme = "ext-provider-a",
                            DisplayName = "External provider A"
                        },
                        new ExternalProvider
                        {
                            AuthenticationScheme = "ext-provider-b",
                            DisplayName = "External provider B"
                        }
                    }
                }.Some<LoginOptions, string>());

            var target = automocker.CreateInstance<AccountController>();

            var result = await target.Login("/auth/?authRequest=123");

            result.Should().BeOfType<ViewResult>();
            var vResult = (ViewResult)result;
            vResult.ViewName.Should().BeNull();
            vResult.Model.Should().BeOfType<LoginViewModel>();
            var vm = (LoginViewModel) vResult.Model;
            vm.ReturnUrl.Should().Be("/auth/?authRequest=123");
            vm.EnableLocalLogin.Should().BeTrue();
            vm.VisibleExternalProviders.Should()
                .Contain(x => x.AuthenticationScheme == "ext-provider-a")
                .And.Contain(x => x.AuthenticationScheme == "ext-provider-b");
        }

        [Test]
        public void Login_LoginOptionsFails_ThrowsException()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<Option<LoginOptions, string>>>(x =>
                    x.GetLoginOptions("/auth/?authRequest=123"))
                .ReturnsAsync(Option.None<LoginOptions, string>("rejected."));

            var target = automocker.CreateInstance<AccountController>();

            Assert.ThrowsAsync<Exception>(() => target.Login("/auth/?authRequest=123"));
        }

        [Test]
        public async Task Login_GivenInvalidModelState_ReturnsViewResult()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<Option<LoginOptions, string>>>(x =>
                    x.GetLoginOptions("/auth/?authRequest=123"))
                .ReturnsAsync(new LoginOptions
                {
                    EnableLocalLogin = true,
                    ExternalProviders = new ExternalProvider[0]
                }.Some<LoginOptions, string>());

            var target = automocker.CreateInstance<AccountController>();

            target.ModelState.AddModelError("PinCode", "PinCode is required");

            var result = await target.Login(new LoginInputModel
            {
                ReturnUrl = "/auth/?authRequest=123",
                PinCode = null
            });

            result.Should().BeOfType<ViewResult>();
            var vResult = (ViewResult)result;
            vResult.ViewName.Should().BeNull();
            vResult.Model.Should().BeOfType<LoginViewModel>();
            var vm = (LoginViewModel)vResult.Model;
            vm.PinCode.Should().BeNull();
            vm.ReturnUrl.Should().Be("/auth/?authRequest=123");
            vm.EnableLocalLogin.Should().BeTrue();
            vm.VisibleExternalProviders.Should().BeEmpty();
        }

        [Test]
        public void Login_GivenInvalidModelStateAuthRequestRequiredMissing_ThrowsException()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<Option<LoginOptions, string>>>(x =>
                    x.GetLoginOptions("/auth/?authRequest=123"))
                .ReturnsAsync(Option.None<LoginOptions, string>("rejected."));

            var target = automocker.CreateInstance<AccountController>();

            target.ModelState.AddModelError("PinCode", "PinCode is required");

            Assert.ThrowsAsync<Exception>(() => target.Login(new LoginInputModel
            {
                ReturnUrl = "/auth/?authRequest=123",
                PinCode = null
            }));
        }

        [Test]
        public async Task Login_GivenUnsuccessfulPinValidation_ReturnsViewResult()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<Option<LoginOptions, string>>>(x =>
                    x.GetLoginOptions("/auth/?authRequest=123"))
                .ReturnsAsync(new LoginOptions
                {
                    EnableLocalLogin = true,
                    ExternalProviders = new ExternalProvider[0]
                }.Some<LoginOptions, string>());

            automocker.Setup<IAccountService, Task<Option<LocalLoginResult, string>>>(x =>
                    x.AttemptLocalLogin("12345", "/auth/?authRequest=123"))
                .ReturnsAsync(Option.None<LocalLoginResult, string>("Not a valid pin"));

            var target = automocker.CreateInstance<AccountController>();

            var result = await target.Login(new LoginInputModel
            {
                ReturnUrl = "/auth/?authRequest=123",
                PinCode = "12345"
            });

            result.Should().BeOfType<ViewResult>();
            var vResult = (ViewResult)result;
            vResult.ViewName.Should().BeNull();
            vResult.Model.Should().BeOfType<LoginViewModel>();
            var vm = (LoginViewModel)vResult.Model;
            vm.PinCode.Should().Be("12345");
            vm.ReturnUrl.Should().Be("/auth/?authRequest=123");
            vm.EnableLocalLogin.Should().BeTrue();
            vm.VisibleExternalProviders.Should().BeEmpty();
        }

        [Test]
        public void Login_GivenUnsuccessfulPinValidationAuthRequestRequiredMissing_ThrowsException()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<Option<LocalLoginResult, string>>>(x =>
                    x.AttemptLocalLogin("12345", "/auth/?authRequest=123"))
                .ReturnsAsync(Option.None<LocalLoginResult, string>("Not a valid pin"));

            automocker.Setup<IAccountService, Task<Option<LoginOptions, string>>>(x =>
                    x.GetLoginOptions("/auth/?authRequest=123"))
                .ReturnsAsync(Option.None<LoginOptions, string>("rejected."));

            var target = automocker.CreateInstance<AccountController>();

            Assert.ThrowsAsync<Exception>(() => target.Login(new LoginInputModel
            {
                ReturnUrl = "/auth/?authRequest=123",
                PinCode = "12345"
            }));
        }

        [Test]
        public async Task Login_GivenSuccessfulPinValidationNonNativeClient_ReturnsRedirectResult()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<Option<LocalLoginResult, string>>>(x =>
                    x.AttemptLocalLogin("12345", null))
                .ReturnsAsync(new LocalLoginResult
                {
                    IsUser = new IdentityServerUser("testuser"),
                    UseNativeClientRedirect = false
                }.Some<LocalLoginResult, string>());

            var target = automocker.CreateInstance<AccountController>().AddTestControllerContext();

            var result = await target.Login(new LoginInputModel
            {
                PinCode = "12345"
            });

            result.Should().BeOfType<RedirectResult>();
            var rResult = (RedirectResult)result;
            rResult.Url.Should().Be("~/");
        }

        [Test]
        public async Task Login_GivenSuccessfulPinValidationNativeClient_ReturnsNativeRedirectResult()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<Option<LocalLoginResult, string>>>(x =>
                    x.AttemptLocalLogin("12345", "native-scheme://return-here"))
                .ReturnsAsync(new LocalLoginResult
                {
                    IsUser = new IdentityServerUser("testuser"),
                    UseNativeClientRedirect = true,
                    TrustedReturnUrl = "native-scheme://return-here".Some()
                }.Some<LocalLoginResult, string>());

            var target = automocker.CreateInstance<AccountController>().AddTestControllerContext();

            var result = await target.Login(new LoginInputModel
            {
                PinCode = "12345",
                ReturnUrl = "native-scheme://return-here"
            });

            result.Should().BeOfType<ViewResult>();
            var vResult = (ViewResult)result;
            vResult.ViewName.Should().Be("Redirect");
            vResult.Model.Should().BeOfType<RedirectViewModel>();
            ((RedirectViewModel) vResult.Model).RedirectUrl.Should().Be("native-scheme://return-here");
        }

        [Test]
        public async Task Cancel_GivenNoTrustedReturnUrl_ReturnRedirectUrlToRoot()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<CancelLoginResult>>(x => x.CancelLogin(null))
                .ReturnsAsync(new CancelLoginResult
                {
                    UseNativeClientRedirect = false
                });

            var target = automocker.CreateInstance<AccountController>().AddTestControllerContext();

            var result = await target.Cancel(new CancelLoginInputModel());

            result.Should().BeOfType<RedirectResult>();
            var rResult = (RedirectResult)result;
            rResult.Url.Should().Be("~/");
        }

        [Test]
        public async Task Cancel_GivenTrustedNativeReturnUrl_ReturnNativeRedirectResult()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<CancelLoginResult>>(x => x.CancelLogin("native-scheme://return-here"))
                .ReturnsAsync(new CancelLoginResult
                {
                    UseNativeClientRedirect = true,
                    TrustedReturnUrl = "native-scheme://return-here".Some()
                });

            var target = automocker.CreateInstance<AccountController>().AddTestControllerContext();

            var result = await target.Cancel(new CancelLoginInputModel
            {
                ReturnUrl = "native-scheme://return-here"
            });

            result.Should().BeOfType<ViewResult>();
            var vResult = (ViewResult)result;
            vResult.ViewName.Should().Be("Redirect");
            vResult.Model.Should().BeOfType<RedirectViewModel>();
            ((RedirectViewModel)vResult.Model).RedirectUrl.Should().Be("native-scheme://return-here");
        }

        [Test]
        public async Task Logout_GivenShowLogoutPromptOptions_ReturnsLogoutViewResult()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<LogoutOptions>>(x => x.GetLogoutOptions("logout-1", It.IsAny<Option<ClaimsPrincipal>>()))
                .ReturnsAsync(new LogoutOptions
                {
                    ShowLogoutPrompt = true
                });

            var target = automocker.CreateInstance<AccountController>().AddTestControllerContext();

            var result = await target.Logout("logout-1");

            result.Should().BeOfType<ViewResult>();
            var vResult = (ViewResult)result;
            vResult.ViewName.Should().BeNull();
            vResult.Model.Should().BeOfType<LogoutViewModel>();
            ((LogoutViewModel)vResult.Model).LogoutId.Should().Be("logout-1");
        }

        [Test]
        public async Task Logout_GivenDontShowLogoutPromptOptions_LogsOutAndReturnsLoggetOutResult()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<LogoutOptions>>(x => x.GetLogoutOptions("logout-1", It.IsAny<Option<ClaimsPrincipal>>()))
                .ReturnsAsync(new LogoutOptions
                {
                    ShowLogoutPrompt = false
                });

            automocker.Setup<IAccountService, Task<LoggedOutResult>>(x => x.LogOut(
                    "logout-1",
                    It.IsAny<Option<ClaimsPrincipal>>(),
                    It.IsAny<Func<string, Task<bool>>>()))
                .ReturnsAsync(new LoggedOutResult
                {
                    LogoutId = "logout-1"
                });

            var target = automocker.CreateInstance<AccountController>().AddTestControllerContext();

            var result = await target.Logout("logout-1");

            result.Should().BeOfType<ViewResult>();
            var vResult = (ViewResult)result;
            vResult.ViewName.Should().Be("LoggedOut");
            vResult.Model.Should().BeOfType<LoggedOutViewModel>();
            ((LoggedOutViewModel)vResult.Model).LogoutId.Should().Be("logout-1");
        }

        [Test]
        public async Task Logout_TriggerExternalLogout_PerformsExternalLogout()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<LoggedOutResult>>(x => x.LogOut(
                    "logout-2",
                    It.IsAny<Option<ClaimsPrincipal>>(),
                    It.IsAny<Func<string, Task<bool>>>()))
                .ReturnsAsync(new LoggedOutResult
                {
                    LogoutId = "logout-2",
                    ExternalAuthenticationScheme = "ext-provider"
                });

            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("/account/logout/?logoutId=logout-2");

            automocker
                .Setup<IUrlHelperFactory, IUrlHelper>(x => x.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            var target = automocker.CreateInstance<AccountController>().AddTestControllerContext(automocker);

            var result = await target.Logout(new LogoutInputModel
            {
                LogoutId = "logout-2"
            });

            result.Should().BeOfType<SignOutResult>();
            var soResult = (SignOutResult)result;
            soResult.Properties.RedirectUri.Should().BeEquivalentTo("/account/logout/?logoutId=logout-2");
            soResult.AuthenticationSchemes.Should().Contain("ext-provider");
        }

        [Test]
        public void AccessDenied_ReturnsViewResult()
        {
            var automocker = new AutoMocker();

            var target = automocker.CreateInstance<AccountController>().AddTestControllerContext();

            var result = target.AccessDenied();

            result.Should().BeOfType<ViewResult>();
            var vResult = (ViewResult)result;
            vResult.ViewName.Should().Be(null);
            vResult.Model.Should().BeNull();
        }
    }
}
