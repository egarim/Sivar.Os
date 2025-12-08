namespace Sivar.Os.Client.Pages;

#nullable enable

public class UserSample
{
    public UserSample(string name, string bio, bool following)
    {
        Name = name;
        Bio = bio;
        IsFollowing = following;
    }

    public string Name { get; }
    public string Bio { get; }
    public bool IsFollowing { get; set; }
    public string Initials => string.Join(string.Empty, Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => char.ToUpperInvariant(p[0])));
}

public class PostSample
{
    public string Author { get; set; } = string.Empty;
    public string AuthorInitials => string.Join(string.Empty, Author.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => char.ToUpperInvariant(p[0])));
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "general";
    public string Visibility { get; set; } = "Public";
    public DateTime Time { get; set; } = DateTime.Now;
    public List<PostMetadataItem> Metadata { get; } = new();
    public int Likes { get; set; }
    public int Comments { get; set; }
    public int Shares { get; set; }
    public bool Liked { get; set; }
    public string? ImageUrl { get; set; }
    public List<PostComment> CommentsList { get; } = new();
    public List<PostReaction> Reactions { get; } = new();
    public bool ShowComments { get; set; }
}

public class PostComment
{
    public PostComment(string author, string text, DateTime time)
    {
        Author = author;
        Text = text;
        Time = time;
    }

    public string Author { get; }
    public string AuthorInitials => string.Join(string.Empty, Author.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => char.ToUpperInvariant(p[0])));
    public string Text { get; }
    public DateTime Time { get; }
    public int Likes { get; set; }
}

public class PostReaction
{
    public PostReaction(string emoji, int count)
    {
        Emoji = emoji;
        Count = count;
    }

    public string Emoji { get; }
    public int Count { get; set; }
    public bool IsActive { get; set; }
}

public class PostMetadataItem
{
    public PostMetadataItem(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }
    public string Value { get; }
}

public class StatItem
{
    public StatItem(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }
    public string Value { get; }
}

public class SavedResultItem
{
    public SavedResultItem(string title, string type)
    {
        Title = title;
        Type = type;
        Id = Guid.NewGuid().ToString();
    }

    public string Id { get; }
    public string Title { get; }
    public string Type { get; }
    public string Initials => string.Join(string.Empty, Title.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => char.ToUpperInvariant(p[0])));
}

public class StatsSummary
{
    public int Followers { get; set; }
    public int Following { get; set; }
    public int Reach { get; set; }
    public int ResponseRate { get; set; }
}

public class Conversation
{
    public Conversation(string title)
    {
        Title = title;
        GuidId = Guid.NewGuid();
        Id = GuidId.ToString();
    }

    public Conversation(Guid guidId, string title)
    {
        GuidId = guidId;
        Id = guidId.ToString();
        Title = title;
    }

    public Guid GuidId { get; }
    public string Id { get; }
    public string Title { get; set; }
    public string Preview { get; set; } = "Start a conversation";
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    public int MessageCount { get; set; }
    public int ResultCount { get; set; }
}

public class ChatMessage
{
    public ChatMessage(string sender, string text, string? messageType = null)
    {
        Sender = sender;
        Text = text;
        MessageType = messageType ?? "text";
    }

    public string Sender { get; }
    public string Text { get; }
    public string MessageType { get; } // "text", "result-card", "welcome"
    public DateTime Time { get; set; } = DateTime.Now;
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ChatResultCard? ResultCard { get; set; }
    public List<string>? QuickActions { get; set; }
}

public class ChatResultCard
{
    public ChatResultCard(string id, string avatar, string name, string type, string description)
    {
        Id = id;
        Avatar = avatar;
        Name = name;
        Type = type;
        Description = description;
    }

    public string Id { get; }
    public string Avatar { get; }
    public string Name { get; }
    public string Type { get; }
    public string Description { get; }
}
