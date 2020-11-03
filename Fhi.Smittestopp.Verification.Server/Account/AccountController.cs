using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Server.Account.Models;
using Fhi.Smittestopp.Verification.Server.Account.ViewModels;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Optional;
using Optional.Async.Extensions;

namespace Fhi.Smittestopp.Verification.Server.Account
{
    /// <summary>
    /// Controller for handling login and logout
    /// </summary>
    [SecurityHeaders]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountController(
            IAccountService accountService)
        {
            _accountService = accountService;
        }

        /// <summary>
        /// Entry point into the login workflow
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            // get login options for the current login request
            var options = await _accountService.GetLoginOptions(returnUrl);

            if (options.IsExternalLoginOnly)
            {
                // we only have one option for logging in and it's an external provider
                return RedirectToAction("Challenge", "External", new { scheme = options.ExternalLoginScheme, returnUrl });
            }

            return View(new LoginViewModel(returnUrl, options));
        }

        /// <summary>
        /// Handle postback from PIN login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel model)
        {
            if (!ModelState.IsValid)
            {
                // Invalid inputs provided, show form with errors from ModelState
                var options = await _accountService.GetLoginOptions(model.ReturnUrl);
                return View(new LoginViewModel(model.ReturnUrl, options)
                {
                    PinCode = model.PinCode
                });
            }

            return await _accountService.AttemptLocalLogin(model.PinCode, model.ReturnUrl).MatchAsync<LocalLoginResult, string, IActionResult>(
                none: async error =>
                {
                    // Raise failed login event
                    ModelState.AddModelError(string.Empty, error);

                    // Show login once again
                    var options = await _accountService.GetLoginOptions(model.ReturnUrl);
                    return View(new LoginViewModel(model.ReturnUrl, options)
                    {
                        PinCode = model.PinCode
                    });
                },
                some: async loginResult =>
                {
                    await HttpContext.SignInAsync(loginResult.IsUser);

                    // Return to trusted url, or just return to the home page
                    var trustedReturnUrl = loginResult.TrustedReturnUrl.ValueOr("~/");

                    return loginResult.UseNativeClientRedirect
                        ? this.LoadingPage("Redirect", trustedReturnUrl)
                        : Redirect(trustedReturnUrl);
                }
            );
        }


        /// <summary>
        /// Handle postback from cancel login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(CancelLoginInputModel model)
        {
            var cancelResult = await _accountService.CancelLogin(model.ReturnUrl);

            // Return to trusted url, or just return to the home page
            var returnUrl = cancelResult.TrustedReturnUrl.ValueOr("~/");

            if (cancelResult.UseNativeClientRedirect)
            {
                // The client is native, so this change in how to
                // return the response is for better UX for the end user.
                return this.LoadingPage("Redirect", returnUrl);
            }

            return Redirect(returnUrl);
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            // get logout options for logout request
            var logoutOptions = await _accountService.GetLogoutOptions(logoutId, User.SomeNotNull());

            if (!logoutOptions.ShowLogoutPrompt)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.
                return await Logout(new LogoutInputModel
                {
                    LogoutId = logoutId
                });
            }

            return View(new LogoutViewModel(logoutId));
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            var authenticatedUser = User.SomeNotNull().Filter(u => u.Identity.IsAuthenticated);

            var logoutResult = await _accountService.LogOut(model.LogoutId, authenticatedUser,
                idp => HttpContext.GetSchemeSupportsSignOutAsync(idp));

            // delete local authentication cookie
            await authenticatedUser.MatchSomeAsync(u => HttpContext.SignOutAsync());

            // check if we need to trigger sign-out at an upstream identity provider
            if (logoutResult.TriggerExternalSignout)
            {
                // build a return URL so the upstream provider will redirect back
                // to us after the user has logged out. this allows us to then
                // complete our single sign-out processing.
                string url = Url.Action("Logout", new { logoutId = logoutResult.LogoutId });

                // this triggers a redirect to the external provider for sign-out
                return SignOut(new AuthenticationProperties { RedirectUri = url }, logoutResult.ExternalAuthenticationScheme);
            }

            return View("LoggedOut", new LoggedOutViewModel(logoutResult));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
