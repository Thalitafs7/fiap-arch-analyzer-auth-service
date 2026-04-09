using Application.Commands.Auth.GenerateApiKey;
using Application.Queries.Auth.ValidateApiKey;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("validate")]
    public async Task<IActionResult> Validate(
        [FromHeader(Name = "x-api-key")] string apiKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { message = "API Key não fornecida" });

        var response = await mediator
            .Send(new ValidateApiKeyQuery(apiKey), cancellationToken);

        if (response is null)
            return Unauthorized(new { message = "API Key inválida." });

        return Ok(response);
    }

    [HttpPost("apikey")]
    public async Task<IActionResult> GenerateApiKey(
        [FromHeader(Name = "x-internal-key")] string internalKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(internalKey))
            return Unauthorized(new { message = "x-internal-key não fornecida" });

        var response = await mediator
            .Send(new GenerateApiKeyCommand(internalKey), cancellationToken);

        return Ok(response);
    }
}
