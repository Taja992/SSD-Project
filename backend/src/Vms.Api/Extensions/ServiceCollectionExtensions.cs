using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vms.Api.Core.Application.Interfaces;
using Vms.Api.Core.Application.Services;
using Vms.Api.Core.Domain.Mapping;
using Vms.Domain.Interfaces;
using Vms.Infrastructure.Context;
using Vms.Infrastructure.Repositories;
using Vms.Infrastructure.Security;

namespace Vms.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVmsPersistence(
        this IServiceCollection services, IConfiguration config)
    {
        // FIND-11. The keys are checked when the API starts: a missing or
        // malformed key stops it immediately with a clear error, instead of on
        // the first request that reads a finding.
        var encryption = services.AddOptions<EncryptionOptions>()
            .Bind(config.GetSection(EncryptionOptions.SectionName));

        // One exception: the build-time OpenAPI step (guide 07) also starts the
        // app, has no keys, and never touches the database.
        if (Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider")
            encryption.Validate(o => new AesGcmFieldEncryptor(o) is not null).ValidateOnStart();

        services.AddSingleton<IFieldEncryptor>(sp =>
            new AesGcmFieldEncryptor(sp.GetRequiredService<IOptions<EncryptionOptions>>().Value));

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
