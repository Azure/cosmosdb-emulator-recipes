# Node.js express using CosmosDB Emulator Linux Docker

This sample app is a notes app that uses the CosmosDB Node.js SDK to connect to a CosmosDB instance running in a Linux Docker container. The sample creates basic CRUD operations.

## Pre-requisites
Docker Compose is required to run this sample. You can download Docker Compose from [here](https://docs.docker.com/compose/install/).

## Running the sample
1. Clone the repo
2. ```cd nodejs/linux```
3. ```touch .env``` and add the following environment variable. See `.env.example` for an example. If you're using Emulator, you can copy the key from the Emulator's Data Explorer. 
    ```
    COSMOSDB_KEY=<your cosmosdb key>
    ```
4. ```docker compose up```

## Notes API 
Once the docker containers are running, you can use the following cURL commands to interact with the API.

### Read all Notes
```curl --location 'http://localhost:3000/notes/'```

### Read a Note
```curl --location 'http://localhost:3000/notes/{id}'```

### Create a Note
```
curl --location 'http://localhost:3000/notes' \
--header 'Content-Type: application/json' \
--data '{
    "content":"My First Note"
}'
```
### Delete a Note
```
curl --location --request DELETE 'http://localhost:3000/notes/{id}'
```