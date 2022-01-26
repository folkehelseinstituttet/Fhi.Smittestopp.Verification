using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Fhi.Smittestopp.Verification.Domain.Verifications
{
    public class VerifyPinUser
    {
        public class Command : IRequest<VerificationResult>
        {
            public string Pseudonym { get; set; }
            public bool IsPinVerified { get; set; }
            public bool SkipMsisLookup { get; set; }

            public Command(string pseudonym, bool isPinVerified, bool skipMsisLookup)
            {
                Pseudonym = pseudonym;
                IsPinVerified = isPinVerified;
                SkipMsisLookup = skipMsisLookup;
            }
        }

        public class Handler : IRequestHandler<Command, VerificationResult>
        {
            private readonly ILogger<VerifyPinUser> _logger;
            private readonly IVerificationLimit _verificationLimit;
            private readonly IVerificationRecordsRepository _verificationRecordsRepository;

            public Handler(ILogger<VerifyPinUser> logger, IVerificationLimit verificationLimit, IVerificationRecordsRepository verificationRecordsRepository)
            {
                _logger = logger;
                _verificationLimit = verificationLimit;
                _verificationRecordsRepository = verificationRecordsRepository;
            }

            public async Task<VerificationResult> Handle(Command request, CancellationToken cancellationToken)
            {
                var existingRecords =
                    await _verificationRecordsRepository.RetrieveRecordsForPseudonym(request.Pseudonym, _verificationLimit.RecordsCutoff);
                var newRecord = new VerificationRecord(request.Pseudonym);

                var verificationRecords = existingRecords.Concat(new[] { newRecord });

                _logger.LogInformation("Creating verification result for pin user");
                var verificationResult = request.IsPinVerified 
                    ? new VerificationResult(new PositiveTestResult() , verificationRecords, _verificationLimit)
                    : new VerificationResult(verificationRecords, _verificationLimit, request.SkipMsisLookup);

                await _verificationRecordsRepository.SaveNewRecord(newRecord);

                return verificationResult;
            }
        }
    }
}
