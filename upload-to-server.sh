#!/bin/bash
# Helper script to upload modified files to server
# Run this from your local machine

SERVER="root@86.48.30.123"
PROJECT_DIR="/opt/sivaros"

echo "📦 Uploading OpenRouter integration files..."
echo "=============================================="

# Upload modified files
echo "1. Uploading ChatServiceOptions.cs..."
scp Sivar.Os/Services/ChatServiceOptions.cs \
    $SERVER:$PROJECT_DIR/Sivar.Os/Services/

echo "2. Uploading Program.cs..."
scp Sivar.Os/Program.cs \
    $SERVER:$PROJECT_DIR/Sivar.Os/

echo "3. Uploading appsettings.json..."
scp Sivar.Os/appsettings.json \
    $SERVER:$PROJECT_DIR/Sivar.Os/

echo "4. Uploading deployment script..."
scp deploy-openrouter.sh \
    $SERVER:$PROJECT_DIR/

echo ""
echo "✅ Files uploaded!"
echo ""
echo "🔧 Next steps on server:"
echo "1. SSH to server: ssh $SERVER"
echo "2. cd $PROJECT_DIR"
echo "3. Build: dotnet build Sivar.Os/Sivar.Os.csproj -c Release"
echo "4. Publish: dotnet publish Sivar.Os/Sivar.Os.csproj -c Release -o publish"
echo "5. Deploy: ./deploy-openrouter.sh"
echo ""
echo "Or run all at once:"
echo "ssh $SERVER 'cd $PROJECT_DIR && dotnet publish Sivar.Os/Sivar.Os.csproj -c Release -o publish && ./deploy-openrouter.sh'"
