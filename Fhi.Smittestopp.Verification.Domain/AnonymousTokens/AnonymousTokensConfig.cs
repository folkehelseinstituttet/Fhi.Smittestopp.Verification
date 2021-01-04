using System;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public class AnonymousTokensConfig
    {
        public bool Enabled { get; set; }
        public string CurveName { get; set; } = "P-256";
        public string MasterKeyCertId { get; set; }
        public bool KeyRotationEnabled { get; set; }
        public TimeSpan KeyRotationInterval { get; set; } = TimeSpan.FromDays(3);
        public TimeSpan KeyRotationRollover { get; set; } = TimeSpan.FromHours(1);
    }
}
