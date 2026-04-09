using Application;
using Application.Common.Constants;
using Application.Common.Interfaces;
using Infrastructure;
using Infrastructure.Logging;
using Infrastructure.Options;
using Infrastructure.Persistence.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UnitTests.Infrastructure;

public class InfrastructureTests
{
    [Fact]
    public void CorrelationIdService_ShouldKeepGeneratedId_WhenNoExplicitValueIsSet()
    {
        var service = new CorrelationIdService();

        var correlationId = service.GetCorrelationId();

        correlationId.Should().NotBeNullOrWhiteSpace();
        Guid.Parse(correlationId).Should().NotBeEmpty();
    }

    [Fact]
    public void CorrelationIdService_ShouldSetExplicitValue_WhenGuidIsValid()
    {
        var service = new CorrelationIdService();
        var correlationId = Guid.NewGuid();

        service.SetCorrelationId(correlationId);

        service.GetCorrelationId().Should().Be(correlationId.ToString());
    }

    [Fact]
    public void InternalKeyValidator_ShouldValidateConfiguredValue()
    {
        var validator = new InternalKeyValidator(Microsoft.Extensions.Options.Options.Create(new SecuritySettings
        {
            XInternalKey = "admin123"
        }));

        validator.IsValid("admin123").Should().BeTrue();
        validator.IsValid("wrong").Should().BeFalse();
    }

    [Fact]
    public void CurrentApiKeyService_ShouldReturnHeaderValueFromHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderNames.ApiKey] = "header-key";
        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };
        var service = new CurrentApiKeyService(accessor);

        service.ApiKey.Should().Be("header-key");
    }

    [Fact]
    public void CurrentApiKeyService_ShouldReturnNull_WhenHttpContextIsMissing()
    {
        var service = new CurrentApiKeyService(new HttpContextAccessor());

        service.ApiKey.Should().BeNull();
    }

    [Fact]
    public void LogService_ShouldWriteInformationEntry()
    {
        var currentApiKeyService = Substitute.For<ICurrentApiKeyService>();
        var correlationIdService = Substitute.For<ICorrelationIdService>();
        currentApiKeyService.ApiKey.Returns("api-key");
        correlationIdService.GetCorrelationId().Returns("correlation-id");
        var logger = new TestLogger<LogServiceTestsTarget>();
        var service = new LogService<LogServiceTestsTarget>(currentApiKeyService, correlationIdService, logger);

        service.LogInicio("Metodo", new { Value = 1 });

        logger.Entries.Should().ContainSingle();
        logger.Entries[0].LogLevel.Should().Be(LogLevel.Information);
        logger.Entries[0].Exception.Should().BeNull();
    }

    [Fact]
    public void LogService_ShouldWriteErrorEntry()
    {
        var currentApiKeyService = Substitute.For<ICurrentApiKeyService>();
        var correlationIdService = Substitute.For<ICorrelationIdService>();
        var logger = new TestLogger<LogServiceTestsTarget>();
        var service = new LogService<LogServiceTestsTarget>(currentApiKeyService, correlationIdService, logger);

        service.LogErro("Metodo", new InvalidOperationException("boom"));

        logger.Entries.Should().ContainSingle();
        logger.Entries[0].LogLevel.Should().Be(LogLevel.Error);
        logger.Entries[0].Exception.Should().NotBeNull();
    }

    [Fact]
    public void ApplicationAndInfrastructureDependencyInjection_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "auth-tests",
                ["MongoDb:ApiKeysCollectionName"] = "apiKeys",
                ["Security:XInternalKey"] = "admin123"
            })
            .Build();

        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();

        provider.GetService<IInternalKeyValidator>().Should().NotBeNull();
        provider.GetService<ICurrentApiKeyService>().Should().NotBeNull();
        provider.GetService<ILogService<LogServiceTestsTarget>>().Should().NotBeNull();
        provider.GetService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>().Should().NotBeNull();
        provider.GetService<Microsoft.Extensions.Options.IOptions<SecuritySettings>>().Should().NotBeNull();
        provider.GetService<MongoDB.Driver.IMongoCollection<ApiKeyDocument>>().Should().NotBeNull();
    }

    [Fact]
    public void ConfigurationTypes_ShouldExposeDefaults()
    {
        MongoDbSettings.SectionName.Should().Be("MongoDb");
        new MongoDbSettings().ApiKeysCollectionName.Should().Be("apiKeys");
        new SecuritySettings().XInternalKey.Should().BeEmpty();
        new LogEntryDto().ApiKey.Should().BeNull();
    }

    private sealed class LogServiceTestsTarget;

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<TestLogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new TestLogEntry(logLevel, formatter(state, exception), exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }

    private sealed record TestLogEntry(LogLevel LogLevel, string State, Exception? Exception);
}
