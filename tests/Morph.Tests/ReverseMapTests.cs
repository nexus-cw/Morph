using Xunit;

namespace Morph.Tests;

public class ReverseMapTests
{
    public class Src { public int Id { get; set; } public string Name { get; set; } = ""; }
    public class Dst { public int Id { get; set; } public string Name { get; set; } = ""; }

    [Fact]
    public void ReverseMap_enables_inverse_mapping()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Src, Dst>().ReverseMap());
        var mapper = cfg.CreateMapper();

        var d = mapper.Map<Src, Dst>(new Src { Id = 1, Name = "a" });
        var s = mapper.Map<Dst, Src>(d);

        Assert.Equal(1, s.Id);
        Assert.Equal("a", s.Name);
    }

    // B1: Morph's default behavior mirrors a forward Ignore() onto the reverse map so a field
    // that the caller explicitly said "never touch" on the forward leg doesn't silently
    // round-trip back via convention matching. This diverges from AutoMapper v14 (which treats
    // Ignore as forward-only) and is opt-outable via MirrorIgnoreOnReverse = false — see the
    // parity test below.
    public class SrcWithSecret { public int Id { get; set; } public string InternalNotes { get; set; } = ""; }
    public class DstWithSecret { public int Id { get; set; } public string InternalNotes { get; set; } = ""; }

    [Fact]
    public void ReverseMap_mirrors_Ignore_by_default_preventing_round_trip_leak()
    {
        var cfg = new MapperConfiguration(c =>
            c.CreateMap<SrcWithSecret, DstWithSecret>()
             .ForMember(d => d.InternalNotes, opt => opt.Ignore())
             .ReverseMap());
        var mapper = cfg.CreateMapper();

        // Forward: Ignore drops InternalNotes.
        var forward = mapper.Map<SrcWithSecret, DstWithSecret>(
            new SrcWithSecret { Id = 1, InternalNotes = "sensitive-forward" });
        Assert.Equal(1, forward.Id);
        Assert.Equal("", forward.InternalNotes);

        // Reverse: without mirror, convention-match would copy InternalNotes. Mirror default-on
        // means it stays at the Src default (""). A DTO carrying an injected value is dropped.
        var reverse = mapper.Map<DstWithSecret, SrcWithSecret>(
            new DstWithSecret { Id = 2, InternalNotes = "sensitive-reverse" });
        Assert.Equal(2, reverse.Id);
        Assert.Equal("", reverse.InternalNotes);
    }

    // Opt-out for strict AutoMapper v14 parity. Migrators whose existing code relied on v14's
    // forward-only Ignore can set MirrorIgnoreOnReverse = false and get the old behavior.
    [Fact]
    public void ReverseMap_matches_v14_when_MirrorIgnoreOnReverse_false()
    {
        var cfg = new MapperConfiguration(c =>
        {
            c.MirrorIgnoreOnReverse = false;
            c.CreateMap<SrcWithSecret, DstWithSecret>()
             .ForMember(d => d.InternalNotes, opt => opt.Ignore())
             .ReverseMap();
        });
        var mapper = cfg.CreateMapper();

        var reverse = mapper.Map<DstWithSecret, SrcWithSecret>(
            new DstWithSecret { Id = 2, InternalNotes = "came-via-convention" });
        Assert.Equal(2, reverse.Id);
        Assert.Equal("came-via-convention", reverse.InternalNotes); // v14 parity — convention copied it.
    }

    // Edge: forward Ignored member has no corresponding member on the reverse destination.
    // Mirror must be a no-op rather than throw or blank-key the dictionary.
    public class SrcNarrow { public int Id { get; set; } }
    public class DstWide { public int Id { get; set; } public string Extra { get; set; } = ""; }

    [Fact]
    public void ReverseMap_Ignore_mirror_skips_members_missing_on_reverse_destination()
    {
        var cfg = new MapperConfiguration(c =>
            c.CreateMap<SrcNarrow, DstWide>()
             .ForMember(d => d.Extra, opt => opt.Ignore())
             .ReverseMap());
        var mapper = cfg.CreateMapper();

        var reverse = mapper.Map<DstWide, SrcNarrow>(new DstWide { Id = 3, Extra = "ignored" });
        Assert.Equal(3, reverse.Id);
    }

    // Explicit reverse configuration wins over the mirrored Ignore default — a caller who
    // Ignored on forward but explicitly wanted the reverse to copy should get the copy.
    [Fact]
    public void ReverseMap_explicit_reverse_config_wins_over_mirrored_Ignore()
    {
        var cfg = new MapperConfiguration(c =>
        {
            var forward = c.CreateMap<SrcWithSecret, DstWithSecret>()
                           .ForMember(d => d.InternalNotes, opt => opt.Ignore());
            forward.ReverseMap()
                   .ForMember(s => s.InternalNotes, opt => opt.MapFrom(d => d.InternalNotes));
        });
        var mapper = cfg.CreateMapper();

        var reverse = mapper.Map<DstWithSecret, SrcWithSecret>(
            new DstWithSecret { Id = 2, InternalNotes = "caller-said-copy-this" });
        Assert.Equal("caller-said-copy-this", reverse.InternalNotes);
    }

    // I5 coverage: renamed-member MapFrom should auto-reverse. AutoMapper v14 inverts a
    // simple `ForMember(d => d.X, opt => opt.MapFrom(s => s.Y))` into the equivalent
    // reverse member map without the user calling ForMember on the reverse expression.
    // Pre-fix, ReverseMap() returned an empty MappingExpression and the reverse map
    // convention-matched by name — "Email" on the reverse source wouldn't find "EmailAddress"
    // on the reverse destination, so the field ended up empty.
    public class SrcRenamed { public int Id { get; set; } public string Email { get; set; } = ""; }
    public class DstRenamed { public int Id { get; set; } public string EmailAddress { get; set; } = ""; }

    [Fact]
    public void ReverseMap_auto_reverses_simple_member_MapFrom()
    {
        var cfg = new MapperConfiguration(c =>
            c.CreateMap<SrcRenamed, DstRenamed>()
             .ForMember(d => d.EmailAddress, opt => opt.MapFrom(s => s.Email))
             .ReverseMap());
        var mapper = cfg.CreateMapper();

        var forward = mapper.Map<SrcRenamed, DstRenamed>(new SrcRenamed { Id = 1, Email = "x@y" });
        Assert.Equal("x@y", forward.EmailAddress);

        var reverse = mapper.Map<DstRenamed, SrcRenamed>(new DstRenamed { Id = 2, EmailAddress = "a@b" });
        Assert.Equal(2, reverse.Id);
        Assert.Equal("a@b", reverse.Email); // <- pre-fix this was "" (empty string default)
    }

    // Non-reversible forward configs (func, resolver, UseValue, Ignore) must NOT be
    // auto-carried to the reverse — the reverse destination member can just fall back to
    // convention matching by name, which is AutoMapper v14's behavior and the safest default.
    public class SrcWithName { public string First { get; set; } = ""; public string Last { get; set; } = ""; }
    public class DstWithName { public string FullName { get; set; } = ""; public string First { get; set; } = ""; public string Last { get; set; } = ""; }

    [Fact]
    public void ReverseMap_drops_non_reversible_func_MapFrom()
    {
        var cfg = new MapperConfiguration(c =>
            c.CreateMap<SrcWithName, DstWithName>()
             .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.First + " " + s.Last))
             .ReverseMap());
        var mapper = cfg.CreateMapper();

        // Reverse map should still work by convention for First and Last. FullName on the
        // reverse-source isn't reversible (it was a computed forward value) — it convention-
        // matches the reverse-dest's FullName, which is fine.
        var reverse = mapper.Map<DstWithName, SrcWithName>(
            new DstWithName { FullName = "ignored", First = "Alice", Last = "Zed" });
        Assert.Equal("Alice", reverse.First);
        Assert.Equal("Zed", reverse.Last);
    }
}
