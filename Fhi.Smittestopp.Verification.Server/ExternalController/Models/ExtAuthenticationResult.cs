using IdentityServer4;
using Optional;

namespace Fhi.Smittestopp.Verification.Server.ExternalController.Models
{
    public class ExtAuthenticationResult
    {
        public bool UseNativeClientRedirect { get; set; }
        public string ReturnUrl { get; set; }
        public IdentityServerUser IsUser { get; set; }
        public Option<string> ExternalIdToken { get; set; }
    }
}
