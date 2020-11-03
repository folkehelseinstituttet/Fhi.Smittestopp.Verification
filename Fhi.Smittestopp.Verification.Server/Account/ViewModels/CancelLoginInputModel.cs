using Optional;

namespace Fhi.Smittestopp.Verification.Server.Account.ViewModels
{
    public class CancelLoginInputModel
    {
        public string ReturnUrl { get; set; }
    }

    public class CancelLoginResult
    {
        public bool UseNativeClientRedirect { get; set; }
        public Option<string> TrustedReturnUrl { get; set; }
    }
}