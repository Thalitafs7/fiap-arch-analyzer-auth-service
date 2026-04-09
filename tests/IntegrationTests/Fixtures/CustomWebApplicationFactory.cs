using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public InMemoryApiKeyRepository Repository { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(Repository);

            RemoveService<IApiKeyRepository>(services);
            RemoveService<IInternalKeyValidator>(services);

            services.AddSingleton<IApiKeyRepository>(Repository);
            services.AddSingleton<IInternalKeyValidator>(new TestInternalKeyValidator());
        });
    }

    public void Reset() => Repository.Reset();

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null)
            services.Remove(descriptor);
    }
}

public class InMemoryApiKeyRepository : IApiKeyRepository
{
    private readonly List<ApiKey> _items = [];

    public IReadOnlyList<ApiKey> Items => _items;

    public Task<ApiKey> CreateAsync(ApiKey apiKey, CancellationToken cancellationToken)
    {
        _items.Add(apiKey);
        return Task.FromResult(apiKey);
    }

    public Task<ApiKey?> GetActiveByKeyAsync(string apiKey, CancellationToken cancellationToken)
    {
        return Task.FromResult(_items.LastOrDefault(item => item.Key == apiKey && !item.Revoked));
    }

    public void Seed(ApiKey apiKey) => _items.Add(apiKey);

    public void Reset() => _items.Clear();
}

public class TestInternalKeyValidator : IInternalKeyValidator
{
    public bool IsValid(string internalKey) => internalKey == "admin123";
}
