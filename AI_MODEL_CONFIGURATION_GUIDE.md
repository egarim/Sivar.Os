# 🤖 AI Model Configuration Guide - Sivar.Os

**Date:** 2026-02-17  
**System:** Chat & Booking AI Configuration  
**Current Setup:** OpenAI gpt-4o-mini via Microsoft.Extensions.AI

---

## 📊 **CURRENT CONFIGURATION**

### **What You're Using Now:**

```json
"ChatService": {
  "Provider": "openai",
  "OpenAI": {
    "ModelId": "gpt-4o-mini",
    "ApiKey": "[from OPENAI_API_KEY env var]"
  },
  "MaxTokens": 2000,
  "Temperature": 0.7
}
```

**Architecture:**
```
Microsoft.Extensions.AI (abstraction)
    ↓
Microsoft.Extensions.AI.OpenAI (provider)
    ↓
OpenAI SDK
    ↓
OpenAI API (gpt-4o-mini)
```

---

## ✅ **YES! YOU CAN USE OPENROUTER!**

### **Why OpenRouter is Perfect:**

**1. OpenAI-Compatible API**
```
OpenRouter uses the same API format as OpenAI
→ Just change the endpoint URL
→ No code changes needed!
```

**2. Access to Many Models**
```
OpenRouter aggregates:
- OpenAI (GPT-4, GPT-4o, GPT-4o-mini)
- Anthropic (Claude 3.5 Sonnet, Haiku, Opus)
- Meta (Llama 3.1, 3.2, 3.3)
- Google (Gemini Pro, Flash)
- Mistral (Mistral Large, Small)
- And 100+ more models!
```

**3. Cheap Options**
```
Starting from $0.06 per million tokens!
Much cheaper than direct OpenAI for some models
```

**4. Same Tool Support**
```
✅ Function calling (for BookingFunctions)
✅ Streaming responses
✅ System prompts
✅ All features you need
```

---

## 🎯 **RECOMMENDED MODELS**

### **For Sivar.Os Chat (Cheap + Tools + Personality):**

### **🏆 TOP PICK: Llama 3.3 70B (OpenRouter)**

```json
Model: meta-llama/llama-3.3-70b-instruct
Cost: $0.35 / 1M input tokens, $0.40 / 1M output tokens
Quality: ⭐⭐⭐⭐⭐ (Excellent)
Tool Support: ✅ Native function calling
Personality: ✅ Great at natural conversations
Speed: Fast (80-100 tokens/sec)

Why Perfect for Sivar.Os:
- Excellent at Spanish (trained on multilingual data)
- Great with tools (booking functions will work perfectly)
- Warm, helpful personality
- 10x cheaper than GPT-4
- Similar quality to GPT-4o for most tasks
```

**Cost Comparison (1000 bookings/day):**
```
Scenario: 1000 chat interactions per day
Average: 500 input tokens + 200 output tokens per interaction

Llama 3.3 70B:
- Input: 1000 × 500 × $0.35/1M = $0.175/day
- Output: 1000 × 200 × $0.40/1M = $0.08/day
- Total: $0.255/day = $7.65/month 💰

GPT-4o-mini (current):
- Input: 1000 × 500 × $0.15/1M = $0.075/day
- Output: 1000 × 200 × $0.60/1M = $0.12/day
- Total: $0.195/day = $5.85/month 💰

GPT-4o:
- Input: 1000 × 500 × $2.50/1M = $1.25/day
- Output: 1000 × 200 × $10.00/1M = $2.00/day
- Total: $3.25/day = $97.50/month 💸
```

**Verdict:** Llama 3.3 70B is slightly more expensive than GPT-4o-mini but MUCH better quality!

---

### **🥈 SECOND CHOICE: Qwen 2.5 72B (Cheapest with Quality)**

```json
Model: qwen/qwen-2.5-72b-instruct
Cost: $0.35 / 1M input tokens, $0.40 / 1M output tokens
Quality: ⭐⭐⭐⭐ (Very Good)
Tool Support: ✅ Function calling
Personality: ✅ Professional and helpful
Speed: Fast

Why Great:
- Excellent multilingual (Chinese model, trained on Spanish)
- Strong tool use capabilities
- Same pricing as Llama 3.3 70B
- Very fast responses
```

---

### **🥉 THIRD CHOICE: Mistral Large 2 (Latest)**

```json
Model: mistralai/mistral-large-2411
Cost: $2.00 / 1M input tokens, $6.00 / 1M output tokens
Quality: ⭐⭐⭐⭐⭐ (Excellent, GPT-4 level)
Tool Support: ✅ Native function calling
Personality: ✅ Professional, engaging
Speed: Very fast

Why Consider:
- Latest Mistral model (Nov 2024)
- Excellent at structured outputs
- Great multilingual including Spanish
- More expensive but still cheaper than GPT-4
```

---

### **💰 ULTRA CHEAP OPTION: Llama 3.1 8B**

```json
Model: meta-llama/llama-3.1-8b-instruct
Cost: $0.06 / 1M input tokens, $0.06 / 1M output tokens
Quality: ⭐⭐⭐ (Good for simple tasks)
Tool Support: ✅ Basic function calling
Personality: ✅ Decent
Speed: Very fast

Cost for 1000 bookings/day:
- Total: $0.042/day = $1.26/month 💰💰💰

Why Consider:
- EXTREMELY cheap
- Good enough for simple booking flows
- Fast responses
- Can handle Spanish adequately

Warning:
- May struggle with complex queries
- Less personality/warmth
- Might need more prompt engineering
```

---

## ⚡ **CURRENT MODEL ANALYSIS: GPT-4o-mini**

```json
Model: gpt-4o-mini
Cost: $0.15 / 1M input, $0.60 / 1M output
Quality: ⭐⭐⭐⭐ (Very good)
Tool Support: ✅ Excellent
Personality: ✅ Great

Pros:
✅ Excellent tool use (best in class)
✅ Great personality
✅ Reliable, proven
✅ Very good at Spanish
✅ Fast

Cons:
⚠️ More expensive than alternatives
⚠️ OpenAI API sometimes has rate limits
⚠️ No control over model updates
```

**Verdict:** GPT-4o-mini is solid! But you can get similar quality for same or less cost with OpenRouter alternatives.

---

## 🛠️ **HOW TO ADD OPENROUTER SUPPORT**

### **Step 1: Add OpenRouter Configuration**

**File:** `appsettings.json`

```json
{
  "ChatService": {
    "Provider": "openrouter",
    "MaxMessagesPerConversation": 1000,
    "DefaultResponseType": "SimpleText",
    "MaxTokens": 2000,
    "Temperature": 0.7,
    "RateLimitPerMinute": 20,
    
    "OpenRouter": {
      "ApiKey": "",
      "BaseUrl": "https://openrouter.ai/api/v1",
      "ModelId": "meta-llama/llama-3.3-70b-instruct",
      "SiteName": "Sivar.Os",
      "SiteUrl": "https://sivar.lat"
    },
    
    "OpenAI": {
      "ModelId": "gpt-4o-mini",
      "ApiKey": ""
    },
    
    "Ollama": {
      "Endpoint": "http://127.0.0.1:11434",
      "ModelId": "llama3.2:latest"
    }
  }
}
```

### **Step 2: Update ChatServiceOptions.cs**

```csharp
public class ChatServiceOptions
{
    public const string SectionName = "ChatService";

    /// <summary>
    /// AI provider: "ollama", "openai", or "openrouter"
    /// </summary>
    public string Provider { get; set; } = "ollama";

    public int MaxMessagesPerConversation { get; set; } = 1000;
    public string DefaultResponseType { get; set; } = "SimpleText";
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.7;
    public int RateLimitPerMinute { get; set; } = 20;

    public OllamaSettings Ollama { get; set; } = new();
    public OpenAISettings OpenAI { get; set; } = new();
    public OpenRouterSettings OpenRouter { get; set; } = new(); // ✨ NEW

    public class OllamaSettings
    {
        public string Endpoint { get; set; } = "http://127.0.0.1:11434";
        public string ModelId { get; set; } = "llama3.2:latest";
    }

    public class OpenAISettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModelId { get; set; } = "gpt-4o";
        public string? OrganizationId { get; set; }
    }

    // ✨ NEW: OpenRouter settings
    public class OpenRouterSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
        public string ModelId { get; set; } = "meta-llama/llama-3.3-70b-instruct";
        public string? SiteName { get; set; }
        public string? SiteUrl { get; set; }
    }
}
```

### **Step 3: Update Program.cs**

```csharp
// Register IChatClient based on provider
builder.Services.AddScoped<IChatClient>(sp =>
{
    var provider = chatServiceOptions.Provider?.ToLowerInvariant() ?? "ollama";

    return provider switch
    {
        "openai" => GetChatClientOpenAI(
            chatServiceOptions.OpenAI.ApiKey, 
            chatServiceOptions.OpenAI.ModelId),
            
        "openrouter" => GetChatClientOpenRouter(  // ✨ NEW
            chatServiceOptions.OpenRouter.ApiKey,
            chatServiceOptions.OpenRouter.BaseUrl,
            chatServiceOptions.OpenRouter.ModelId,
            chatServiceOptions.OpenRouter.SiteName,
            chatServiceOptions.OpenRouter.SiteUrl),
            
        "ollama" => GetChatClientOllama(
            chatServiceOptions.Ollama.Endpoint, 
            chatServiceOptions.Ollama.ModelId),
            
        _ => throw new InvalidOperationException(
            $"Unknown AI provider: {provider}. " +
            $"Supported: 'openai', 'openrouter', 'ollama'")
    };
});

// ✨ NEW: OpenRouter client factory
static IChatClient GetChatClientOpenRouter(
    string apiKey, 
    string baseUrl, 
    string modelId,
    string? siteName,
    string? siteUrl)
{
    // OpenRouter is OpenAI-compatible, use OpenAI client with custom endpoint
    var options = new OpenAI.OpenAIClientOptions
    {
        Endpoint = new Uri(baseUrl)
    };
    
    var client = new OpenAIClient(apiKey, options);
    
    var chatClient = client
        .GetChatClient(modelId)
        .AsIChatClient()
        .AsBuilder()
        .UseOpenTelemetry(sourceName: "SivarChat", configure: c => c.EnableSensitiveData = true);
    
    // Add OpenRouter-specific headers if provided
    if (!string.IsNullOrEmpty(siteName) || !string.IsNullOrEmpty(siteUrl))
    {
        chatClient = chatClient.Use((messages, chatOptions, next, cancellationToken) =>
        {
            // Add HTTP-Referer and X-Title headers for OpenRouter
            // (OpenRouter uses these for analytics/tracking)
            if (!string.IsNullOrEmpty(siteUrl))
            {
                // Note: This requires custom header support in the client
                // May need to use HTTP client middleware
            }
            return next(messages, chatOptions, cancellationToken);
        });
    }
    
    return chatClient.Build();
}

// Existing OpenAI function (keep as-is)
static IChatClient GetChatClientOpenAI(string apiKey, string modelId)
{
    var client = new OpenAIClient(apiKey);
    return client
        .GetChatClient(modelId)
        .AsIChatClient()
        .AsBuilder()
        .UseOpenTelemetry(sourceName: "SivarChat", configure: c => c.EnableSensitiveData = true)
        .Build();
}

// Existing Ollama function (keep as-is)
static IChatClient GetChatClientOllama(string endpoint, string modelId)
{
    return new OllamaChatClient(endpoint, modelId: modelId)
        .AsBuilder()
        .UseOpenTelemetry(sourceName: "SivarChat", configure: c => c.EnableSensitiveData = true)
        .Build();
}
```

### **Step 4: Environment Variable Support**

**Update appsettings configuration in Program.cs:**

```csharp
// Override API keys from environment variables
var envOpenAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var envOpenRouterKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"); // ✨ NEW

if (!string.IsNullOrEmpty(envOpenAIKey))
{
    chatServiceOptions.OpenAI.ApiKey = envOpenAIKey;
}

if (!string.IsNullOrEmpty(envOpenRouterKey)) // ✨ NEW
{
    chatServiceOptions.OpenRouter.ApiKey = envOpenRouterKey;
}
```

---

## 🚀 **HOW TO SWITCH TO OPENROUTER**

### **Option 1: Environment Variables (Recommended)**

```bash
# On your server
export OPENROUTER_API_KEY="sk-or-v1-your-key-here"

# Update appsettings.json
{
  "ChatService": {
    "Provider": "openrouter"
  }
}

# Restart service
sudo systemctl restart sivaros
```

### **Option 2: Direct Configuration**

**Edit:** `/opt/sivaros/appsettings.json`

```json
{
  "ChatService": {
    "Provider": "openrouter",
    "OpenRouter": {
      "ApiKey": "sk-or-v1-your-key-here",
      "ModelId": "meta-llama/llama-3.3-70b-instruct",
      "SiteName": "Sivar.Os",
      "SiteUrl": "https://sivar.lat"
    }
  }
}
```

---

## 🎯 **MY RECOMMENDATION FOR SIVAR.OS**

### **Best Setup:**

```json
{
  "ChatService": {
    "Provider": "openrouter",
    "MaxTokens": 2000,
    "Temperature": 0.7,
    "OpenRouter": {
      "ModelId": "meta-llama/llama-3.3-70b-instruct",
      "ApiKey": "[from env var]"
    }
  }
}
```

**Why Llama 3.3 70B:**

✅ **Cost:** ~$8/month for 1000 chats/day (affordable!)  
✅ **Quality:** GPT-4 level, excellent for booking conversations  
✅ **Spanish:** Native multilingual support  
✅ **Tools:** Perfect function calling for booking system  
✅ **Personality:** Warm, helpful, natural conversational style  
✅ **Speed:** Fast enough for real-time chat  
✅ **Reliability:** Stable, proven model  

**Perfect for:**
- Natural booking conversations in Spanish
- Tool use (all 8 BookingFunctions)
- Handling edge cases gracefully
- Warm personality for customer service
- Complex queries (multi-step bookings)

---

## 💰 **COST PROJECTIONS**

### **Scenario: Photo Studio Launch**

**Assumptions:**
- 100 bookings/month (starting small)
- 3 messages per booking (avg)
- 500 tokens input + 200 tokens output per message

**Monthly Costs:**

```
GPT-4o-mini (current):
- 100 bookings × 3 messages × (500 × $0.15 + 200 × $0.60) / 1M
- = $0.585/month
- Acceptable! ✅

Llama 3.3 70B (recommended):
- 100 bookings × 3 messages × (500 × $0.35 + 200 × $0.40) / 1M
- = $0.765/month
- Still very cheap! ✅

GPT-4o (if you want premium):
- 100 bookings × 3 messages × (500 × $2.50 + 200 × $10.00) / 1M
- = $9.75/month
- Overkill for this stage ⚠️
```

**Scale Scenario: 1000 bookings/month**

```
GPT-4o-mini:     $5.85/month   💰
Llama 3.3 70B:   $7.65/month   💰
Llama 3.1 8B:    $1.26/month   💰💰💰
GPT-4o:          $97.50/month  💸
```

**Recommendation:** Start with Llama 3.3 70B or keep GPT-4o-mini. Both are cheap enough!

---

## 🎨 **PERSONALITY CUSTOMIZATION**

### **System Prompt for Sivar.Os Agent:**

```markdown
# Sivar AI Assistant - System Prompt

You are Sivar, the AI assistant for Sivar.Os, El Salvador's social network 
where people can discover AND take action.

## Your Personality:
- Warm, friendly, and helpful
- Salvadoran at heart (use local expressions naturally)
- Professional but never stiff
- Proactive - anticipate needs
- Patient with questions
- Celebratory when completing bookings

## Your Capabilities:
- Help users discover businesses and services
- Answer questions about profiles and posts
- Book appointments, order food, schedule services
- Provide information about businesses
- Check availability and pricing
- Manage existing bookings

## Language:
- Primary: Spanish (Salvadoran Spanish)
- Use "vos" form naturally when appropriate
- Mix in local expressions: "¡Qué chilero!", "Está chivo", etc.
- Can switch to English if user prefers

## Booking Flow:
When helping with bookings:
1. Understand what they need
2. Show relevant options with enthusiasm
3. Check availability proactively
4. Confirm details clearly
5. Celebrate completion: "¡Listo! 🎉"

## Examples:
User: "Necesito un fotógrafo para mi boda"
You: "¡Qué emoción! 💒 Te recomiendo Studio Fotográfico El Salvador. 
      Tienen un paquete especial de bodas por $800 (8 horas de cobertura, 
      álbum premium, entrega digital). ¿Cuándo es la boda?"

User: "15 de junio"
You: "Perfecto! Déjame verificar la disponibilidad para el 15 de junio... 
      ✅ ¡Tienen disponibilidad! ¿Qué horario preferís?"

Remember: You're not just a chatbot, you're a helpful friend who gets things done! 🚀
```

**This works great with:**
- Llama 3.3 70B (excellent at personality!)
- GPT-4o-mini (very good)
- Mistral Large (professional but can be warm)

**Struggles with:**
- Llama 3.1 8B (simpler personality)
- Very small models (<7B)

---

## 🧪 **TESTING DIFFERENT MODELS**

### **Easy A/B Testing:**

```bash
# Test Llama 3.3 70B
export OPENROUTER_API_KEY="your-key"
# In appsettings.json: "Provider": "openrouter", "ModelId": "meta-llama/llama-3.3-70b-instruct"
systemctl restart sivaros

# Test a few bookings, check quality

# Try Qwen 2.5 72B
# In appsettings.json: "ModelId": "qwen/qwen-2.5-72b-instruct"
systemctl restart sivaros

# Compare responses
```

**What to Test:**
1. Spanish fluency
2. Tool calling accuracy
3. Booking flow smoothness
4. Personality/warmth
5. Response time
6. Cost per interaction

---

## 📊 **MODEL COMPARISON TABLE**

| Model | Cost/1M | Quality | Spanish | Tools | Personality | Speed | Verdict |
|-------|---------|---------|---------|-------|-------------|-------|---------|
| **Llama 3.3 70B** | $0.35 in | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | Fast | **BEST** |
| GPT-4o-mini | $0.15 in | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | Fast | Great! |
| Qwen 2.5 72B | $0.35 in | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | Fast | Solid |
| Mistral Large | $2.00 in | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | Fast | Premium |
| Llama 3.1 8B | $0.06 in | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | Very fast | Budget |
| GPT-4o | $2.50 in | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | Medium | Overkill |

---

## ✅ **ACTION PLAN**

### **What I Recommend:**

**Option 1: Switch to OpenRouter + Llama 3.3 70B (Recommended)**
```
Time: 30 minutes
Cost: ~$8/month (1000 chats)
Benefits: Better quality than GPT-4o-mini, similar cost, more control
```

**Option 2: Stay with GPT-4o-mini**
```
Time: 0 (no change)
Cost: ~$6/month (1000 chats)
Benefits: Already working, reliable, proven
```

**Option 3: Try Both! (Best Approach)**
```
1. Keep GPT-4o-mini as fallback
2. Add OpenRouter as primary
3. Test Llama 3.3 70B for a week
4. Compare quality & cost
5. Choose winner
```

---

## 🛠️ **IMPLEMENTATION PLAN**

**Want me to:**

1. **Add OpenRouter support code** (30 min)
   - Update ChatServiceOptions
   - Add GetChatClientOpenRouter function
   - Update Program.cs registration
   - Test locally

2. **Configure for Llama 3.3 70B** (10 min)
   - Update appsettings.json
   - Add OPENROUTER_API_KEY env var
   - Deploy to server

3. **Test booking flow** (with API key)
   - Spanish conversations
   - Tool calling accuracy
   - Personality assessment
   - Cost tracking

4. **Compare models** (1-2 days)
   - A/B test different models
   - Measure quality & cost
   - Pick winner

---

**¿Qué opción preferís?** 

My vote: **Option 1** - Add OpenRouter + Llama 3.3 70B. You'll get GPT-4 level quality for the same cost as GPT-4o-mini! 🚀

Want me to implement the code changes now?
