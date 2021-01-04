namespace Fhi.Smittestopp.Verification.Domain.Constants
{
    public static class VerificationClaims
    {
        public const string VerifiedPositiveTestDate = "vptd";
        public const string AnonymousToken = "covid19_anonymous_token";

        public class AnonymousTokenValues
        {
            public const string Available = "available";
        }
    }
}
