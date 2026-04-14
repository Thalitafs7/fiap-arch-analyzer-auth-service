using Application.DTOs;
using MediatR;

namespace Application.Queries.Auth.ValidateApiKey;

public record ValidateApiKeyQuery(string ApiKey) : IRequest<ValidateApiKeyResponse?>;
