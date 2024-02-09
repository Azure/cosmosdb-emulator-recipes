#!/bin/bash

# Define the IP address variable
ipAddress="localhost"

# Define the timeout in seconds
timeout_seconds=30  # Adjust as needed

# Get the start time
start_time=$(date +%s)

# Start a loop
while true; do
    # Get the current time
    current_time=$(date +%s)

    # Calculate the elapsed time
    elapsed_time=$((current_time - start_time))

    # Print remaining time
    remaining_time=$((timeout_seconds - elapsed_time))
    echo "Remaining time: $remaining_time seconds"

    # Check if the timeout is reached
    if [ $elapsed_time -ge $timeout_seconds ]; then
        echo "Timeout reached. Exiting..."
        break
    fi

    # Sleep for 2 seconds
    sleep 2

    # Define the command
    command="curl -k \"https://${ipAddress}:8081/_explorer/emulator.pem\""

    # Print the command
    echo "$command"

    # Execute the command and store the result
    resultCommand=$(eval "$command")

    # Check the exit status of the curl command
    if [ $? -ne 0 ]; then
        echo "Curl command failed. Retrying..."
        continue
    fi

    echo "Emulator Started successfully."
    exit 0

done
