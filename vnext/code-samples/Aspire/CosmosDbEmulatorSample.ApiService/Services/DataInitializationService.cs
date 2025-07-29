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

            // Check if data already exists - check all entity types for more robust detection
            var existingProducts = await productService.GetProductsAsync();
            var existingCustomers = await customerService.GetCustomersAsync();
            var existingOrders = await orderService.GetOrdersAsync();
            
            if (existingProducts.Any() && existingCustomers.Any() && existingOrders.Any())
            {
                _logger.LogInformation("Sample data already exists (Products: {ProductCount}, Customers: {CustomerCount}, Orders: {OrderCount}), skipping initialization", 
                    existingProducts.Count, existingCustomers.Count, existingOrders.Count);
                return;
            }

            _logger.LogInformation("Found {ProductCount} products, {CustomerCount} customers, {OrderCount} orders. Proceeding with data initialization...", 
                existingProducts.Count, existingCustomers.Count, existingOrders.Count);

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
                    // Check if product already exists by searching for it
                    var existingProduct = existingProducts.FirstOrDefault(p => 
                        p.Name.Equals(product.Name, StringComparison.OrdinalIgnoreCase) && 
                        p.Category.Equals(product.Category, StringComparison.OrdinalIgnoreCase));
                    
                    if (existingProduct != null)
                    {
                        _logger.LogInformation("Product already exists: {ProductName}, skipping creation", product.Name);
                        createdProducts.Add(existingProduct);
                    }
                    else
                    {
                        var created = await productService.CreateProductAsync(product);
                        createdProducts.Add(created);
                        _logger.LogInformation("Created product: {ProductName}", created.Name);
                    }
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
                    // Check if customer already exists by email
                    var existingCustomer = existingCustomers.FirstOrDefault(c => 
                        c.Email.Equals(customer.Email, StringComparison.OrdinalIgnoreCase));
                    
                    if (existingCustomer != null)
                    {
                        _logger.LogInformation("Customer already exists: {CustomerEmail}, skipping creation", customer.Email);
                        createdCustomers.Add(existingCustomer);
                    }
                    else
                    {
                        var created = await customerService.CreateCustomerAsync(customer);
                        createdCustomers.Add(created);
                        _logger.LogInformation("Created customer: {CustomerName}", $"{created.FirstName} {created.LastName}");
                    }
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
                        // Check if order already exists for this customer with same products
                        var customerOrders = existingOrders.Where(o => o.CustomerId == order.CustomerId).ToList();
                        var orderExists = customerOrders.Any(existingOrder => 
                            existingOrder.Items.Count == order.Items.Count &&
                            existingOrder.Items.All(ei => order.Items.Any(oi => 
                                oi.ProductId == ei.ProductId && oi.Quantity == ei.Quantity)));
                        
                        if (orderExists)
                        {
                            _logger.LogInformation("Similar order already exists for customer: {CustomerId}, skipping creation", order.CustomerId);
                        }
                        else
                        {
                            var created = await orderService.CreateOrderAsync(order);
                            _logger.LogInformation("Created order: {OrderId} for customer {CustomerId}", created.Id, created.CustomerId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create order for customer: {CustomerId}", order.CustomerId);
                    }
                }
            }

            _logger.LogInformation("Data initialization completed successfully!");
            _logger.LogInformation("Products: {ProductCount} total ({NewProductCount} created), Customers: {CustomerCount} total ({NewCustomerCount} created)", 
                createdProducts.Count, createdProducts.Count - existingProducts.Count,
                createdCustomers.Count, createdCustomers.Count - existingCustomers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize sample data");
        }
    }
}
