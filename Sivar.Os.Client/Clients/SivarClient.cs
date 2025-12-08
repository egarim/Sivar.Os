using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Sivar.Os.Shared;
using Sivar.Os.Shared.Clients;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Main client for interacting with Sivar API - orchestrates all sub-clients
/// </summary>
public class SivarClient : ISivarClient
{
    private readonly HttpClient _httpClient;
    private readonly SivarClientOptions _options;
    private readonly ILoggerFactory? _loggerFactory;

    // Lazy-loaded sub-clients
    private IAuthClient? _auth;
    private ISivarChatClient? _chat;
    private IPostsClient? _posts;
    private IProfilesClient? _profiles;
    private ICommentsClient? _comments;
    private IReactionsClient? _reactions;
    private IFollowersClient? _followers;
    private INotificationsClient? _notifications;
    private IFilesClient? _files;
    private IUsersClient? _users;
    private IProfileTypesClient? _profileTypes;
    private IActivitiesClient? _activities;

    public SivarClient(HttpClient httpClient, IOptions<SivarClientOptions> options, ILoggerFactory? loggerFactory = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Authentication operations (login, status)
    /// </summary>
    public IAuthClient Auth => _auth ??= new AuthClient(_httpClient, _options);

    /// <summary>
    /// AI Chat operations (conversations, messages, saved results)
    /// </summary>
    public ISivarChatClient Chat => _chat ??= new ChatClient(_httpClient, _options, _loggerFactory?.CreateLogger<ChatClient>());

    /// <summary>
    /// Posts operations (CRUD, feed, search, analytics)
    /// </summary>
    public IPostsClient Posts => _posts ??= new PostsClient(_httpClient, _options);

    /// <summary>
    /// Profile operations (my profile, discovery, statistics)
    /// </summary>
    public IProfilesClient Profiles => _profiles ??= new ProfilesClient(_httpClient, _options);

    /// <summary>
    /// Comments operations (CRUD, replies, threads)
    /// </summary>
    public ICommentsClient Comments => _comments ??= new CommentsClient(_httpClient, _options);

    /// <summary>
    /// Reactions operations (add, remove, analytics)
    /// </summary>
    public IReactionsClient Reactions => _reactions ??= new ReactionsClient(_httpClient, _options);

    /// <summary>
    /// Followers operations (follow, unfollow, stats)
    /// </summary>
    public IFollowersClient Followers => _followers ??= new FollowersClient(_httpClient, _options);

    /// <summary>
    /// Notifications operations (get, mark read, delete)
    /// </summary>
    public INotificationsClient Notifications => _notifications ??= new NotificationsClient(_httpClient, _options);

    /// <summary>
    /// File operations (upload, download, delete)
    /// </summary>
    public IFilesClient Files => _files ??= new FilesClient(_httpClient, _options);

    /// <summary>
    /// User operations (get, preferences, admin)
    /// </summary>
    public IUsersClient Users => _users ??= new UsersClient(_httpClient, _options);

    /// <summary>
    /// Profile types operations (CRUD, admin)
    /// </summary>
    public IProfileTypesClient ProfileTypes => _profileTypes ??= new ProfileTypesClient(_httpClient, _options);

    /// <summary>
    /// Activity stream operations (feed, profile activities, trending)
    /// </summary>
    public IActivitiesClient Activities => _activities ??= new ActivitiesClient(_httpClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<ActivitiesClient>.Instance);
}
