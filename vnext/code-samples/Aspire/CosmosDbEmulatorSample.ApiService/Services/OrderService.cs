using Microsoft.Azure.Cosmos;
using CosmosDbEmulatorSample.ApiService.Models;

namespace CosmosDbEmulatorSample.ApiService.Services;

/// <summary>
/// Service for managing orders in Cosmos DB
/// </summary>
public class OrderService
{
    private readonly CosmosClient _cosmosClient;
    private Container _container = null!;
    private readonly ILogger<OrderService> _logger;

    public OrderService(CosmosClient cosmosClient, ILogger<OrderService> logger)
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
                _logger.LogInformation("Initializing Orders container (attempt {Retry}/{MaxRetries})", retry, maxRetries);
                
                // Create database if it doesn't exist
                var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync("SampleDB");
                
                // Create container if it doesn't exist
                _container = await database.Database.CreateContainerIfNotExistsAsync(
                    id: "Orders",
                    partitionKeyPath: "/customerId",
                    throughput: 400);

                _logger.LogInformation("Orders container initialized successfully");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Orders container (attempt {Retry}/{MaxRetries})", retry, maxRetries);
                
                if (retry == maxRetries)
                {
                    _logger.LogError(ex, "Failed to initialize Orders container after {MaxRetries} attempts", maxRetries);
                    throw;
                }
                
                await Task.Delay(delayMs, CancellationToken.None);
            }
        }
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    public async Task<Order> CreateOrderAsync(Order order)
    {
        try
        {
            order.Id = Guid.NewGuid().ToString();
            order.OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}";
            order.OrderDate = DateTime.UtcNow;
            
            // Calculate total amount
            order.TotalAmount = order.Items.Sum(item => item.TotalPrice);
            
            // Set expected delivery date (7 days from order date)
            order.ExpectedDeliveryDate = order.OrderDate.AddDays(7);

            var response = await _container.CreateItemAsync(order, new PartitionKey(order.CustomerId));
            _logger.LogInformation("Created order with ID: {OrderId}", order.Id);
            
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order");
            throw;
        }
    }

    /// <summary>
    /// Get an order by ID and customer ID
    /// </summary>
    public async Task<Order?> GetOrderAsync(string id, string customerId)
    {
        try
        {
            var response = await _container.ReadItemAsync<Order>(id, new PartitionKey(customerId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Order not found: {OrderId}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order: {OrderId}", id);
            throw;
        }
    }

    /// <summary>
    /// Get all orders for a customer
    /// </summary>
    public async Task<List<Order>> GetOrdersByCustomerAsync(string customerId, int maxItems = 100)
    {
        try
        {
            var queryText = "SELECT * FROM c WHERE c.customerId = @customerId ORDER BY c.orderDate DESC";
            var queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@customerId", customerId);

            var query = _container.GetItemQueryIterator<Order>(queryDefinition);
            var results = new List<Order>();

            while (query.HasMoreResults && results.Count < maxItems)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            _logger.LogInformation("Retrieved {Count} orders for customer: {CustomerId}", results.Count, customerId);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders for customer: {CustomerId}", customerId);
            throw;
        }
    }

    /// <summary>
    /// Get all orders with optional status filter
    /// </summary>
    public async Task<List<Order>> GetOrdersAsync(OrderStatus? status = null, int maxItems = 100)
    {
        try
        {
            var queryText = status.HasValue 
                ? "SELECT * FROM c WHERE c.status = @status ORDER BY c.orderDate DESC"
                : "SELECT * FROM c ORDER BY c.orderDate DESC";

            var queryDefinition = new QueryDefinition(queryText);
            if (status.HasValue)
            {
                queryDefinition.WithParameter("@status", status.Value.ToString());
            }

            var query = _container.GetItemQueryIterator<Order>(queryDefinition);
            var results = new List<Order>();

            while (query.HasMoreResults && results.Count < maxItems)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            _logger.LogInformation("Retrieved {Count} orders", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders");
            throw;
        }
    }

    /// <summary>
    /// Update an existing order
    /// </summary>
    public async Task<Order?> UpdateOrderAsync(string id, string customerId, Order updatedOrder)
    {
        try
        {
            var existingOrder = await GetOrderAsync(id, customerId);
            if (existingOrder == null)
            {
                return null;
            }

            // Preserve the original ID, order number, order date
            updatedOrder.Id = id;
            updatedOrder.CustomerId = customerId;
            updatedOrder.OrderNumber = existingOrder.OrderNumber;
            updatedOrder.OrderDate = existingOrder.OrderDate;
            updatedOrder.ETag = existingOrder.ETag;
            
            // Recalculate total amount
            updatedOrder.TotalAmount = updatedOrder.Items.Sum(item => item.TotalPrice);

            var response = await _container.ReplaceItemAsync(
                updatedOrder, 
                id, 
                new PartitionKey(customerId));

            _logger.LogInformation("Updated order with ID: {OrderId}", id);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Order not found for update: {OrderId}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order: {OrderId}", id);
            throw;
        }
    }

    /// <summary>
    /// Update order status
    /// </summary>
    public async Task<Order?> UpdateOrderStatusAsync(string id, string customerId, OrderStatus newStatus)
    {
        try
        {
            var order = await GetOrderAsync(id, customerId);
            if (order == null)
            {
                return null;
            }

            order.Status = newStatus;
            
            // Set actual delivery date if status is Delivered
            if (newStatus == OrderStatus.Delivered && order.ActualDeliveryDate == null)
            {
                order.ActualDeliveryDate = DateTime.UtcNow;
            }

            var response = await _container.ReplaceItemAsync(order, id, new PartitionKey(customerId));
            _logger.LogInformation("Updated order status to {Status} for order: {OrderId}", newStatus, id);
            
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order status for order: {OrderId}", id);
            throw;
        }
    }

    /// <summary>
    /// Delete an order
    /// </summary>
    public async Task<bool> DeleteOrderAsync(string id, string customerId)
    {
        try
        {
            await _container.DeleteItemAsync<Order>(id, new PartitionKey(customerId));
            _logger.LogInformation("Deleted order with ID: {OrderId}", id);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Order not found for deletion: {OrderId}", id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete order: {OrderId}", id);
            throw;
        }
    }

    /// <summary>
    /// Get order summary statistics
    /// </summary>
    public async Task<object> GetOrderSummaryAsync()
    {
        try
        {
            var queryText = @"SELECT 
                            COUNT(1) as TotalOrders,
                            SUM(c.totalAmount) as TotalRevenue,
                            AVG(c.totalAmount) as AverageOrderValue,
                            c.status
                            FROM c 
                            GROUP BY c.status";

            var queryDefinition = new QueryDefinition(queryText);
            var query = _container.GetItemQueryIterator<dynamic>(queryDefinition);
            var results = new List<dynamic>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            _logger.LogInformation("Retrieved order summary statistics");
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order summary");
            throw;
        }
    }
}
