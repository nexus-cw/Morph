// Realistic v14-style consumer profile. Uses features a production app commonly combines:
// ForMember + MapFrom lambda, Ignore, nested types, collections, enum→string.
//
// Source is written against Morph directly. The `Consumer.AutoMapper.csproj` project
// sed-rewrites `using Morph;` → `using AutoMapper;` at build time so the same source
// compiles against AutoMapper v14 NuGet. Any behavioral difference → test delta.
using Morph;
using Compat.Shared.Domain;
using System;

namespace Compat.Shared.Profiles;

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Address, AddressDto>().ReverseMap();
        CreateMap<Order, OrderDto>().ReverseMap();

        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FirstName + " " + s.LastName))
            .ForMember(d => d.Age, o => o.MapFrom(s => YearsBetween(s.DateOfBirth, new DateTime(2026, 4, 15))))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
    }

    private static int YearsBetween(DateTime from, DateTime to)
    {
        var years = to.Year - from.Year;
        if (to < from.AddYears(years)) years--;
        return years;
    }
}

public class PersonProfile : Profile
{
    public PersonProfile()
    {
        CreateMap<PersonSource, ImmutablePerson>()
            .ConstructUsing(src => new ImmutablePerson(src.FirstName, src.LastName));
    }
}

public class MoneyProfile : Profile
{
    public MoneyProfile()
    {
        CreateMap<MoneyAmount, FormattedMoney>()
            .ConvertUsing<MoneyConverter>();
    }
}

public class MoneyConverter : ITypeConverter<MoneyAmount, FormattedMoney>
{
    public FormattedMoney Convert(MoneyAmount source, FormattedMoney destination, ResolutionContext context)
        => new FormattedMoney { Display = $"{source.Currency} {source.Value:0.00}" };
}
