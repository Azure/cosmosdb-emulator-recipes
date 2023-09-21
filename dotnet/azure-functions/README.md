# Azure Functions App using CosmosDB Emulator in a Linux Docker

This is a sample Azure Functions application showcasing an HTTP-triggered API connected to CosmosDB using the .NET SDK. The application and CosmosDB instance are containerized using Docker and orchestrated with Docker Compose. The samples exposes a REST API to create, read and delete notes.

## Prerequisites
Docker Compose is required to run this sample. You can download Docker Compose from [here](https://docs.docker.com/compose/install/).

## Running the sample
1. Clone the repo
2. ```cd dotnet/azure-functions```
3. ```touch .env``` and add the following environment variable. See `.env.example` for an example. If you're using Emulator, you can copy the key from the Emulator's Data Explorer. 
    ```
    COSMOSDB_KEY=<your cosmosdb key>
    ```
4. ```docker compose up```

## Notes API 
Once the docker containers are running, you can use the following cURL commands to interact with the API.

### Read all Notes
```curl --location 'http://localhost:8080/api/notes/'```

### Read a Note
```curl --location 'http://localhost:8080/api/notes/{id}'```
Replace `id` with the Id of the note you want to fetch.

### Create a Note
```
curl --location 'http://localhost:8080/api/notes' \
--header 'Content-Type: application/json' \
--data '{
    "content":"My First Note"
}'
```
### Delete a Note
```
curl --location --request DELETE 'http://localhost:3000/notes/{id}'
```
Replace `id` with the Id of the note you want to delete.