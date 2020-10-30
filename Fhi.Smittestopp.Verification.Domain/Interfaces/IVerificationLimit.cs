using System.Collections.Generic;
using Fhi.Smittestopp.Verification.Domain.Models;

namespace Fhi.Smittestopp.Verification.Domain.Interfaces
{
    public interface IVerificationLimit
    {
        IVerificationLimitConfig Config { get; }

        bool HasExceededLimit(IEnumerable<VerificationRecord> records);
    }
}