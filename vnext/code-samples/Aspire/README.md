# Azure Cosmos DB Emulator Sample with .NET Aspire

This sample demonstrates how to use Azure Cosmos DB with .NET Aspire, featuring a complete e-commerce application with Products, Customers, and Orders management.

## 🏗️ Architecture

The solution consists of:

- **CosmosDbEmulatorSample.AppHost**: Aspire orchestration host that configures the Cosmos DB emulator and application services
- **CosmosDbEmulatorSample.ApiService**: REST API service providing CRUD operations for Products, Customers, and Orders
- **CosmosDbEmulatorSample.Web**: Blazor Server web application with UI for managing data
- **CosmosDbEmulatorSample.ServiceDefaults**: Shared service configuration and defaults

## 🚀 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for Cosmos DB emulator)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or [VS Code](https://code.visualstudio.com/) with C# extension
- [LibMan CLI](https://docs.microsoft.com/en-us/aspnet/core/client-side/libman/libman-cli) (for client-side library management)

## 🏃‍♂️ Getting Started

### 1. Start the Aspire Application

Navigate to the project directory and run the AppHost:

```bash
# Navigate to the Aspire sample directory
cd cosmosdb-emulator-recipes/vnext/code-samples/Aspire

# Install LibMan CLI (if not already installed)
dotnet tool install -g Microsoft.Web.LibraryManager.Cli

# Restore client-side libraries (Bootstrap, etc.)
cd CosmosDbEmulatorSample.Web && libman restore && cd ..

# Build the solution first (recommended)
dotnet build

# Run the AppHost project (use the full absolute path to the .csproj file)
dotnet run --project CosmosDbEmulatorSample.AppHost/CosmosDbEmulatorSample.AppHost.csproj
```

The application will start and display the Aspire dashboard URL (typically `http://localhost:17103`).

**Note**: The first run may take several minutes as it downloads and starts the Cosmos DB emulator Docker container.

### 2. Access the Aspire Dashboard

Open your browser and navigate to the Aspire dashboard URL shown in the console. The dashboard provides:

- **Overview**: Application topology and service health
- **Resources**: All configured resources including Cosmos DB emulator
- **Console Logs**: Real-time logs from all services
- **Traces**: Distributed tracing information
- **Metrics**: Performance metrics and telemetry

### 3. Open Azure Cosmos DB Data Explorer

From the Aspire dashboard:

1. Navigate to the **Resources** tab
2. Find the **cosmos-db** resource
3. Click on the **Data Explorer** endpoint (`http://localhost:8081/_explorer/index.html`)

The Data Explorer allows you to:
- Browse databases and containers
- Query data using SQL
- View and edit documents
- Monitor performance metrics

### 4. Access the Sample Web Application

From the Aspire dashboard:

1. Navigate to the **Resources** tab
2. Find the **webapp** resource
3. Click on the external endpoint (`https://localhost:7160`)

The web application provides interfaces to:
- **Products**: View, create, edit, and delete products with categories
- **Customers**: Manage customer information and addresses
- **Orders**: Create and track orders with multiple items
- **Dashboard**: Overview of data statistics

### 5. Test the API with curl or api-test.sh

#### Using the provided test script:

```bash
# Make the script executable
chmod +x api-test.sh

# Run all tests (discovers API URL automatically)
./api-test.sh

# Run with verbose output
./api-test.sh --verbose

# Run with debug information
./api-test.sh --debug
```

#### Using curl manually:

First, find the API service URL from the Aspire dashboard (`https://localhost:7554`):

```bash
# Get API information
curl -k https://localhost:7554/

# Test Products API
curl -k https://localhost:7554/products
curl -k https://localhost:7554/products?category=electronics

# Create a new product
curl -k -X POST https://localhost:7554/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Sample Product",
    "description": "A test product",
    "price": 29.99,
    "category": "electronics",
    "stockQuantity": 100
  }'

# Test Customers API
curl -k https://localhost:7554/customers

# Test Orders API
curl -k https://localhost:7554/orders
```

## 📊 Sample Data

The application automatically initializes with sample data including:

- **Products**: Electronics, clothing, books, and home goods
- **Customers**: Sample customer profiles with addresses
- **Orders**: Example orders with multiple items and different statuses

## 🔧 Configuration

### Cosmos DB Emulator Settings

The emulator is configured in `CosmosDbEmulatorSample.AppHost/Program.cs`:

```csharp
var cosmosDb = builder.AddAzureCosmosDB("cosmos-db").RunAsPreviewEmulator(
    emulator =>
    {
        emulator.WithDataExplorer();      // Enables Data Explorer UI
        emulator.WithGatewayPort(8081);   // Sets the gateway port
    });
```

### Database Structure

- **Database**: `ECommerceDB`
- **Containers**:
  - `Products` (partition key: `/category`)
  - `Customers` (partition key: `/customerId`)
  - `Orders` (partition key: `/customerId`)

## 🛠️ Development

### Project Structure

```
├── CosmosDbEmulatorSample.AppHost/          # Aspire orchestration
├── CosmosDbEmulatorSample.ApiService/       # REST API
│   ├── Models/                              # Data models
│   ├── Services/                            # Business logic
│   └── Program.cs                           # API endpoints
├── CosmosDbEmulatorSample.Web/              # Blazor web app
│   ├── Components/Pages/                    # Razor pages
│   ├── Services/                            # HTTP clients
│   ├── libman.json                          # Client-side library configuration
│   └── wwwroot/lib/                         # Generated client-side libraries (not in source control)
├── CosmosDbEmulatorSample.ServiceDefaults/  # Shared configuration
└── api-test.sh                              # API testing script
```

### Client-Side Dependencies

This project uses [LibMan (Library Manager)](https://docs.microsoft.com/en-us/aspnet/core/client-side/libman/) to manage client-side libraries like Bootstrap. The `libman.json` file specifies which libraries to download, and `libman restore` downloads them to `wwwroot/lib/`. 

**Note**: The `wwwroot/lib/` directory is excluded from source control via `.gitignore` since these are generated files.

### Key Features

- **Service Discovery**: Automatic service-to-service communication
- **Health Checks**: Built-in health monitoring
- **Telemetry**: Distributed tracing and metrics
- **Configuration**: Centralized application configuration
- **Resilience**: Retry policies and circuit breakers

## 📚 API Endpoints

### Products
- `GET /products` - List all products (optional `?category=` filter)
- `GET /products/{id}?category={category}` - Get specific product
- `POST /products` - Create new product
- `PUT /products/{id}?category={category}` - Update product
- `DELETE /products/{id}?category={category}` - Delete product

### Customers
- `GET /customers` - List all customers
- `GET /customers/{id}` - Get specific customer
- `POST /customers` - Create new customer
- `PUT /customers/{id}` - Update customer
- `DELETE /customers/{id}` - Delete customer

### Orders
- `GET /orders` - List all orders (optional `?customerId=` filter)
- `GET /orders/{id}` - Get specific order
- `POST /orders` - Create new order
- `PUT /orders/{id}` - Update order
- `DELETE /orders/{id}` - Delete order

## 🔗 Documentation

For more information about Azure Cosmos DB integration with .NET Aspire, see:
[Use Linux-based emulator (preview)](https://learn.microsoft.com/en-us/dotnet/aspire/database/azure-cosmos-db-integration?tabs=dotnet-cli#use-linux-based-emulator-preview)

## 🐛 Troubleshooting

### Common Issues

1. **Docker not running**: Ensure Docker Desktop is running before starting the application
2. **Port conflicts**: Check that ports 8081 (Cosmos DB) and others are not in use
3. **Emulator startup**: The Cosmos DB emulator may take a few minutes to fully initialize

### Logs and Diagnostics

- Check the Aspire dashboard console logs for detailed error information
- Use `docker logs` to inspect the Cosmos DB emulator container
- Enable verbose logging by setting `Logging:LogLevel:Default=Debug` in appsettings

## 🏷️ Tags

`aspire`, `cosmos-db`, `emulator`, `blazor`, `web-api`, `docker`, `sample`, `ecommerce`