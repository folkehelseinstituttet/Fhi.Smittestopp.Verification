namespace Fhi.Smittestopp.Verification.Domain.Constants
{
    public static class VerificationScopes
    {
        public const string UploadApi = "upload-api";
        public const string SkipMsisLookup = "no-msis";
        public const string VerificationInfo = "verification-info";
        /// <summary>
        /// Scope compatible with DK-app Smitte|Stop
        /// </summary>
        public const string DkSmittestop = "smittestop";
    }
}
