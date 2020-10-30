using Optional;

namespace Fhi.Smittestopp.Verification.Server.Account.Models
{
    public class LoggedOutResult
    {
        public string LogoutId { get; set; }
        public Option<string> SignOutIframeUrl { get; set; }
        public Option<string> ClientName { get; set; }
        public Option<string> PostLogoutRedirectUri { get; set; }
        public bool AutomaticRedirectAfterSignOut { get; set; }
        public string ExternalAuthenticationScheme { get; set; }
        public bool TriggerExternalSignout => ExternalAuthenticationScheme != null;
    }
}
