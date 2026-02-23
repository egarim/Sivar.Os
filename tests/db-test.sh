#!/bin/bash
# Sivar.Os Database Test Suite
# Validates database schema and data integrity

DB_HOST="${DB_HOST:-86.48.30.121}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-sivaros}"
DB_USER="${DB_USER:-postgres}"
DB_PASS="${DB_PASS:-Xa1Hf4M3EnAKG8g}"

PASSED=0
FAILED=0

echo "🗄️  Sivar.Os Database Test Suite"
echo "Database: $DB_HOST:$DB_PORT/$DB_NAME"
echo ""

# Helper function
run_test() {
    local description=$1
    local query=$2
    local expected=$3
    
    echo -n "Testing: $description... "
    
    result=$(PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c "$query" 2>/dev/null | xargs) || result="ERROR"
    
    if [ "$result" = "$expected" ]; then
        echo "✅ PASS"
        ((PASSED++))
    else
        echo "❌ FAIL (expected: $expected, got: $result)"
        ((FAILED++))
    fi
    return 0
}

echo "=== Schema Validation ==="
run_test "Users table exists" \
    "SELECT COUNT(*) FROM pg_tables WHERE tablename = 'Sivar_Users';" \
    "1"

run_test "Profiles table exists" \
    "SELECT COUNT(*) FROM pg_tables WHERE tablename = 'Sivar_Profiles';" \
    "1"

run_test "Posts table exists" \
    "SELECT COUNT(*) FROM pg_tables WHERE tablename = 'Sivar_Posts';" \
    "1"

run_test "Bookings table exists" \
    "SELECT COUNT(*) FROM pg_tables WHERE tablename = 'Sivar_ResourceBookings';" \
    "1"

echo ""
echo "=== Data Integrity ==="
run_test "Users have valid emails" \
    "SELECT COUNT(*) FROM \"Sivar_Users\" WHERE \"Email\" NOT LIKE '%@%';" \
    "0"

run_test "Profiles have users" \
    "SELECT COUNT(*) FROM \"Sivar_Profiles\" p LEFT JOIN \"Sivar_Users\" u ON p.\"UserId\" = u.\"Id\" WHERE u.\"Id\" IS NULL;" \
    "0"

echo ""
echo "=== Migrations ==="
run_test "Migrations table exists" \
    "SELECT COUNT(*) FROM pg_tables WHERE tablename = '__EFMigrationsHistory';" \
    "1"

MIGRATION_COUNT=$(PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";" 2>/dev/null | xargs)
echo "Migrations applied: $MIGRATION_COUNT"

echo ""
echo "=== Statistics ==="
USER_COUNT=$(PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c "SELECT COUNT(*) FROM \"Sivar_Users\";" 2>/dev/null | xargs)
PROFILE_COUNT=$(PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c "SELECT COUNT(*) FROM \"Sivar_Profiles\";" 2>/dev/null | xargs)
POST_COUNT=$(PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c "SELECT COUNT(*) FROM \"Sivar_Posts\";" 2>/dev/null | xargs)

echo "Users: $USER_COUNT"
echo "Profiles: $PROFILE_COUNT"
echo "Posts: $POST_COUNT"

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
