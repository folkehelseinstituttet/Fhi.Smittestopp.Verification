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

        public bool HasExceededLimit(IEnumerable<VerificationRecord> records)
        {
            var verificationLimitStartTime = DateTime.UtcNow - Config.MaxLimitDuration;
            return records.Count(x => x.VerifiedAtTime.ToUniversalTime() >= verificationLimitStartTime) >
                   Config.MaxVerificationsAllowed;
        }
    }

    public class VerificationLimitConfig : IVerificationLimitConfig
    {
        public int MaxVerificationsAllowed { get; set; } = 3;
        public TimeSpan MaxLimitDuration { get; set; } = TimeSpan.FromDays(1);
    }
}
