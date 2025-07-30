using System.Text.Json.Serialization;

namespace CosmosDbEmulatorSample.ApiService.Models;

/// <summary>
/// Represents an order in the e-commerce system
/// </summary>
public class Order
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [JsonPropertyName("items")]
    public List<OrderItem> Items { get; set; } = new();

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("shippingAddress")]
    public Address? ShippingAddress { get; set; }

    [JsonPropertyName("orderDate")]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("expectedDeliveryDate")]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [JsonPropertyName("actualDeliveryDate")]
    public DateTime? ActualDeliveryDate { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("_etag")]
    public string? ETag { get; set; }
}

/// <summary>
/// Represents an item within an order
/// </summary>
public class OrderItem
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice => Quantity * UnitPrice;
}

/// <summary>
/// Represents the status of an order
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Refunded
}
