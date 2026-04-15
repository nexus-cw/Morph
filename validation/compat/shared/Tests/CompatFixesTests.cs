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

    // --- B1: forward Ignore() does NOT mirror onto the reverse map in AutoMapper v14.
    // The reverse leg convention-matches the same-named member and copies it. This test
    // pins that behavior as the parity contract on feat/drop-in-compat.
    //
    // Morph makes a different, safer default on fix/cve-hardening (Ignore mirrored by name),
    // opt-outable via MirrorIgnoreOnReverse = false. When the hardening branch merges here
    // the compat harness gains a second assertion proving the flag-off path matches v14.

    [Fact]
    public void B1_Ignore_IsForwardOnly_MatchesV14_ReverseCopiesByConvention()
    {
        var mapper = BuildMapper();

        // Forward: Ignore drops the value as expected on both libs.
        var source = new AuditSource { Id = 1, InternalNotes = "secret-on-source" };
        var dto = mapper.Map<AuditSource, AuditDto>(source);
        dto.Id.ShouldBe(1);
        dto.InternalNotes.ShouldBe("");

        // Reverse: v14 does NOT mirror the Ignore. Convention-match on the same-named member
        // copies it straight through. Morph on this branch matches v14; the round-trip carries
        // a value the forward Ignore would have dropped.
        var tainted = new AuditDto { Id = 2, InternalNotes = "injected-by-client" };
        var back = mapper.Map<AuditDto, AuditSource>(tainted);

        back.Id.ShouldBe(2);
        back.InternalNotes.ShouldBe("injected-by-client");
    }
}
