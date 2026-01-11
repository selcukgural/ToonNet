namespace ToonNet.Demo.Samples;

/// <summary>
/// Complete e-commerce order with nested objects, collections, and all TOON-supported types.
/// Demonstrates: strings, numbers, booleans, nulls, dates, nested objects, arrays, dictionaries
/// </summary>
public class ECommerceOrder
{
    public string OrderId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Customer Customer { get; set; } = new();
    public Address ShippingAddress { get; set; } = new();
    public Address BillingAddress { get; set; } = new();
    public List<OrderItem> Items { get; set; } = new();
    public PaymentInfo PaymentInfo { get; set; } = new();
    public ShippingInfo Shipping { get; set; } = new();
    public PricingInfo Pricing { get; set; } = new();
    public OrderMetadata Metadata { get; set; } = new();
}

public class Customer
{
    public string CustomerId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string MembershipLevel { get; set; } = string.Empty;
    public DateTime MemberSince { get; set; }
    public int LoyaltyPoints { get; set; }
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public GeoCoordinates Coordinates { get; set; } = new();
}

public class GeoCoordinates
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public double TaxRate { get; set; }
    public decimal Total { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
    public List<ProductReview> Reviews { get; set; } = new();
}

public class ProductReview
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool Verified { get; set; }
}

public class PaymentInfo
{
    public string Method { get; set; } = string.Empty;
    public string CardType { get; set; } = string.Empty;
    public string Last4Digits { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string AuthorizationCode { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public bool IsSuccessful { get; set; }
}

public class ShippingInfo
{
    public string Method { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime EstimatedDelivery { get; set; }
    public decimal Cost { get; set; }
    public bool IsFreeShipping { get; set; }
}

public class PricingInfo
{
    public decimal Subtotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "USD";
}

public class OrderMetadata
{
    public string Source { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string? ReferralCode { get; set; }
    public List<string> CouponCodes { get; set; } = new();
    public bool IsGiftOrder { get; set; }
    public string? GiftMessage { get; set; }
    public string? Notes { get; set; }
}
