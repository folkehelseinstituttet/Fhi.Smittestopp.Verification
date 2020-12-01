using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Dtos;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using MediatR;
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

            public Handler(IAnonymousTokenIssueRecordRepository anonymousTokenIssueRecordRepository)
            {
                _anonymousTokenIssueRecordRepository = anonymousTokenIssueRecordRepository;
            }

            public async Task<Option<AnonymousTokenResponse, string>> Handle(Command request, CancellationToken cancellationToken)
            {
                var existingRecord =
                    await _anonymousTokenIssueRecordRepository.RetrieveRecordsJwtToken(request.JwtTokenId);

                if (existingRecord.Any())
                {
                    return Option.None<AnonymousTokenResponse, string>("Anonymous token already issued for user");
                }
                else
                {
                    var tokenResult = CreateAnonymousTokenForRequest(request.RequestData);
                    await _anonymousTokenIssueRecordRepository.SaveNewRecord(new AnonymousTokenIssueRecord(request.JwtTokenId, request.JwtTokenExpiry));
                    return tokenResult.Some<AnonymousTokenResponse, string>();
                }
            }

            private AnonymousTokenResponse CreateAnonymousTokenForRequest(AnonymousTokenRequest request)
            {
                // TODO: create anonymous token for request
                return new AnonymousTokenResponse
                {

                };
            }
        }
    }
}
