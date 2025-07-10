# Azure Cosmos DB vnext-preview Demo

This demo shows how to use the Azure Cosmos DB vnext-preview emulator with hierarchical partition keys.

## Prerequisites

- Docker and Docker Compose
- .NET 9.0 SDK

## Quick Start

1. **Start the Cosmos DB vnext-preview emulator:**
   ```bash
   ./start-emulator.sh
   ```

   Or manually:
   ```bash
   docker-compose up -d cosmosdb-vnext
   ```

2. **Wait for the emulator to be ready** (usually takes 30-60 seconds)

3. **Run the demo application:**
   ```bash
   dotnet run --project src/cosmosdb-vnext-test/cosmosdb-vnext-test.csproj
   ```

## What the Demo Does

The demo application demonstrates:

- **Hierarchical Partition Keys**: Uses `/pk` and `/queryfield` as a hierarchical partition key
- **CRUD Operations**: Create, Read, Update, Delete documents with hierarchical partition keys
- **Querying**: Various query scenarios including ORDER BY
- **Change Feed**: Demonstrates the Change Feed functionality
- **Upsert Operations**: Shows how to upsert documents

## Accessing the Data Explorer

Once the emulator is running, you can access the Data Explorer at:
- **Data Explorer**: http://localhost:1234
- **Cosmos DB Endpoint**: https://localhost:8081

## Important Notes

- The **vnext-preview** version uses **HTTPS by default** (unlike the regular emulator)
- Data Explorer runs on port **1234** (not 8081/_explorer)
- The .NET SDK requires HTTPS mode with this version
- SSL certificate validation is bypassed in the demo code for local development

## Troubleshooting

- **Emulator won't start**: Check Docker logs with `docker-compose logs cosmosdb-vnext`
- **SSL/TLS errors**: The demo code includes SSL bypass for the local emulator
- **Port conflicts**: Ensure ports 8081, 8900-8902, 10250-10256, and 10350 are available

## Stopping the Emulator

```bash
docker-compose down
```

## Notes

- The emulator uses the standard Cosmos DB emulator key
- Data persistence is disabled by default for faster startup
- The emulator allocates 4GB of memory
