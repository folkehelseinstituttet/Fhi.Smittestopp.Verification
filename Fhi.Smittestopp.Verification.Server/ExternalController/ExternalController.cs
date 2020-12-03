using System;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Server.ExternalController.Models;
using IdentityServer4;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Optional;

namespace Fhi.Smittestopp.Verification.Server.ExternalController
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class ExternalController : Controller
    {
        private readonly IExternalService _externalService;

        public ExternalController(
            IExternalService externalService)
        {
            _externalService = externalService;
        }

        /// <summary>
        /// initiate roundtrip to external authentication provider
        /// </summary>
        [HttpGet]
        public IActionResult Challenge(string scheme, string returnUrl)
        {
            var result = _externalService.CreateChallenge(new ExtChallengeRequest
            {
                Scheme = scheme,
                ReturnUrl = returnUrl.SomeNotNull()
            });

            return result.Match(
                none: e => throw new Exception(e),
                some: r =>
                {
                    var props = new AuthenticationProperties
                    {
                        RedirectUri = Url.Action(nameof(Callback)),
                        Items =
                        {
                            { "returnUrl", r.TrustedReturnUrl },
                            { "scheme", r.Scheme },
                        }
                    };

                    return Challenge(props, scheme);
                }
            );
            
        }

        /// <summary>
        /// Post processing of external authentication
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Callback()
        {
            // read external identity from the temporary cookie
            var externalResult = await HttpContext.AuthenticateAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);

            var result = (await _externalService.ProcessExternalAuthentication(externalResult))
                .ValueOr(e => throw new Exception(e));

            var localSignInProps = new AuthenticationProperties { IsPersistent = false };
            result.ExternalIdToken.MatchSome(extIdToken =>
            {
                // if the external provider issued an id_token, we'll keep it for signout
                localSignInProps.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = extIdToken } });
            });

            await HttpContext.SignInAsync(result.IsUser, localSignInProps);

            // delete temporary cookie used during external authentication
            await HttpContext.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);

            return result.UseNativeClientRedirect
                ? this.LoadingPage("Redirect", result.ReturnUrl)
                : Redirect(result.ReturnUrl);
        }
    }
}