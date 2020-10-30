using System;

namespace Fhi.Smittestopp.Verification.Domain.Interfaces
{
    public interface IVerificationLimitConfig
    {
        int MaxVerificationsAllowed { get; }
        TimeSpan MaxLimitDuration { get; }
    }
}