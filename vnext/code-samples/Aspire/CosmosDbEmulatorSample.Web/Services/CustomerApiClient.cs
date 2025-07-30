using System.Text.Json.Serialization;

namespace CosmosDbEmulatorSample.Web.Services;

public class CustomerApiClient(HttpClient httpClient, ILogger<CustomerApiClient> logger)
{
    public async Task<List<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Fetching customers from API");
            var customers = await httpClient.GetFromJsonAsync<List<CustomerDto>>("/customers", cancellationToken);
            logger.LogInformation("Successfully fetched {Count} customers", customers?.Count ?? 0);
            return customers ?? new List<CustomerDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch customers");
            throw;
        }
    }

    public async Task<CustomerDto?> GetCustomerAsync(string id, string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Fetching customer {Id} from API", id);
            var customer = await httpClient.GetFromJsonAsync<CustomerDto>($"/customers/{id}?customerId={Uri.EscapeDataString(customerId)}", cancellationToken);
            logger.LogInformation("Successfully fetched customer {Id}", id);
            return customer;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch customer {Id}", id);
            throw;
        }
    }

    public async Task<CustomerDto> CreateCustomerAsync(CustomerDto customer, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Creating new customer: {Name}", $"{customer.FirstName} {customer.LastName}");
            var response = await httpClient.PostAsJsonAsync("/customers", customer, cancellationToken);
            response.EnsureSuccessStatusCode();
            var createdCustomer = await response.Content.ReadFromJsonAsync<CustomerDto>(cancellationToken);
            logger.LogInformation("Successfully created customer {Id}", createdCustomer?.Id);
            return createdCustomer!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create customer");
            throw;
        }
    }
}

public record CustomerDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;
    
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("address")]
    public AddressDto? Address { get; set; }
    
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Helper property to display address as a single string
    public string AddressString => Address?.ToString() ?? string.Empty;
}

public record AddressDto
{
    [JsonPropertyName("street")]
    public string Street { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("zipCode")]
    public string ZipCode { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    public override string ToString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(Street)) parts.Add(Street);
        if (!string.IsNullOrEmpty(City)) parts.Add(City);
        if (!string.IsNullOrEmpty(State)) parts.Add(State);
        if (!string.IsNullOrEmpty(ZipCode)) parts.Add(ZipCode);
        if (!string.IsNullOrEmpty(Country)) parts.Add(Country);
        return string.Join(", ", parts);
    }
}
