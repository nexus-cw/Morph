// Compatibility tests — exercise realistic v14 consumer patterns. Run under both
// AutoMapper v14 and Morph. Any behavioral drift is a compat regression.
using Morph;
using Compat.Shared.Domain;
using Compat.Shared.Profiles;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Compat.Shared.Tests;

public class CompatTests
{
    private static IMapper BuildMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CustomerProfile>();
            cfg.AddProfile<PersonProfile>();
            cfg.AddProfile<MoneyProfile>();
        });
        return config.CreateMapper();
    }

    // --- Basic property-by-name convention ---

    [Fact]
    public void PropertyByName_MapsMatchingProperties()
    {
        var mapper = BuildMapper();
        var src = new Order { OrderId = 42, PlacedAt = new DateTime(2026, 1, 15), Total = 99.50m };

        var dto = mapper.Map<Order, OrderDto>(src);

        dto.OrderId.ShouldBe(42);
        dto.PlacedAt.ShouldBe(new DateTime(2026, 1, 15));
        dto.Total.ShouldBe(99.50m);
    }

    [Fact]
    public void PropertyByName_NestedObjectsAreMapped()
    {
        var mapper = BuildMapper();
        var customer = new Customer
        {
            Id = 1, FirstName = "Ada", LastName = "Lovelace",
            DateOfBirth = new DateTime(1815, 12, 10),
            HomeAddress = new Address { Street = "1 Countess Ln", City = "London", PostalCode = "W1" }
        };

        var dto = mapper.Map<Customer, CustomerDto>(customer);

        dto.HomeAddress.ShouldNotBeNull();
        dto.HomeAddress!.Street.ShouldBe("1 Countess Ln");
        dto.HomeAddress.City.ShouldBe("London");
    }

    [Fact]
    public void PropertyByName_NullNestedSource_ProducesNullDestination()
    {
        var mapper = BuildMapper();
        var customer = new Customer { Id = 1, FirstName = "A", LastName = "B" }; // HomeAddress null

        var dto = mapper.Map<Customer, CustomerDto>(customer);

        dto.HomeAddress.ShouldBeNull();
    }

    // --- ForMember + MapFrom ---

    [Fact]
    public void ForMember_MapFromLambda_ComputesFullName()
    {
        var mapper = BuildMapper();
        var customer = new Customer { FirstName = "Ada", LastName = "Lovelace" };

        var dto = mapper.Map<Customer, CustomerDto>(customer);

        dto.FullName.ShouldBe("Ada Lovelace");
    }

    [Fact]
    public void ForMember_MapFromMethodCall_ComputesAge()
    {
        var mapper = BuildMapper();
        var customer = new Customer
        {
            FirstName = "A", LastName = "B",
            DateOfBirth = new DateTime(1990, 4, 15)
        };

        var dto = mapper.Map<Customer, CustomerDto>(customer);

        dto.Age.ShouldBe(36); // Relative to 2026-04-15 in profile
    }

    [Fact]
    public void ForMember_MapFromEnumToString()
    {
        var mapper = BuildMapper();
        var customer = new Customer { FirstName = "A", LastName = "B", Status = CustomerStatus.Suspended };

        var dto = mapper.Map<Customer, CustomerDto>(customer);

        dto.Status.ShouldBe("Suspended");
    }

    // --- Collections ---

    [Fact]
    public void Collections_ListOfOrders_MapsElementWise()
    {
        var mapper = BuildMapper();
        var customer = new Customer
        {
            FirstName = "A", LastName = "B",
            Orders = new List<Order>
            {
                new Order { OrderId = 1, Total = 10m },
                new Order { OrderId = 2, Total = 20m },
                new Order { OrderId = 3, Total = 30m }
            }
        };

        var dto = mapper.Map<Customer, CustomerDto>(customer);

        dto.Orders.Count.ShouldBe(3);
        dto.Orders.Select(o => o.OrderId).ShouldBe(new[] { 1, 2, 3 });
        dto.Orders.Select(o => o.Total).ShouldBe(new[] { 10m, 20m, 30m });
    }

    [Fact]
    public void Collections_EmptySource_ProducesEmptyDestination()
    {
        var mapper = BuildMapper();
        var customer = new Customer { FirstName = "A", LastName = "B", Orders = new List<Order>() };

        var dto = mapper.Map<Customer, CustomerDto>(customer);

        dto.Orders.ShouldNotBeNull();
        dto.Orders.Count.ShouldBe(0);
    }

    // --- ReverseMap ---

    [Fact]
    public void ReverseMap_AddressDto_MapsBackToAddress()
    {
        var mapper = BuildMapper();
        var dto = new AddressDto { Street = "5 Oak St", City = "Wellington", PostalCode = "6011" };

        var model = mapper.Map<AddressDto, Address>(dto);

        model.Street.ShouldBe("5 Oak St");
        model.City.ShouldBe("Wellington");
        model.PostalCode.ShouldBe("6011");
    }

    [Fact]
    public void ReverseMap_RoundTripPreservesValues()
    {
        var mapper = BuildMapper();
        var original = new Order { OrderId = 99, PlacedAt = new DateTime(2026, 3, 1), Total = 77.77m };

        var dto = mapper.Map<Order, OrderDto>(original);
        var roundTripped = mapper.Map<OrderDto, Order>(dto);

        roundTripped.OrderId.ShouldBe(99);
        roundTripped.PlacedAt.ShouldBe(new DateTime(2026, 3, 1));
        roundTripped.Total.ShouldBe(77.77m);
    }

    // --- ConstructUsing ---

    [Fact]
    public void ConstructUsing_BuildsImmutableDestination()
    {
        var mapper = BuildMapper();
        var src = new PersonSource { FirstName = "Grace", LastName = "Hopper" };

        var person = mapper.Map<PersonSource, ImmutablePerson>(src);

        person.FirstName.ShouldBe("Grace");
        person.LastName.ShouldBe("Hopper");
    }

    // --- ConvertUsing / ITypeConverter ---

    [Fact]
    public void ConvertUsing_TypeConverter_FormatsMoney()
    {
        var mapper = BuildMapper();
        var src = new MoneyAmount { Value = 1234.5m, Currency = "NZD" };

        var formatted = mapper.Map<MoneyAmount, FormattedMoney>(src);

        formatted.Display.ShouldBe("NZD 1234.50");
    }

    // --- Map(source, existingDestination) overload ---

    [Fact]
    public void MapIntoExistingDestination_OverwritesFields()
    {
        var mapper = BuildMapper();
        var existing = new AddressDto { Street = "OLD", City = "OLD", PostalCode = "OLD" };
        var src = new Address { Street = "NEW", City = "NEW", PostalCode = "NEW" };

        var result = mapper.Map(src, existing);

        result.ShouldBeSameAs(existing);
        existing.Street.ShouldBe("NEW");
        existing.City.ShouldBe("NEW");
        existing.PostalCode.ShouldBe("NEW");
    }

    // --- Profiles / AddProfile ---

    [Fact]
    public void Profiles_MultipleProfilesAllRegistered()
    {
        var mapper = BuildMapper();

        // If all three profiles registered, all three map paths work.
        Should.NotThrow(() => mapper.Map<Order, OrderDto>(new Order()));
        Should.NotThrow(() => mapper.Map<PersonSource, ImmutablePerson>(new PersonSource()));
        Should.NotThrow(() => mapper.Map<MoneyAmount, FormattedMoney>(new MoneyAmount { Currency = "X" }));
    }

    // --- Non-generic runtime-type overload ---

    [Fact]
    public void Map_RuntimeTypes_Dispatch()
    {
        var mapper = BuildMapper();
        var src = new Address { Street = "7 Kauri", City = "Auckland", PostalCode = "1010" };

        object? result = mapper.Map(src, typeof(Address), typeof(AddressDto));

        var dto = result.ShouldBeOfType<AddressDto>();
        dto.City.ShouldBe("Auckland");
    }

    [Fact]
    public void Map_GenericDestinationOnly_UsesRuntimeSourceType()
    {
        var mapper = BuildMapper();
        object src = new Address { Street = "9 Rimu", City = "Hamilton", PostalCode = "3200" };

        var dto = mapper.Map<AddressDto>(src);

        dto.City.ShouldBe("Hamilton");
    }

    // --- Primitive/coercion behavior ---

    [Fact]
    public void Decimal_Total_PreservesPrecision()
    {
        var mapper = BuildMapper();
        var order = new Order { OrderId = 1, Total = 3.14159m };

        var dto = mapper.Map<Order, OrderDto>(order);

        dto.Total.ShouldBe(3.14159m);
    }

    [Fact]
    public void DateTime_PlacedAt_PreservesKindAndValue()
    {
        var mapper = BuildMapper();
        var placed = new DateTime(2026, 6, 15, 12, 30, 45, DateTimeKind.Utc);
        var order = new Order { OrderId = 1, PlacedAt = placed };

        var dto = mapper.Map<Order, OrderDto>(order);

        dto.PlacedAt.ShouldBe(placed);
    }

    // --- AssertConfigurationIsValid ---

    [Fact]
    public void AssertConfigurationIsValid_OnValidConfig_DoesNotThrow()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Address, AddressDto>();
        });

        Should.NotThrow(() => config.AssertConfigurationIsValid());
    }

    // --- Large-ish end-to-end flow ---

    [Fact]
    public void EndToEnd_CustomerWithOrdersAndAddress_MapsCompletely()
    {
        var mapper = BuildMapper();
        var customer = new Customer
        {
            Id = 7,
            FirstName = "Ada",
            LastName = "Lovelace",
            DateOfBirth = new DateTime(1990, 1, 1),
            Status = CustomerStatus.Active,
            InternalNotes = "secret",
            HomeAddress = new Address { Street = "1 Countess", City = "London", PostalCode = "W1" },
            Orders = new List<Order>
            {
                new Order { OrderId = 1, PlacedAt = new DateTime(2025, 12, 1), Total = 50m },
                new Order { OrderId = 2, PlacedAt = new DateTime(2026, 2, 14), Total = 125.99m }
            }
        };

        var dto = mapper.Map<Customer, CustomerDto>(customer);

        dto.Id.ShouldBe(7);
        dto.FullName.ShouldBe("Ada Lovelace");
        dto.Age.ShouldBe(36);
        dto.Status.ShouldBe("Active");
        dto.HomeAddress!.City.ShouldBe("London");
        dto.Orders.Count.ShouldBe(2);
        dto.Orders[1].Total.ShouldBe(125.99m);
    }

    [Fact]
    public void EndToEnd_ReverseCustomerMapNotConfigured_ThrowsOrIgnoredConsistently()
    {
        // This is a one-way map (Customer → CustomerDto has no ReverseMap). AutoMapper and
        // Morph both refuse to reverse it. Behavior: throw on Map attempt.
        var mapper = BuildMapper();
        var dto = new CustomerDto { Id = 1, FullName = "X Y", Age = 30, Status = "Active" };

        Should.Throw<Exception>(() => mapper.Map<CustomerDto, Customer>(dto));
    }
}
