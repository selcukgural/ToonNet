using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.SourceGenerators.Tests.Models;

/// <summary>
/// Simple test model with basic properties.
/// </summary>
[ToonSerializable]
public partial class SimpleModel
{
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the age.</summary>
    public int Age { get; set; }

    /// <summary>Gets or sets the balance.</summary>
    public decimal Balance { get; set; }

    /// <summary>Gets or sets whether the model is active.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Model with nullable properties for null handling tests.
/// </summary>
[ToonSerializable]
public partial class NullableModel
{
    /// <summary>Gets or sets the optional name.</summary>
    public string? OptionalName { get; set; }

    /// <summary>Gets or sets the optional age.</summary>
    public int? OptionalAge { get; set; }

    /// <summary>Gets or sets the score.</summary>
    public double Score { get; set; }
}

/// <summary>
/// Model with camelCase naming policy.
/// </summary>
[ToonSerializable(NamingPolicy = PropertyNamingPolicy.CamelCase)]
public partial class CamelCaseModel
{
    /// <summary>Gets or sets the first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address.</summary>
    public string EmailAddress { get; set; } = string.Empty;
}

/// <summary>
/// Model with snake_case naming policy.
/// </summary>
[ToonSerializable(NamingPolicy = PropertyNamingPolicy.SnakeCase)]
public partial class SnakeCaseModel
{
    /// <summary>Gets or sets the first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the phone number.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the is verified flag.</summary>
    public bool IsVerified { get; set; }
}
