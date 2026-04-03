using Application.Common.Interfaces;
using Domain.Interfaces;
using Infrastructure.Logging;
using Infrastructure.Options;
using Infrastructure.Persistence.Models;
using Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));

        services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;

            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                throw new InvalidOperationException("MongoDb:ConnectionString não configurada.");

            return new MongoClient(settings.ConnectionString);
        });

        services.AddScoped(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var client = serviceProvider.GetRequiredService<IMongoClient>();

            if (string.IsNullOrWhiteSpace(settings.DatabaseName))
                throw new InvalidOperationException("MongoDb:DatabaseName não configurado.");

            return client.GetDatabase(settings.DatabaseName);
        });

        services.AddScoped(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var database = serviceProvider.GetRequiredService<IMongoDatabase>();

            if (string.IsNullOrWhiteSpace(settings.ApiKeysCollectionName))
                throw new InvalidOperationException("MongoDb:ApiKeysCollectionName não configurado.");

            return database.GetCollection<ApiKeyDocument>(settings.ApiKeysCollectionName);
        });

        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

        services.AddScoped<ICorrelationIdService, CorrelationIdService>();

        return services;
    }
}
