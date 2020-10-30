using System.Collections.Generic;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Server.Account;
using Fhi.Smittestopp.Verification.Server.Account.Models;
using Fhi.Smittestopp.Verification.Server.Account.ViewModels;
using FluentAssertions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
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

            automocker.Setup<IAccountService, Task<LoginOptions>>(x =>
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
                });

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

            automocker.Setup<IAccountService, Task<LoginOptions>>(x =>
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
                });

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
        public async Task Login_GivenInvalidModelState_ReturnsViewResult()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<LoginOptions>>(x =>
                    x.GetLoginOptions("/auth/?authRequest=123"))
                .ReturnsAsync(new LoginOptions
                {
                    EnableLocalLogin = true,
                    ExternalProviders = new ExternalProvider[0]
                });

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
        public async Task Login_GivenUnsuccessfullPinValidation_ReturnsViewResult()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IAccountService, Task<LoginOptions>>(x =>
                    x.GetLoginOptions("/auth/?authRequest=123"))
                .ReturnsAsync(new LoginOptions
                {
                    EnableLocalLogin = true,
                    ExternalProviders = new ExternalProvider[0]
                });

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
    }
}
