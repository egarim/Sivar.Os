# 🔧 Troubleshooting Guide - Sivar.Os

Common issues and how to fix them.

---

## 🚨 App Won't Start

### Symptom
```bash
systemctl status sivaros
# Shows: failed or inactive (dead)
```

### Diagnosis
```bash
# Check recent logs
journalctl -u sivaros -n 50

# Check error log
tail -50 /var/log/sivaros/error.log
```

### Common Causes

#### 1. Port Already in Use
```bash
# Check what's using port 5001
ss -tlnp | grep 5001

# Kill the process
kill -9 <PID>

# Restart service
systemctl restart sivaros
```

#### 2. Database Connection Failed
```bash
# Test database connection
PGPASSWORD=Xa1Hf4M3EnAKG8g psql -h 86.48.30.121 -U postgres -d sivaros -c "SELECT 1;"

# If fails, check:
# - Database server is running
# - Firewall allows connection
# - Credentials are correct
```

#### 3. Build Errors
```bash
cd /root/.openclaw/workspace/SivarOs.Prototype/Sivar.Os
dotnet build -c Release

# Fix any errors, then:
systemctl restart sivaros
```

---

## 🌐 Can't Access Website

### Symptom
- Browser shows "Connection refused"
- Or "This site can't be reached"

### Diagnosis
```bash
# Check if app is running
ss -tlnp | grep 5001

# Check if Nginx is running (if configured)
systemctl status nginx

# Check firewall
ufw status
```

### Solutions

#### App Not Running
```bash
systemctl start sivaros
```

#### Nginx Not Configured
```bash
cd /root/.openclaw/workspace/SivarOs.Prototype
./deploy.sh setup-nginx
```

#### DNS Not Updated
- Check DNS propagation: https://dnschecker.org
- Wait 5-10 minutes for DNS to propagate
- Try: `nslookup your-domain.com`

---

## 🔐 Authentication Issues

### "Invalid Hostname" Error

**Symptom:** Get HTML error "Bad Request - Invalid Hostname"

**Cause:** App is in Production mode and rejects unrecognized hostnames

**Solution 1 - Add Domain to AllowedHosts:**
```bash
# Edit appsettings.json
nano /root/.openclaw/workspace/SivarOs.Prototype/Sivar.Os/appsettings.json

# Change:
"AllowedHosts": "*",

# To:
"AllowedHosts": "localhost;127.0.0.1;86.48.30.123;*.sivar.lat;your-domain.com",

# Restart
systemctl restart sivaros
```

**Solution 2 - Use Development Mode (temporary):**
```bash
# Edit service file
nano /etc/systemd/system/sivaros.service

# Change:
Environment=ASPNETCORE_ENVIRONMENT=Production

# To:
Environment=ASPNETCORE_ENVIRONMENT=Development

# Reload and restart
systemctl daemon-reload
systemctl restart sivaros
```

### Dev Login Not Working

**Symptom:** POST to `/api/DevAuth/login` fails

**Check:**
```bash
# Test endpoint
curl -X POST http://localhost:5001/api/DevAuth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com"}'

# Check logs
tail -f /var/log/sivaros/error.log
```

**Common Issues:**
- Database connection failed → Check DB
- Schema mismatch → Run migration fixes
- Service not running → Start service

---

## 💾 Database Issues

### Can't Connect to Database

```bash
# Test connection
PGPASSWORD=Xa1Hf4M3EnAKG8g psql -h 86.48.30.121 -U postgres -d sivaros -c "SELECT 1;"

# If fails:
# 1. Check database server is up
# 2. Check firewall allows port 5432
# 3. Verify credentials
```

### Migration Errors

**Symptom:** App crashes on startup with migration error

**Solution:**
```bash
# Check applied migrations
PGPASSWORD=Xa1Hf4M3EnAKG8g psql -h 86.48.30.121 -U postgres -d sivaros \
  -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\";"

# If schema mismatch, apply manual fixes:
cd /root/.openclaw/workspace/SivarOs.Prototype
PGPASSWORD=Xa1Hf4M3EnAKG8g psql -h 86.48.30.121 -U postgres -d sivaros -f add-profile-columns.sql
```

### Column Missing Error

**Symptom:** Error like "column X does not exist"

**Solution:** Check `add-profile-columns.sql` has been applied
```bash
PGPASSWORD=Xa1Hf4M3EnAKG8g psql -h 86.48.30.121 -U postgres -d sivaros \
  -c "\d \"Sivar_Profiles\"" | grep <column_name>
```

---

## 📸 Image Upload Fails

### Azure Blob Not Configured

**Symptom:** Upload fails with "Connection string not configured"

**Solutions:**

**For Development - Use Azurite:**
```bash
# Check Azurite is running
systemctl status azurite

# Start if needed
systemctl start azurite

# Verify appsettings.Development.json has Azurite config
cat /root/.openclaw/workspace/SivarOs.Prototype/Sivar.Os/appsettings.Development.json
```

**For Production - Update Azure SAS Token:**
```bash
# Edit appsettings.json
nano /root/.openclaw/workspace/SivarOs.Prototype/Sivar.Os/appsettings.json

# Update:
"AzureBlobStorage": {
  "Enabled": true,
  "ConnectionString": "<YOUR_NEW_SAS_TOKEN>"
}

# Restart
systemctl restart sivaros
```

---

## 🔄 Nginx Issues

### Nginx Won't Start

```bash
# Test config
nginx -t

# Check error
systemctl status nginx

# Common fix - config syntax error
nano /etc/nginx/sites-available/sivaros
# Fix any syntax errors

# Reload
nginx -t && systemctl reload nginx
```

### 502 Bad Gateway

**Cause:** Nginx can't reach backend (Sivar.Os)

**Solution:**
```bash
# Check backend is running
ss -tlnp | grep 5001

# Start if needed
systemctl start sivaros

# Check Nginx upstream config
cat /etc/nginx/sites-available/sivaros | grep upstream
```

### 504 Gateway Timeout

**Cause:** Backend is slow to respond

**Solution:** Increase timeout in Nginx config
```bash
nano /etc/nginx/sites-available/sivaros

# Add inside location block:
proxy_read_timeout 300s;
proxy_connect_timeout 300s;

# Reload
systemctl reload nginx
```

---

## 🔒 SSL Certificate Issues

### Certificate Won't Install

```bash
# Stop Nginx temporarily
systemctl stop nginx

# Try again
certbot certonly --standalone -d your-domain.com

# Start Nginx
systemctl start nginx
```

### Certificate Expired

```bash
# Renew
certbot renew

# Reload Nginx
systemctl reload nginx
```

### Auto-Renewal Not Working

```bash
# Test renewal
certbot renew --dry-run

# Check cron/timer
systemctl list-timers | grep certbot
```

---

## 📊 Performance Issues

### App is Slow

**Check Resource Usage:**
```bash
# CPU/Memory
htop

# Database connections
PGPASSWORD=Xa1Hf4M3EnAKG8g psql -h 86.48.30.121 -U postgres -d sivaros \
  -c "SELECT count(*) FROM pg_stat_activity WHERE datname='sivaros';"
```

**Solutions:**
- Enable Redis caching
- Optimize database queries
- Add indexes
- Scale horizontally (multiple instances)

### Database is Slow

```bash
# Check long-running queries
PGPASSWORD=Xa1Hf4M3EnAKG8g psql -h 86.48.30.121 -U postgres -d sivaros \
  -c "SELECT pid, now() - pg_stat_activity.query_start AS duration, query 
      FROM pg_stat_activity 
      WHERE state = 'active' AND now() - pg_stat_activity.query_start > interval '5 seconds';"
```

---

## 🗑️ Clean Up / Reset

### Clear Logs
```bash
# Truncate logs (keeps file, clears content)
truncate -s 0 /var/log/sivaros/app.log
truncate -s 0 /var/log/sivaros/error.log
```

### Reset Database (⚠️ DESTROYS DATA)
```bash
# Backup first!
./deploy.sh backup

# Drop and recreate
PGPASSWORD=Xa1Hf4M3EnAKG8g psql -h 86.48.30.121 -U postgres -c "DROP DATABASE sivaros;"
PGPASSWORD=Xa1Hf4M3EnAKG8g psql -h 86.48.30.121 -U postgres -c "CREATE DATABASE sivaros;"

# Apply migrations
cd /root/.openclaw/workspace/SivarOs.Prototype/Sivar.Os
dotnet ef database update
```

### Fresh Install
```bash
# Stop services
systemctl stop sivaros azurite nginx

# Remove data
rm -rf /var/lib/azurite/*
rm -f /var/log/sivaros/*

# Rebuild
cd /root/.openclaw/workspace/SivarOs.Prototype/Sivar.Os
dotnet clean
dotnet build -c Release

# Restart
systemctl start sivaros azurite
```

---

## 📞 Still Stuck?

### Collect Debug Info
```bash
# Create debug report
cat > debug-report.txt <<EOF
=== System Info ===
$(uname -a)

=== Service Status ===
$(systemctl status sivaros --no-pager)

=== Recent Logs ===
$(tail -50 /var/log/sivaros/error.log)

=== Database Connection ===
$(PGPASSWORD=Xa1Hf4M3EnAKG8g psql -h 86.48.30.121 -U postgres -d sivaros -c "SELECT version();" 2>&1)

=== Port Status ===
$(ss -tlnp | grep -E "5001|5432|80|443")

=== Build Info ===
$(dotnet --version)
EOF

# Share debug-report.txt
```

### Quick Health Check Script
```bash
#!/bin/bash
echo "🏥 Sivar.Os Health Check"
echo ""
echo "✓ App Service:"
systemctl is-active sivaros || echo "  ❌ Service not running"
echo ""
echo "✓ Port 5001:"
ss -tlnp | grep 5001 || echo "  ❌ Port not listening"
echo ""
echo "✓ Database:"
PGPASSWORD=Xa1Hf4M3EnAKG8g psql -h 86.48.30.121 -U postgres -d sivaros -c "SELECT 1;" > /dev/null 2>&1 && echo "  ✅ Connected" || echo "  ❌ Connection failed"
echo ""
echo "✓ Recent Errors:"
grep -i "error\|exception\|fail" /var/log/sivaros/error.log | tail -5
```

---

## 🎯 Common Error Messages

| Error | Meaning | Solution |
|-------|---------|----------|
| `EADDRINUSE` | Port already in use | Kill process on port 5001 |
| `Connection refused` | Service not running | `systemctl start sivaros` |
| `column does not exist` | Schema mismatch | Apply SQL fix |
| `Invalid hostname` | Host not allowed | Update AllowedHosts |
| `Connection timeout` | DB unreachable | Check firewall/network |
| `502 Bad Gateway` | Backend down | Start backend service |
| `404 Not Found` | Route doesn't exist | Check API documentation |
| `401 Unauthorized` | Not logged in | Login via `/api/DevAuth/login` |

---

**Remember:** Always check logs first! 📜  
`tail -f /var/log/sivaros/error.log`
