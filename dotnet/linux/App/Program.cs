using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel;


namespace MyApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            var builder = new ConfigurationBuilder().AddEnvironmentVariables();
            var root = builder.Build();
            Console.WriteLine("Connecting..");

            string endpoint = root["COSMOS_ENDPOINT"];
            string key = root["COSMOS_KEY"];
            Console.WriteLine($"Endpoint: {endpoint} Key: {key.Substring(0, 5)}*********{key.Substring(key.Length - 5)}");
            if (string.IsNullOrEmpty(endpoint))
            {
                endpoint = "https://localhost:8081";
            }
            if (string.IsNullOrEmpty(key))
            {
                Console.WriteLine("COSMOS_KEY not found, exiting..");
                System.Environment.Exit(1);
            }

            var client = CreateClient(endpoint, key);

            // Database reference with creation if it does not already exist
            Database database = await client.CreateDatabaseIfNotExistsAsync(id: "cosmicworks");
            Console.WriteLine($"New database:\t{database.Id}");

            ContainerProperties containerProperties = new ContainerProperties()
            {
                Id = "products",
                PartitionKeyPath = "/categoryId",
                IndexingPolicy = new IndexingPolicy()
                {
                    Automatic = false,
                    IndexingMode = IndexingMode.Lazy,
                }
            };
            ContainerResponse response = await database.CreateContainerIfNotExistsAsync(
                                                containerProperties,
                                         ThroughputProperties.CreateAutoscaleThroughput(5000));


            Console.WriteLine($"Container Response: {response.StatusCode}");
            var container = database.GetContainer(id: "products");

            Console.WriteLine($"New container:\t{container.Id}");

            Console.WriteLine("Creating 1000 items..");
            Random rnd = new Random();
            for (int i = 0; i < 1000; i++)
            {

                string id = Guid.NewGuid().ToString();
                string catId = Guid.NewGuid().ToString();
                Product newItem = new(
                        id: id,
                        categoryId: catId,
                        categoryName: "gear-surf-surfboards - " + catId,
                        name: "Yamba Surfboard - " + id,
                        quantity: rnd.Next(42),
                        sale: false
                );

                Product createdItem = await container.CreateItemAsync<Product>(
                        item: newItem,
                        partitionKey: new PartitionKey(catId)
                    );

                Console.WriteLine($"Created item {i + 1}:\t{createdItem.id}\t[{createdItem.categoryName}]");
                await Task.Delay(rnd.Next(rnd.Next(100)));
            }
        }

        public record Product(
            string id,
            string categoryId,
            string categoryName,
            string name,
            int quantity,
            bool sale
        );

        private static CosmosClient CreateClient(string endpoint, string key)
        {
            return new CosmosClient(endpoint, key, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                },
                HttpClientFactory = () =>
                {
                    /*                               *** WARNING ***
                        * This code is for demo purposes only. In production, you should use the default behavior,
                        * which relies on the operating system's certificate store to validate the certificates.
                    */
                    HttpMessageHandler httpMessageHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                    return new HttpClient(httpMessageHandler);
                },
                ConnectionMode = ConnectionMode.Direct
            });
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            Environment.Exit(1);
        }
    }
}
