using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.DependencyInjection;
using SmartApiary.Application.Interfaces.Messaging;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Application.Interfaces.Storage;
using SmartApiary.Domain.Models;
using SmartApiary.Infrastructure.Persistence.AzureBlob.Storages;
using SmartApiary.Infrastructure.Persistence.AzureQueue.Services;
using SmartApiary.Infrastructure.Persistence.AzureSql.Repositories;
using SmartApiary.Infrastructure.Persistence.AzureTable.Common;
using SmartApiary.Infrastructure.Persistence.AzureTable.Entities;
using SmartApiary.Infrastructure.Persistence.AzureTable.KeyProviders;
using SmartApiary.Infrastructure.Persistence.AzureTable.Mappers;
using SmartApiary.Infrastructure.Persistence.AzureTable.Repositories;

namespace SmartApiary.Infrastructure.Extensions
{
    internal static class AzureStorageExtensions
    {
        public static IServiceCollection AddAzureSql(this IServiceCollection services)
        {
            services.AddScoped<IApiaryRepository, ApiaryRepository>();
            services.AddScoped<IHiveRepository, HiveRepository>();
            services.AddScoped<IDeviceRepository, DeviceRepository>();
            services.AddScoped<IHiveInspectionRepository, HiveInspectionRepository>();
            services.AddScoped<IPlotRepository, PlotRepository>();
            services.AddScoped<ISprayingRepository, SprayingRepository>();

            return services;
        }

        public static IServiceCollection AddAzureTables(
            this IServiceCollection services,
            string connectionString)
        {
            services.AddSingleton(new TableServiceClient(connectionString));

            services.AddSingleton<ITableMapper<User, UserEntity>, UserTableMapper>();
            services.AddSingleton<ITableKeyProvider<User>, UserTableKeyProvider>();
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddSingleton<ITableMapper<TelemetrySample, TelemetrySampleEntity>, TelemetrySampleTableMapper>();
            services.AddSingleton<ITableKeyProvider<TelemetrySample>, TelemetrySampleTableKeyProvider>();
            services.AddScoped<ITelemetrySampleRepository, TelemetrySampleRepository>();

            return services;
        }

        public static IServiceCollection AddAzureBlobs(
            this IServiceCollection services,
            string connectionString)
        {
            services.AddSingleton(_ => new BlobServiceClient(connectionString));
            services.AddScoped<IApiaryImageStorage, ApiaryImageStorage>();
            return services;
        }

        public static IServiceCollection AddAzureQueues(
            this IServiceCollection services,
            string connectionString)
        {
            var queueServiceClient = new QueueServiceClient(connectionString, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            });

            queueServiceClient.GetQueueClient("telemetry-queue").CreateIfNotExists();

            services.AddSingleton(queueServiceClient);
            services.AddSingleton<ITelemetryQueueService, TelemetryQueueService>();

            return services;
        }
    }
}