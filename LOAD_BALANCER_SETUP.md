# 🔧 Load Balancer Configuration Guide

**Goal:** Configure load balancer (86.48.30.122) to forward `dev.sivar.lat` to dev server (86.48.30.123)

---

## 📋 Before You Start

**Identify your load balancer software:**

```bash
# SSH to load balancer:
ssh root@86.48.30.122

# Check what's running:
which nginx && echo "Using Nginx" || echo "Not Nginx"
which haproxy && echo "Using HAProxy" || echo "Not HAProxy"

# Or check running processes:
ps aux | grep -E 'nginx|haproxy|traefik'
```

---

## 🔵 If Using Nginx

### Step 1: Upload Config

**Copy the config file:**

```bash
# From load balancer (86.48.30.122):
scp root@86.48.30.123:/root/.openclaw/workspace/SivarOs.Prototype/load-balancer-nginx.conf /etc/nginx/sites-available/dev-sivar

# Or manually create:
nano /etc/nginx/sites-available/dev-sivar
# Paste the contents from load-balancer-nginx.conf
```

### Step 2: Enable Site

```bash
# Create symlink
ln -sf /etc/nginx/sites-available/dev-sivar /etc/nginx/sites-enabled/dev-sivar

# Remove any conflicting configs
ls /etc/nginx/sites-enabled/
# If you see other configs for dev.sivar.lat, remove them
```

### Step 3: Test & Apply

```bash
# Test configuration
nginx -t

# If OK, reload:
systemctl reload nginx

# Check status:
systemctl status nginx
```

### Step 4: Verify

```bash
# From load balancer:
curl -H "Host: dev.sivar.lat" http://localhost/

# Should return Sivar.Os HTML (not Prometheus)
```

---

## 🟢 If Using HAProxy

### Step 1: Backup Current Config

```bash
cp /etc/haproxy/haproxy.cfg /etc/haproxy/haproxy.cfg.backup
```

### Step 2: Edit Config

```bash
nano /etc/haproxy/haproxy.cfg
```

Add the backend section from `load-balancer-haproxy.cfg` (I created this file for you).

### Step 3: Test & Apply

```bash
# Test configuration
haproxy -c -f /etc/haproxy/haproxy.cfg

# If OK, reload:
systemctl reload haproxy

# Check status:
systemctl status haproxy
```

---

## 🟣 If Using Traefik

### Step 1: Create Dynamic Config

```bash
nano /etc/traefik/dynamic/dev-sivar.yml
```

```yaml
http:
  routers:
    dev-sivar-router:
      rule: "Host(`dev.sivar.lat`)"
      service: dev-sivar-service
      entryPoints:
        - web
        - websecure
      
  services:
    dev-sivar-service:
      loadBalancer:
        servers:
          - url: "http://86.48.30.123:80"
```

### Step 2: Reload

```bash
# Traefik auto-reloads dynamic configs
# Or restart:
systemctl restart traefik
```

---

## 🟡 If Using Something Else

**Tell me what you're using and I'll provide the config!**

Common options:
- Caddy
- Apache
- Envoy
- Cloudflare Workers

---

## ✅ After Configuration

Test from your laptop:

```bash
curl http://dev.sivar.lat/
# Should return Sivar.Os landing page

curl http://dev.sivar.lat/api/Health
# Should return: {"status":"healthy","service":"Sivar.Os"...}
```

Browse to: http://dev.sivar.lat  
Should see: Sivar.Os (not Prometheus!)

---

## 🔐 SSL Certificate (After HTTP Works)

Once HTTP traffic is working, run on **load balancer**:

```bash
# Install certbot
apt-get update && apt-get install -y certbot python3-certbot-nginx

# Get certificate
certbot --nginx -d dev.sivar.lat --email admin@sivar.lat --agree-tos --non-interactive --redirect

# Or for HAProxy:
certbot certonly --standalone -d dev.sivar.lat --email admin@sivar.lat --agree-tos
```

---

## 🆘 Troubleshooting

### "nginx: command not found"
```bash
export PATH="/usr/sbin:/usr/local/sbin:$PATH"
nginx -t
```

### "Connection refused"
```bash
# Check if backend is reachable:
curl http://86.48.30.123:80/api/Health

# Check firewall:
ufw status
iptables -L -n | grep 80
```

### "502 Bad Gateway"
```bash
# Check if dev server is running:
ssh root@86.48.30.123 'systemctl status sivaros'

# Check nginx on dev server:
ssh root@86.48.30.123 'systemctl status nginx'
```

### Still seeing Prometheus?
```bash
# Check which backend is configured:
grep -r "prometheus" /etc/nginx/sites-enabled/
grep -r "86.48.30" /etc/nginx/sites-enabled/

# Or for HAProxy:
grep -r "prometheus" /etc/haproxy/
grep -r "86.48.30" /etc/haproxy/
```

---

## 🚀 Need Help?

**Option 1: Give Dennis SSH Access**

```bash
# On load balancer (86.48.30.122):
echo "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIHIUp2no6df5t5hxYz8cLgwfZKHoWkvatRiC9dvOJHTH sivaros-dev-server" >> /root/.ssh/authorized_keys

# Dennis can then configure it automatically
```

**Option 2: Share Config Files**

```bash
# Show me your current config:
cat /etc/nginx/sites-enabled/* 
# or
cat /etc/haproxy/haproxy.cfg
```

**Option 3: Screen Share**

Jump on a call and I'll guide you through it!

---

**Files Created:**
- `load-balancer-nginx.conf` - Ready-to-use Nginx config
- `load-balancer-haproxy.cfg` - Ready-to-use HAProxy config
- `LOAD_BALANCER_SETUP.md` - This guide

**Location:** `/root/.openclaw/workspace/SivarOs.Prototype/`
