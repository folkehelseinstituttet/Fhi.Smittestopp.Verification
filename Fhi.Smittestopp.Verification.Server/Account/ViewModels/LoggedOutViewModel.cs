using Fhi.Smittestopp.Verification.Server.Account.Models;
using Optional.Unsafe;

namespace Fhi.Smittestopp.Verification.Server.Account.ViewModels
{
    public class LoggedOutViewModel
    {
        private readonly LoggedOutResult _logoutResult;

        public LoggedOutViewModel(LoggedOutResult logoutResult)
        {
            _logoutResult = logoutResult;
        }

        public string PostLogoutRedirectUri => _logoutResult.PostLogoutRedirectUri.ValueOrDefault();
        public string ClientName => _logoutResult.ClientName.ValueOrDefault();
        public string SignOutIframeUrl => _logoutResult.SignOutIframeUrl.ValueOrDefault();
        public bool AutomaticRedirectAfterSignOut => _logoutResult.AutomaticRedirectAfterSignOut;
        public string LogoutId => _logoutResult.LogoutId;
    }
}