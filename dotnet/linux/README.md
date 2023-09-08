# .NET Core Sample App using CosmosDB Linux Docker

This sample app is a .NET Core console app that uses the CosmosDB .NET Core SDK to connect to a CosmosDB instance running in a Linux Docker container. The sample creates random documents in the database. 

## Prerequisites
Docker Compose is required to run this sample. You can download Docker Compose from [here](https://docs.docker.com/compose/install/).

## Running the sample
1. Clone the repo
2. ```cd dotnet/linux```
3. ```touch .env``` and add the following environment variable. See `.env.example` for an example. If you're using Emulator, you can copy the key from the Emulator's Data Explorer. 
    ```
    COSMOSDB_KEY=<your cosmosdb key>
    ```
4. ```docker compose up```
