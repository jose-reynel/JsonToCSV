using JsonToCsv.Application.Interfaces;
using JsonToCsv.Application.UseCases;
using JsonToCsv.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JsonToCsv.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IJsonToCsvConverterService, JsonToCsvConverterService>();
        services.AddScoped<ConvertJsonToCsvUseCase>();
        return services;
    }
}
