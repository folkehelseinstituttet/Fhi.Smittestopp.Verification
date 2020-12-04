using System;
using System.Collections.Generic;
using Fhi.Smittestopp.Verification.Domain.Models;

namespace Fhi.Smittestopp.Verification.Domain.Interfaces
{
    public interface IVerificationLimit
    {
        IVerificationLimitConfig Config { get; }
        DateTime RecordsCutoff { get; }
        bool HasReachedLimit(IEnumerable<VerificationRecord> priorVerifications);
    }
}