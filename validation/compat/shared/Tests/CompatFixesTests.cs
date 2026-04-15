// Compat coverage for the four parity fixes (I3 subtype, I4 resolver re-entry, I5 renamed,
// B1 Ignore mirror). Isolated from CompatTests.cs so the scenarios are clearly grouped.
//
// Scope note on I4: the global `MaxDepth` setter lives on different surfaces between Morph
// (IMapperConfigurationExpression.MaxDepth) and AutoMapper v14 (per-map .MaxDepth()), so we
// don't try to exercise the depth-limit *threshold* here — that's covered by Morph's internal
// ResolverDepthTests. What the compat harness verifies is the observable public behavior:
// a resolver that calls context.Mapper.Map<>() must produce identical output under both libs.
using Morph;
using Compat.Shared.Domain;
using Compat.Shared.Profiles;
using Shouldly;
using System;
using Xunit;

namespace Compat.Shared.Tests;

public class CompatFixesTests
{
    private static IMapper BuildMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CompatFixesProfile>();
        });
        return config.CreateMapper();
    }

    // --- I3: concrete Dog assigned to Pet (declared Animal) must map through the base
    // Animal → AnimalDto map, not fall through to coercion. Pre-fix Morph did exact-type
    // lookup on (Dog, AnimalDto) and blew up; AutoMapper v14 walks assignability.

    [Fact]
    public void I3_SubtypeNestedMember_UsesBaseTypeMap()
    {
        var mapper = BuildMapper();
        var owner = new Owner
        {
            OwnerName = "Ada",
            Pet = new Dog { Name = "Rex", Breed = "Corgi" }
        };

        var dto = mapper.Map<Owner, OwnerDto>(owner);

        dto.OwnerName.ShouldBe("Ada");
        dto.Pet.ShouldNotBeNull();
        dto.Pet!.Name.ShouldBe("Rex");
    }

    // --- I4: resolver that calls context.Mapper.Map<>() for a nested member must produce
    // the same result on both libs. Pre-fix Morph had divergent behavior because the
    // ResolutionContext handed resolvers a plain IMapper rather than the depth-aware wrapper
    // v14 uses; the observable symptom here is simply that the nested map works.

    [Fact]
    public void I4_ResolverReentry_MapsNestedMember()
    {
        var mapper = BuildMapper();
        var outer = new OuterNode { Inner = new InnerNode { Value = 42 } };

        var dto = mapper.Map<OuterNode, OuterNodeDto>(outer);

        dto.Inner.ShouldNotBeNull();
        dto.Inner!.Value.ShouldBe(42);
    }

    [Fact]
    public void I4_ResolverReentry_NullInner_ProducesNull()
    {
        var mapper = BuildMapper();
        var outer = new OuterNode { Inner = null };

        var dto = mapper.Map<OuterNode, OuterNodeDto>(outer);

        dto.Inner.ShouldBeNull();
    }

    // --- I5 renamed: MapFrom on forward, ReverseMap must auto-reverse the rename so the
    // round trip preserves data. Pre-fix Morph dropped renamed members on reverse.

    [Fact]
    public void I5_RenamedMember_RoundTripsViaReverseMap()
    {
        var mapper = BuildMapper();
        var contact = new Contact { Id = 1, Email = "ada@example.com" };

        var dto = mapper.Map<Contact, ContactDto>(contact);
        dto.EmailAddress.ShouldBe("ada@example.com");

        var roundTripped = mapper.Map<ContactDto, Contact>(dto);
        roundTripped.Id.ShouldBe(1);
        roundTripped.Email.ShouldBe("ada@example.com");
    }

    // --- B1: forward Ignore() handling on the reverse map. This is the one deliberate
    // deviation between Morph's default and AutoMapper v14:
    //   - AutoMapper v14: Ignore is forward-only; reverse convention-copies the member.
    //   - Morph default:  Ignore mirrors onto the reverse, keeping the field at its default
    //                     (hardening against silent round-trip leaks of sensitive fields).
    //   - Morph opt-out:  MirrorIgnoreOnReverse = false reproduces v14 byte-for-byte.
    //
    // The harness runs the same source under both libs, so we use COMPAT_MORPH to gate the
    // assertions. The Morph leg runs TWO tests (default-on deviation + flag-off parity);
    // the AutoMapper leg runs ONE test (parity only — it has no flag to toggle).

    [Fact]
    public void B1_Ignore_MatchesV14_WhenForwardOnly()
    {
        // On AutoMapper v14 this is the only behavior. On Morph, we set the flag off so the
        // configured mapper matches v14's forward-only interpretation.
        var config = new MapperConfiguration(cfg =>
        {
#if COMPAT_MORPH
            cfg.MirrorIgnoreOnReverse = false;
#endif
            cfg.AddProfile<CompatFixesProfile>();
        });
        var mapper = config.CreateMapper();

        // Forward: Ignore drops the value (both libs agree).
        var forward = mapper.Map<AuditSource, AuditDto>(
            new AuditSource { Id = 1, InternalNotes = "secret-on-source" });
        forward.InternalNotes.ShouldBe("");

        // Reverse: convention-match copies the same-named member. v14 behavior.
        var back = mapper.Map<AuditDto, AuditSource>(
            new AuditDto { Id = 2, InternalNotes = "came-via-convention" });
        back.Id.ShouldBe(2);
        back.InternalNotes.ShouldBe("came-via-convention");
    }

#if COMPAT_MORPH
    // Morph-only: with the default config, Ignore mirrors onto the reverse. Proves the
    // hardening default is active and drops a potentially-injected value the forward
    // Ignore would have kept out.
    [Fact]
    public void B1_Morph_DefaultConfig_MirrorsIgnoreOnReverse()
    {
        var mapper = BuildMapper(); // MirrorIgnoreOnReverse defaults to true
        var tainted = new AuditDto { Id = 2, InternalNotes = "injected-by-client" };
        var back = mapper.Map<AuditDto, AuditSource>(tainted);

        back.Id.ShouldBe(2);
        back.InternalNotes.ShouldBe(""); // mirrored Ignore drops it
    }
#endif
}
