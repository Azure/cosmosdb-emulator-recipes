using Microsoft.Azure.Cosmos;
using CosmosDbEmulatorSample.ApiService.Models;

namespace CosmosDbEmulatorSample.ApiService.Services;

/// <summary>
/// Service for managing customers in Cosmos DB
/// </summary>
public class CustomerService
{
    private readonly CosmosClient _cosmosClient;
    private Container _container = null!;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(CosmosClient cosmosClient, ILogger<CustomerService> logger)
    {
        _cosmosClient = cosmosClient;
        _logger = logger;
        
        // Initialize database and container
        InitializeAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeAsync()
    {
        const int maxRetries = 10;
        const int delayMs = 2000;
        
        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                _logger.LogInformation("Initializing Customers container (attempt {Retry}/{MaxRetries})", retry, maxRetries);
                
                // Create database if it doesn't exist
                var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync("SampleDB");
                
                // Create container if it doesn't exist
                _container = await database.Database.CreateContainerIfNotExistsAsync(
                    id: "Customers",
                    partitionKeyPath: "/customerId",
                    throughput: 400);

                _logger.LogInformation("Customers container initialized successfully");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Customers container (attempt {Retry}/{MaxRetries})", retry, maxRetries);
                
                if (retry == maxRetries)
                {
                    _logger.LogError(ex, "Failed to initialize Customers container after {MaxRetries} attempts", maxRetries);
                    throw;
                }
                
                await Task.Delay(delayMs, CancellationToken.None);
            }
        }
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        try
        {
            customer.Id = Guid.NewGuid().ToString();
            
            // Generate CustomerId if not provided
            if (string.IsNullOrEmpty(customer.CustomerId))
            {
                customer.CustomerId = $"CUST-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}";
            }
            
            customer.CreatedAt = DateTime.UtcNow;
            customer.UpdatedAt = DateTime.UtcNow;

            var response = await _container.CreateItemAsync(customer, new PartitionKey(customer.CustomerId));
            _logger.LogInformation("Created customer with ID: {Id} and CustomerId: {CustomerId}", customer.Id, customer.CustomerId);
            
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create customer");
            throw;
        }
    }

    /// <summary>
    /// Get a customer by ID and customer ID
    /// </summary>
    public async Task<Customer?> GetCustomerAsync(string id, string customerId)
    {
        try
        {
            var response = await _container.ReadItemAsync<Customer>(id, new PartitionKey(customerId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Customer not found: {CustomerId}", customerId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get customer: {CustomerId}", customerId);
            throw;
        }
    }

    /// <summary>
    /// Get customer by customer ID
    /// </summary>
    public async Task<Customer?> GetCustomerByCustomerIdAsync(string customerId)
    {
        try
        {
            var queryText = "SELECT * FROM c WHERE c.customerId = @customerId";
            var queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@customerId", customerId);

            var query = _container.GetItemQueryIterator<Customer>(queryDefinition);
            
            if (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                return response.FirstOrDefault();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get customer by customer ID: {CustomerId}", customerId);
            throw;
        }
    }

    /// <summary>
    /// Get all customers
    /// </summary>
    public async Task<List<Customer>> GetCustomersAsync(int maxItems = 100)
    {
        try
        {
            var queryText = "SELECT * FROM c WHERE c.isActive = true";
            var queryDefinition = new QueryDefinition(queryText);

            var query = _container.GetItemQueryIterator<Customer>(queryDefinition);
            var results = new List<Customer>();

            while (query.HasMoreResults && results.Count < maxItems)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            _logger.LogInformation("Retrieved {Count} customers", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get customers");
            throw;
        }
    }

    /// <summary>
    /// Update an existing customer
    /// </summary>
    public async Task<Customer?> UpdateCustomerAsync(string id, string customerId, Customer updatedCustomer)
    {
        try
        {
            var existingCustomer = await GetCustomerAsync(id, customerId);
            if (existingCustomer == null)
            {
                return null;
            }

            // Preserve the original ID, customer ID, creation date, and update timestamp
            updatedCustomer.Id = id;
            updatedCustomer.CustomerId = customerId;
            updatedCustomer.CreatedAt = existingCustomer.CreatedAt;
            updatedCustomer.UpdatedAt = DateTime.UtcNow;
            updatedCustomer.ETag = existingCustomer.ETag;

            var response = await _container.ReplaceItemAsync(
                updatedCustomer, 
                id, 
                new PartitionKey(customerId));

            _logger.LogInformation("Updated customer with ID: {CustomerId}", customerId);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Customer not found for update: {CustomerId}", customerId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update customer: {CustomerId}", customerId);
            throw;
        }
    }

    /// <summary>
    /// Delete a customer (soft delete by setting isActive to false)
    /// </summary>
    public async Task<bool> DeleteCustomerAsync(string id, string customerId)
    {
        try
        {
            var customer = await GetCustomerAsync(id, customerId);
            if (customer == null)
            {
                return false;
            }

            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;

            await _container.ReplaceItemAsync(customer, id, new PartitionKey(customerId));
            _logger.LogInformation("Soft deleted customer with ID: {CustomerId}", customerId);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Customer not found for deletion: {CustomerId}", customerId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete customer: {CustomerId}", customerId);
            throw;
        }
    }

    /// <summary>
    /// Search customers by email
    /// </summary>
    public async Task<Customer?> GetCustomerByEmailAsync(string email)
    {
        try
        {
            var queryText = "SELECT * FROM c WHERE c.email = @email AND c.isActive = true";
            var queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@email", email);

            var query = _container.GetItemQueryIterator<Customer>(queryDefinition);
            
            if (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                return response.FirstOrDefault();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get customer by email: {Email}", email);
            throw;
        }
    }
}
