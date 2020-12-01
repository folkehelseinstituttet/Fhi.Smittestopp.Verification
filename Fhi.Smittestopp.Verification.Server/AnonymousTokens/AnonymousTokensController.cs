using System;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Fhi.Smittestopp.Verification.Domain.Dtos;
using IdentityModel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Optional;
using Optional.Collections;

namespace Fhi.Smittestopp.Verification.Server.AnonymousTokens
{
    [ApiController]
    [Authorize(Policy = Policies.AnonymousTokens)]
    [Route("api/[controller]")]
    public class AnonymousTokensController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AnonymousTokensController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<AnonymousTokenResponse>> IssueNewToken(AnonymousTokenRequest request)
        {
            var result = await _mediator.Send(new IssueAnonymousToken.Command
            {
                JwtTokenId = User.Claims
                    .FirstOrNone(x => x.Type == JwtClaimTypes.JwtId)
                    .Map(x => x.Value)
                    .ValueOr(() => throw new Exception($"Required claim {JwtClaimTypes.JwtId} was not found")),
                JwtTokenExpiry = User.Claims
                    .FirstOrNone(x => x.Type == JwtClaimTypes.Expiration)
                    .FlatMap(x => int.TryParse(x.Value, out var number) ? number.Some() : default)
                    .Map(x => DateTimeOffset.FromUnixTimeSeconds(x).UtcDateTime)
                    .ValueOr(() => throw new Exception($"Required claim {JwtClaimTypes.Expiration} was not found or invalid format")),
                RequestData = request
                
            });

            return result.Match<ActionResult<AnonymousTokenResponse>>(
                none: e => Conflict(e),
                some: r => r
            );
        }
    }
}
