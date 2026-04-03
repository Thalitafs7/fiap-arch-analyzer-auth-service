using Application.DTOs;
using MediatR;

namespace Application.Commands.Auth.GenerateApiKey;

public record GenerateApiKeyCommand : IRequest<GenerateApiKeyResponse>;
