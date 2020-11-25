using System;
using System.Linq;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Msis.Interfaces;
using Fhi.Smittestopp.Verification.Msis.Models;

namespace Fhi.Smittestopp.Verification.Msis
{
    public class MockMsisClient : IMsisClient
    {
        private static readonly string[] TechnicalErrorUsers = {
            "08089403198",
            "08089403783"
        };

        /// <summary>
        /// Create a fake covid-19 status result based on date of birth to get consistent mock behaviour
        /// - Last digit is even -> positive case
        /// - Month of birth determines days since positive test (gives range within last two weeks)
        /// </summary>
        /// <param name="nationalId">The national identifier to create a result for</param>
        /// <returns>Covid 19 status result</returns>
        public Task<Covid19Status> GetCovid19Status(string nationalId)
        {
            return Task.FromResult(CreateCovid19Status(nationalId));
        }

        public Task<bool> GetMsisOnlineStatus()
        {
            return Task.FromResult(true);
        }

        private Covid19Status CreateCovid19Status(string nationalId)
        {
            if (TechnicalErrorUsers.Contains(nationalId))
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

                return new Covid19Status
                {
                    HarPositivCovid19Prove = true,
                    Provedato = DateTime.Today.AddDays(-monthNumber)
                };
            }

            return new Covid19Status
            {
                HarPositivCovid19Prove = false,
                Provedato = null
            };
        }
    }
}
