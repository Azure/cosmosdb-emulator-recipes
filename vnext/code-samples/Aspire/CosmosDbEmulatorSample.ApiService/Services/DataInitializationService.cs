using CosmosDbEmulatorSample.ApiService.Models;

namespace CosmosDbEmulatorSample.ApiService.Services;

/// <summary>
/// Background service that initializes sample data when the application starts
/// </summary>
public class DataInitializationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataInitializationService> _logger;

    public DataInitializationService(IServiceProvider serviceProvider, ILogger<DataInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit for the application to fully start
        await Task.Delay(3000, stoppingToken);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var productService = scope.ServiceProvider.GetRequiredService<ProductService>();
            var customerService = scope.ServiceProvider.GetRequiredService<CustomerService>();
            var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

            _logger.LogInformation("Starting data initialization...");

            // Check if data already exists
            var existingProducts = await productService.GetProductsAsync();
            if (existingProducts.Any())
            {
                _logger.LogInformation("Sample data already exists, skipping initialization");
                return;
            }

            // Create sample products
            var products = new List<Product>
            {
                new() 
                { 
                    Name = "Laptop", 
                    Description = "High-performance laptop", 
                    Price = 999.99m, 
                    Category = "Electronics", 
                    StockQuantity = 10 
                },
                new() 
                { 
                    Name = "Smartphone", 
                    Description = "Latest smartphone", 
                    Price = 699.99m, 
                    Category = "Electronics", 
                    StockQuantity = 25 
                },
                new() 
                { 
                    Name = "Coffee Mug", 
                    Description = "Ceramic coffee mug", 
                    Price = 12.99m, 
                    Category = "Home", 
                    StockQuantity = 100 
                },
                new() 
                { 
                    Name = "Programming Book", 
                    Description = "Learn .NET programming", 
                    Price = 29.99m, 
                    Category = "Books", 
                    StockQuantity = 50 
                },
                new() 
                { 
                    Name = "Wireless Headphones", 
                    Description = "Premium wireless headphones", 
                    Price = 149.99m, 
                    Category = "Electronics", 
                    StockQuantity = 30 
                }
            };

            var createdProducts = new List<Product>();
            foreach (var product in products)
            {
                try
                {
                    var created = await productService.CreateProductAsync(product);
                    createdProducts.Add(created);
                    _logger.LogInformation("Created product: {ProductName}", created.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create product: {ProductName}", product.Name);
                }
            }

            // Create sample customers
            var customers = new List<Customer>
            {
                new() 
                { 
                    FirstName = "John", 
                    LastName = "Doe", 
                    Email = "john.doe@example.com", 
                    PhoneNumber = "555-0123" 
                },
                new() 
                { 
                    FirstName = "Jane", 
                    LastName = "Smith", 
                    Email = "jane.smith@example.com", 
                    PhoneNumber = "555-0124" 
                },
                new() 
                { 
                    FirstName = "Bob", 
                    LastName = "Johnson", 
                    Email = "bob.johnson@example.com", 
                    PhoneNumber = "555-0125" 
                }
            };

            var createdCustomers = new List<Customer>();
            foreach (var customer in customers)
            {
                try
                {
                    var created = await customerService.CreateCustomerAsync(customer);
                    createdCustomers.Add(created);
                    _logger.LogInformation("Created customer: {CustomerName}", $"{created.FirstName} {created.LastName}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create customer: {CustomerName}", $"{customer.FirstName} {customer.LastName}");
                }
            }

            // Create sample orders (only if we have products and customers)
            if (createdProducts.Count > 0 && createdCustomers.Count > 0)
            {
                var orders = new List<Order>
                {
                    new()
                    {
                        CustomerId = createdCustomers[0].CustomerId,
                        Items = new List<OrderItem>
                        {
                            new() 
                            { 
                                ProductId = createdProducts[0].Id, 
                                ProductName = createdProducts[0].Name, 
                                Quantity = 1, 
                                UnitPrice = createdProducts[0].Price 
                            },
                            new() 
                            { 
                                ProductId = createdProducts[4].Id, 
                                ProductName = createdProducts[4].Name, 
                                Quantity = 1, 
                                UnitPrice = createdProducts[4].Price 
                            }
                        }
                    },
                    new()
                    {
                        CustomerId = createdCustomers[1].CustomerId,
                        Items = new List<OrderItem>
                        {
                            new() 
                            { 
                                ProductId = createdProducts[1].Id, 
                                ProductName = createdProducts[1].Name, 
                                Quantity = 1, 
                                UnitPrice = createdProducts[1].Price 
                            }
                        }
                    }
                };

                foreach (var order in orders)
                {
                    try
                    {
                        var created = await orderService.CreateOrderAsync(order);
                        _logger.LogInformation("Created order: {OrderId} for customer {CustomerId}", created.Id, created.CustomerId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create order for customer: {CustomerId}", order.CustomerId);
                    }
                }
            }

            _logger.LogInformation("Data initialization completed successfully!");
            _logger.LogInformation("Created {ProductCount} products, {CustomerCount} customers", 
                createdProducts.Count, createdCustomers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize sample data");
        }
    }
}
