#!/bin/bash
# Deployment script for OpenRouter configuration
# Run this on the server (86.48.30.123) after deploying the code

set -e

echo "🚀 Deploying Sivar.Os with OpenRouter (Llama 3.3 70B)"
echo "=================================================="

# 1. Set environment variable for OpenRouter API key
echo "📝 Setting OPENROUTER_API_KEY environment variable..."
sudo tee /etc/systemd/system/sivaros.service.d/openrouter.conf > /dev/null <<EOF
[Service]
Environment="OPENROUTER_API_KEY=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"
EOF

# 2. Reload systemd to pick up new environment
echo "🔄 Reloading systemd configuration..."
sudo systemctl daemon-reload

# 3. Restart the service
echo "🔄 Restarting sivaros service..."
sudo systemctl restart sivaros

# 4. Wait a few seconds for startup
echo "⏳ Waiting for service to start..."
sleep 5

# 5. Check service status
echo "✅ Checking service status..."
sudo systemctl status sivaros --no-pager -l | head -20

# 6. Check logs for OpenRouter initialization
echo ""
echo "📋 Recent logs:"
sudo journalctl -u sivaros -n 30 --no-pager

echo ""
echo "✅ Deployment complete!"
echo ""
echo "🎯 Next steps:"
echo "1. Test chat at: https://dev.sivar.lat/app/chat"
echo "2. Try: 'Necesito un fotógrafo para mi boda'"
echo "3. Monitor logs: sudo journalctl -u sivaros -f"
echo ""
echo "Model: Llama 3.3 70B (meta-llama/llama-3.3-70b-instruct)"
echo "Provider: OpenRouter via https://openrouter.ai"
