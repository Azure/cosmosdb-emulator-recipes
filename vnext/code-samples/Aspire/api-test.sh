#!/bin/bash

# Azure Cosmos DB Emulator Sample - Advanced API Test Script
# This script dynamically discovers the API URL from Aspire and tests all endpoints

# Enable debug mode if DEBUG environment variable is set
DEBUG=${DEBUG:-false}
VERBOSE=${VERBOSE:-false}

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Debug function
debug() {
    if [ "$DEBUG" = "true" ]; then
        echo -e "${BLUE}[DEBUG]${NC} $1" >&2
    fi
}

# Verbose function
verbose() {
    if [ "$VERBOSE" = "true" ]; then
        echo -e "${YELLOW}[VERBOSE]${NC} $1"
    fi
}

echo "🚀 Azure Cosmos DB Emulator Sample - Advanced API Test"
echo "======================================================"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --debug)
            DEBUG=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        --help|-h)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --debug     Enable debug output"
            echo "  --verbose   Enable verbose output"
            echo "  --help, -h  Show this help message"
            echo ""
            echo "Environment Variables:"
            echo "  DEBUG=true    Enable debug mode"
            echo "  VERBOSE=true  Enable verbose mode"
            echo ""
            echo "Examples:"
            echo "  $0                    # Run with default settings"
            echo "  $0 --debug           # Run with debug output"
            echo "  DEBUG=true $0        # Run with debug via environment"
            exit 0
            ;;
        *)
            echo "Unknown parameter: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

debug "Debug mode enabled"
verbose "Verbose mode enabled"

# Check if Aspire dashboard is running
ASPIRE_DASHBOARD="https://localhost:17103"
echo "📡 Checking if Aspire Dashboard is running..."

# Wait for Aspire to be ready
timeout 30 bash -c "until curl -k -s $ASPIRE_DASHBOARD > /dev/null; do sleep 2; done" || {
    echo "❌ Aspire Dashboard is not responding. Start it with:"
    echo "   dotnet run --project CosmosDbEmulatorSample.AppHost"
    exit 1
}

echo "✅ Aspire Dashboard is running!"

# Initialize test tracking variables
declare -a TEST_RESULTS=()
declare -a TEST_NAMES=()
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Function to record test result
record_test() {
    local test_name="$1"
    local success="$2"
    local details="$3"
    
    TEST_NAMES+=("$test_name")
    if [ "$success" = "true" ]; then
        TEST_RESULTS+=("✅ PASS")
        ((PASSED_TESTS++))
    else
        TEST_RESULTS+=("❌ FAIL")
        ((FAILED_TESTS++))
    fi
    ((TOTAL_TESTS++))
    
    if [ -n "$details" ]; then
        TEST_RESULTS[${#TEST_RESULTS[@]}-1]+=" - $details"
    fi
}

# Function to test API endpoint
test_endpoint() {
    local method=$1
    local endpoint=$2
    local description=$3
    local data=$4
    
    echo ""
    echo "🧪 Testing: $description"
    echo "   $method $API_URL$endpoint"
    
    debug "Sending request: $method $API_URL$endpoint"
    if [ -n "$data" ]; then
        debug "Request data: $data"
    fi
    
    if [ "$method" = "GET" ]; then
        response=$(curl -k -s "$API_URL$endpoint")
        http_code=$(curl -k -s -o /dev/null -w "%{http_code}" "$API_URL$endpoint")
    elif [ "$method" = "POST" ]; then
        if [ -n "$data" ]; then
            response=$(curl -k -s -X POST -H "Content-Type: application/json" -d "$data" "$API_URL$endpoint")
            http_code=$(curl -k -s -o /dev/null -w "%{http_code}" -X POST -H "Content-Type: application/json" -d "$data" "$API_URL$endpoint")
        else
            response=$(curl -k -s -X POST "$API_URL$endpoint")
            http_code=$(curl -k -s -o /dev/null -w "%{http_code}" -X POST "$API_URL$endpoint")
        fi
    elif [ "$method" = "PUT" ]; then
        if [ -n "$data" ]; then
            response=$(curl -k -s -X PUT -H "Content-Type: application/json" -d "$data" "$API_URL$endpoint")
            http_code=$(curl -k -s -o /dev/null -w "%{http_code}" -X PUT -H "Content-Type: application/json" -d "$data" "$API_URL$endpoint")
        else
            response=$(curl -k -s -X PUT "$API_URL$endpoint")
            http_code=$(curl -k -s -o /dev/null -w "%{http_code}" -X PUT "$API_URL$endpoint")
        fi
    elif [ "$method" = "DELETE" ]; then
        response=$(curl -k -s -X DELETE "$API_URL$endpoint")
        http_code=$(curl -k -s -o /dev/null -w "%{http_code}" -X DELETE "$API_URL$endpoint")
    fi
    
    debug "Response code: $http_code"
    debug "Response body: $response"
    
    # Record test result based on HTTP status code
    local success="false"
    local status_details="HTTP $http_code"
    
    if [[ "$http_code" =~ ^2[0-9][0-9]$ ]]; then
        success="true"
        status_details="HTTP $http_code (Success)"
    elif [[ "$http_code" =~ ^4[0-9][0-9]$ ]]; then
        if [ "$method" = "DELETE" ] && [ "$http_code" = "404" ]; then
            # 404 on DELETE might be expected if item was already deleted
            success="true"
            status_details="HTTP $http_code (Not Found - Expected for DELETE)"
        else
            status_details="HTTP $http_code (Client Error)"
        fi
    elif [[ "$http_code" =~ ^5[0-9][0-9]$ ]]; then
        status_details="HTTP $http_code (Server Error)"
    else
        status_details="HTTP $http_code (Unknown)"
    fi
    
    record_test "$description" "$success" "$status_details"
    
    if echo "$response" | python3 -m json.tool > /dev/null 2>&1; then
        echo "$response" | python3 -m json.tool | head -20
        if [ $(echo "$response" | wc -c) -gt 1000 ]; then
            echo "   ... (response truncated)"
        fi
    else
        echo "   Response: $response"
    fi
}

# Try to find API URL by testing the known API service ports
echo ""
echo "🔍 Discovering API service URL..."

API_URL=""
# Test the specific ports used by the API service
for port in 7554 5563; do
    for protocol in https http; do
        test_url="$protocol://localhost:$port"
        if timeout 3 curl -k -s "$test_url/" 2>/dev/null | grep -q "Azure Cosmos DB Emulator Sample API"; then
            API_URL="$test_url"
            echo "✅ Found API service at: $API_URL"
            break 2
        fi
    done
done

if [ -z "$API_URL" ]; then
    echo "❌ Could not find API service at ports 7554 or 5563."
    echo "💡 Make sure the API service is running. Try:"
    echo "   - Check Aspire Dashboard at $ASPIRE_DASHBOARD"
    echo "   - Or run API directly: dotnet run --project CosmosDbEmulatorSample.ApiService"
    exit 1
fi

# Test root endpoint
test_endpoint "GET" "/" "API Root - Shows available endpoints"

# Seed sample data first
test_endpoint "POST" "/seed-data" "Seed sample data"

echo ""
echo "======================================================"
echo "🧪 COMPREHENSIVE CRUD TESTING"
echo "======================================================"

# === PRODUCT CRUD TESTS ===
echo ""
echo "📦 PRODUCT CRUD OPERATIONS"
echo "------------------------"

# READ: Get all products
test_endpoint "GET" "/products" "READ: Get all products"

# INSERT: Create a new product
NEW_PRODUCT='{
    "name": "Test Gaming Mouse",
    "description": "High-precision gaming mouse with RGB lighting",
    "price": 79.99,
    "category": "Electronics",
    "stockQuantity": 25
}'
echo ""
echo "🆕 INSERT: Creating new product..."
test_endpoint "POST" "/products" "INSERT: Create new gaming mouse" "$NEW_PRODUCT"

# Store the created product ID for later operations
CREATED_PRODUCT_ID=""
if [ -n "$response" ]; then
    CREATED_PRODUCT_ID=$(echo "$response" | python3 -c "import sys, json; print(json.load(sys.stdin).get('id', ''))" 2>/dev/null || echo "")
    echo "   📝 Created Product ID: $CREATED_PRODUCT_ID"
fi

# READ: Get products by category
test_endpoint "GET" "/products?category=Electronics" "READ: Get products by Electronics category"

# === CUSTOMER CRUD TESTS ===
echo ""
echo "👥 CUSTOMER CRUD OPERATIONS"
echo "--------------------------"

# READ: Get all customers
test_endpoint "GET" "/customers" "READ: Get all customers"

# INSERT: Create a new customer
NEW_CUSTOMER='{
    "firstName": "Alice",
    "lastName": "Johnson",
    "email": "alice.johnson@example.com",
    "phoneNumber": "+1-555-0199",
    "address": {
        "street": "456 Oak Avenue",
        "city": "Springfield",
        "state": "IL",
        "zipCode": "62701",
        "country": "USA"
    }
}'
echo ""
echo "🆕 INSERT: Creating new customer..."
test_endpoint "POST" "/customers" "INSERT: Create new customer Alice" "$NEW_CUSTOMER"

# Store the created customer info for later operations
CREATED_CUSTOMER_ID=""
CREATED_CUSTOMER_CUSTOMER_ID=""
if [ -n "$response" ]; then
    CREATED_CUSTOMER_ID=$(echo "$response" | python3 -c "import sys, json; print(json.load(sys.stdin).get('id', ''))" 2>/dev/null || echo "")
    CREATED_CUSTOMER_CUSTOMER_ID=$(echo "$response" | python3 -c "import sys, json; print(json.load(sys.stdin).get('customerId', ''))" 2>/dev/null || echo "")
    echo "   📝 Created Customer ID: $CREATED_CUSTOMER_ID"
    echo "   📝 Created Customer CustomerId: $CREATED_CUSTOMER_CUSTOMER_ID"
fi

# READ: Get customer by email
test_endpoint "GET" "/customers/by-email/alice.johnson@example.com" "READ: Get customer by email"

# UPDATE: Update the customer (if we have the IDs)
if [ -n "$CREATED_CUSTOMER_ID" ] && [ -n "$CREATED_CUSTOMER_CUSTOMER_ID" ]; then
    UPDATED_CUSTOMER='{
        "firstName": "Alice",
        "lastName": "Johnson-Smith",
        "email": "alice.johnson-smith@example.com",
        "phoneNumber": "+1-555-0199",
        "address": {
            "street": "789 Maple Street",
            "city": "Springfield",
            "state": "IL",
            "zipCode": "62702",
            "country": "USA"
        }
    }'
    echo ""
    echo "✏️ UPDATE/UPSERT: Updating customer..."
    test_endpoint "PUT" "/customers/$CREATED_CUSTOMER_ID?customerId=$CREATED_CUSTOMER_CUSTOMER_ID" "UPDATE: Update customer lastname and address" "$UPDATED_CUSTOMER"
fi

# === ORDER CRUD TESTS ===
echo ""
echo "🛒 ORDER CRUD OPERATIONS"
echo "-----------------------"

# READ: Get all orders
test_endpoint "GET" "/orders" "READ: Get all orders"

# INSERT: Create a new order (using existing customer if available)
if [ -n "$CREATED_CUSTOMER_CUSTOMER_ID" ]; then
    NEW_ORDER='{
        "customerId": "'$CREATED_CUSTOMER_CUSTOMER_ID'",
        "items": [
            {
                "productId": "prod-1",
                "productName": "Laptop",
                "quantity": 1,
                "unitPrice": 999.99
            },
            {
                "productId": "prod-2",
                "productName": "Mouse",
                "quantity": 2,
                "unitPrice": 29.99
            }
        ],
        "status": "Pending"
    }'
    echo ""
    echo "🆕 INSERT: Creating new order..."
    test_endpoint "POST" "/orders" "INSERT: Create new order with 2 items" "$NEW_ORDER"
    
    # Store the created order info
    CREATED_ORDER_ID=""
    if [ -n "$response" ]; then
        CREATED_ORDER_ID=$(echo "$response" | python3 -c "import sys, json; print(json.load(sys.stdin).get('id', ''))" 2>/dev/null || echo "")
        echo "   📝 Created Order ID: $CREATED_ORDER_ID"
    fi
fi

# READ: Get order summary
test_endpoint "GET" "/orders/summary" "READ: Get order summary statistics"

# === DELETE OPERATIONS ===
echo ""
echo "🗑️ DELETE OPERATIONS"
echo "-------------------"

# DELETE: Customer (if we have the IDs)
if [ -n "$CREATED_CUSTOMER_ID" ] && [ -n "$CREATED_CUSTOMER_CUSTOMER_ID" ]; then
    echo ""
    echo "🗑️ DELETE: Deleting test customer..."
    response=$(curl -k -s -X DELETE "$API_URL/customers/$CREATED_CUSTOMER_ID?customerId=$CREATED_CUSTOMER_CUSTOMER_ID")
    http_code=$(curl -k -s -o /dev/null -w "%{http_code}" -X DELETE "$API_URL/customers/$CREATED_CUSTOMER_ID?customerId=$CREATED_CUSTOMER_CUSTOMER_ID")
    
    if [[ "$http_code" =~ ^2[0-9][0-9]$ ]]; then
        echo "✅ DELETE: Customer deleted successfully"
        echo "   Response code: $http_code"
        record_test "DELETE: Remove test customer" "true" "HTTP $http_code"
    else
        echo "❌ DELETE: Failed to delete customer"
        echo "   Response code: $http_code"
        record_test "DELETE: Remove test customer" "false" "HTTP $http_code"
    fi
    
    # Verify deletion - check if it's soft delete (isActive: false) or hard delete (404)
    echo ""
    echo "🔍 VERIFY: Checking if customer was deleted..."
    response=$(curl -k -s "$API_URL/customers/$CREATED_CUSTOMER_ID?customerId=$CREATED_CUSTOMER_CUSTOMER_ID")
    verify_http_code=$(curl -k -s -o /dev/null -w "%{http_code}" "$API_URL/customers/$CREATED_CUSTOMER_ID?customerId=$CREATED_CUSTOMER_CUSTOMER_ID")
    
    if [ "$verify_http_code" = "404" ] || echo "$response" | grep -q "not found\|404"; then
        echo "✅ VERIFY: Customer hard deleted (404 response)"
        record_test "VERIFY: Customer deletion confirmed" "true" "HTTP $verify_http_code (Not Found)"
    elif [ "$verify_http_code" = "200" ] && echo "$response" | grep -q '"isActive": false'; then
        echo "✅ VERIFY: Customer soft deleted (isActive: false)"
        record_test "VERIFY: Customer deletion confirmed" "true" "HTTP $verify_http_code (Soft Delete)"
    else
        echo "⚠️ VERIFY: Customer deletion unclear"
        echo "   Response: $response"
        record_test "VERIFY: Customer deletion confirmed" "false" "HTTP $verify_http_code"
    fi
fi

echo ""
echo "======================================================"
echo "🔍 FINAL VERIFICATION READS"
echo "======================================================"

# Final verification reads
test_endpoint "GET" "/products" "FINAL: Verify all products"
test_endpoint "GET" "/customers" "FINAL: Verify all customers"
test_endpoint "GET" "/orders" "FINAL: Verify all orders"

echo ""
echo "✅ Advanced API tests completed!"

echo ""
echo "======================================================"
echo "📊 TEST REPORT SUMMARY"
echo "======================================================"

echo ""
echo "📈 Test Statistics:"
echo "   Total Tests: $TOTAL_TESTS"
echo "   Passed: $PASSED_TESTS"
echo "   Failed: $FAILED_TESTS"

if [ $TOTAL_TESTS -gt 0 ]; then
    pass_percentage=$((PASSED_TESTS * 100 / TOTAL_TESTS))
    echo "   Success Rate: $pass_percentage%"
else
    echo "   Success Rate: N/A"
fi

echo ""
echo "📋 Detailed Results:"
echo "--------------------"

for i in "${!TEST_NAMES[@]}"; do
    printf "%-3s %-50s %s\n" "$((i+1))." "${TEST_NAMES[$i]}" "${TEST_RESULTS[$i]}"
done

echo ""
if [ $FAILED_TESTS -eq 0 ]; then
    echo "🎉 All tests passed! Your Azure Cosmos DB Emulator API is working perfectly!"
elif [ $PASSED_TESTS -gt $FAILED_TESTS ]; then
    echo "✅ Most tests passed! Some operations may need initialization time."
else
    echo "⚠️ Several tests failed. Check if Cosmos DB containers are fully initialized."
fi

echo ""
echo "💡 Troubleshooting Tips:"
if [ $FAILED_TESTS -gt 0 ]; then
    echo "   - If you see 500 errors, wait 2-3 minutes for Cosmos DB to initialize"
    echo "   - Check Cosmos DB Data Explorer: http://localhost:8081/_explorer/index.html"
    echo "   - Verify containers are created: Products, Customers, Orders"
    echo "   - Re-run this script after initialization: ./test-api-advanced.sh"
fi
echo "   - Monitor logs in Aspire Dashboard: $ASPIRE_DASHBOARD"
echo "   - Check API health: $API_URL/health (if available)"

echo ""
echo "🌐 Access points:"
echo "   - API Service: $API_URL"
echo "   - API Documentation: $API_URL/swagger"
echo "   - Aspire Dashboard: $ASPIRE_DASHBOARD"
echo "   - Cosmos DB Data Explorer: http://localhost:8081/_explorer/index.html"
echo ""
echo "📚 For more information, check the documentation in the Aspire Dashboard."
