#!/bin/bash
# Sivar.Os Screenshot Tool
# Takes screenshots of key pages for visual validation

set -e

BASE_URL="${1:-http://127.0.0.1:5001}"
OUTPUT_DIR="screenshots/$(date +%Y%m%d-%H%M%S)"

echo "📸 Sivar.Os Screenshot Tool"
echo "Base URL: $BASE_URL"
echo "Output: $OUTPUT_DIR"
echo ""

mkdir -p "$OUTPUT_DIR"

# Landing page
echo "1/6 Capturing landing page..."
npx playwright screenshot "$BASE_URL/" "$OUTPUT_DIR/01-landing.png" --wait-for-timeout 2000

# Health endpoint (as HTML)
echo "2/6 Capturing health endpoint..."
npx playwright screenshot "$BASE_URL/api/Health" "$OUTPUT_DIR/02-health.png"

# Profile page (if accessible)
echo "3/6 Capturing profile page..."
npx playwright screenshot "$BASE_URL/app/home" "$OUTPUT_DIR/03-home.png" --wait-for-timeout 3000 || echo "  ⚠️  Skipped (requires auth)"

# Mobile view
echo "4/6 Capturing mobile view..."
npx playwright screenshot "$BASE_URL/" "$OUTPUT_DIR/04-mobile.png" \
  --viewport-size 375,667 --wait-for-timeout 2000

# Tablet view
echo "5/6 Capturing tablet view..."
npx playwright screenshot "$BASE_URL/" "$OUTPUT_DIR/05-tablet.png" \
  --viewport-size 768,1024 --wait-for-timeout 2000

# Desktop wide
echo "6/6 Capturing desktop wide view..."
npx playwright screenshot "$BASE_URL/" "$OUTPUT_DIR/06-desktop-wide.png" \
  --viewport-size 1920,1080 --wait-for-timeout 2000

echo ""
echo "✅ Screenshots complete!"
echo "Location: $OUTPUT_DIR"
echo ""
ls -lh "$OUTPUT_DIR"
