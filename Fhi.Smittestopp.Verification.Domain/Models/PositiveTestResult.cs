using System;
using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public class PositiveTestResult
    {
        public Option<DateTime> PositiveTestDate { get; set; }
    }
}
