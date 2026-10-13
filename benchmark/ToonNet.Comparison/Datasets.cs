namespace ToonNet.Comparison;

/// <summary>Deterministic test data.</summary>
public static class Datasets
{
    private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static List<Employee> Employees(int count) => Enumerable.Range(0, count).Select(i => new Employee
    {
        Id = i,
        Name = $"User {i}",
        Email = $"user{i}@example.com",
        Age = 20 + i % 50,
        Score = 50.5 + i % 50 * 0.25,
        IsActive = i % 2 == 0,
        Department = $"Dept {i % 10}",
        Salary = 50_000.50m + i % 50_000,
        JoinedAt = Start.AddMinutes(i * 37)
    }).ToList();

    public static List<Order> Orders(int count) => Enumerable.Range(0, count).Select(i => new Order
    {
        Id = i,
        Customer = new Customer { Name = $"Customer {i}", Email = $"customer{i}@example.com" },
        ShipTo = new Address { City = $"City {i % 100}", ZipCode = $"{10000 + i % 90000}" },
        Total = 10.25m + i % 1000,
        Paid = i % 3 != 0,
        CreatedAt = Start.AddSeconds(i * 61.5),
        Tags = [$"tag{i % 5}", $"tag{i % 7}"]
    }).ToList();

    public static List<Product> Products(int count) => Enumerable.Range(0, count).Select(i => new Product
    {
        Sku = $"SKU-{i:D6}",
        Name = $"Product {i}",
        Size = new Dimensions { Width = 10 + i % 7, Height = 2.5 + i % 3, Depth = 0.75 * (1 + i % 4) },
        Price = new Money { Amount = 9.99m + i % 100, Currency = i % 2 == 0 ? "EUR" : "USD" },
        Stock = i % 250
    }).ToList();

    public static Dictionary<string, Server> Servers(int count) => Enumerable.Range(0, count).ToDictionary(
        i => $"node-{i}",
        i => new Server { Host = $"10.0.{i / 256 % 256}.{i % 256}", Port = 8000 + i % 1000, Region = i % 3 == 0 ? "eu-west" : "us-east", Healthy = i % 5 != 0 });
}
