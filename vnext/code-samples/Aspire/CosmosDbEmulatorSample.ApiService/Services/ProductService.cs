using Microsoft.Azure.Cosmos;
using CosmosDbEmulatorSample.ApiService.Models;

namespace CosmosDbEmulatorSample.ApiService.Services;

/// <summary>
/// Service for managing products in Cosmos DB
/// </summary>
public class ProductService
{
    private readonly CosmosClient _cosmosClient;
    private Container _container = null!;
    private readonly ILogger<ProductService> _logger;
    private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
    private bool _isInitialized = false;

    public ProductService(CosmosClient cosmosClient, ILogger<ProductService> logger)
    {
        _cosmosClient = cosmosClient;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized)
        {
            // Double-check that the container still exists and is accessible
            if (await IsContainerAccessibleAsync())
                return;
            
            // Container is not accessible, reset initialization flag
            _logger.LogWarning("Products container is not accessible, reinitializing...");
            _isInitialized = false;
        }

        await _initializationSemaphore.WaitAsync();
        try
        {
            if (_isInitialized && await IsContainerAccessibleAsync())
                return;

            await InitializeAsync();
            _isInitialized = true;
        }
        finally
        {
            _initializationSemaphore.Release();
        }
    }

    private async Task<bool> IsContainerAccessibleAsync()
    {
        try
        {
            if (_container == null)
                return false;

            // Try to read the container properties to verify it exists and is accessible
            await _container.ReadContainerAsync();
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Products container not found, will reinitialize");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Products container not accessible, will reinitialize");
            return false;
        }
    }

    private async Task InitializeAsync()
    {
        const int maxRetries = 10;
        const int delayMs = 2000;
        
        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                _logger.LogInformation("Initializing Products container (attempt {Retry}/{MaxRetries})", retry, maxRetries);
                
                // Create database if it doesn't exist
                var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync("SampleDB");
                
                // Create container if it doesn't exist
                _container = await database.Database.CreateContainerIfNotExistsAsync(
                    id: "Products",
                    partitionKeyPath: "/category",
                    throughput: 400);

                _logger.LogInformation("Products container initialized successfully");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Products container (attempt {Retry}/{MaxRetries})", retry, maxRetries);
                
                if (retry == maxRetries)
                {
                    _logger.LogError(ex, "Failed to initialize Products container after {MaxRetries} attempts", maxRetries);
                    throw;
                }
                
                await Task.Delay(delayMs, CancellationToken.None);
            }
        }
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    public async Task<Product> CreateProductAsync(Product product)
    {
        await EnsureInitializedAsync();
        
        try
        {
            product.Id = Guid.NewGuid().ToString();
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            var response = await _container.CreateItemAsync(product, new PartitionKey(product.Category));
            _logger.LogInformation("Created product with ID: {ProductId}", product.Id);
            
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create product");
            throw;
        }
    }

    /// <summary>
    /// Get a product by ID and category
    /// </summary>
    public async Task<Product?> GetProductAsync(string id, string category)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var response = await _container.ReadItemAsync<Product>(id, new PartitionKey(category));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Product not found: {ProductId}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get product: {ProductId}", id);
            throw;
        }
    }

    /// <summary>
    /// Get all products with optional category filter
    /// </summary>
    public async Task<List<Product>> GetProductsAsync(string? category = null, int maxItems = 100)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var queryText = category != null 
                ? "SELECT * FROM c WHERE c.category = @category"
                : "SELECT * FROM c";

            var queryDefinition = new QueryDefinition(queryText);
            if (category != null)
            {
                queryDefinition.WithParameter("@category", category);
            }

            var query = _container.GetItemQueryIterator<Product>(queryDefinition);
            var results = new List<Product>();

            while (query.HasMoreResults && results.Count < maxItems)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            _logger.LogInformation("Retrieved {Count} products", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get products");
            throw;
        }
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    public async Task<Product?> UpdateProductAsync(string id, string category, Product updatedProduct)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var existingProduct = await GetProductAsync(id, category);
            if (existingProduct == null)
            {
                return null;
            }

            // Preserve the original ID, creation date, and update timestamp
            updatedProduct.Id = id;
            updatedProduct.CreatedAt = existingProduct.CreatedAt;
            updatedProduct.UpdatedAt = DateTime.UtcNow;
            updatedProduct.ETag = existingProduct.ETag;

            var response = await _container.ReplaceItemAsync(
                updatedProduct, 
                id, 
                new PartitionKey(category));

            _logger.LogInformation("Updated product with ID: {ProductId}", id);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Product not found for update: {ProductId}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update product: {ProductId}", id);
            throw;
        }
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    public async Task<bool> DeleteProductAsync(string id, string category)
    {
        await EnsureInitializedAsync();
        
        try
        {
            await _container.DeleteItemAsync<Product>(id, new PartitionKey(category));
            _logger.LogInformation("Deleted product with ID: {ProductId}", id);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Product not found for deletion: {ProductId}", id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete product: {ProductId}", id);
            throw;
        }
    }
}
