using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartApiary.Application.Common.Options;
using SmartApiary.Infrastructure.Common.Options;
using SmartApiary.Infrastructure.Extensions;

namespace SmartApiary.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AzureTableOptions>(configuration.GetSection(nameof(AzureTableOptions)));
        services.Configure<AzureBlobOptions>(configuration.GetSection(nameof(AzureBlobOptions)));
        services.Configure<AzureQueueOptions>(configuration.GetSection(nameof(AzureQueueOptions)));
        services.Configure<AzureSqlOptions>(configuration.GetSection(nameof(AzureSqlOptions)));
        services.Configure<JwtOptions>(configuration.GetSection(nameof(JwtOptions)));
        services.Configure<EmailOptions>(configuration.GetSection(nameof(EmailOptions)));
        services.Configure<FrontendOptions>(configuration.GetSection(nameof(FrontendOptions)));
        services.Configure<WeatherOptions>(configuration.GetSection(nameof(WeatherOptions)));

        var tableConn = configuration.GetValue<string>("AzureTableOptions:ConnectionString")
            ?? throw new InvalidOperationException("AzureTableOptions:ConnectionString is not configured.");

        var blobConn = configuration.GetValue<string>("AzureBlobOptions:ConnectionString")
            ?? throw new InvalidOperationException("AzureBlobOptions:ConnectionString is not configured.");

        var queueConn = configuration.GetValue<string>("AzureQueueOptions:ConnectionString")
            ?? throw new InvalidOperationException("AzureQueueOptions:ConnectionString is not configured.");

        services
            .AddServices()
            .AddAzureSql()
            .AddAzureTables(tableConn)
            .AddAzureBlobs(blobConn)
            .AddAzureQueues(queueConn);

        return services;
    }
}
