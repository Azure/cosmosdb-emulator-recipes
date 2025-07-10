#!/bin/bash
set -e

echo "Starting Azure Cosmos DB vnext-preview emulator..."

# Start the Cosmos DB emulator
docker-compose up -d cosmosdb-vnext

echo "Waiting for Cosmos DB emulator to be ready..."
sleep 30

# Check if the emulator is responding
echo "Checking if Cosmos DB emulator is ready..."
max_attempts=12
attempt=1

while [ $attempt -le $max_attempts ]; do
    if curl -k -s -f https://localhost:8081 > /dev/null 2>&1; then
        echo "✅ Cosmos DB emulator is ready!"
        break
    else
        echo "⏳ Attempt $attempt/$max_attempts - Cosmos DB emulator not ready yet, waiting..."
        sleep 10
        ((attempt++))
    fi
done

if [ $attempt -gt $max_attempts ]; then
    echo "❌ Cosmos DB emulator failed to start after $max_attempts attempts"
    echo "Check the logs with: docker-compose logs cosmosdb-vnext"
    exit 1
fi

echo "🚀 Ready to run your application!"
echo "You can now run: dotnet run --project src/cosmosdb-vnext-test/cosmosdb-vnext-test.csproj"
echo "Cosmos DB endpoint: https://localhost:8081"
echo "Data Explorer: http://localhost:1234"
