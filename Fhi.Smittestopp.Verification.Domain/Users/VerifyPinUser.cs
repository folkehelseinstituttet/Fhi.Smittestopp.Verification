using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Fhi.Smittestopp.Verification.Domain.Users
{
    public class VerifyPinUser
    {
        public class Command : IRequest<VerificationResult>
        {
            public string Pseudonym { get; set; }

            public Command(string pseudonym)
            {
                Pseudonym = pseudonym;
            }
        }

        public class Handler : IRequestHandler<Command, VerificationResult>
        {
            private readonly ILogger<CreateFromExternalAuthentication> _logger;
            private readonly IVerificationLimit _verificationLimit;
            private readonly IVerificationRecordsRepository _verificationRecordsRepository;

            public Handler(ILogger<CreateFromExternalAuthentication> logger, IVerificationLimit verificationLimit, IVerificationRecordsRepository verificationRecordsRepository)
            {
                _logger = logger;
                _verificationLimit = verificationLimit;
                _verificationRecordsRepository = verificationRecordsRepository;
            }

            public async Task<VerificationResult> Handle(Command request, CancellationToken cancellationToken)
            {
                var existingRecords =
                    await _verificationRecordsRepository.RetrieveRecordsForPseudonym(request.Pseudonym);
                var newRecord = new VerificationRecord(request.Pseudonym);

                var verificationRecords = existingRecords.Concat(new[] { newRecord });

                _logger.LogInformation("Creating verified positive result for pin user");
                var verificationResult = new VerificationResult(new PositiveTestResult(), verificationRecords, _verificationLimit);

                await _verificationRecordsRepository.SaveNewRecord(newRecord);

                return verificationResult;
            }
        }
    }
}
