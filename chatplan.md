# Sivar.Os AI Agent Implementation Plan

## Overview

Upgrade the existing chat system from using `IChatClient` directly to the **Microsoft Agent Framework** (`Microsoft.Agents.AI`), enabling more sophisticated AI agent capabilities including multi-step reasoning, better tool management, and potential multi-agent orchestration.

### ⚠️ Critical: Multi-User Platform Considerations

This is a **multi-user platform** where:
- Multiple users may use the AI agent simultaneously
- Each user has their own profile, permissions, and data access
- Function calls must be scoped to the authenticated user's context
- Agent state must NOT leak between users

**Key Design Decisions:**

1. **Scoped Services**: All function services must be `Scoped` (not `Singleton`) to ensure per-request isolation
2. **User Context Injection**: Each request must inject the authenticated user's `ProfileId` before agent execution
3. **No Shared State**: Agents and functions must not store user-specific state in static/shared fields
4. **Data Access Control**: All repository queries must filter by user permissions

## Current State Analysis

### What Already Exists ✅

1. **UI Components** (`Sivar.Os.Client/Components/AIChat/`):
   - `AIChatPanel.razor` - Main chat drawer panel
   - `ChatMessages.razor` - Message list display
   - `ChatInput.razor` - User input component
   - `ChatHistory.razor` - Conversation history sidebar
   - `AIFloatingButton.razor` - Floating chat toggle button
   - Full conversation management with history

2. **Backend Services** (`Sivar.Os/Services/`):
   - `ChatService.cs` - Uses `IChatClient` with function calling
   - `ChatFunctionService.cs` - AI-callable functions with `[Description]` attributes
   - `ChatServiceOptions.cs` - Configuration for provider, tokens, temperature

3. **Existing AI Functions** (in `ChatFunctionService.cs`):
   - `SearchProfiles()` - Search users by name/type/description
   - `SearchPosts()` - Search posts by content/author
   - `GetMyProfile()` - Get current user's profile
   - `GetPostDetails()` - Get specific post details
   - `FollowProfile()` - Follow a profile
   - `UnfollowProfile()` - Unfollow a profile

4. **AI Client Configuration**:
   - OpenAI helper: `GetChatClientOpenAiImp()` in `Program.cs`
   - Ollama helper: `GetChatClientOllamaImp()` in `Program.cs`
   - Currently hardcoded to Ollama in DI registration
   - `ChatServiceOptions` supports both providers in config

5. **Database Entities**:
   - `Conversation` - Stores conversation metadata
   - `ChatMessage` - Stores individual messages
   - Repositories for both entities

### What Needs to Change 🔄

| Component | Current | Target |
|-----------|---------|--------|
| AI Client | `IChatClient` | `AIAgent` from Microsoft.Agents.AI |
| Registration | Manual DI | `builder.AddAIAgent()` keyed services |
| Functions | Inline in ChatService | Dedicated service classes |
| Streaming | Not implemented | Use `agent.RunStreamingAsync()` |
| Configuration | Hardcoded | appsettings.json with provider switch |
| Telemetry | Basic logging | OpenTelemetry via Agent middleware |

---

## Implementation Plan

### Phase 1: NuGet Packages & Configuration (Day 1)

#### 1.1 Add Microsoft Agent Framework Packages

Add to `Sivar.Os.csproj`:

```xml
<!-- Microsoft Agent Framework packages -->
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-preview.*" />
<PackageReference Include="Microsoft.Agents.AI.Abstractions" Version="1.0.0-preview.*" />
<PackageReference Include="Microsoft.Agents.AI.Hosting" Version="1.0.0-preview.*" />
<PackageReference Include="Microsoft.Agents.AI.Hosting.OpenAI" Version="1.0.0-alpha.*" />
<PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.0.0-preview.*" />
```

#### 1.2 Update Configuration Schema

Update `appsettings.json`:

```json
{
  "ChatService": {
    "Provider": "openai",
    "MaxMessagesPerConversation": 1000,
    "MaxTokens": 2000,
    "Temperature": 0.7,
    "RateLimitPerMinute": 20,
    "Ollama": {
      "Endpoint": "http://127.0.0.1:11434",
      "ModelId": "llama3.2:latest"
    },
    "OpenAI": {
      "ApiKey": "YOUR_API_KEY_HERE",
      "ModelId": "gpt-4o",
      "OrganizationId": ""
    }
  }
}
```

Use User Secrets for API key:
```bash
dotnet user-secrets set "ChatService:OpenAI:ApiKey" "sk-..."
```

---

### Phase 2: Refactor Function Services (Day 1-2)

#### 2.1 Multi-User Security Architecture

**Critical**: Every function service must be request-scoped and receive user context:

```csharp
/// <summary>
/// Base class for all agent function services.
/// Provides secure user context for multi-tenant operations.
/// </summary>
public abstract class BaseAgentFunctions
{
    protected Guid CurrentProfileId { get; private set; }
    protected bool IsContextSet { get; private set; }

    /// <summary>
    /// Set the authenticated user's profile for this request.
    /// Must be called before any agent function execution.
    /// </summary>
    public void SetUserContext(Guid profileId)
    {
        if (profileId == Guid.Empty)
            throw new ArgumentException("Profile ID cannot be empty", nameof(profileId));
        
        CurrentProfileId = profileId;
        IsContextSet = true;
    }

    /// <summary>
    /// Validate that user context has been set before executing functions.
    /// </summary>
    protected void EnsureUserContext()
    {
        if (!IsContextSet || CurrentProfileId == Guid.Empty)
            throw new InvalidOperationException("User context not set. Call SetUserContext() before executing agent functions.");
    }
}
```

**Why Scoped, Not Singleton:**
- `Singleton`: One instance shared by ALL users - **DANGEROUS** for user-specific state
- `Scoped`: One instance per HTTP request - **SAFE** for user context

#### 2.2 Create Dedicated Function Services

Split `ChatFunctionService.cs` into focused services:

```
Sivar.Os/Services/AgentFunctions/
├── ProfileFunctions.cs      # SearchProfiles, GetMyProfile, FollowProfile, UnfollowProfile
├── PostFunctions.cs         # SearchPosts, GetPostDetails, CreatePost
├── ActivityFunctions.cs     # GetRecentActivities, GetNotifications
└── SocialFunctions.cs       # GetFollowers, GetFollowing, GetRecommendations
```

Each function class:
- Single responsibility
- Injected via DI
- Uses `[Description]` attributes for AI understanding
- Returns structured JSON for rich UI rendering

#### 2.3 Example: Secure ProfileFunctions

```csharp
public class ProfileFunctions : BaseAgentFunctions
{
    private readonly IProfileRepository _profileRepository;
    private readonly IProfileFollowerRepository _followerRepository;
    private readonly ILogger<ProfileFunctions> _logger;

    public ProfileFunctions(
        IProfileRepository profileRepository,
        IProfileFollowerRepository followerRepository,
        ILogger<ProfileFunctions> logger)
    {
        _profileRepository = profileRepository;
        _followerRepository = followerRepository;
        _logger = logger;
    }

    [Description("Search for user profiles on the social network")]
    public async Task<string> SearchProfiles(
        [Description("The search query - name, type, or description")] string query,
        [Description("Maximum results (default 5, max 10)")] int maxResults = 5)
    {
        // Validate user context is set
        EnsureUserContext();
        
        _logger.LogInformation(
            "[ProfileFunctions.SearchProfiles] ProfileId={ProfileId}, Query={Query}", 
            CurrentProfileId, query);

        // Query is now scoped to authenticated user's view
        var profiles = await _profileRepository.SearchAsync(query, maxResults);
        
        // Include relationship context (are they following this user?)
        var results = profiles.Select(p => new
        {
            id = p.Id,
            displayName = p.DisplayName,
            profileType = p.ProfileType?.Name,
            isFollowing = _followerRepository.IsFollowingAsync(CurrentProfileId, p.Id).Result,
            isMe = p.Id == CurrentProfileId
        });

        return JsonSerializer.Serialize(results);
    }

    [Description("Follow a profile")]
    public async Task<string> FollowProfile(
        [Description("The profile ID to follow")] Guid targetProfileId)
    {
        EnsureUserContext();
        
        // Prevent following yourself
        if (targetProfileId == CurrentProfileId)
            return "You cannot follow yourself";
        
        // Action is tied to authenticated user
        var result = await _followerRepository.FollowAsync(CurrentProfileId, targetProfileId);
        return result ? "Successfully followed" : "Already following";
    }
}
```

#### 2.4 New Functions to Add

| Function | Description | Returns |
|----------|-------------|---------|
| `CreatePost` | Create a new post | Post ID, URL |
| `LikePost` | React to a post | Success/failure |
| `CommentOnPost` | Add a comment | Comment ID |
| `GetRecentActivities` | Get activity stream | List of activities |
| `GetNotifications` | Get user notifications | Notification list |
| `GetFollowers` | List followers | Profile list |
| `GetFollowing` | List following | Profile list |
| `GetTrendingPosts` | Get trending content | Post list |

---

### Phase 3: Implement AIAgent Registration (Day 2)

#### 3.1 Create Agent Factory

Create `Sivar.Os/Services/Agents/SivarAgentFactory.cs`:

```csharp
public static class SivarAgentFactory
{
    public static AIAgent CreateSivarAgent(
        IServiceProvider sp, 
        string key,
        IChatClient chatClient,
        ChatServiceOptions options)
    {
        var profileFunctions = sp.GetRequiredService<ProfileFunctions>();
        var postFunctions = sp.GetRequiredService<PostFunctions>();
        var activityFunctions = sp.GetRequiredService<ActivityFunctions>();
        var logger = sp.GetRequiredService<ILogger<Program>>();

        var systemPrompt = """
            You are Sivar AI, a helpful assistant for the Sivar.Os social network.
            
            You can help users:
            - Find and connect with other users (search profiles, follow/unfollow)
            - Discover and interact with posts (search, view, like, comment)
            - Create new posts and share content
            - View their activity feed and notifications
            - Get recommendations for people to follow
            
            When showing results, format them nicely and offer follow-up actions.
            Be friendly, concise, and helpful. Use emojis sparingly for personality.
            
            If a user asks about something outside the platform, politely redirect them
            to the social network features you can help with.
            """;

        return chatClient.CreateAIAgent(
            name: key,
            instructions: systemPrompt,
            description: "Sivar.Os social network assistant",
            tools: [
                // Profile functions
                AIFunctionFactory.Create(profileFunctions.SearchProfiles),
                AIFunctionFactory.Create(profileFunctions.GetMyProfile),
                AIFunctionFactory.Create(profileFunctions.FollowProfile),
                AIFunctionFactory.Create(profileFunctions.UnfollowProfile),
                
                // Post functions
                AIFunctionFactory.Create(postFunctions.SearchPosts),
                AIFunctionFactory.Create(postFunctions.GetPostDetails),
                AIFunctionFactory.Create(postFunctions.CreatePost),
                AIFunctionFactory.Create(postFunctions.LikePost),
                
                // Activity functions
                AIFunctionFactory.Create(activityFunctions.GetRecentActivities),
                AIFunctionFactory.Create(activityFunctions.GetNotifications)
            ]
        )
        .AsBuilder()
        .UseOpenTelemetry(c => c.EnableSensitiveData = true)
        .Build();
    }
}
```

#### 3.2 Update Program.cs Registration

```csharp
// Configure chat options from appsettings
builder.Services.Configure<ChatServiceOptions>(
    builder.Configuration.GetSection(ChatServiceOptions.SectionName));

// Register function services as SCOPED (one instance per request, per user)
builder.Services.AddScoped<ProfileFunctions>();
builder.Services.AddScoped<PostFunctions>();
builder.Services.AddScoped<ActivityFunctions>();

// Register IChatClient based on provider (can be Singleton - stateless)
builder.Services.AddScoped<IChatClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<ChatServiceOptions>>().Value;
    
    return options.Provider.ToLowerInvariant() switch
    {
        "openai" => GetChatClientOpenAiImp(
            options.OpenAI.ApiKey, 
            options.OpenAI.ModelId),
        "ollama" => GetChatClientOllamaImp(
            options.Ollama.Endpoint, 
            options.Ollama.ModelId),
        _ => throw new InvalidOperationException($"Unknown AI provider: {options.Provider}")
    };
});

// Register AI Agent using Microsoft Agent Framework
builder.AddAIAgent("SivarAgent", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var options = sp.GetRequiredService<IOptions<ChatServiceOptions>>().Value;
    
    return SivarAgentFactory.CreateSivarAgent(sp, key, chatClient, options);
});
```

---

### Phase 4: Update ChatService to Use AIAgent (Day 2-3)

#### 4.1 Modify ChatService (Multi-User Safe)

The agent itself is stateless, but function services need user context per request:

```csharp
public class ChatService : IChatService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly ILogger<ChatService> _logger;
    
    public ChatService(
        IServiceProvider serviceProvider,
        IConversationRepository conversationRepository,
        IChatMessageRepository messageRepository,
        ILogger<ChatService> logger)
    {
        _serviceProvider = serviceProvider;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _logger = logger;
    }

    public async Task<ChatResponseDto> SendMessageAsync(SendMessageDto dto)
    {
        // ⚠️ CRITICAL: Set user context on ALL function services before agent execution
        SetUserContextOnFunctions(dto.ProfileId);
        
        // Get the agent (keyed service)
        var agent = _serviceProvider.GetRequiredKeyedService<AIAgent>("SivarAgent");
        
        // Build message history for THIS user's conversation
        var messages = await BuildChatHistoryAsync(dto.ConversationId, dto.ProfileId);
        messages.Add(new ChatMessage(ChatRole.User, dto.Content));
        
        // Execute agent - functions will use the authenticated user's context
        var response = await agent.RunAsync(messages);
        
        // Save messages...
        return new ChatResponseDto { /* ... */ };
    }

    /// <summary>
    /// Set user context on all function services before agent execution.
    /// This ensures all tool calls are scoped to the authenticated user.
    /// </summary>
    private void SetUserContextOnFunctions(Guid profileId)
    {
        // Get all function services and set context
        var profileFunctions = _serviceProvider.GetRequiredService<ProfileFunctions>();
        var postFunctions = _serviceProvider.GetRequiredService<PostFunctions>();
        var activityFunctions = _serviceProvider.GetRequiredService<ActivityFunctions>();
        
        profileFunctions.SetUserContext(profileId);
        postFunctions.SetUserContext(profileId);
        activityFunctions.SetUserContext(profileId);
        
        _logger.LogInformation(
            "[ChatService] User context set for agent execution - ProfileId={ProfileId}",
            profileId);
    }

    private async Task<List<ChatMessage>> BuildChatHistoryAsync(Guid conversationId, Guid profileId)
    {
        // Verify conversation belongs to this user (security check)
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation?.ProfileId != profileId)
            throw new UnauthorizedAccessException("Conversation does not belong to this user");
        
        // Load messages for this conversation only
        var messages = await _messageRepository.GetConversationMessagesAsync(conversationId);
        
        return messages.Select(m => new ChatMessage(
            m.Role == "user" ? ChatRole.User : ChatRole.Assistant,
            m.Content
        )).ToList();
    }
}
```

**Key Multi-User Security Points:**

1. **User Context Injection**: `SetUserContextOnFunctions()` is called BEFORE every agent execution
2. **Conversation Ownership**: Verify conversation belongs to requesting user
3. **Scoped Services**: Function services are `Scoped`, so each request gets fresh instances
4. **No Shared State**: Agent doesn't store user state - it's passed via function context
```

---

### Phase 5: Add Streaming Support to UI (Day 3)

#### 5.1 Update API Controller

Add streaming endpoint to `ChatMessagesController.cs`:

```csharp
[HttpPost("stream")]
public async IAsyncEnumerable<string> SendMessageStreaming(
    [FromBody] SendMessageDto dto,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    await foreach (var chunk in _chatService.SendMessageStreamingAsync(dto, cancellationToken))
    {
        yield return chunk;
    }
}
```

#### 5.2 Update Client to Use SSE

Use Server-Sent Events or chunked transfer for streaming responses in the UI.

---

### Phase 6: Advanced Features (Day 4+)

#### 6.1 Multi-Agent Support (Optional)

Create specialized agents:

```csharp
// Content curator agent
builder.AddAIAgent("ContentAgent", ...);

// Social connections agent  
builder.AddAIAgent("SocialAgent", ...);

// Main coordinator
builder.AddAIAgent("SivarAgent", (sp, key) =>
{
    var contentAgent = sp.GetRequiredKeyedService<AIAgent>("ContentAgent");
    var socialAgent = sp.GetRequiredKeyedService<AIAgent>("SocialAgent");
    
    // Coordinator can delegate to specialized agents
    ...
});
```

#### 6.2 Custom Middleware

Add rate limiting, logging, or caching middleware:

```csharp
.AsBuilder()
.Use(async (messages, options, next, ct) =>
{
    // Pre-processing: rate limiting, input validation
    logger.LogInformation("Agent processing request...");
    
    var result = await next(messages, options, ct);
    
    // Post-processing: response filtering, analytics
    return result;
})
.Build();
```

#### 6.3 Structured Responses

Return rich, actionable responses:

```csharp
public class AgentResponseDto
{
    public string Text { get; set; }
    public string? ResponseType { get; set; } // "profiles", "posts", "actions"
    public List<ProfileResult>? Profiles { get; set; }
    public List<PostResult>? Posts { get; set; }
    public List<QuickAction>? QuickActions { get; set; }
}
```

---

## Multi-User Architecture Summary

### Service Lifetime Strategy

| Service | Lifetime | Reason |
|---------|----------|--------|
| `IChatClient` | Scoped | Stateless, thread-safe, can share |
| `AIAgent` | Scoped | Created per-request with current function instances |
| `ProfileFunctions` | **Scoped** | Holds `CurrentProfileId` per request |
| `PostFunctions` | **Scoped** | Holds `CurrentProfileId` per request |
| `ActivityFunctions` | **Scoped** | Holds `CurrentProfileId` per request |
| `ChatService` | Scoped | Orchestrates per-request logic |
| Repositories | Scoped | Per-request DB context |

### Request Flow (Multi-User Safe)

```
User A sends message         User B sends message
        │                            │
        ▼                            ▼
┌─────────────────┐          ┌─────────────────┐
│ HTTP Request A  │          │ HTTP Request B  │
│ ProfileId: AAA  │          │ ProfileId: BBB  │
└────────┬────────┘          └────────┬────────┘
         │                            │
         ▼                            ▼
┌─────────────────┐          ┌─────────────────┐
│ Scoped Services │          │ Scoped Services │
│ (Instance A)    │          │ (Instance B)    │
│                 │          │                 │
│ ProfileFunctions│          │ ProfileFunctions│
│ .ProfileId=AAA  │          │ .ProfileId=BBB  │
└────────┬────────┘          └────────┬────────┘
         │                            │
         ▼                            ▼
┌─────────────────┐          ┌─────────────────┐
│ Agent executes  │          │ Agent executes  │
│ SearchProfiles()│          │ FollowProfile() │
│ for User A      │          │ for User B      │
└─────────────────┘          └─────────────────┘
```

### Security Checklist

- [ ] All function services inherit from `BaseAgentFunctions`
- [ ] `EnsureUserContext()` called at start of every function
- [ ] `SetUserContext()` called before agent execution in ChatService
- [ ] Conversation ownership verified before loading messages
- [ ] No static fields storing user data
- [ ] All services registered as Scoped (not Singleton)

---

## Configuration Summary

### appsettings.json

```json
{
  "ChatService": {
    "Provider": "openai",
    "MaxMessagesPerConversation": 1000,
    "MaxTokens": 2000,
    "Temperature": 0.7,
    "RateLimitPerMinute": 20,
    "Ollama": {
      "Endpoint": "http://127.0.0.1:11434",
      "ModelId": "llama3.2:latest"
    },
    "OpenAI": {
      "ApiKey": "",
      "ModelId": "gpt-4o",
      "OrganizationId": ""
    }
  }
}
```

### User Secrets (Development)

```bash
# Set OpenAI API key securely
dotnet user-secrets set "ChatService:OpenAI:ApiKey" "sk-your-key-here"
```

### Switching Providers

Just change `"Provider"`:

| Provider | Value | Requirements |
|----------|-------|--------------|
| OpenAI | `"openai"` | Valid API key in secrets |
| Ollama | `"ollama"` | Ollama running locally with model |

---

## File Changes Summary

### New Files

```
Sivar.Os/Services/AgentFunctions/
├── ProfileFunctions.cs
├── PostFunctions.cs
├── ActivityFunctions.cs
└── SocialFunctions.cs

Sivar.Os/Services/Agents/
└── SivarAgentFactory.cs
```

### Modified Files

| File | Changes |
|------|---------|
| `Sivar.Os.csproj` | Add Agent Framework NuGet packages |
| `appsettings.json` | Add ChatService configuration section |
| `Program.cs` | Register AIAgent, update IChatClient to use config |
| `ChatService.cs` | Use AIAgent instead of IChatClient |
| `ChatMessagesController.cs` | Add streaming endpoint |
| `ChatFunctionService.cs` | Refactor into separate function classes |

---

## Testing Plan

1. **Unit Tests**
   - Test each function service in isolation
   - Mock repositories
   
2. **Integration Tests**
   - Test agent with mock AI responses
   - Verify function calling works
   
3. **Manual Testing**
   - Test with Ollama locally
   - Test with OpenAI API
   - Verify streaming works
   - Test all tool functions

---

## Timeline Estimate

| Phase | Duration | Description |
|-------|----------|-------------|
| Phase 1 | 2-3 hours | NuGet packages, configuration |
| Phase 2 | 3-4 hours | Refactor function services |
| Phase 3 | 2-3 hours | AIAgent registration |
| Phase 4 | 3-4 hours | Update ChatService |
| Phase 5 | 2-3 hours | Streaming support |
| Phase 6 | Optional | Advanced features |

**Total: ~1.5-2 days for core implementation**

---

## Questions Before Starting

1. ✅ **OpenAI API Key**: You have one - will use User Secrets
2. ✅ **Ollama**: Will add as fallback configuration
3. **Priority**: Should we implement all new functions (CreatePost, LikePost, etc.) or start with existing ones only?
4. **Streaming**: Is streaming UI support a priority, or can we start with non-streaming?

---

## Ready to Start?

When you're ready, we can begin with **Phase 1** - adding the NuGet packages and updating configuration.
