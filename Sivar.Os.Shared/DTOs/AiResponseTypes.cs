using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// Base class for all AI structured responses
/// </summary>
public abstract record AiResponseBase
{
    /// <summary>
    /// Type of response (ProfileSearch, PostSearch, SimpleText, Markdown, ActionConfirmation)
    /// </summary>
    [JsonPropertyName("responseType")]
    [Description("Type of AI response")]
    public string ResponseType { get; init; } = string.Empty;
}

/// <summary>
/// Simple text response from AI
/// </summary>
public record SimpleTextResponse : AiResponseBase
{
    [JsonPropertyName("text")]
    [Description("Plain text response")]
    public string Text { get; init; } = string.Empty;

    public SimpleTextResponse()
    {
        ResponseType = "SimpleText";
    }
}

/// <summary>
/// Markdown formatted response from AI
/// </summary>
public record MarkdownResponse : AiResponseBase
{
    [JsonPropertyName("markdown")]
    [Description("Markdown formatted response")]
    public string Markdown { get; init; } = string.Empty;

    public MarkdownResponse()
    {
        ResponseType = "Markdown";
    }
}

/// <summary>
/// Profile search result from AI
/// </summary>
public record ProfileSearchResponse : AiResponseBase
{
    [JsonPropertyName("profiles")]
    [Description("List of profiles found")]
    public List<ProfileSearchItem> Profiles { get; init; } = new();

    [JsonPropertyName("summary")]
    [Description("Summary of search results")]
    public string Summary { get; init; } = string.Empty;

    public ProfileSearchResponse()
    {
        ResponseType = "ProfileSearch";
    }
}

public record ProfileSearchItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("profileType")]
    public string ProfileType { get; init; } = string.Empty;

    [JsonPropertyName("bio")]
    public string Bio { get; init; } = string.Empty;

    [JsonPropertyName("relevanceScore")]
    [Description("How relevant this profile is to the search query (0-100)")]
    public int RelevanceScore { get; init; }
}

/// <summary>
/// Post search result from AI
/// </summary>
public record PostSearchResponse : AiResponseBase
{
    [JsonPropertyName("posts")]
    [Description("List of posts found")]
    public List<PostSearchItem> Posts { get; init; } = new();

    [JsonPropertyName("summary")]
    [Description("Summary of search results")]
    public string Summary { get; init; } = string.Empty;

    public PostSearchResponse()
    {
        ResponseType = "PostSearch";
    }
}

public record PostSearchItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("authorName")]
    public string AuthorName { get; init; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("relevanceScore")]
    [Description("How relevant this post is to the search query (0-100)")]
    public int RelevanceScore { get; init; }
}

/// <summary>
/// Action confirmation response from AI
/// </summary>
public record ActionConfirmationResponse : AiResponseBase
{
    [JsonPropertyName("action")]
    [Description("The action that was performed (Follow, Unfollow, CreatePost, etc.)")]
    public string Action { get; init; } = string.Empty;

    [JsonPropertyName("success")]
    [Description("Whether the action was successful")]
    public bool Success { get; init; }

    [JsonPropertyName("message")]
    [Description("Confirmation message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("entityId")]
    [Description("ID of the entity affected by the action")]
    public Guid? EntityId { get; init; }

    public ActionConfirmationResponse()
    {
        ResponseType = "ActionConfirmation";
    }
}

/// <summary>
/// Post details response from AI
/// </summary>
public record PostDetailsResponse : AiResponseBase
{
    [JsonPropertyName("post")]
    [Description("Detailed post information")]
    public PostSearchItem Post { get; init; } = null!;

    [JsonPropertyName("analysis")]
    [Description("AI analysis or summary of the post")]
    public string Analysis { get; init; } = string.Empty;

    public PostDetailsResponse()
    {
        ResponseType = "PostDetails";
    }
}
