// Profile for the four parity fix scenarios (I3 subtype, I4 resolver re-entry, I5 renamed,
// B1 Ignore mirror). These exercise the v14 behaviors the Morph fixes are claimed to match.
// Source is compiled twice (Morph leg + AutoMapper 14.0.0 leg) via the compat harness; green
// on both legs proves the parity claim.
using Morph;
using Compat.Shared.Domain;

namespace Compat.Shared.Profiles;

public class CompatFixesProfile : Profile
{
    public CompatFixesProfile()
    {
        // I3: only the base-class map is registered. A concrete Dog assigned to Pet must
        // use this map, not fall through to coercion.
        CreateMap<Animal, AnimalDto>();
        CreateMap<Owner, OwnerDto>();

        // I4: resolver that calls context.Mapper.Map<>() for a nested member. The compat
        // harness proves the observable public behavior matches across libs; depth-threading
        // internals are covered by Morph's own ResolverDepthTests (which can set MaxDepth
        // against Morph's surface directly).
        CreateMap<InnerNode, InnerNodeDto>();
        CreateMap<OuterNode, OuterNodeDto>()
            .ForMember(d => d.Inner, o => o.MapFrom<InnerNodeResolver>());

        // I5 renamed: MapFrom on forward, ReverseMap auto-reverses.
        CreateMap<Contact, ContactDto>()
            .ForMember(d => d.EmailAddress, o => o.MapFrom(s => s.Email))
            .ReverseMap();

        // B1: Ignore on forward, ReverseMap must mirror Ignore onto the reverse destination.
        CreateMap<AuditSource, AuditDto>()
            .ForMember(d => d.InternalNotes, o => o.Ignore())
            .ReverseMap();
    }
}

public class InnerNodeResolver : IValueResolver<OuterNode, OuterNodeDto, InnerNodeDto?>
{
    public InnerNodeDto? Resolve(OuterNode source, OuterNodeDto destination, InnerNodeDto? destMember, ResolutionContext context)
        => source.Inner is null ? null : context.Mapper.Map<InnerNodeDto>(source.Inner);
}
