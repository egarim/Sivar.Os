using Sivar.Os.Shared;
using Sivar.Os.Shared.Clients;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of ISivarClient that aggregates all repository-based clients
/// </summary>
public class SivarClient : ISivarClient
{
    public IAuthClient Auth { get; }
    public ISivarChatClient Chat { get; }
    public IPostsClient Posts { get; }
    public IProfilesClient Profiles { get; }
    public ICommentsClient Comments { get; }
    public IReactionsClient Reactions { get; }
    public IFollowersClient Followers { get; }
    public INotificationsClient Notifications { get; }
    public IFilesClient Files { get; }
    public IUsersClient Users { get; }
    public IProfileTypesClient ProfileTypes { get; }
    public IActivitiesClient Activities { get; }

    public SivarClient(
        IAuthClient auth,
        ISivarChatClient chat,
        IPostsClient posts,
        IProfilesClient profiles,
        ICommentsClient comments,
        IReactionsClient reactions,
        IFollowersClient followers,
        INotificationsClient notifications,
        IFilesClient files,
        IUsersClient users,
        IProfileTypesClient profileTypes,
        IActivitiesClient activities)
    {
        Auth = auth ?? throw new ArgumentNullException(nameof(auth));
        Chat = chat ?? throw new ArgumentNullException(nameof(chat));
        Posts = posts ?? throw new ArgumentNullException(nameof(posts));
        Profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        Comments = comments ?? throw new ArgumentNullException(nameof(comments));
        Reactions = reactions ?? throw new ArgumentNullException(nameof(reactions));
        Followers = followers ?? throw new ArgumentNullException(nameof(followers));
        Notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        Files = files ?? throw new ArgumentNullException(nameof(files));
        Users = users ?? throw new ArgumentNullException(nameof(users));
        ProfileTypes = profileTypes ?? throw new ArgumentNullException(nameof(profileTypes));
        Activities = activities ?? throw new ArgumentNullException(nameof(activities));
    }
}
