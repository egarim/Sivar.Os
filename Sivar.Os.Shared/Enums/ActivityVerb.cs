namespace Sivar.Os.Shared.Enums;

/// <summary>
/// Standard verbs for activities following Activity Streams 2.0 vocabulary
/// Represents the action performed in an activity
/// </summary>
public enum ActivityVerb
{
    /// <summary>
    /// Create a new object (post, comment, profile, etc.)
    /// </summary>
    Create = 0,

    /// <summary>
    /// Update/edit an existing object
    /// </summary>
    Update = 1,

    /// <summary>
    /// Delete an object
    /// </summary>
    Delete = 2,

    /// <summary>
    /// Like/favorite an object
    /// </summary>
    Like = 3,

    /// <summary>
    /// Unlike/unfavorite an object
    /// </summary>
    Unlike = 4,

    /// <summary>
    /// Add a comment to an object
    /// </summary>
    Comment = 5,

    /// <summary>
    /// Share/repost an object
    /// </summary>
    Share = 6,

    /// <summary>
    /// Unshare an object
    /// </summary>
    Unshare = 7,

    /// <summary>
    /// Follow a profile or object
    /// </summary>
    Follow = 8,

    /// <summary>
    /// Unfollow a profile or object
    /// </summary>
    Unfollow = 9,

    /// <summary>
    /// Join a group, event, or community
    /// </summary>
    Join = 10,

    /// <summary>
    /// Leave a group, event, or community
    /// </summary>
    Leave = 11,

    /// <summary>
    /// Add an object to a collection or target
    /// </summary>
    Add = 12,

    /// <summary>
    /// Remove an object from a collection or target
    /// </summary>
    Remove = 13,

    /// <summary>
    /// Mention/tag another profile in content
    /// </summary>
    Mention = 14,

    /// <summary>
    /// Tag content with a label or category
    /// </summary>
    Tag = 15,

    /// <summary>
    /// View or read an object
    /// </summary>
    View = 16,

    /// <summary>
    /// Accept a request, invitation, or offer
    /// </summary>
    Accept = 17,

    /// <summary>
    /// Reject a request, invitation, or offer
    /// </summary>
    Reject = 18,

    /// <summary>
    /// Request something (connection, access, etc.)
    /// </summary>
    Request = 19,

    /// <summary>
    /// Invite someone to something
    /// </summary>
    Invite = 20,

    /// <summary>
    /// Announce something publicly
    /// </summary>
    Announce = 21,

    /// <summary>
    /// Bookmark or save for later
    /// </summary>
    Bookmark = 22,

    /// <summary>
    /// Remove a bookmark
    /// </summary>
    Unbookmark = 23,

    /// <summary>
    /// Block a profile or content
    /// </summary>
    Block = 24,

    /// <summary>
    /// Unblock a profile or content
    /// </summary>
    Unblock = 25,

    /// <summary>
    /// Report content or profile as inappropriate
    /// </summary>
    Report = 26,

    /// <summary>
    /// Pin content to top of feed or profile
    /// </summary>
    Pin = 27,

    /// <summary>
    /// Unpin content
    /// </summary>
    Unpin = 28,

    /// <summary>
    /// Archive content
    /// </summary>
    Archive = 29,

    /// <summary>
    /// Restore archived content
    /// </summary>
    Restore = 30
}
