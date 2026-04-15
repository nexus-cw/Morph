// Domain models used by the compat fixture. Shared source compiled against both
// AutoMapper v14 and Morph — any difference in behavior shows up as a test delta.
namespace Compat.Shared.Domain;

using System;
using System.Collections.Generic;

public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    public Address? HomeAddress { get; set; }
    public List<Order> Orders { get; set; } = new();
    public CustomerStatus Status { get; set; }
    public string? InternalNotes { get; set; }
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string PostalCode { get; set; } = "";
}

public class Order
{
    public int OrderId { get; set; }
    public DateTime PlacedAt { get; set; }
    public decimal Total { get; set; }
}

public enum CustomerStatus { Active, Suspended, Closed }

// --- DTOs ---

public class CustomerDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";          // MapFrom lambda
    public int Age { get; set; }                        // MapFrom + resolver
    public AddressDto? HomeAddress { get; set; }
    public List<OrderDto> Orders { get; set; } = new();
    public string Status { get; set; } = "";            // enum → string coercion
    // InternalNotes intentionally absent — tests Ignore()
}

public class AddressDto
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string PostalCode { get; set; } = "";
}

public class OrderDto
{
    public int OrderId { get; set; }
    public DateTime PlacedAt { get; set; }
    public decimal Total { get; set; }
}

// Constructor-based destination for ConstructUsing tests.
public class ImmutablePerson
{
    public ImmutablePerson(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
    public string FirstName { get; }
    public string LastName { get; }
}

public class PersonSource
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}

// Type converter scenario.
public class MoneyAmount
{
    public decimal Value { get; set; }
    public string Currency { get; set; } = "";
}

public class FormattedMoney
{
    public string Display { get; set; } = "";
}
