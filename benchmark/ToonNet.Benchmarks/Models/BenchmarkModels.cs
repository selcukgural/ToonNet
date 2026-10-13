using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.Benchmarks.Models;

/// <summary>
///     Simple benchmark model with 5 properties.
/// </summary>
[ToonSerializable]
public partial class SimpleBenchmarkModel
{
    public string Name { get; set; } = "TestUser";
    public int Age { get; set; } = 30;
    public string Email { get; set; } = "test@example.com";
    public double Score { get; set; } = 95.5;
    public bool IsActive { get; set; } = true;
}

/// <summary>
///     Medium complexity model with 10 properties.
/// </summary>
[ToonSerializable]
public partial class MediumBenchmarkModel
{
    public string FirstName { get; set; } = "John";
    public string LastName { get; set; } = "Doe";
    public int Age { get; set; } = 30;
    public string Email { get; set; } = "john@example.com";
    public string Phone { get; set; } = "555-1234";
    public decimal Salary { get; set; } = 75000.50m;
    public double Rating { get; set; } = 4.5;
    public bool IsManager { get; set; } = false;
    public int DepartmentId { get; set; } = 5;
    public long EmployeeId { get; set; } = 123456789;
}

/// <summary>
///     Complex model with 15 properties and various types.
/// </summary>
[ToonSerializable]
public partial class ComplexBenchmarkModel
{
    public string CompanyName { get; set; } = "TechCorp";
    public string Address { get; set; } = "123 Main St";
    public string City { get; set; } = "Springfield";
    public string State { get; set; } = "IL";
    public string ZipCode { get; set; } = "62701";
    public string Phone { get; set; } = "217-555-1234";
    public string Website { get; set; } = "https://techcorp.com";
    public int FoundedYear { get; set; } = 1995;
    public decimal Revenue { get; set; } = 5000000.00m;
    public double EmployeeCount { get; set; } = 250;
    public float MarketShare { get; set; } = 12.5f;
    public byte Status { get; set; } = 1;
    public short SectorCode { get; set; } = 10;
    public uint RegistrationId { get; set; } = 987654321;
    public bool IsPublic { get; set; } = true;
}

/// <summary>
///     Model with nullable properties for testing null handling.
/// </summary>
[ToonSerializable]
public partial class NullableBenchmarkModel
{
    public string? OptionalName { get; set; }
    public int? OptionalAge { get; set; }
    public decimal? OptionalSalary { get; set; }
    public bool IsVerified { get; set; }
    public string RequiredField { get; set; } = "Always present";
}
/// <summary>
///     Order status for <see cref="OrderBenchmarkModel"/>.
/// </summary>
public enum OrderStatus
{
    Pending,
    Shipped,
    Delivered
}

/// <summary>
///     Customer of an order, a positional record (read through its constructor).
/// </summary>
[ToonSerializable]
public partial record CustomerBenchmarkModel(string Name, string Email);

/// <summary>
///     Shipping address of an order.
/// </summary>
[ToonSerializable]
public partial class AddressBenchmarkModel
{
    public string City { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

/// <summary>
///     An order with a nested record, a nested object, a list and common scalar types; lists of it are written as
///     expanded list items.
/// </summary>
[ToonSerializable]
public partial class OrderBenchmarkModel
{
    public int Id { get; set; }
    public CustomerBenchmarkModel Customer { get; set; } = new("", "");
    public AddressBenchmarkModel Address { get; set; } = new();
    public decimal Total { get; set; }
    public double Rating { get; set; }
    public bool Paid { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public List<string> Tags { get; set; } = [];
}

/// <summary>
///     A flat row of primitives; lists of it are written in tabular form.
/// </summary>
[ToonSerializable]
public partial class RowBenchmarkModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public double Score { get; set; }
    public bool IsActive { get; set; }
    public string Department { get; set; } = "";
    public long Salary { get; set; }
}
