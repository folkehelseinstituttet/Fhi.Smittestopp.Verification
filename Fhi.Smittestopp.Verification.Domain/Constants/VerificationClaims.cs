namespace Fhi.Smittestopp.Verification.Domain.Constants
{
    public static class VerificationClaims
    {
        public const string VerifiedPositiveTestDate = "vptd";
        public const string AnonymousTokenAvailable = "covid19_at_available";

        public class AnonymousTokenAvailableValues
        {
            public const string IsAvailable = "true";
        }
    }
}
