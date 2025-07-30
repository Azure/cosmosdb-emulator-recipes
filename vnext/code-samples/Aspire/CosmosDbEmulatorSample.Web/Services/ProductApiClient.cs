using System.Text.Json.Serialization;

namespace CosmosDbEmulatorSample.Web.Services;

public class ProductApiClient(HttpClient httpClient, ILogger<ProductApiClient> logger)
{
    public async Task<List<ProductDto>> GetProductsAsync(string? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = "/products";
            if (!string.IsNullOrEmpty(category))
                url += $"?category={Uri.EscapeDataString(category)}";
                
            logger.LogInformation("Fetching products from API: {Url}", url);
            var products = await httpClient.GetFromJsonAsync<List<ProductDto>>(url, cancellationToken);
            logger.LogInformation("Successfully fetched {Count} products", products?.Count ?? 0);
            return products ?? new List<ProductDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch products");
            throw;
        }
    }

    public async Task<ProductDto?> GetProductAsync(string id, string category, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Fetching product {Id} from API", id);
            var product = await httpClient.GetFromJsonAsync<ProductDto>($"/products/{id}?category={Uri.EscapeDataString(category)}", cancellationToken);
            logger.LogInformation("Successfully fetched product {Id}", id);
            return product;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch product {Id}", id);
            throw;
        }
    }

    public async Task<ProductDto> CreateProductAsync(ProductDto product, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Creating new product: {Name}", product.Name);
            var response = await httpClient.PostAsJsonAsync("/products", product, cancellationToken);
            response.EnsureSuccessStatusCode();
            var createdProduct = await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken);
            logger.LogInformation("Successfully created product {Id}", createdProduct?.Id);
            return createdProduct!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create product");
            throw;
        }
    }
}

public record ProductDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("stockQuantity")]
    public int StockQuantity { get; set; }
}
