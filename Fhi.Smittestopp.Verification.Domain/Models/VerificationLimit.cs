using System;
using System.Collections.Generic;
using System.Linq;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public class VerificationLimit : IVerificationLimit
    {
        public VerificationLimit(IOptions<VerificationLimitConfig> config)
        {
            Config = config.Value;
        }

        public IVerificationLimitConfig Config { get; }

        public bool HasExceededLimit(IEnumerable<VerificationRecord> priorVerifications)
        {
            return priorVerifications.Count(x => x.VerifiedAtTime.ToUniversalTime() >= RecordsCutoff) >= Config.MaxVerificationsAllowed;
        }

        public DateTime RecordsCutoff => DateTime.UtcNow - Config.MaxLimitDuration;
    }

    public class VerificationLimitConfig : IVerificationLimitConfig
    {
        public int MaxVerificationsAllowed { get; set; } = 3;
        public TimeSpan MaxLimitDuration { get; set; } = TimeSpan.FromDays(1);
    }
}
