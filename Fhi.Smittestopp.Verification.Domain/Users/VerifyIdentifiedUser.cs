using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;
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

        public class Handler : IRequestHandler<Command, VerificationResult>
        {
            private readonly IMsisLookupService _msisLookupService;
            private readonly ILogger<CreateFromExternalAuthentication> _logger;
            private readonly IVerificationLimit _verificationLimit;
            private readonly IVerificationRecordsRepository _verificationRecordsRepository;

            public Handler(IMsisLookupService msisLookupService, ILogger<CreateFromExternalAuthentication> logger, IVerificationLimit verificationLimit, IVerificationRecordsRepository verificationRecordsRepository)
            {
                _msisLookupService = msisLookupService;
                _logger = logger;
                _verificationLimit = verificationLimit;
                _verificationRecordsRepository = verificationRecordsRepository;
            }

            public async Task<VerificationResult> Handle(Command request, CancellationToken cancellationToken)
            {
                var positiveTest = await _msisLookupService.FindPositiveTestResult(request.NationalIdentifier);

                return await positiveTest.MatchAsync(
                    none: CreateNonPositiveResult,
                    some: pt => CreatePositiveVerificationResult(pt, request.Pseudonym));
            }

            private async Task<VerificationResult> CreatePositiveVerificationResult(PositiveTestResult testResult, string pseudonym)
            {
                var existingRecords =
                    await _verificationRecordsRepository.RetrieveRecordsForPseudonym(pseudonym);
                var newRecord = new VerificationRecord(pseudonym);

                var verificationRecords = existingRecords.Concat(new[] { newRecord });

                _logger.LogInformation("Creating verified positive result for identified user");
                var verificationResult = new VerificationResult(testResult, verificationRecords, _verificationLimit);

                await _verificationRecordsRepository.SaveNewRecord(newRecord);

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
