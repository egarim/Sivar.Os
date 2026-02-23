#!/bin/bash
# Sivar.Os Master Test Runner
# Runs all test suites

set -e

BASE_URL="${BASE_URL:-http://127.0.0.1:5001}"
TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "🧪 Sivar.Os Test Suite Runner"
echo "================================"
echo "Base URL: $BASE_URL"
echo "Test Dir: $TEST_DIR"
echo ""

TOTAL_PASSED=0
TOTAL_FAILED=0

# Make scripts executable
chmod +x "$TEST_DIR"/*.sh

# Test 1: API Tests
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "1/4: API Tests"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if "$TEST_DIR/api-test.sh" "$BASE_URL"; then
    echo "✅ API tests passed"
    ((TOTAL_PASSED++))
else
    echo "❌ API tests failed"
    ((TOTAL_FAILED++))
fi
echo ""

# Test 2: Database Tests
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "2/4: Database Tests"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if "$TEST_DIR/db-test.sh"; then
    echo "✅ Database tests passed"
    ((TOTAL_PASSED++))
else
    echo "❌ Database tests failed"
    ((TOTAL_FAILED++))
fi
echo ""

# Test 3: Screenshots
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "3/4: Screenshots"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if "$TEST_DIR/screenshot.sh" "$BASE_URL"; then
    echo "✅ Screenshots captured"
    ((TOTAL_PASSED++))
else
    echo "❌ Screenshot capture failed"
    ((TOTAL_FAILED++))
fi
echo ""

# Test 4: Load Test (optional - only if --load flag is passed)
if [ "$1" = "--load" ]; then
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "4/4: Load Test"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    if k6 run "$TEST_DIR/load-test.js" --env BASE_URL="$BASE_URL"; then
        echo "✅ Load test passed"
        ((TOTAL_PASSED++))
    else
        echo "❌ Load test failed"
        ((TOTAL_FAILED++))
    fi
    echo ""
else
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "4/4: Load Test (Skipped)"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "ℹ️  Run with --load flag to include load testing"
    echo ""
fi

# Summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Test Summary"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Passed: $TOTAL_PASSED"
echo "Failed: $TOTAL_FAILED"
echo ""

if [ $TOTAL_FAILED -eq 0 ]; then
    echo "✅ All test suites passed!"
    exit 0
else
    echo "❌ Some test suites failed"
    exit 1
fi
