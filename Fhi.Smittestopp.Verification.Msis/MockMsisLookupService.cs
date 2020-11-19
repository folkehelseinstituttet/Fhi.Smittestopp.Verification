using System;
using System.Linq;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using Optional;

namespace Fhi.Smittestopp.Verification.Msis
{
    public class MockMsisLookupService : IMsisLookupService
    {
        /// <summary>
        /// Create a fake test result based on date of birth to get consistent mock behaviour
        /// - Last digit is even -> positive case
        /// - Month of birth determines days since positive test (gives range within last two weeks)
        /// </summary>
        /// <param name="nationalId">The national identifier to create a result for</param>
        /// <returns>Positive test result if found, otherwise none</returns>
        public Task<Option<PositiveTestResult>> FindPositiveTestResult(string nationalId)
        {
            return Task.FromResult(CreateMockResult(nationalId));
        }

        private Option<PositiveTestResult> CreateMockResult(string nationalId)
        {
            if (nationalId == "08089403198")
            {
                throw new Exception("Mock technical error");
            }
            if (int.TryParse(nationalId.Last().ToString(), out var lastDigit) && lastDigit % 2 == 0)
            {
                var monthDigits = nationalId.Substring(2, 2);
                if (!int.TryParse(monthDigits[0] == '0' ? monthDigits[1].ToString() : monthDigits, out var monthNumber))
                {
                    monthNumber = 5; // fallback for unexpected formats
                }
                return new PositiveTestResult
                {
                    PositiveTestDate = DateTime.Today.AddDays(-monthNumber).Some()
                }.Some();
            }
            return Option.None<PositiveTestResult>();
        }
    }
}
