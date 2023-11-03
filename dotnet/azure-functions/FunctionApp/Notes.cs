using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctionApp
{
    public static class Notes
    {

        static Notes()
        {
            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            var builder = new ConfigurationBuilder().AddEnvironmentVariables();
            var root = builder.Build();
            EndpointUrl = root["COSMOS_ENDPOINT"];
            PrimaryKey = root["COSMOS_KEY"];


            var client = CreateClient(EndpointUrl, PrimaryKey);
            // Create the database if it does not exist
            client.CreateDatabaseIfNotExistsAsync("Notes").Wait();

            // Create the container if it does not exist. 
            client.GetDatabase("Notes").CreateContainerIfNotExistsAsync("Notes", "/id").Wait();
            var database = client.GetDatabase("Notes");
            Container = database.GetContainer("Notes");
        }

        private static readonly Container Container;

        private static readonly string EndpointUrl;

        private static readonly string PrimaryKey;


        [FunctionName("Notes")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post","delete", Route = "notes/{id?}")]
            HttpRequest req, string id,
            ILogger log)
        {

            log.LogInformation("C# HTTP trigger function processed a request.");
            try
            {
                switch (req.Method.ToLowerInvariant())
                {
                    case "get":
                        var notesResult = await GetNotes(id);
                        return new OkObjectResult(notesResult);
                    case "post":
                        // Implement your POST logic here
                        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                        dynamic data = JsonConvert.DeserializeObject(requestBody);
                        string content = data?.content;
                        var newNote = await SaveNote(content);
                        if (newNote != null)
                        {
                            return new OkObjectResult(newNote);
                        }

                        return new BadRequestObjectResult("Error saving note");
                    case "delete":
                        // Implement your DELETE logic here
                        if (string.IsNullOrEmpty(id)) return new BadRequestResult();
                        var deleteResult = await DeleteNote(id);
                        if (deleteResult)
                        {
                            return new OkObjectResult("Note deleted");
                        }

                        return new BadRequestObjectResult("Error deleting note");

                    default:
                        return new BadRequestObjectResult("Method not supported");
                }
            }
            catch (Exception ex)
            {
                log.LogInformation("Exception Occurred");
                log.LogInformation(ex.ToString());
                return new InternalServerErrorResult();
            }
        }

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
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                    return new HttpClient(httpMessageHandler);
                },
                ConnectionMode = ConnectionMode.Direct
            });
        }

        private static async Task<IEnumerable<Note>> GetNotes(string id)
        {
            QueryDefinition queryDefinition;
            if (string.IsNullOrEmpty(id))
            {
                queryDefinition = new QueryDefinition("SELECT * FROM c");
            }
            else
            {
                queryDefinition = new QueryDefinition("SELECT * FROM c where c.id= @value")
                    .WithParameter("@value", id.Trim());
            }

            QueryRequestOptions requestOptions = new QueryRequestOptions
            {
                MaxItemCount = 10
            };

            FeedIterator<Note> queryResultSetIterator =
                Container.GetItemQueryIterator<Note>(queryDefinition, requestOptions: requestOptions);
            List<Note> notes = new List<Note>();
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Note> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Note note in currentResultSet)
                {
                    notes.Add(note);
                }

            }

            return notes;
        }

        private static async Task<Note> SaveNote(string content)
        {
            Note note = new Note
            {
                Id = Guid.NewGuid().ToString(),
                NoteText = content
            };
            ItemResponse<Note> response = await Container.CreateItemAsync(note, new PartitionKey(note.Id));
            return response.StatusCode == System.Net.HttpStatusCode.Created ? note : null;
        }

        private static async Task<bool> DeleteNote(string id)
        {
            ItemResponse<Note> response = await Container.DeleteItemAsync<Note>(id, new PartitionKey(id));
            return response.StatusCode == System.Net.HttpStatusCode.NoContent;
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
        }
    }


    public class Note
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "note")]
        public string NoteText { get; set; }
    }
}