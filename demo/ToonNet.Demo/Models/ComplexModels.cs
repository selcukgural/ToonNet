namespace ToonNet.Demo.Models;

/// <summary>
/// Demonstrates all supported primitive types
/// </summary>
public class PrimitiveTypesModel
{
    // Integer types
    public byte ByteValue { get; set; }
    public sbyte SByteValue { get; set; }
    public short ShortValue { get; set; }
    public ushort UShortValue { get; set; }
    public int IntValue { get; set; }
    public uint UIntValue { get; set; }
    public long LongValue { get; set; }
    public ulong ULongValue { get; set; }
    
    // Floating point types
    public float FloatValue { get; set; }
    public double DoubleValue { get; set; }
    public decimal DecimalValue { get; set; }
    
    // Other primitives
    public bool BoolValue { get; set; }
    public char CharValue { get; set; }
    public string StringValue { get; set; } = string.Empty;
    public DateTime DateTimeValue { get; set; }
    public DateTimeOffset DateTimeOffsetValue { get; set; }
    public TimeSpan TimeSpanValue { get; set; }
    public Guid GuidValue { get; set; }
}

/// <summary>
/// Demonstrates nullable types
/// </summary>
public class NullableTypesModel
{
    public int? NullableInt { get; set; }
    public bool? NullableBool { get; set; }
    public DateTime? NullableDateTime { get; set; }
    public Guid? NullableGuid { get; set; }
    public string? NullableString { get; set; }
}

/// <summary>
/// Demonstrates enum support
/// </summary>
public enum Priority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum Status
{
    Pending,
    InProgress,
    Completed,
    Cancelled
}

/// <summary>
/// Complex nested model with enums
/// </summary>
public class TaskModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Demonstrates collection types
/// </summary>
public class CollectionsModel
{
    public List<int> IntList { get; set; } = new();
    public string[] StringArray { get; set; } = Array.Empty<string>();
    public HashSet<string> StringSet { get; set; } = new();
    public Dictionary<string, int> StringIntDict { get; set; } = new();
    public List<List<int>> NestedList { get; set; } = new();
}

/// <summary>
/// Deep nesting demonstration
/// </summary>
public class Company
{
    public string Name { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public Coordinates Coordinates { get; set; } = new();
}

public class Coordinates
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class Department
{
    public string Name { get; set; } = string.Empty;
    public Employee Manager { get; set; } = new();
    public List<Employee> Employees { get; set; } = new();
    public Budget Budget { get; set; } = new();
}

public class Employee
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public decimal Salary { get; set; }
    public EmployeeType Type { get; set; }
    public List<string> Skills { get; set; } = new();
    public Address? HomeAddress { get; set; }
}

public enum EmployeeType
{
    FullTime,
    PartTime,
    Contractor,
    Intern
}

public class Budget
{
    public decimal Allocated { get; set; }
    public decimal Spent { get; set; }
    public Dictionary<string, decimal> CategoryBreakdown { get; set; } = new();
}

/// <summary>
/// Record type demonstration
/// </summary>
public record PersonRecord(
    string FirstName,
    string LastName,
    int Age,
    string Email);

/// <summary>
/// Record struct demonstration - using regular properties
/// </summary>
public struct Point
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}

/// <summary>
/// Regular struct demonstration
/// </summary>
public struct Rectangle
{
    public double Width { get; set; }
    public double Height { get; set; }
    public Point TopLeft { get; set; }
    public Point BottomRight { get; set; }
}

/// <summary>
/// Complex model combining all types
/// </summary>
public class UltimateComplexModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PrimitiveTypesModel Primitives { get; set; } = new();
    public NullableTypesModel Nullables { get; set; } = new();
    public CollectionsModel Collections { get; set; } = new();
    public Company Company { get; set; } = new();
    public List<TaskModel> Tasks { get; set; } = new();
    public PersonRecord[] People { get; set; } = Array.Empty<PersonRecord>();
    public Rectangle Bounds { get; set; }
    public Dictionary<string, List<Point>> PointsByCategory { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
