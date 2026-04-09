using Application.Commands.Auth.GenerateApiKey;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.DTOs;
using Application.Queries.Auth.ValidateApiKey;
using Domain.Entities;
using Domain.Interfaces;

namespace UnitTests.Application;

public class AuthHandlersTests
{
    [Fact]
    public async Task GenerateApiKeyHandler_ShouldPersistAndReturnGeneratedApiKey()
    {
        var repository = Substitute.For<IApiKeyRepository>();
        var internalKeyValidator = Substitute.For<IInternalKeyValidator>();
        var logService = Substitute.For<ILogService<GenerateApiKeyHandler>>();
        internalKeyValidator.IsValid("admin123").Returns(true);
        repository.CreateAsync(Arg.Any<ApiKey>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<ApiKey>()));

        var handler = new GenerateApiKeyHandler(repository, internalKeyValidator, logService);

        var response = await handler.Handle(new GenerateApiKeyCommand("admin123"), CancellationToken.None);

        response.ApiKey.Should().NotBeNullOrWhiteSpace();
        response.ApiKey.Length.Should().Be(64);
        await repository.Received(1).CreateAsync(Arg.Any<ApiKey>(), Arg.Any<CancellationToken>());
        logService.Received(1).LogInicio("Handle", Arg.Any<object>());
        logService.Received(1).LogFim("Handle", Arg.Any<GenerateApiKeyResponse>());
    }

    [Fact]
    public async Task GenerateApiKeyHandler_ShouldThrowUnauthorized_WhenInternalKeyIsInvalid()
    {
        var repository = Substitute.For<IApiKeyRepository>();
        var internalKeyValidator = Substitute.For<IInternalKeyValidator>();
        var logService = Substitute.For<ILogService<GenerateApiKeyHandler>>();
        internalKeyValidator.IsValid("invalid").Returns(false);

        var handler = new GenerateApiKeyHandler(repository, internalKeyValidator, logService);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GenerateApiKeyCommand("invalid"), CancellationToken.None));

        logService.Received(1).LogErro("Handle", Arg.Any<UnauthorizedException>());
    }

    [Fact]
    public async Task ValidateApiKeyHandler_ShouldReturnResponse_WhenApiKeyIsValid()
    {
        var repository = Substitute.For<IApiKeyRepository>();
        var logService = Substitute.For<ILogService<ValidateApiKeyHandler>>();
        repository.GetActiveByKeyAsync("valid-api-key", Arg.Any<CancellationToken>())
            .Returns(new ApiKey { Key = "valid-api-key", Revoked = false });

        var handler = new ValidateApiKeyHandler(repository, logService);

        var response = await handler.Handle(new ValidateApiKeyQuery("valid-api-key"), CancellationToken.None);

        response.Should().NotBeNull();
        response!.Message.Should().Be("Authorized");
        logService.Received(1).LogFim("Handle", response);
    }

    [Fact]
    public async Task ValidateApiKeyHandler_ShouldReturnNull_WhenApiKeyIsInvalid()
    {
        var repository = Substitute.For<IApiKeyRepository>();
        var logService = Substitute.For<ILogService<ValidateApiKeyHandler>>();
        repository.GetActiveByKeyAsync("invalid", Arg.Any<CancellationToken>())
            .Returns((ApiKey?)null);

        var handler = new ValidateApiKeyHandler(repository, logService);

        var response = await handler.Handle(new ValidateApiKeyQuery("invalid"), CancellationToken.None);

        response.Should().BeNull();
        logService.Received(1).LogFim("Handle", null);
    }

    [Fact]
    public async Task ValidateApiKeyHandler_ShouldLogAndRethrow_WhenRepositoryFails()
    {
        var repository = Substitute.For<IApiKeyRepository>();
        var logService = Substitute.For<ILogService<ValidateApiKeyHandler>>();
        repository.GetActiveByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<ApiKey?>(new InvalidOperationException("mongo-failed")));

        var handler = new ValidateApiKeyHandler(repository, logService);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ValidateApiKeyQuery("valid-api-key"), CancellationToken.None));

        logService.Received(1).LogErro("Handle", Arg.Any<InvalidOperationException>());
    }
}
