# 🚀 Quick Deployment Guide

## Step 1: Upload Package to Server

```bash
# From your local machine, upload the package
scp /root/.openclaw/workspace/SivarOs.Prototype/openrouter-update.tar.gz \
    root@86.48.30.123:/tmp/
```

## Step 2: Deploy on Server

```bash
# SSH to server
ssh root@86.48.30.123

# Extract package
cd /opt/sivaros
tar -xzf /tmp/openrouter-update.tar.gz

# Run deployment script
chmod +x deploy-package.sh
./deploy-package.sh
```

## Step 3: Verify Deployment

Visit: https://dev.sivar.lat/api/health

Should return: `{"status":"Healthy",...}`

## Step 4: Test Chat with OpenRouter

1. Visit: https://dev.sivar.lat/app/chat
2. Sign in (see below for test account)
3. Send message: "Necesito un fotógrafo para mi boda"
4. Expect: Warm Spanish response from Llama 3.3 70B

---

## 🧪 Test Account Options

### Option A: Create New Account

1. Visit: https://dev.sivar.lat
2. Click "Sign Up" (if available)
3. Or use dev auth: https://dev.sivar.lat/dev-auth

### Option B: Use Existing Test User

If you have test user credentials from before, use those.

### Option C: Create via API

```bash
# Create test user via dev-auth
curl -X POST https://dev.sivar.lat/api/dev-auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "joche@test.com"}'
```

---

## 🎯 What to Test

### Test 1: Basic Chat
```
You: "Hola!"
Expected: Warm Spanish greeting
```

### Test 2: Search for Services
```
You: "Necesito un fotógrafo"
Expected: Show Studio Fotográfico with details
```

### Test 3: Book Appointment
```
You: "Quiero reservar el paquete de boda"
Expected: Ask for date/time, check availability, create booking
```

### Test 4: Check Quality
```
Compare responses to what you got before
- Is Spanish better?
- Is personality warmer?
- Are responses more helpful?
```

---

## 📊 Monitor Logs

```bash
# Watch logs in real-time
ssh root@86.48.30.123
sudo journalctl -u sivaros -f

# Look for:
✅ "meta-llama/llama-3.3-70b-instruct"
✅ Successful chat completions
✅ Fast response times

❌ API errors
❌ Slow responses
```

---

## 🆘 Rollback (if needed)

```bash
# Switch back to OpenAI
ssh root@86.48.30.123
cd /opt/sivaros/publish

# Edit appsettings.json
sed -i 's/"Provider": "openrouter"/"Provider": "openai"/' appsettings.json

# Restart
systemctl restart sivaros
```

---

**Ready!** Let me know when you've deployed and I'll help you test! 🎯
