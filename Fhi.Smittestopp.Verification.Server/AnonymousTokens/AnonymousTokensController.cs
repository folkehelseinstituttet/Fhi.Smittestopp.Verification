using System;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Fhi.Smittestopp.Verification.Domain.Dtos;
using IdentityModel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Optional;
using Optional.Collections;

namespace Fhi.Smittestopp.Verification.Server.AnonymousTokens
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnonymousTokensController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AnonymousTokensController> _logger;

        public AnonymousTokensController(IMediator mediator, ILogger<AnonymousTokensController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [Authorize(Policy = Policies.AnonymousTokens)]
        [HttpPost]
        public async Task<ActionResult<AnonymousTokenResponse>> IssueNewToken(AnonymousTokenRequest request)
        {
            var jwtTokenId = User.Claims
                .FirstOrNone(x => x.Type == JwtClaimTypes.JwtId)
                .Map(x => x.Value)
                .ValueOr(() => throw new Exception($"Required claim {JwtClaimTypes.JwtId} was not found"));

            var jwtTokenExpiry = User.Claims
                .FirstOrNone(x => x.Type == JwtClaimTypes.Expiration)
                .FlatMap(x => int.TryParse(x.Value, out var number)
                    ? DateTimeOffset.FromUnixTimeSeconds(number).UtcDateTime.Some()
                    : default)
                .ValueOr(() => throw new Exception($"Required claim {JwtClaimTypes.Expiration} was not found or invalid format"));

            var result = await _mediator.Send(new IssueAnonymousToken.Command
            {
                JwtTokenId = jwtTokenId,
                JwtTokenExpiry = jwtTokenExpiry,
                RequestData = request
            });

            return result
                .Map<ActionResult<AnonymousTokenResponse>>(r => r)
                .ValueOr(e =>
                {
                    _logger.LogWarning("Anonymous token request was rejected with reason: " + e);
                    return Conflict(e);
                });
        }

        [HttpGet("atks")]
        public async Task<ActionResult<AnonymousTokenKeySet>> GetKeySet()
        {
            return await _mediator.Send(new GetAnonymousTokenKeySet.Query());
        }
    }
}
