using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Users;
using Fhi.Smittestopp.Verification.Server.Account.Models;
using Fhi.Smittestopp.Verification.Server.Account.ViewModels;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Optional;
using Optional.Async.Extensions;

namespace Fhi.Smittestopp.Verification.Server.Account
{
    public interface IAccountService
    {
        Task<LoginOptions> GetLoginOptions(string returnUrl);
        Task<LogoutOptions> GetLogoutOptions(string logoutId, Option<ClaimsPrincipal> user);

        Task<Option<LocalLoginResult, string>> AttemptLocalLogin(string pincode, string returnUrl);

        Task<LoggedOutResult> LogOut(string logoutId, Option<ClaimsPrincipal> user, Func<string, Task<bool>> schemeSupportsSignoutLookup);
        Task<CancelLoginResult> CancelLogin(string returnUrl);
    }

    public class AccountService : IAccountService
    {
        private readonly IClientStore _clientStore;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IMediator _mediator;
        private readonly IEventService _events;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IActionContextAccessor _actionContextAccessor;

        public AccountService(IClientStore clientStore, IIdentityServerInteractionService interaction, IAuthenticationSchemeProvider schemeProvider, IMediator mediator, IEventService events, IUrlHelperFactory urlHelperFactory, IActionContextAccessor actionContextAccessor)
        {
            _clientStore = clientStore;
            _interaction = interaction;
            _schemeProvider = schemeProvider;
            _mediator = mediator;
            _events = events;
            _urlHelperFactory = urlHelperFactory;
            _actionContextAccessor = actionContextAccessor;
        }

        public async Task<LoginOptions> GetLoginOptions(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            // handle requests for specific identity provider
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                var localLoginRequested = context.IdP == IdentityServerConstants.LocalIdentityProvider;

                // this is meant to short circuit the UI and only trigger the one external IdP
                return new LoginOptions
                {
                    EnableLocalLogin = localLoginRequested,
                    LoginHint = context.LoginHint,
                    ExternalProviders = localLoginRequested
                        ? Enumerable.Empty<ExternalProvider>()
                        : new[] { new ExternalProvider { AuthenticationScheme = context.IdP } }
                };
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null)
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName ?? x.Name,
                    AuthenticationScheme = x.Name
                }).ToList();

            var allowLocal = true;
            if (context?.Client.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            return new LoginOptions
            {
                EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                LoginHint = context?.LoginHint,
                ExternalProviders = providers.ToArray()
            };
        }

        public async Task<LogoutOptions> GetLogoutOptions(string logoutId, Option<ClaimsPrincipal> user)
        {
            var isAuthenticated = user.Map(u => u.Identity.IsAuthenticated).ValueOr(false);
            if (!isAuthenticated)
            {
                // if the user is not authenticated, then just show logged out page
                return new LogoutOptions
                {
                    ShowLogoutPrompt = false
                };
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                return new LogoutOptions
                {
                    ShowLogoutPrompt = false
                };
            }

            return new LogoutOptions
            {
                ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt
            };
        }

        public async Task<Option<LocalLoginResult, string>> AttemptLocalLogin(string pincode, string returnUrl)
        {
            // check if we are in the context of an authorization request
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            var trustedReturlUrl = Option.None<string>();
            if (context != null)
            {
                // since we have a context, we know the return url is safe
                trustedReturlUrl = returnUrl.Some();
            }
            else if (_urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext).IsLocalUrl(returnUrl))
            {
                trustedReturlUrl = returnUrl.Some();
            }
            else if (!string.IsNullOrEmpty(returnUrl))
            {
                // user might have clicked on a malicious link - should be logged
                throw new Exception("invalid return URL");
            }

            // Try logging in user based on PIN-code
            return await _mediator.Send(new CreateFromPinCode.Command(pincode)).MatchAsync(
                none: async () =>
                {
                    // Raise failed login event
                    await _events.RaiseAsync(new UserLoginFailureEvent(pincode, "invalid PIN-code", clientId: context?.Client.ClientId));
                    return Option.None<LocalLoginResult, string>(AccountOptions.InvalidCredentialsErrorMessage);
                },
                some: async pinUser =>
                {
                    await _events.RaiseAsync(new UserLoginSuccessEvent(pinUser.Id.ToString(), pinUser.Id.ToString(), pinUser.DisplayName,
                        clientId: context?.Client.ClientId));


                    return new LocalLoginResult
                    {
                        // create identity server user with subject ID, username and claims
                        IsUser = new IdentityServerUser(pinUser.Id.ToString())
                        {
                            DisplayName = pinUser.DisplayName,
                            AdditionalClaims = pinUser.GetCustomClaims().ToList()
                        },
                        TrustedReturnUrl = trustedReturlUrl,
                        UseNativeClientRedirect = context?.IsNativeClient() ?? false
                    }.Some<LocalLoginResult, string>();
                });
        }

        public async Task<CancelLoginResult> CancelLogin(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            // the user clicked the "cancel" button
            if (context != null)
            {
                // if the user cancels, send a result back into IdentityServer as if they 
                // denied the consent (even if this client does not require consent).
                // this will send back an access denied OIDC error response to the client.
                await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

                return new CancelLoginResult
                {
                    UseNativeClientRedirect = context.IsNativeClient(),
                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    TrustedReturnUrl = returnUrl.Some()
                };
            }

            // since we don't have a valid context, then we just go back to the home page
            return new CancelLoginResult();
        }

        public async Task<LoggedOutResult> LogOut(string logoutId, Option<ClaimsPrincipal> user, Func<string, Task<bool>> schemeSupportsSignoutLookup)
        {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logoutRequest = (await _interaction.GetLogoutContextAsync(logoutId)).SomeNotNull();

            var logoutResult = new LoggedOutResult
            {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logoutRequest.Map(x => x.PostLogoutRedirectUri),
                ClientName = logoutRequest.FlatMap(x => x.ClientName.SomeNotNull().Filter(cn => !string.IsNullOrEmpty(cn)).Else(x.ClientId.SomeNotNull())),
                SignOutIframeUrl = logoutRequest.Map(x => x.SignOutIFrameUrl),
                LogoutId = logoutId
            };

            // perform ext ID-provider signout if applicable
            await user
                .FlatMap(u => u.FindFirst(JwtClaimTypes.IdentityProvider).SomeNotNull())
                .Filter(idpClaim => idpClaim.Value != IdentityServerConstants.LocalIdentityProvider)
                .MatchSomeAsync(async extIdpClaim =>
                {
                    if (await schemeSupportsSignoutLookup(extIdpClaim.Value))
                    {
                        if (logoutResult.LogoutId == null)
                        {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            logoutResult.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        logoutResult.ExternalAuthenticationScheme = extIdpClaim.Value;
                    }
                });


            // raise the logout event
            await user.MatchSomeAsync(u => _events.RaiseAsync(new UserLogoutSuccessEvent(u.GetSubjectId(), u.GetDisplayName())));

            return logoutResult;
        }
    }
}
