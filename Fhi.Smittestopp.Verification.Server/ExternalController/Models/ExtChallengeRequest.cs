using Optional;

namespace Fhi.Smittestopp.Verification.Server.ExternalController.Models
{
    public class ExtChallengeRequest
    {
        public string Scheme { get; set; }
        public Option<string> ReturnUrl { get; set; }
    }
}
