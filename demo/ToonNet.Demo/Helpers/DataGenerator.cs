using System;
using System.Collections.Generic;
using ToonNet.Demo.Models;

namespace ToonNet.Demo.Helpers;

/// <summary>
/// Generates realistic test data for demonstrations
/// </summary>
public static class DataGenerator
{
    public static PrimitiveTypesModel CreatePrimitiveTypes()
    {
        return new PrimitiveTypesModel
        {
            ByteValue = 255,
            SByteValue = -128,
            ShortValue = -32768,
            UShortValue = 65535,
            IntValue = -2147483648,
            UIntValue = 4294967295,
            LongValue = -9223372036854775808,
            ULongValue = 18446744073709551615,
            FloatValue = 3.14159f,
            DoubleValue = 2.71828,
            DecimalValue = 123456.789m,
            BoolValue = true,
            CharValue = 'A',
            StringValue = "Hello, TOON! 🎉",
            DateTimeValue = new DateTime(2026, 1, 11, 13, 30, 0),
            DateTimeOffsetValue = new DateTimeOffset(2026, 1, 11, 13, 30, 0, TimeSpan.FromHours(3)),
            TimeSpanValue = TimeSpan.FromHours(2.5),
            GuidValue = Guid.Parse("12345678-1234-1234-1234-123456789abc")
        };
    }

    public static TaskModel CreateTask()
    {
        return new TaskModel
        {
            Id = Guid.NewGuid(),
            Title = "Implement TOON Serialization",
            Description = "Create a comprehensive serialization library for TOON format",
            Priority = Priority.High,
            Status = Status.InProgress,
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            CompletedAt = null,
            Tags = new List<string> { "serialization", "toon", "library", "performance" },
            Metadata = new Dictionary<string, string>
            {
                { "assignee", "John Doe" },
                { "sprint", "Sprint 5" },
                { "points", "8" }
            }
        };
    }

    public static Company CreateCompany()
    {
        return new Company
        {
            Name = "TechCorp International",
            Address = new Address
            {
                Street = "123 Innovation Drive",
                City = "San Francisco",
                State = "CA",
                ZipCode = "94105",
                Country = "USA",
                Coordinates = new Coordinates { Latitude = 37.7749, Longitude = -122.4194 }
            },
            Departments = new List<Department>
            {
                new()
                {
                    Name = "Engineering",
                    Manager = new Employee
                    {
                        Id = Guid.NewGuid(),
                        FirstName = "Alice",
                        LastName = "Johnson",
                        Email = "alice.johnson@techcorp.com",
                        Phone = "+1-555-0101",
                        HireDate = new DateTime(2020, 3, 15),
                        Salary = 150000m,
                        Type = EmployeeType.FullTime,
                        Skills = new List<string> { "C#", "Architecture", "Leadership" }
                    },
                    Employees = new List<Employee>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            FirstName = "Bob",
                            LastName = "Smith",
                            Email = "bob.smith@techcorp.com",
                            Phone = "+1-555-0102",
                            HireDate = new DateTime(2021, 6, 1),
                            Salary = 120000m,
                            Type = EmployeeType.FullTime,
                            Skills = new List<string> { "C#", "ASP.NET", "SQL" }
                        }
                    },
                    Budget = new Budget
                    {
                        Allocated = 5000000m,
                        Spent = 3200000m,
                        CategoryBreakdown = new Dictionary<string, decimal>
                        {
                            { "Salaries", 2500000m },
                            { "Equipment", 400000m }
                        }
                    }
                }
            }
        };
    }

    public static UltimateComplexModel CreateUltimateModel()
    {
        return new UltimateComplexModel
        {
            Id = Guid.NewGuid(),
            Name = "Ultimate Complex Demo Model",
            Primitives = CreatePrimitiveTypes(),
            Nullables = new NullableTypesModel { NullableInt = 42, NullableString = "Nullable" },
            Collections = new CollectionsModel
            {
                IntList = new List<int> { 1, 2, 3, 5, 8, 13, 21 },
                StringArray = new[] { "alpha", "beta", "gamma" },
                StringIntDict = new Dictionary<string, int> { { "one", 1 }, { "two", 2 } }
            },
            Company = CreateCompany(),
            Tasks = new List<TaskModel> { CreateTask() },
            People = Array.Empty<PersonRecord>(), // Records require special handling
            Bounds = new Rectangle
            {
                Width = 1920,
                Height = 1080,
                TopLeft = new Point { X = 0, Y = 0, Z = 0 },
                BottomRight = new Point { X = 1920, Y = 1080, Z = 0 }
            },
            CreatedAt = DateTime.UtcNow
        };
    }
}
