using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Dtos;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using MediatR;
using Microsoft.Extensions.Options;
using Optional;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public class IssueAnonymousToken
    {
        public class Command : IRequest<Option<AnonymousTokenResponse, string>>
        {
            public string JwtTokenId { get; set; }
            public DateTime JwtTokenExpiry { get; set; }
            public AnonymousTokenRequest RequestData { get; set; }
        }

        public class Handler : IRequestHandler<Command, Option<AnonymousTokenResponse, string>>
        {
            private readonly IAnonymousTokenIssueRecordRepository _anonymousTokenIssueRecordRepository;
            private readonly IAnonymousTokensCertLocator _certLocator;
            private readonly AnonymousTokensConfig _config;

            public Handler(IAnonymousTokenIssueRecordRepository anonymousTokenIssueRecordRepository, IAnonymousTokensCertLocator certLocator, IOptions<AnonymousTokensConfig> config)
            {
                _anonymousTokenIssueRecordRepository = anonymousTokenIssueRecordRepository;
                _certLocator = certLocator;
                _config = config.Value;
            }

            public async Task<Option<AnonymousTokenResponse, string>> Handle(Command request, CancellationToken cancellationToken)
            {
                if (!_config.Enabled)
                {
                    return Option.None<AnonymousTokenResponse, string>("Anonymous tokens are not enabled for this environment.");
                }

                var existingRecord =
                    await _anonymousTokenIssueRecordRepository.RetrieveRecordsJwtToken(request.JwtTokenId);

                if (existingRecord.Any())
                {
                    return Option.None<AnonymousTokenResponse, string>("Anonymous token already issued for the provided JWT-token ID.");
                }

                var tokenCert = await _certLocator.GetCertificateAsync();
                var tokenResult = CreateAnonymousTokenForRequest(request.RequestData, tokenCert);
                await _anonymousTokenIssueRecordRepository.SaveNewRecord(new AnonymousTokenIssueRecord(request.JwtTokenId, request.JwtTokenExpiry));
                return tokenResult.Some<AnonymousTokenResponse, string>();
            }

            private AnonymousTokenResponse CreateAnonymousTokenForRequest(AnonymousTokenRequest request, X509Certificate2 x509Certificate2)
            {
                // TODO: create anonymous token for request and cert
                return new AnonymousTokenResponse
                {

                };
            }
        }
    }
}
