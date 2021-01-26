using AnonymousTokens.Server.Protocol;

using Fhi.Smittestopp.Verification.Domain.Dtos;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;

using MediatR;

using Microsoft.Extensions.Options;

using Optional;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            private readonly IAnonymousTokensKeyStore _keyStore;
            private readonly ITokenGenerator _tokenGenerator;
            private readonly AnonymousTokensConfig _config;

            public Handler(
                IAnonymousTokenIssueRecordRepository anonymousTokenIssueRecordRepository,
                IAnonymousTokensKeyStore keyStore,
                ITokenGenerator tokenGenerator,
                IOptions<AnonymousTokensConfig> config)
            {
                _anonymousTokenIssueRecordRepository = anonymousTokenIssueRecordRepository;
                _keyStore = keyStore;
                _tokenGenerator = tokenGenerator;
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

                var tokenResult = await CreateAnonymousTokenForRequestAsync(request.RequestData);

                await _anonymousTokenIssueRecordRepository.SaveNewRecord(new AnonymousTokenIssueRecord(request.JwtTokenId, request.JwtTokenExpiry));

                return tokenResult.Some<AnonymousTokenResponse, string>();
            }

            private async Task<AnonymousTokenResponse> CreateAnonymousTokenForRequestAsync(AnonymousTokenRequest request)
            {
                var signingKeyPair = await _keyStore.GetActiveSigningKeyPair();
                var privateKey = signingKeyPair.PrivateKey;
                var publicKey = signingKeyPair.PublicKey;
                var maskedPoint = signingKeyPair.EcParameters.Curve.DecodePoint(Convert.FromBase64String(request.MaskedPoint));

                var token = _tokenGenerator.GenerateToken(privateKey, publicKey, signingKeyPair.EcParameters, maskedPoint);
                var signedPoint = token.Q;
                var proofChallenge = token.c;
                var proofResponse = token.z;

                return new AnonymousTokenResponse(signingKeyPair.Kid, signedPoint, proofChallenge, proofResponse);
            }
        }
    }
}
