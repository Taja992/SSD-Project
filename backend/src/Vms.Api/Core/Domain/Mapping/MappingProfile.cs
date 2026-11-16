using AutoMapper;
using Vms.Api.Core.Domain.Dto.Finding;
using Entities = Vms.Domain.Entities;

namespace Vms.Api.Core.Domain.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Outbound. MaxDepth is defence in depth against recursion DoS; the
        // CVEs themselves are fixed by the 17.0.0 upgrade.
        CreateMap<Entities.Finding, FindingDto>()
            .MaxDepth(5)
            .ForMember(d => d.Severity, o => o.MapFrom(s => s.Severity.ToString()))
            .ForMember(d => d.Status,   o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Source,   o => o.MapFrom(s => s.Source.ToString()));

        // Inbound create. Every server-controlled field is explicitly ignored —
        // AutoMapper must never be the reason a privileged field gets written.
        CreateMap<CreateFindingDto, Entities.Finding>()
            .MaxDepth(5)
            .ForMember(d => d.Id,                o => o.Ignore())
            .ForMember(d => d.Status,            o => o.Ignore())
            .ForMember(d => d.Source,            o => o.Ignore())
            .ForMember(d => d.AssigneeUsername,  o => o.Ignore())
            .ForMember(d => d.DueDate,           o => o.Ignore())
            .ForMember(d => d.CreatedByUsername, o => o.Ignore())
            .ForMember(d => d.ReporterEmail,     o => o.Ignore())
            .ForMember(d => d.CreatedAt,         o => o.Ignore())
            .ForMember(d => d.UpdatedAt,         o => o.Ignore())
            .ForMember(d => d.DiscoveredAt,      o => o.Ignore());

        // Inbound update. Same discipline: only the four editable fields map.
        CreateMap<UpdateFindingDto, Entities.Finding>()
            .MaxDepth(5)
            .ForAllMembers(o => o.Ignore());
    }
}
