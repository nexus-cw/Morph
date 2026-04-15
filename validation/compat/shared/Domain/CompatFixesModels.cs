// Domain models for compat scenarios that exercise the four parity fixes (I3, I4, I5, B1).
// Kept separate from Models.cs so the existing compat scenarios aren't touched.
namespace Compat.Shared.Domain;

// --- I3: subtype nested map. Source has a concrete Dog; destination wants an AnimalDto.
// Only the base Animal → AnimalDto map is registered. Pre-fix Morph did exact (Dog, AnimalDto)
// lookup and fell through to coercion (throws). v14 walks assignability and uses the base map.

public abstract class Animal
{
    public string Name { get; set; } = "";
}

public class Dog : Animal
{
    public string Breed { get; set; } = "";
}

public class AnimalDto
{
    public string Name { get; set; } = "";
}

public class Owner
{
    public string OwnerName { get; set; } = "";
    public Animal? Pet { get; set; }
}

public class OwnerDto
{
    public string OwnerName { get; set; } = "";
    public AnimalDto? Pet { get; set; }
}

// --- I4: resolver that re-enters the mapper. MaxDepth = 1 should trip on both libraries.

public class OuterNode
{
    public InnerNode? Inner { get; set; }
}

public class InnerNode
{
    public int Value { get; set; }
}

public class OuterNodeDto
{
    public InnerNodeDto? Inner { get; set; }
}

public class InnerNodeDto
{
    public int Value { get; set; }
}

// --- I5 renamed member: round-trip across different member names via ReverseMap.

public class Contact
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
}

public class ContactDto
{
    public int Id { get; set; }
    public string EmailAddress { get; set; } = "";
}

// --- B1/I5 Ignore: sensitive field Ignored on forward must not round-trip on reverse.

public class AuditSource
{
    public int Id { get; set; }
    public string InternalNotes { get; set; } = "";
}

public class AuditDto
{
    public int Id { get; set; }
    public string InternalNotes { get; set; } = "";
}
