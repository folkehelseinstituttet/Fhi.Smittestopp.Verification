using IdentityServer4;
using Optional;

namespace Fhi.Smittestopp.Verification.Server.Account.Models
{
    public class LocalLoginResult
    {
        public bool UseNativeClientRedirect { get; set; }
        public Option<string> TrustedReturnUrl { get; set; }
        public IdentityServerUser IsUser { get; set; }
    }
}
