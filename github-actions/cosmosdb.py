import os
import uuid
from azure.cosmos import CosmosClient, PartitionKey, exceptions
from requests.utils import DEFAULT_CA_BUNDLE_PATH
import random

print(DEFAULT_CA_BUNDLE_PATH)

# Configure the connection to the Cosmos DB emulator
endpoint = "https://localhost:8081/"
key = os.getenv("COSMOS_DB_KEY")
database_name = "SampleDatabase"
container_name = "SampleContainer"

# Initialize the Cosmos client
client = CosmosClient(endpoint, key)

try:
    # Create a database
    database = client.create_database_if_not_exists(id=database_name)
    print(f"Database '{database_name}' created or already exists")

    # Create a container
    container = database.create_container_if_not_exists(
        id=container_name,
        partition_key=PartitionKey(path="/id")
    )
    print(f"Container '{container_name}' created or already exists")

    # Insert 100 documents
    for i in range(10000):
        item = {
            'id': str(uuid.uuid4()),
            'name': f'Test Item {i+1}',
            'age': 30 + (i % 10),  # Adding some variation to the data
            'size': i+1  # Increase the size of each document
        }

        # Add more random fields to increase document size
        for j in range(10):
            field_name = f'field{j+1}'
            field_value = random.randint(1, 100)
            item[field_name] = field_value

        # Insert the item
        container.create_item(body=item)

    # Query the inserted items
    query = "SELECT * FROM c WHERE c.name LIKE 'Test Item%'"
    items = list(container.query_items(query=query, enable_cross_partition_query=True))
    print(f"Queried items: {len(items)} items found")

    # Query items with age greater than 35
    query = "SELECT * FROM c WHERE c.age > 35"
    items = list(container.query_items(query=query, enable_cross_partition_query=True))
    print(f"Queried items: {len(items)} items found")

    # Query items with size less than or equal to 50
    query = "SELECT * FROM c WHERE c.size <= 50"
    items = list(container.query_items(query=query, enable_cross_partition_query=True))
    print(f"Queried items: {len(items)} items found")

except exceptions.CosmosHttpResponseError as e:
    print(f"An error occurred: {e}")

print("Cosmos DB emulator test completed successfully.")