using Microsoft.EntityFrameworkCore;
using Vms.Api.Core.Application.Interfaces;
using Vms.Api.Core.Application.Services;
using Vms.Api.Core.Domain.Mapping;
using Vms.Domain.Interfaces;
using Vms.Infrastructure.Context;
using Vms.Infrastructure.Repositories;

namespace Vms.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVmsPersistence(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<VmsDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("Default")));
        services.AddScoped<IFindingRepository, FindingRepository>();
        return services;
    }

    public static IServiceCollection AddVmsApplication(this IServiceCollection services)
    {
        services.AddScoped<IFindingService, FindingService>();
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        return services;
    }
}
