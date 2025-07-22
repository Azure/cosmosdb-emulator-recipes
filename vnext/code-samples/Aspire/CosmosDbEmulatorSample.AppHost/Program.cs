#pragma warning disable ASPIRECOSMOSDB001

var builder = DistributedApplication.CreateBuilder(args);

var cosmosDb = builder.AddAzureCosmosDB("cosmos-db").RunAsPreviewEmulator(
    emulator =>
    {
        emulator.WithDataExplorer();
        emulator.WithGatewayPort(8081);
    });

// The database and containers will be created automatically by the services
// when they first connect to the emulator

var apiService = builder.AddProject<Projects.CosmosDbEmulatorSample_ApiService>("apiservice")
    .WithReference(cosmosDb);

var webApp = builder.AddProject<Projects.CosmosDbEmulatorSample_Web>("webapp")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
