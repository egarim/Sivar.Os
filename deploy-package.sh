#!/bin/bash
# Complete deployment package for OpenRouter
# Run this script on the server (86.48.30.123)

set -e

echo "🚀 Deploying OpenRouter Integration"
echo "===================================="

# Change to project directory
cd /opt/sivaros

# Backup current appsettings
echo "📦 Backing up current config..."
cp publish/appsettings.json publish/appsettings.json.backup.$(date +%Y%m%d_%H%M%S)

# Build and publish
echo "🔨 Building project..."
dotnet publish Sivar.Os/Sivar.Os.csproj -c Release -o publish

# Set API key environment variable
echo "🔑 Configuring API key..."
mkdir -p /etc/systemd/system/sivaros.service.d/
cat > /etc/systemd/system/sivaros.service.d/openrouter.conf << 'EOF'
[Service]
Environment="OPENROUTER_API_KEY=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"
EOF

# Reload and restart
echo "🔄 Restarting service..."
systemctl daemon-reload
systemctl restart sivaros

# Wait for startup
echo "⏳ Waiting for service to start..."
sleep 5

# Check health
echo "🏥 Checking health endpoint..."
curl -s https://dev.sivar.lat/api/health | jq . || echo "Health check pending..."

# Show status
echo ""
echo "✅ Service status:"
systemctl status sivaros --no-pager -l | head -20

echo ""
echo "📋 Recent logs:"
journalctl -u sivaros -n 30 --no-pager

echo ""
echo "✅ DEPLOYMENT COMPLETE!"
echo ""
echo "🧪 Test it now:"
echo "1. Visit: https://dev.sivar.lat/app/chat"
echo "2. Sign in"
echo "3. Try: 'Necesito un fotógrafo para mi boda'"
echo ""
echo "Model: meta-llama/llama-3.3-70b-instruct (via OpenRouter)"
echo "Expected: Warm Spanish response with booking assistance"
