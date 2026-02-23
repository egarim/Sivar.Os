#!/bin/bash
# Sivar.Os Deployment Script
# Usage: ./deploy.sh [build|restart|logs|status]

set -e

APP_DIR="/root/.openclaw/workspace/SivarOs.Prototype/Sivar.Os"
SERVICE_NAME="sivaros"

case "$1" in
    build)
        echo "🔨 Building Sivar.Os..."
        cd "$APP_DIR"
        dotnet build -c Release
        echo "✅ Build complete"
        ;;
    
    restart)
        echo "🔄 Restarting Sivar.Os service..."
        systemctl restart $SERVICE_NAME
        sleep 3
        systemctl status $SERVICE_NAME --no-pager
        echo "✅ Service restarted"
        ;;
    
    deploy)
        echo "🚀 Full deployment..."
        cd "$APP_DIR"
        
        # Stop service
        echo "⏸️  Stopping service..."
        systemctl stop $SERVICE_NAME || true
        
        # Build
        echo "🔨 Building..."
        dotnet build -c Release
        
        # Start service
        echo "▶️  Starting service..."
        systemctl start $SERVICE_NAME
        sleep 3
        
        # Check status
        systemctl status $SERVICE_NAME --no-pager
        echo "✅ Deployment complete"
        ;;
    
    logs)
        echo "📜 Showing logs (Ctrl+C to exit)..."
        journalctl -u $SERVICE_NAME -f
        ;;
    
    logs-app)
        echo "📜 App logs..."
        tail -f /var/log/sivaros/app.log
        ;;
    
    logs-error)
        echo "📜 Error logs..."
        tail -f /var/log/sivaros/error.log
        ;;
    
    status)
        echo "📊 Service status..."
        systemctl status $SERVICE_NAME --no-pager
        echo ""
        echo "🔌 Port check..."
        ss -tlnp | grep 5001 || echo "⚠️  Port 5001 not listening"
        ;;
    
    health)
        echo "🏥 Health check..."
        curl -s http://localhost:5001/api/DevAuth/status | jq . || echo "⚠️  Health check failed"
        ;;
    
    setup-nginx)
        echo "🌐 Setting up Nginx..."
        read -p "Enter your domain (e.g., app.sivar.lat): " DOMAIN
        
        if [ -z "$DOMAIN" ]; then
            echo "❌ Domain required"
            exit 1
        fi
        
        # Install nginx if needed
        if ! command -v nginx &> /dev/null; then
            echo "📦 Installing Nginx..."
            apt-get update
            apt-get install -y nginx
        fi
        
        # Create config
        sed "s/YOUR_DOMAIN/$DOMAIN/g" /root/.openclaw/workspace/SivarOs.Prototype/nginx-sivaros.conf > /etc/nginx/sites-available/sivaros
        
        # Enable site
        ln -sf /etc/nginx/sites-available/sivaros /etc/nginx/sites-enabled/sivaros
        
        # Test config
        nginx -t
        
        # Reload nginx
        systemctl reload nginx
        
        echo "✅ Nginx configured for $DOMAIN"
        echo "📝 Add DNS A record: $DOMAIN -> $(curl -s ifconfig.me)"
        ;;
    
    ssl)
        echo "🔒 Setting up SSL..."
        read -p "Enter your domain: " DOMAIN
        
        if [ -z "$DOMAIN" ]; then
            echo "❌ Domain required"
            exit 1
        fi
        
        # Install certbot
        if ! command -v certbot &> /dev/null; then
            echo "📦 Installing Certbot..."
            apt-get update
            apt-get install -y certbot python3-certbot-nginx
        fi
        
        # Get certificate
        certbot --nginx -d $DOMAIN
        
        echo "✅ SSL configured"
        ;;
    
    backup)
        echo "💾 Creating database backup..."
        BACKUP_FILE="/root/sivaros-backup-$(date +%Y%m%d-%H%M%S).sql"
        PGPASSWORD=Xa1Hf4M3EnAKG8g pg_dump -h 86.48.30.121 -U postgres sivaros > "$BACKUP_FILE"
        gzip "$BACKUP_FILE"
        echo "✅ Backup saved: ${BACKUP_FILE}.gz"
        ;;
    
    *)
        echo "Sivar.Os Deployment Script"
        echo ""
        echo "Usage: $0 [command]"
        echo ""
        echo "Commands:"
        echo "  build        - Build the application"
        echo "  restart      - Restart the service"
        echo "  deploy       - Full deployment (stop, build, start)"
        echo "  logs         - Show live logs (journalctl)"
        echo "  logs-app     - Show application logs"
        echo "  logs-error   - Show error logs"
        echo "  status       - Show service status"
        echo "  health       - Run health check"
        echo "  setup-nginx  - Configure Nginx reverse proxy"
        echo "  ssl          - Setup SSL with Let's Encrypt"
        echo "  backup       - Backup database"
        echo ""
        exit 1
        ;;
esac
