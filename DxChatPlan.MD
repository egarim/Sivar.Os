# DevExpress DxAIChat Migration Plan

## Overview

This document outlines the plan to migrate the current custom chat component in Sivar.Os to the DevExpress `DxAIChat` component. The goal is to leverage DevExpress's robust AI chat functionality while maintaining the existing features and MudBlazor theming consistency.

**Reference Documentation:**
- [DxAIChat Class](https://docs.devexpress.com/Blazor/DevExpress.AIIntegration.Blazor.Chat.DxAIChat)
- [Manual Message Processing](https://docs.devexpress.com/Blazor/DevExpress.AIIntegration.Blazor.Chat.DxAIChat#manual-message-processing)
- [Prompt Suggestions](https://docs.devexpress.com/Blazor/DevExpress.AIIntegration.Blazor.Chat.DxAIChat#prompt-suggestions)

---

## Table of Contents

1. [Current Architecture Analysis](#1-current-architecture-analysis)
2. [DevExpress DxAIChat Features Mapping](#2-devexpress-dxaichat-features-mapping)
3. [NuGet Packages Required](#3-nuget-packages-required)
4. [Theme Integration Strategy](#4-theme-integration-strategy)
5. [Component Migration Tasks](#5-component-migration-tasks)
6. [Manual Message Processing Implementation](#6-manual-message-processing-implementation)
7. [Prompt Suggestions Implementation](#7-prompt-suggestions-implementation)
8. [Structured Results Integration](#8-structured-results-integration)
9. [Migration Phases](#9-migration-phases)
10. [Testing Strategy](#10-testing-strategy)
11. [Rollback Plan](#11-rollback-plan)

---

## 1. Current Architecture Analysis

### Current Custom Chat Components

Located in `Sivar.Os.Client/Components/AIChat/`:

| Component | Purpose | DxAIChat Equivalent |
|-----------|---------|---------------------|
| `AIChatPanel.razor` | Main chat panel container | `DxAIChat` component |
| `ChatMessages.razor` | Message list rendering | `MessageTemplate` / `MessageContentTemplate` |
| `ChatMessage.razor` | Individual message display | `MessageTemplate` / `MessageContentTemplate` |
| `ChatInput.razor` | Text input field | Built-in input (native to DxAIChat) |
| `ChatHistory.razor` | Conversation history sidebar | Custom implementation (keep) |
| `ConversationItem.razor` | Conversation list item | Custom implementation (keep) |
| `ChatResultCard.razor` | Business result cards | Custom `MessageContentTemplate` |
| `LocationPrompt.razor` | Location selection dialog | Keep as external component |
| `SavedResultsPanel.razor` | Saved/bookmarked results | Keep as external component |
| `SearchResultsMap.razor` | Map with search results | Custom template integration |
| `RankingExplanation.razor` | Ranking info tooltip | Custom template integration |
| `AIFloatingButton.razor` | Floating chat toggle button | Keep as external component |

### Current Backend Service

- **`ChatService.cs`** in `Sivar.Os/Services/`
  - Uses Microsoft Agent Framework
  - Intent classification for routing
  - AgentFactory for dynamic agent loading
  - Structured search results via `ChatFunctionService`
  - Location-aware queries

### Current Theme Configuration (MudBlazor)

Located in `MainLayout.razor`:

```csharp
// Light Palette
private readonly PaletteLight _lightPalette = new()
{
    Black = "#110e2d",
    AppbarText = "#424242",
    AppbarBackground = "rgba(255,255,255,0.8)",
    DrawerBackground = "#ffffff",
    GrayLight = "#e8e8e8",
    GrayLighter = "#f9f9f9",
};

// Dark Palette
private readonly PaletteDark _darkPalette = new()
{
    Primary = "#7e6fff",
    Surface = "#1e1e2d",
    Background = "#1a1a27",
    BackgroundGray = "#151521",
    AppbarText = "#92929f",
    AppbarBackground = "rgba(26,26,39,0.8)",
    DrawerBackground = "#1a1a27",
    ActionDefault = "#74718e",
    TextPrimary = "#b2b0bf",
    TextSecondary = "#92929f",
    Info = "#4a86ff",
    Success = "#3dcb6c",
    Warning = "#ffb545",
};
```

---

## 2. DevExpress DxAIChat Features Mapping

### Key Features to Utilize

| DxAIChat Feature | Current Feature | Implementation Strategy |
|------------------|-----------------|------------------------|
| `MessageSent` event | `OnSendMessage` callback | Manual message processing |
| `PromptSuggestions` | `QuickActionItems` | Map to `DxAIChatPromptSuggestion` |
| `MessageContentTemplate` | Custom `ChatMessage.razor` | Render structured results |
| `MessageTemplate` | Full message customization | Custom avatars, styling |
| `EmptyMessageAreaTemplate` | Welcome message | Show suggestions when empty |
| `UseStreaming` | Not implemented | Enable for better UX |
| `Initialized` event | `OnInitialized` | Load system prompt, history |
| `LoadMessages` / `SaveMessages` | Conversation persistence | Integrate with ChatService |
| `ResponseContentFormat.Markdown` | Markdown rendering | Enable markdown support |
| `CssClass` | Custom styling | Apply MudBlazor-compatible CSS |
| `SizeMode` | Responsive sizing | Match MudBlazor sizing |

### Features NOT Available in DxAIChat (Keep Custom)

1. **Conversation History Sidebar** - Keep `ChatHistory.razor`
2. **Location Awareness** - Keep `LocationPrompt.razor` + integration
3. **Saved Results Panel** - Keep `SavedResultsPanel.razor`
4. **Map Integration** - Keep `SearchResultsMap.razor`
5. **Profile Bookmarks** - Custom business logic

---

## 3. NuGet Packages Required

Add to `Sivar.Os.Client.csproj`:

```xml
<PackageReference Include="DevExpress.AIIntegration.Blazor.Chat" Version="25.1.*" />
<PackageReference Include="DevExpress.Blazor" Version="25.1.*" />
```

Add to `Sivar.Os.csproj` (Server):

```xml
<PackageReference Include="DevExpress.AIIntegration.Blazor.Chat" Version="25.1.*" />
```

**Note:** The existing project may already have DevExpress packages - verify version compatibility.

---

## 4. Theme Integration Strategy

### Approach: CSS Variables Bridge

Create a CSS file that maps MudBlazor CSS variables to DevExpress DxAIChat styling:

**File:** `Sivar.Os.Client/wwwroot/css/dx-mudblazor-theme.css`

```css
/* DxAIChat MudBlazor Theme Bridge */
.sivar-ai-chat {
    /* Match MudBlazor surface colors */
    --dx-color-background: var(--mud-palette-surface);
    --dx-color-text: var(--mud-palette-text-primary);
    --dx-color-text-secondary: var(--mud-palette-text-secondary);
    
    /* Light Mode */
    background-color: var(--mud-palette-surface, #ffffff);
    color: var(--mud-palette-text-primary, #424242);
}

/* Dark Mode Support */
.mud-theme-dark .sivar-ai-chat {
    background-color: var(--mud-palette-surface, #1e1e2d);
    color: var(--mud-palette-text-primary, #b2b0bf);
}

/* Message Bubbles - User */
.sivar-ai-chat .demo-chat-message.demo-user-message {
    background: linear-gradient(135deg, var(--mud-palette-primary) 0%, var(--mud-palette-primary-darken) 100%);
    color: white;
    border-radius: 18px 18px 4px 18px;
}

/* Message Bubbles - Assistant */
.sivar-ai-chat .demo-chat-message.demo-assistant-message {
    background-color: var(--mud-palette-surface);
    border: 1px solid var(--mud-palette-lines-default);
    border-radius: 18px 18px 18px 4px;
}

.mud-theme-dark .sivar-ai-chat .demo-chat-message.demo-assistant-message {
    background-color: var(--mud-palette-background-gray, #151521);
    border-color: var(--mud-palette-lines-default, #2a2833);
}

/* Prompt Suggestions - Match MudBlazor chips */
.sivar-ai-chat .dx-aichat-prompt-suggestion {
    background: rgba(var(--mud-palette-primary-rgb), 0.1);
    border: 1px solid var(--mud-palette-primary);
    border-radius: 20px;
    color: var(--mud-palette-primary);
    transition: all 0.2s ease;
}

.sivar-ai-chat .dx-aichat-prompt-suggestion:hover {
    background: var(--mud-palette-primary);
    color: white;
}

/* Input Area */
.sivar-ai-chat .dx-aichat-input-area {
    background-color: var(--mud-palette-background);
    border-top: 1px solid var(--mud-palette-lines-default);
}

/* Scrollbar styling to match MudBlazor */
.sivar-ai-chat::-webkit-scrollbar {
    width: 6px;
}

.sivar-ai-chat::-webkit-scrollbar-thumb {
    background-color: var(--mud-palette-action-default);
    border-radius: 3px;
}
```

### DevExpress Theme Configuration

In `Program.cs` or `_Host.cshtml`, configure DevExpress to use a neutral/compatible theme:

```csharp
// Use bootstrap-external theme to allow CSS customization
builder.Services.AddDevExpressBlazor(configure => configure.BootstrapVersion = BootstrapVersion.v5);
```

---

## 5. Component Migration Tasks

### Phase 1: Create New DxAIChat Wrapper Component

**New File:** `Sivar.Os.Client/Components/AIChat/SivarAIChat.razor`

```razor
@using DevExpress.AIIntegration.Blazor.Chat
@using Sivar.Os.Client.Pages
@using Sivar.Os.Shared.DTOs
@inject IJSRuntime JS
@inject NavigationManager NavigationManager

<DxAIChat @ref="_chat"
          CssClass="sivar-ai-chat"
          Initialized="OnChatInitialized"
          MessageSent="OnMessageSent"
          UseStreaming="false"
          ResponseContentFormat="ResponseContentFormat.Markdown">
    
    <PromptSuggestions>
        @foreach (var suggestion in PromptSuggestions)
        {
            <DxAIChatPromptSuggestion 
                Title="@suggestion.Label"
                Text="@suggestion.Description"
                PromptMessage="@suggestion.DefaultQuery" />
        }
    </PromptSuggestions>
    
    <MessageContentTemplate>
        @if (context.Role == ChatMessageRole.Assistant && TryGetStructuredResults(context, out var results))
        {
            @* Render structured business results *@
            <div class="structured-results-container">
                @((MarkupString)RenderMarkdown(context.Content))
                <div class="results-carousel">
                    @foreach (var business in results.Businesses)
                    {
                        <SivarBusinessCard Business="@business" OnNavigate="NavigateToProfile" />
                    }
                </div>
            </div>
        }
        else
        {
            @* Standard message rendering *@
            <div class="message-content">
                @((MarkupString)RenderMarkdown(context.Content))
            </div>
        }
    </MessageContentTemplate>
    
    <EmptyMessageAreaTemplate>
        <div class="empty-chat-welcome">
            <div class="welcome-icon">🤖</div>
            <h3>Sivar AI Assistant</h3>
            <p>@WelcomeMessage</p>
        </div>
    </EmptyMessageAreaTemplate>
    
</DxAIChat>

@code {
    private DxAIChat? _chat;
    
    [Parameter] public List<QuickActionDto> PromptSuggestions { get; set; } = new();
    [Parameter] public string WelcomeMessage { get; set; } = "¿En qué puedo ayudarte hoy?";
    [Parameter] public EventCallback<string> OnUserMessage { get; set; }
    [Parameter] public ChatLocationContext? CurrentLocation { get; set; }
    
    // Storage for structured results associated with messages
    private Dictionary<string, SearchResultsCollectionDto> _messageResults = new();
    
    private async Task OnChatInitialized(IAIChat chat)
    {
        // Load system prompt
        var systemPrompt = GetSystemPrompt();
        chat.LoadMessages(new[] {
            new BlazorChatMessage(ChatRole.System, systemPrompt)
        });
    }
    
    private async Task OnMessageSent(MessageSentEventArgs args)
    {
        // Manual message processing - delegate to ChatService
        await OnUserMessage.InvokeAsync(args.Content);
    }
    
    public async Task SendAssistantMessage(string content, SearchResultsCollectionDto? results = null)
    {
        if (_chat != null)
        {
            var messageId = Guid.NewGuid().ToString();
            if (results != null)
            {
                _messageResults[messageId] = results;
            }
            await _chat.SendMessage(content, ChatRole.Assistant);
        }
    }
    
    // Additional helper methods...
}
```

---

## 6. Manual Message Processing Implementation

### Key Concept

Instead of letting DxAIChat call AI directly, we intercept the `MessageSent` event and route through our existing `ChatService`.

### Implementation Flow

```
User sends message in DxAIChat
       │
       ▼
MessageSent event fires
       │
       ▼
SivarAIChat.OnMessageSent()
       │
       ▼
Call parent component (MainLayout)
       │
       ▼
MainLayout.SendChat() (existing logic)
       │
       ▼
ChatService.SendMessageAsync()
       │
       ▼
AI Agent processes (AgentFactory)
       │
       ▼
Return response + structured results
       │
       ▼
SivarAIChat.SendAssistantMessage()
       │
       ▼
DxAIChat displays response
```

### Backend Integration

```csharp
// In MainLayout.razor or parent component

private async Task HandleUserMessage(string content)
{
    _isTyping = true;
    StateHasChanged();
    
    try
    {
        var dto = new SendMessageDto
        {
            ConversationId = _currentConversationGuid,
            Content = content,
            Location = _chatLocation
        };
        
        var response = await SivarClient.Chat.SendMessageAsync(dto, _currentProfileId);
        
        // Send response to DxAIChat
        await _sivarChat.SendAssistantMessage(
            response.AssistantMessage.Content,
            response.SearchResults
        );
    }
    finally
    {
        _isTyping = false;
        StateHasChanged();
    }
}
```

---

## 7. Prompt Suggestions Implementation

### Mapping QuickActionDto to DxAIChatPromptSuggestion

**Current QuickActionDto structure:**
```csharp
public record QuickActionDto
{
    public Guid Id { get; init; }
    public string Label { get; init; }  // e.g., "Restaurantes"
    public string Icon { get; init; }   // e.g., "🍽️"
    public string DefaultQuery { get; init; }  // e.g., "Buscar restaurantes cerca"
    public string? MudBlazorIcon { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}
```

**DxAIChatPromptSuggestion mapping:**
```razor
<PromptSuggestions>
    @foreach (var action in QuickActions.Where(a => a.IsActive).OrderBy(a => a.SortOrder))
    {
        <DxAIChatPromptSuggestion 
            Title="@($"{action.Icon} {action.Label}")"
            Text="@action.Label"
            PromptMessage="@action.DefaultQuery" />
    }
</PromptSuggestions>
```

### Custom Prompt Suggestion Template (Optional)

```razor
<PromptSuggestionContentTemplate>
    <div class="sivar-suggestion">
        <span class="suggestion-icon">@GetIcon(context)</span>
        <span class="suggestion-title">@context.Title</span>
    </div>
</PromptSuggestionContentTemplate>
```

---

## 8. Structured Results Integration

### Challenge

DxAIChat doesn't natively support "attached data" per message. We need to:
1. Track structured results separately
2. Associate them with specific messages
3. Render them in `MessageContentTemplate`

### Solution: Message Metadata Pattern

```csharp
// In SivarAIChat.razor.cs

private class MessageMetadata
{
    public string MessageId { get; set; }
    public SearchResultsCollectionDto? StructuredResults { get; set; }
    public DateTime Timestamp { get; set; }
}

private List<MessageMetadata> _messageMetadata = new();

public async Task SendAssistantMessageWithResults(
    string content, 
    SearchResultsCollectionDto? results)
{
    var metadata = new MessageMetadata
    {
        MessageId = Guid.NewGuid().ToString(),
        StructuredResults = results,
        Timestamp = DateTime.UtcNow
    };
    _messageMetadata.Add(metadata);
    
    // Encode metadata reference in message (hidden)
    var enhancedContent = $"<!--MSG:{metadata.MessageId}-->{content}";
    
    await _chat.SendMessage(enhancedContent, ChatRole.Assistant);
}

private bool TryGetStructuredResults(
    BlazorChatMessage message, 
    out SearchResultsCollectionDto? results)
{
    results = null;
    
    // Extract message ID from content
    var match = Regex.Match(message.Content, @"<!--MSG:([^>]+)-->");
    if (match.Success)
    {
        var msgId = match.Groups[1].Value;
        results = _messageMetadata
            .FirstOrDefault(m => m.MessageId == msgId)?
            .StructuredResults;
        return results?.HasResults == true;
    }
    return false;
}
```

### Business Card Sub-Component

**File:** `Sivar.Os.Client/Components/AIChat/SivarBusinessCard.razor`

Reuse existing card styling from `ChatMessage.razor` lines 35-100 (business card rendering).

---

## 9. Migration Phases

### Phase 1: Preparation (1-2 days) ✅ COMPLETED
- [x] Install DevExpress NuGet packages
- [x] Verify version compatibility
- [x] Create CSS theme bridge file
- [x] Set up DevExpress services in `Program.cs`

### Phase 2: Component Creation (2-3 days) ✅ COMPLETED
- [x] Create `SivarAIChat.razor` wrapper component
- [x] Create `SivarBusinessCard.razor` extracted component
- [x] Implement `MessageContentTemplate` with structured results
- [x] Implement `PromptSuggestions` mapping
- [x] Implement `EmptyMessageAreaTemplate` welcome state

### Phase 3: Integration (2-3 days) ✅ COMPLETED
- [x] Update `MainLayout.razor` to use new `SivarAIChat`
- [x] Implement manual message processing flow
- [x] Connect to existing `ChatService`
- [x] Wire up location context passing
- [x] Integrate conversation history (keep existing sidebar)

### Phase 4: Feature Parity (2-3 days) ✅ COMPLETED
- [x] Implement message loading from history
- [x] Save/bookmark integration
- [x] Share functionality
- [x] Map integration for search results
- [x] Typing indicator

### Phase 5: Styling & Polish (1-2 days) ✅ COMPLETED
- [x] Fine-tune dark/light mode CSS
- [x] Responsive design adjustments (tablet/mobile/small-mobile breakpoints)
- [x] Animation/transition polish (global CSS transitions)
- [x] Accessibility review (ARIA labels, focus states, keyboard navigation, reduced motion)
- [x] Performance optimization (CSS containment, efficient selectors)

### Phase 6: Testing & Rollout (2-3 days) 🔄 IN PROGRESS
- [ ] Unit tests for new components
- [ ] Integration tests
- [ ] E2E testing
- [ ] Bug fixes
- [ ] Documentation update

**Total Estimated Time: 10-16 days**

---

## 10. Testing Strategy

### Unit Tests
- SivarAIChat message routing
- QuickAction to PromptSuggestion mapping
- Structured results extraction
- Theme switching

### Integration Tests
- End-to-end message flow
- ChatService integration
- Conversation persistence
- Location context handling

### Manual Testing Checklist
- [ ] Send message and receive response
- [ ] Prompt suggestions clickable
- [ ] Structured results display correctly
- [ ] Business cards navigate on click
- [ ] Bookmark/save functionality works
- [ ] Share functionality works
- [ ] Conversation history loads
- [ ] New conversation creation
- [ ] Dark mode styling correct
- [ ] Light mode styling correct
- [ ] Mobile responsive layout
- [ ] Location indicator works
- [ ] Map view displays results

---

## 11. Rollback Plan

### Strategy: Feature Flag

```csharp
// appsettings.json
{
  "Features": {
    "UseDxAIChat": false  // Set to true to enable new component
  }
}
```

```razor
@* In MainLayout.razor *@
@if (FeatureFlags.UseDxAIChat)
{
    <SivarAIChat ... />
}
else
{
    @* Existing custom implementation *@
    <ChatMessages ... />
    <ChatInput ... />
}
```

### Keep Original Components

Do NOT delete original components until new implementation is proven:
- `ChatMessages.razor`
- `ChatMessage.razor`
- `ChatInput.razor`
- `AIChatPanel.razor`

Mark them with `[Obsolete]` attribute after migration is complete.

---

## Appendix A: File Changes Summary

### New Files
- `Sivar.Os.Client/Components/AIChat/SivarAIChat.razor`
- `Sivar.Os.Client/Components/AIChat/SivarAIChat.razor.cs`
- `Sivar.Os.Client/Components/AIChat/SivarAIChat.razor.css`
- `Sivar.Os.Client/Components/AIChat/SivarBusinessCard.razor`
- `Sivar.Os.Client/wwwroot/css/dx-mudblazor-theme.css`

### Modified Files
- `Sivar.Os.Client/Sivar.Os.Client.csproj` (add packages)
- `Sivar.Os/Sivar.Os.csproj` (add packages)
- `Sivar.Os.Client/Program.cs` (add DevExpress services)
- `Sivar.Os.Client/Layout/MainLayout.razor` (integrate new component)
- `Sivar.Os.Client/_Imports.razor` (add DevExpress namespaces)
- `Sivar.Os.Client/wwwroot/index.html` (add CSS reference)

### Deprecated Files (keep, mark obsolete)
- `AIChatPanel.razor`
- `ChatMessages.razor`
- `ChatMessage.razor`
- `ChatInput.razor`

---

## Appendix B: Key DxAIChat API Reference

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `CssClass` | string | Custom CSS class |
| `UseStreaming` | bool | Enable streaming responses |
| `ResponseContentFormat` | enum | `Text` or `Markdown` |
| `Temperature` | float? | AI response randomness |
| `MaxTokens` | int? | Token limit |
| `FileUploadEnabled` | bool | Enable file attachments |

### Events
| Event | Args Type | Description |
|-------|-----------|-------------|
| `Initialized` | `IAIChat` | Called when chat is ready |
| `MessageSent` | `MessageSentEventArgs` | User sends message |

### Methods (via IAIChat)
| Method | Description |
|--------|-------------|
| `LoadMessages()` | Load existing messages |
| `SaveMessages()` | Get all messages |
| `SendMessage()` | Programmatically send message |
| `SetupAssistantAsync()` | Configure OpenAI Assistant |

### Templates
| Template | Context Type | Description |
|----------|--------------|-------------|
| `MessageTemplate` | `BlazorChatMessage` | Full message layout |
| `MessageContentTemplate` | `BlazorChatMessage` | Message content only |
| `EmptyMessageAreaTemplate` | - | Empty state |
| `PromptSuggestionContentTemplate` | `DxAIChatPromptSuggestion` | Suggestion bubble |

---

## Appendix C: CSS Class Reference

### DxAIChat Internal Classes (for targeting)
```css
.dx-aichat { }                    /* Root container */
.dx-aichat-messages { }           /* Messages area */
.dx-aichat-message { }            /* Individual message */
.dx-aichat-message-user { }       /* User message */
.dx-aichat-message-assistant { }  /* Assistant message */
.dx-aichat-input-area { }         /* Input container */
.dx-aichat-prompt-suggestions { } /* Suggestions container */
.dx-aichat-prompt-suggestion { }  /* Individual suggestion */
```

---

*Document Version: 1.0*
*Created: December 15, 2025*
*Author: AI Assistant*
*Project: Sivar.Os Chat Migration*
