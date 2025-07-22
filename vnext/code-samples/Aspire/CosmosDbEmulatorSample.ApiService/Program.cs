using CosmosDbEmulatorSample.ApiService.Models;
using CosmosDbEmulatorSample.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add Azure Cosmos DB client with proper serialization options
builder.AddAzureCosmosClient("cosmos-db", configureClientOptions: options =>
{
    // Configure Cosmos DB client options for proper JSON serialization
    options.SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
        IgnoreNullValues = true
    };
});

// Configure JSON options for HTTP responses
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<ProductService>();
builder.Services.AddSingleton<CustomerService>();
builder.Services.AddSingleton<OrderService>();

// Add background service for data initialization
builder.Services.AddHostedService<DataInitializationService>();

// Add OpenAPI/Swagger support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// API Root endpoint
app.MapGet("/", () => new
{
    Message = "Azure Cosmos DB Emulator Sample API",
    Version = "1.0",
    Documentation = "/swagger",
    Endpoints = new[]
    {
        "/products",
        "/customers", 
        "/orders"
    }
});

// === PRODUCT ENDPOINTS ===
app.MapGet("/products", async (ProductService productService, string? category = null) =>
{
    var products = await productService.GetProductsAsync(category);
    return Results.Ok(products);
}).WithName("GetProducts");

app.MapGet("/products/{id}", async (ProductService productService, string id, string category) =>
{
    var product = await productService.GetProductAsync(id, category);
    return product != null ? Results.Ok(product) : Results.NotFound();
}).WithName("GetProduct");

app.MapPost("/products", async (ProductService productService, Product product) =>
{
    var createdProduct = await productService.CreateProductAsync(product);
    return Results.Created($"/products/{createdProduct.Id}?category={createdProduct.Category}", createdProduct);
}).WithName("CreateProduct");

app.MapPut("/products/{id}", async (ProductService productService, string id, string category, Product product) =>
{
    var updatedProduct = await productService.UpdateProductAsync(id, category, product);
    return updatedProduct != null ? Results.Ok(updatedProduct) : Results.NotFound();
}).WithName("UpdateProduct");

app.MapDelete("/products/{id}", async (ProductService productService, string id, string category) =>
{
    var deleted = await productService.DeleteProductAsync(id, category);
    return deleted ? Results.NoContent() : Results.NotFound();
}).WithName("DeleteProduct");

// === CUSTOMER ENDPOINTS ===
app.MapGet("/customers", async (CustomerService customerService) =>
{
    var customers = await customerService.GetCustomersAsync();
    return Results.Ok(customers);
}).WithName("GetCustomers");

app.MapGet("/customers/{id}", async (CustomerService customerService, string id, string customerId) =>
{
    var customer = await customerService.GetCustomerAsync(id, customerId);
    return customer != null ? Results.Ok(customer) : Results.NotFound();
}).WithName("GetCustomer");

app.MapGet("/customers/by-customer-id/{customerId}", async (CustomerService customerService, string customerId) =>
{
    var customer = await customerService.GetCustomerByCustomerIdAsync(customerId);
    return customer != null ? Results.Ok(customer) : Results.NotFound();
}).WithName("GetCustomerByCustomerId");

app.MapGet("/customers/by-email/{email}", async (CustomerService customerService, string email) =>
{
    var customer = await customerService.GetCustomerByEmailAsync(email);
    return customer != null ? Results.Ok(customer) : Results.NotFound();
}).WithName("GetCustomerByEmail");

app.MapPost("/customers", async (CustomerService customerService, Customer customer) =>
{
    var createdCustomer = await customerService.CreateCustomerAsync(customer);
    return Results.Created($"/customers/{createdCustomer.Id}?customerId={createdCustomer.CustomerId}", createdCustomer);
}).WithName("CreateCustomer");

app.MapPut("/customers/{id}", async (CustomerService customerService, string id, string customerId, Customer customer) =>
{
    var updatedCustomer = await customerService.UpdateCustomerAsync(id, customerId, customer);
    return updatedCustomer != null ? Results.Ok(updatedCustomer) : Results.NotFound();
}).WithName("UpdateCustomer");

app.MapDelete("/customers/{id}", async (CustomerService customerService, string id, string customerId) =>
{
    var deleted = await customerService.DeleteCustomerAsync(id, customerId);
    return deleted ? Results.NoContent() : Results.NotFound();
}).WithName("DeleteCustomer");

// === ORDER ENDPOINTS ===
app.MapGet("/orders", async (OrderService orderService, [FromQuery] OrderStatus? status = null) =>
{
    var orders = await orderService.GetOrdersAsync(status);
    return Results.Ok(orders);
}).WithName("GetOrders");

app.MapGet("/orders/{id}", async (OrderService orderService, string id, string customerId) =>
{
    var order = await orderService.GetOrderAsync(id, customerId);
    return order != null ? Results.Ok(order) : Results.NotFound();
}).WithName("GetOrder");

app.MapGet("/orders/customer/{customerId}", async (OrderService orderService, string customerId) =>
{
    var orders = await orderService.GetOrdersByCustomerAsync(customerId);
    return Results.Ok(orders);
}).WithName("GetOrdersByCustomer");

app.MapPost("/orders", async (OrderService orderService, Order order) =>
{
    var createdOrder = await orderService.CreateOrderAsync(order);
    return Results.Created($"/orders/{createdOrder.Id}?customerId={createdOrder.CustomerId}", createdOrder);
}).WithName("CreateOrder");

app.MapPut("/orders/{id}", async (OrderService orderService, string id, string customerId, Order order) =>
{
    var updatedOrder = await orderService.UpdateOrderAsync(id, customerId, order);
    return updatedOrder != null ? Results.Ok(updatedOrder) : Results.NotFound();
}).WithName("UpdateOrder");

app.MapPatch("/orders/{id}/status", async (OrderService orderService, string id, string customerId, [FromBody] OrderStatus status) =>
{
    var updatedOrder = await orderService.UpdateOrderStatusAsync(id, customerId, status);
    return updatedOrder != null ? Results.Ok(updatedOrder) : Results.NotFound();
}).WithName("UpdateOrderStatus");

app.MapDelete("/orders/{id}", async (OrderService orderService, string id, string customerId) =>
{
    var deleted = await orderService.DeleteOrderAsync(id, customerId);
    return deleted ? Results.NoContent() : Results.NotFound();
}).WithName("DeleteOrder");

app.MapGet("/orders/summary", async (OrderService orderService) =>
{
    var summary = await orderService.GetOrderSummaryAsync();
    return Results.Ok(summary);
}).WithName("GetOrderSummary");

// === DATA SEEDING ENDPOINT ===
app.MapPost("/seed-data", async (ProductService productService, CustomerService customerService, OrderService orderService) =>
{
    try
    {
        // Create sample products
        var products = new List<Product>
        {
            new() { Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, Category = "Electronics", StockQuantity = 10 },
            new() { Name = "Smartphone", Description = "Latest smartphone", Price = 699.99m, Category = "Electronics", StockQuantity = 25 },
            new() { Name = "Coffee Mug", Description = "Ceramic coffee mug", Price = 12.99m, Category = "Home", StockQuantity = 100 },
            new() { Name = "Book", Description = "Programming guide", Price = 29.99m, Category = "Books", StockQuantity = 50 },
            new() { Name = "Headphones", Description = "Wireless headphones", Price = 149.99m, Category = "Electronics", StockQuantity = 30 }
        };

        var createdProducts = new List<Product>();
        foreach (var product in products)
        {
            createdProducts.Add(await productService.CreateProductAsync(product));
        }

        // Create sample customers
        var customers = new List<Customer>
        {
            new() { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", PhoneNumber = "555-0123" },
            new() { FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com", PhoneNumber = "555-0124" },
            new() { FirstName = "Bob", LastName = "Johnson", Email = "bob.johnson@example.com", PhoneNumber = "555-0125" }
        };

        var createdCustomers = new List<Customer>();
        foreach (var customer in customers)
        {
            createdCustomers.Add(await customerService.CreateCustomerAsync(customer));
        }

        // Create sample orders
        var orders = new List<Order>
        {
            new()
            {
                CustomerId = createdCustomers[0].CustomerId,
                Items = new List<OrderItem>
                {
                    new() { ProductId = createdProducts[0].Id, ProductName = createdProducts[0].Name, Quantity = 1, UnitPrice = createdProducts[0].Price },
                    new() { ProductId = createdProducts[4].Id, ProductName = createdProducts[4].Name, Quantity = 1, UnitPrice = createdProducts[4].Price }
                }
            },
            new()
            {
                CustomerId = createdCustomers[1].CustomerId,
                Items = new List<OrderItem>
                {
                    new() { ProductId = createdProducts[1].Id, ProductName = createdProducts[1].Name, Quantity = 1, UnitPrice = createdProducts[1].Price }
                }
            }
        };

        var createdOrders = new List<Order>();
        foreach (var order in orders)
        {
            createdOrders.Add(await orderService.CreateOrderAsync(order));
        }

        return Results.Ok(new
        {
            Message = "Sample data created successfully",
            ProductsCreated = createdProducts.Count,
            CustomersCreated = createdCustomers.Count,
            OrdersCreated = createdOrders.Count
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to seed data: {ex.Message}");
    }
}).WithName("SeedData");

app.MapDefaultEndpoints();

app.Run();
