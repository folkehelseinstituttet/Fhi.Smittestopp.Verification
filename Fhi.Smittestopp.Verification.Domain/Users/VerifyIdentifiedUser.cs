using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Optional;
using Optional.Async.Extensions;

namespace Fhi.Smittestopp.Verification.Domain.Users
{
    public class VerifyIdentifiedUser
    {
        public class Command : IRequest<VerificationResult>
        {
            public string NationalIdentifier { get; set; }
            public string Pseudonym { get; set; }

            public Command(string nationalIdentifier, string pseudonym)
            {
                NationalIdentifier = nationalIdentifier;
                Pseudonym = pseudonym;
            }
        }

        public class Config
        {
            public bool UseFixedTestCases { get; set; } = false;
            public TestCaseConfig TestCases { get; set; }
        }

        public class TestCaseConfig
        {
            public bool OddEvenInfectionResults { get; set; } = false;
            public int FixedDaysSincePositiveTest { get; set; } = 14;
            public bool FixedLimitExceededResults { get; set; } = false;
            public string[] TechnicalErrorUsers { get; set; } = new string[0];
            public string[] LimitExceededUsers { get; set; } = new string[0];
        }

        public class Handler : IRequestHandler<Command, VerificationResult>
        {
            private readonly IMsisLookupService _msisLookupService;
            private readonly ILogger<CreateFromExternalAuthentication> _logger;
            private readonly IVerificationLimit _verificationLimit;
            private readonly IVerificationRecordsRepository _verificationRecordsRepository;
            private readonly Config _config;

            public Handler(IMsisLookupService msisLookupService, ILogger<CreateFromExternalAuthentication> logger, IVerificationLimit verificationLimit, IVerificationRecordsRepository verificationRecordsRepository, IOptions<Config> config)
            {
                _msisLookupService = msisLookupService;
                _logger = logger;
                _verificationLimit = verificationLimit;
                _verificationRecordsRepository = verificationRecordsRepository;
                _config = config.Value;
            }

            public Task<VerificationResult> Handle(Command request, CancellationToken cancellationToken)
            {
                return _config.UseFixedTestCases
                    ? CreateTestCaseVerificationResult(request.NationalIdentifier, request.Pseudonym)
                    : CreateRealVerificationResult(request.NationalIdentifier, request.Pseudonym);
            }

            public async Task<VerificationResult> CreateRealVerificationResult(string nationalIdentifier, string pseudonym)
            {
                var positiveTest = await _msisLookupService.FindPositiveTestResult(nationalIdentifier);

                return await positiveTest.MatchAsync(
                    none: CreateNonPositiveResult,
                    some: async pt =>
                    {
                        var existingRecords =
                            await _verificationRecordsRepository.RetrieveRecordsForPseudonym(pseudonym, _verificationLimit.RecordsCutoff);
                        return await CreatePositiveVerificationResult(pt, pseudonym, existingRecords);
                    });
            }

            /// <summary>
            /// Medified version of CreateRealVerificationResult where behaviour can be overridden based on configuration
            /// </summary>
            private async Task<VerificationResult> CreateTestCaseVerificationResult(string nationalIdentifier, string pseudonym)
            {
                if (_config.TestCases.TechnicalErrorUsers.Contains(nationalIdentifier))
                {
                    throw new Exception("Provided national identifier is configured to cause technical error");
                }

                var positiveTest = _config.TestCases.OddEvenInfectionResults
                    ? int.TryParse(nationalIdentifier.Last().ToString(), out var lastDigit) && lastDigit % 2 == 0
                        ? new PositiveTestResult { PositiveTestDate = DateTime.Now.AddDays(-_config.TestCases.FixedDaysSincePositiveTest).Some() }.Some()
                        : Option.None<PositiveTestResult>()
                    : await _msisLookupService.FindPositiveTestResult(nationalIdentifier);

                return await positiveTest.MatchAsync(
                    none: CreateNonPositiveResult,
                    some: async pt =>
                    {
                        var existingRecords = _config.TestCases.FixedLimitExceededResults
                            ? Enumerable.Empty<VerificationRecord>()
                            : await _verificationRecordsRepository.RetrieveRecordsForPseudonym(pseudonym, _verificationLimit.RecordsCutoff);

                        // Fixed "verification limit exceeded" users for testing
                        if (_config.TestCases.LimitExceededUsers.Contains(nationalIdentifier))
                        {
                            existingRecords = existingRecords.Concat(Enumerable.Range(0, _verificationLimit.Config.MaxVerificationsAllowed)
                                .Select(x => new VerificationRecord(pseudonym)));
                        }

                        return await CreatePositiveVerificationResult(pt, pseudonym, existingRecords);
                    });
            }

            private async Task<VerificationResult> CreatePositiveVerificationResult(PositiveTestResult testResult, string pseudonym, IEnumerable<VerificationRecord> existingRecords)
            {
                _logger.LogInformation("Creating verified positive result for identified user");
                var verificationResult = new VerificationResult(testResult, existingRecords, _verificationLimit);

                if (!verificationResult.VerificationLimitExceeded)
                {
                    // Save new record of non-limited verification
                    await _verificationRecordsRepository.SaveNewRecord(new VerificationRecord(pseudonym));
                }

                return verificationResult;
            }

            private Task<VerificationResult> CreateNonPositiveResult()
            {
                _logger.LogInformation("Creating non-positive verification result for identified user");
                return Task.FromResult(new VerificationResult());
            }
        }
    }
}
