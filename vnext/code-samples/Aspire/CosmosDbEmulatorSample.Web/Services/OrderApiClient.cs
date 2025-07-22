using System.Text.Json.Serialization;

namespace CosmosDbEmulatorSample.Web.Services;

public class OrderApiClient(HttpClient httpClient, ILogger<OrderApiClient> logger)
{
    public async Task<List<OrderDto>> GetOrdersAsync()
    {
        try
        {
            logger.LogInformation("Fetching orders from API");
            var orders = await httpClient.GetFromJsonAsync<List<OrderDto>>("/orders");
            logger.LogInformation("Successfully fetched {Count} orders", orders?.Count ?? 0);
            return orders ?? new List<OrderDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch orders");
            throw;
        }
    }

    public async Task<OrderDto?> GetOrderAsync(string id, string customerId)
    {
        try
        {
            logger.LogInformation("Fetching order {Id} from API", id);
            var order = await httpClient.GetFromJsonAsync<OrderDto>($"/orders/{id}?customerId={Uri.EscapeDataString(customerId)}");
            logger.LogInformation("Successfully fetched order {Id}", id);
            return order;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch order {Id}", id);
            throw;
        }
    }

    public async Task<OrderDto> CreateOrderAsync(OrderDto order)
    {
        try
        {
            logger.LogInformation("Creating new order for customer {CustomerId}", order.CustomerId);
            var response = await httpClient.PostAsJsonAsync("/orders", order);
            response.EnsureSuccessStatusCode();
            var createdOrder = await response.Content.ReadFromJsonAsync<OrderDto>();
            logger.LogInformation("Successfully created order {Id}", createdOrder?.Id);
            return createdOrder!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create order");
            throw;
        }
    }
}

public record OrderDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;
    
    [JsonPropertyName("orderDate")]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";
    
    [JsonPropertyName("items")]
    public List<OrderItemDto> Items { get; set; } = new();
    
    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }
}

public record OrderItemDto
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
