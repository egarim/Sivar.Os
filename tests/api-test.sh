#!/bin/bash
# Sivar.Os API Test Suite
# Tests critical API endpoints

BASE_URL="${1:-http://127.0.0.1:5001}"
PASSED=0
FAILED=0

echo "🧪 Sivar.Os API Test Suite"
echo "Base URL: $BASE_URL"
echo ""

# Helper function to test endpoint
test_endpoint() {
    local method=$1
    local path=$2
    local expected_status=$3
    local description=$4
    
    echo -n "Testing: $description... "
    
    if http --check-status --timeout 10 "$method" "$BASE_URL$path" > /dev/null 2>&1; then
        echo "✅ PASS"
        ((PASSED++))
        return 0
    else
        echo "❌ FAIL"
        ((FAILED++))
        return 0
    fi
}

# Run tests
echo "=== Health Checks ==="
test_endpoint GET "/api/Health" 200 "Basic health check"
test_endpoint GET "/api/Health/detailed" 200 "Detailed health check"

echo ""
echo "=== Authentication ==="
test_endpoint GET "/api/DevAuth/status" 200 "Dev auth status"

echo ""
echo "=== Static Assets ==="
test_endpoint GET "/" 200 "Landing page"
test_endpoint GET "/favicon.ico" 200 "Favicon"

echo ""
echo "=== Results ==="
echo "Passed: $PASSED"
echo "Failed: $FAILED"

if [ $FAILED -eq 0 ]; then
    echo "✅ All tests passed!"
    exit 0
else
    echo "❌ Some tests failed"
    exit 1
fi
