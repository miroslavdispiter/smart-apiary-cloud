using Microsoft.Extensions.DependencyInjection;
using SmartApiary.Application.Interfaces;
using SmartApiary.Infrastructure.Services;
using System.Text.Json;

namespace SmartApiary.Infrastructure.Extensions;

internal static class ServiceExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IImageResizer, ImageResizerService>();
        services.AddScoped<IWeatherService, WeatherService>();
        services.AddSingleton(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });
        services.AddSingleton<IJsonSerializer, SystemTextJsonSerializer>();

        services.AddHttpContextAccessor();
        services.AddHttpClient("weather");
        return services;
    }
}