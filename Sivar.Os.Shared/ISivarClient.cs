


using Sivar.Os.Shared.Clients;

namespace Sivar.Os.Shared;

/// <summary>
/// Main client interface for interacting with Sivar API
/// </summary>
public interface ISivarClient
{
    /// <summary>
    /// Authentication operations (login, status)
    /// </summary>
    IAuthClient Auth { get; }

    /// <summary>
    /// AI Chat operations (conversations, messages, saved results)
    /// </summary>
    ISivarChatClient Chat { get; }

    /// <summary>
    /// Posts operations (CRUD, feed, search, analytics)
    /// </summary>
    IPostsClient Posts { get; }

    /// <summary>
    /// Profile operations (my profile, discovery, statistics)
    /// </summary>
    IProfilesClient Profiles { get; }

    /// <summary>
    /// Comments operations (CRUD, replies, threads)
    /// </summary>
    ICommentsClient Comments { get; }

    /// <summary>
    /// Reactions operations (add, remove, analytics)
    /// </summary>
    IReactionsClient Reactions { get; }

    /// <summary>
    /// Followers operations (follow, unfollow, stats)
    /// </summary>
    IFollowersClient Followers { get; }

    /// <summary>
    /// Notifications operations (get, mark read, delete)
    /// </summary>
    INotificationsClient Notifications { get; }

    /// <summary>
    /// File operations (upload, download, delete)
    /// </summary>
    IFilesClient Files { get; }

    /// <summary>
    /// User operations (get, preferences, admin)
    /// </summary>
    IUsersClient Users { get; }

    /// <summary>
    /// Profile types operations (CRUD, admin)
    /// </summary>
    IProfileTypesClient ProfileTypes { get; }

    /// <summary>
    /// Activity stream operations (feed, profile activities, trending)
    /// </summary>
    IActivitiesClient Activities { get; }
}
