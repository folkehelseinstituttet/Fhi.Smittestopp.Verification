namespace Fhi.Smittestopp.Verification.Domain.Constants
{
    public class DkSmittestopClaims
    {
        /// <summary>
        /// The COVID19 infection status
        /// </summary>
        public const string Covid19Status = "covid19_status";
        /// <summary>
        /// Blocked status of the user
        /// - true: User has not exceeded max number of verifications, and should not be given access to upload keys
        /// - false: User has not exceeded max number of verifications, and should be given access to upload keys
        /// </summary>
        public const string Covid19Blocked = "covid19_blokeret";
        /// <summary>
        /// Infection start date, based on test sampling date
        /// </summary>
        public const string Covid19InfectionStart = "covid19_smitte_start";
        /// <summary>
        /// Infection end date, not used in Smittestopp (NO app)
        /// </summary>
        public const string Covid19InfectionEnd = "covid19_smitte_stop";
        /// <summary>
        /// Timespan verification limit count is measured for (in hours)
        /// </summary>
        public const string Covid19LimitDuration = "covid19_limit_duration";
        /// <summary>
        /// Maximum number of verifications allowed per timespan.
        /// </summary>
        public const string Covid19LimitCount = "covid19_limit_count";

        public class StatusValues
        {
            /// <summary>
            /// If positive test within last 14 days and person above 16 years of age
            /// </summary>
            public const string Positive = "positiv";
            /// <summary>
            /// No positive test within last 14 days, or below 16 year of age
            /// </summary>
            public const string Negative = "negativ";
            /// <summary>
            /// If verification lookup for authenticated user fails due to transient errors
            /// </summary>
            public const string Unknwon = "ukendt";
        }
    }
}
