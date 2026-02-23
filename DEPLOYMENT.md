# 🚀 Sivar.Os Deployment Guide

## ✅ What's Been Set Up

### 1. Production Infrastructure
- ✅ **Systemd Service** - Auto-restart, runs on boot
- ✅ **Logging** - Structured logs to `/var/log/sivaros/`
- ✅ **Deployment Script** - `deploy.sh` for easy updates
- ✅ **Nginx Config** - Ready for reverse proxy
- ✅ **Health Endpoint** - `/api/Health` and `/api/Health/detailed`

### 2. Database
- ✅ **Schema Fixed** - Added 18+ missing Profile columns
- ✅ **Data Types** - Fixed CategoryKeys, AllowedViewers, Tags
- ✅ **Demo Data** - Photo studio sample content ready

### 3. Authentication
- ✅ **Dev Login** - Email-only login for development
- ✅ **Cookie Auth** - Session management working
- ✅ **User Creation** - Auto-creates users on first login

---

## 📦 Deployment Commands

### Quick Deploy
```bash
cd /root/.openclaw/workspace/SivarOs.Prototype
./deploy.sh deploy
```

### Other Commands
```bash
./deploy.sh build          # Build only
./deploy.sh restart        # Restart service
./deploy.sh logs           # View live logs
./deploy.sh status         # Check service status
./deploy.sh health         # Run health check
./deploy.sh backup         # Backup database
```

---

## 🌐 Domain Setup (When Ready)

### Step 1: DNS Configuration
Add an A record:
```
Type: A
Name: app (or your subdomain)
Value: 86.48.30.123
TTL: 300
```

### Step 2: Configure Nginx
```bash
cd /root/.openclaw/workspace/SivarOs.Prototype
./deploy.sh setup-nginx
# Enter your domain when prompted (e.g., app.sivar.lat)
```

### Step 3: Enable SSL
```bash
./deploy.sh ssl
# Enter your domain when prompted
```

---

## 🔧 Service Management

### Check Service Status
```bash
systemctl status sivaros
```

### View Logs
```bash
# Application logs
tail -f /var/log/sivaros/app.log

# Error logs
tail -f /var/log/sivaros/error.log

# Systemd logs
journalctl -u sivaros -f
```

### Restart Service
```bash
systemctl restart sivaros
```

### Stop Service
```bash
systemctl stop sivaros
```

### Start Service
```bash
systemctl start sivaros
```

---

## 📊 Health Checks

### Basic Health
```bash
curl http://localhost:5001/api/Health
```

**Expected Response:**
```json
{
  "status": "healthy",
  "timestamp": "2026-02-17T10:00:00Z",
  "service": "Sivar.Os",
  "version": "1.0.0-prototype"
}
```

### Detailed Health (with DB stats)
```bash
curl http://localhost:5001/api/Health/detailed
```

---

## 🗄️ Database Management

### Backup Database
```bash
./deploy.sh backup
# Saves to /root/sivaros-backup-YYYYMMDD-HHMMSS.sql.gz
```

### Manual Backup
```bash
PGPASSWORD=Xa1Hf4M3EnAKG8g pg_dump -h 86.48.30.121 -U postgres sivaros > backup.sql
gzip backup.sql
```

### Restore from Backup
```bash
gunzip backup.sql.gz
PGPASSWORD=Xa1Hf4M3EnAKG8g psql -h 86.48.30.121 -U postgres sivaros < backup.sql
```

---

## 🎬 Create Demo Data

Run the demo data script to populate the photo studio:

```bash
cd /root/.openclaw/workspace/SivarOs.Prototype
./create-demo-data.sh
```

This creates:
- Photo studio user (`studio@sivar.os`)
- 4 sample posts (wedding, quinceañera, corporate, testimonial)

---

## 🚨 Troubleshooting

### App Won't Start
```bash
# Check logs
journalctl -u sivaros -n 50

# Check if port is in use
ss -tlnp | grep 5001

# Try building again
cd /root/.openclaw/workspace/SivarOs.Prototype/Sivar.Os
dotnet build -c Release
```

### Database Connection Issues
```bash
# Test connection
PGPASSWORD=Xa1Hf4M3EnAKG8g psql -h 86.48.30.121 -U postgres -d sivaros -c "SELECT 1;"
```

### Nginx Issues
```bash
# Test config
nginx -t

# Reload config
systemctl reload nginx

# Check logs
tail -f /var/log/nginx/error.log
```

---

## 📝 Configuration Files

### Systemd Service
`/etc/systemd/system/sivaros.service`

### Nginx Config
`/root/.openclaw/workspace/SivarOs.Prototype/nginx-sivaros.conf`

### App Settings
`/root/.openclaw/workspace/SivarOs.Prototype/Sivar.Os/appsettings.json`

### Deployment Script
`/root/.openclaw/workspace/SivarOs.Prototype/deploy.sh`

---

## 🔐 Security Notes

### Dev Authentication
⚠️ **IMPORTANT:** Dev authentication is enabled for testing only!

Before going live:
1. Fix Keycloak admin credentials
2. Remove `DevAuthController.cs`
3. Disable dev routes in `Program.cs`
4. Test production Keycloak login

### Firewall
Current: No firewall configured (port 5001 exposed)

For production:
```bash
# Allow HTTP/HTTPS only
ufw allow 80/tcp
ufw allow 443/tcp
ufw enable
```

---

## 📈 Monitoring

### Application Metrics
- View count: Check `/api/Analytics/profile/{profileId}`
- User stats: Check `/api/Health/detailed`

### System Metrics
```bash
# CPU/Memory
htop

# Disk space
df -h

# Network
ss -tlnp
```

---

## 🔄 Update Workflow

1. Pull latest code (if using Git)
2. Run `./deploy.sh deploy`
3. Check logs for errors
4. Test endpoints
5. Done!

---

## 🆘 Emergency Rollback

If something goes wrong:

1. Stop the service
   ```bash
   systemctl stop sivaros
   ```

2. Restore database backup
   ```bash
   ./deploy.sh backup  # Create current backup first!
   # Then restore old backup
   ```

3. Revert code changes (if using Git)
   ```bash
   git checkout <previous-commit>
   ```

4. Rebuild and start
   ```bash
   ./deploy.sh deploy
   ```

---

**Last Updated:** 2026-02-17  
**Version:** 1.0.0-prototype  
**Support:** Check logs first, then review TROUBLESHOOTING.md
