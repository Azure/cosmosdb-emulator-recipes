// Program.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

public class TestDocument
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("queryfield")]
    public string Queryfield { get; set; }

    [JsonProperty("pk")]
    public string PartitionKey { get; set; }  // This property must match the partition key path without leading slash

    [JsonProperty("city")]
    public string City { get; set; }
}

public class CosmosDbDemo
{
    // Update these constants with your own Cosmos DB endpoint and key
    private const string EndpointUrl = "http://localhost:8081";  // Use your actual endpoint and match protocol (http/https)
    private const string PrimaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    
    private CosmosClient cosmosClient;

    public static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Beginning CosmosDB Demo...");
            var demo = new CosmosDbDemo();
            await demo.RunDemoAsync();
            Console.WriteLine("Demo completed successfully!");
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"Cosmos DB Error: {ex.StatusCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    public CosmosDbDemo()
    {
        // Configure the connection to Cosmos DB
        // For local emulator, we need to configure TLS/SSL validation
        cosmosClient = new CosmosClient(EndpointUrl, PrimaryKey, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            HttpClientFactory = () => new System.Net.Http.HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            })
        });
    }

    private async Task RunDemoAsync()
    {
        // Create a unique database name and container name
        string databaseName = $"db-{Guid.NewGuid():N}";
        string containerName = $"container-{Guid.NewGuid():N}";
        
        Console.WriteLine($"Creating database: {databaseName}");
        Database database = await CreateDatabaseAsync(databaseName);
        
        Console.WriteLine($"Creating container: {containerName}");
        Container container = await CreateContainerAsync(database, containerName);
        
        // Create documents with different partition keys
        string partitionKey1 = "p1";
        string partitionKey2 = "p2";
        
        Console.WriteLine("Creating documents...");
        TestDocument document1 = await CreateDocumentAsync(container, "document1", "field1", partitionKey1, "Seattle");
        TestDocument document2 = await CreateDocumentAsync(container, "document2", "field2", partitionKey2, "Portland");

        Console.WriteLine("\nUpdating document...");
        await UpdateDocumentAndVerifyAsync(container, "document1", partitionKey1, "Chicago");
        
        Console.WriteLine("Reading documents with partition key filter...");
        await QueryDocumentsByPartitionKeyAsync(container, partitionKey1);
        
        Console.WriteLine("Reading all documents...");
        await QueryAllDocumentsAsync(container);

        Console.WriteLine("\nDeleting document...");
        await DeleteDocumentAndVerifyAsync(container, "document1", partitionKey1);

        Console.WriteLine("Cleaning up...");
        await database.DeleteAsync();
    }

    private async Task<Database> CreateDatabaseAsync(string databaseName)
    {
        // Create a new database
        DatabaseResponse databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
        Console.WriteLine($"Database created with status: {databaseResponse.StatusCode}");
        return databaseResponse.Database;
    }

    private async Task<Container> CreateContainerAsync(Database database, string containerName)
    {
        // Create a new container with a partition key
        ContainerProperties containerProperties = new ContainerProperties
        {
            Id = containerName,
            PartitionKeyPath = "/pk",  // Note: this path must match a property in your document
            IndexingPolicy = new IndexingPolicy
            {
                Automatic = true,
                IndexingMode = IndexingMode.Consistent
            }
        };

        ContainerResponse containerResponse = await database.CreateContainerIfNotExistsAsync(containerProperties);
        Console.WriteLine($"Container created with status: {containerResponse.StatusCode}");
        return containerResponse.Container;
    }

    private async Task<TestDocument> CreateDocumentAsync(Container container, string id, string queryField, string partitionKey, string city)
    {
        TestDocument document = new TestDocument
        {
            Id = id,
            Queryfield = queryField,
            PartitionKey = partitionKey,
            City = city
        };

        ItemResponse<TestDocument> response = await container.CreateItemAsync(document, new PartitionKey(partitionKey));
        Console.WriteLine($"Created document {id} - Status: {response.StatusCode}");
        return document;
    }

    private async Task UpdateDocumentAndVerifyAsync(Container container, string id, string partitionKey, string newCity)
    {
        Console.WriteLine($"Updating document {id} with new city: {newCity}");
        
        try
        {
            // First read the existing item
            ItemResponse<TestDocument> readResponse = await container.ReadItemAsync<TestDocument>(
                id: id,
                partitionKey: new PartitionKey(partitionKey)
            );
            
            TestDocument existingDocument = readResponse.Resource;
            Console.WriteLine($"Retrieved document: Id={existingDocument.Id}, City={existingDocument.City}");
            
            // Update the property
            existingDocument.City = newCity;
            
            // Replace the item with the updated document
            ItemResponse<TestDocument> updateResponse = await container.ReplaceItemAsync(
                item: existingDocument,
                id: id,
                partitionKey: new PartitionKey(partitionKey)
            );
            
            Console.WriteLine($"Updated document - Status: {updateResponse.StatusCode}");
            
            // Query to verify the update
            Console.WriteLine("Verifying update by querying the document:");
            ItemResponse<TestDocument> verifyResponse = await container.ReadItemAsync<TestDocument>(
                id: id,
                partitionKey: new PartitionKey(partitionKey)
            );
            
            TestDocument updatedDocument = verifyResponse.Resource;
            Console.WriteLine($"Verified document: Id={updatedDocument.Id}, City={updatedDocument.City}");
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine($"Document with id {id} not found!");
        }
    }

    private async Task DeleteDocumentAndVerifyAsync(Container container, string id, string partitionKey)
    {
        Console.WriteLine($"Deleting document {id} with partition key {partitionKey}");
        
        try
        {
            // Delete the document
            ItemResponse<TestDocument> deleteResponse = await container.DeleteItemAsync<TestDocument>(
                id: id,
                partitionKey: new PartitionKey(partitionKey)
            );
            
            Console.WriteLine($"Deleted document - Status: {deleteResponse.StatusCode}");
            
            // Verify deletion by attempting to read the document (should fail)
            Console.WriteLine("Verifying deletion by attempting to read the document:");
            try
            {
                ItemResponse<TestDocument> verifyResponse = await container.ReadItemAsync<TestDocument>(
                    id: id,
                    partitionKey: new PartitionKey(partitionKey)
                );
                
                // If we get here, deletion failed
                Console.WriteLine("Warning: Document still exists after deletion attempt!");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Verified: Document was successfully deleted (not found anymore)");
            }
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine($"Document with id {id} not found!");
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"Error deleting document: {ex.StatusCode} - {ex.Message}");
        }
    }

    private async Task QueryDocumentsByPartitionKeyAsync(Container container, string partitionKey)
    {
        Console.WriteLine($"Querying documents with partition key: {partitionKey}");
        
        // Query documents using LINQ with a partition key filter
        var queryable = container.GetItemLinqQueryable<TestDocument>(
            allowSynchronousQueryExecution: true,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) }
        );
        
        int count = 0;
        foreach (var document in queryable)
        {
            Console.WriteLine($"Found document: Id={document.Id}, Queryfield={document.Queryfield},
             PartitionKey={document.PartitionKey}, City={document.City}");
            count++;
        }
        
        Console.WriteLine($"Found {count} document(s) with partition key {partitionKey}");
    }

    private async Task QueryAllDocumentsAsync(Container container)
    {
        Console.WriteLine("Querying all documents...");
        
        string sqlQuery = "SELECT * FROM c";
        var queryDefinition = new QueryDefinition(sqlQuery);
        
        int count = 0;
        using (var iterator = container.GetItemQueryIterator<TestDocument>(queryDefinition))
        {
            while (iterator.HasMoreResults)
            {
                foreach (var document in await iterator.ReadNextAsync())
                {
                    Console.WriteLine($"Found document: Id={document.Id}, Queryfield={document.Queryfield}, PartitionKey={document.PartitionKey}, City={document.City}");
                    count++;
                }
            }
        }
        
        Console.WriteLine($"Found {count} document(s) in total");
    }
}