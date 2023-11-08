# Node.js express using CosmosDB Emulator Linux Docker

This sample application is a note-taking app that leverages the MongoDB Node.js Driver to establish a connection with a CosmosDB MongoDB API instance, which is hosted within a Linux Docker container. This sample showcases fundamental CRUD (Create, Read, Update, Delete) operations.

## Pre-requisites
Docker Compose is required to run this sample. You can download Docker Compose from [here](https://docs.docker.com/compose/install/).

## Connection String
If you want to access the emulator from inside another docker container, use following connection string in `.env` file:

`mongodb://azurecosmoslinuxemulator:C2y6yDjf5%2FR%2Bob0N8A7Cgv30VRDJIWEHLM%2B4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw%2FJw%3D%3D@azurecosmoslinuxemulator:10255/admin?ssl=true&retrywrites=false&directConnection=true`

For other cases, simply use following connection string:

`mongodb://127.0.0.1:C2y6yDjf5%2FR%2Bob0N8A7Cgv30VRDJIWEHLM%2B4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw%2FJw%3D%3D@127.0.0.1:10255/admin?ssl=true&retrywrites=false&directConnection=true`

## Running the sample
1. Clone the repo
2. ```cd mongodb/nodejs/linux```
3. ```touch .env``` and add the following environment variable `COSMOSDB_CONNECTION_STRING`.  See `.env.example` for an example. 
    ```
    COSMOSDB_CONNECTION_STRING=<your cosmosdb connection string>
    ```
4. ```docker compose up```

## Notes API 
Once the docker containers are running, you can use the following curl commands to interact with the API.

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