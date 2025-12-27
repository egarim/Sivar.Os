# ORM & Services Cleanup Inventory

This document inventories all ORM repositories and services in the Sivar.Os project to help identify unused functionality for potential cleanup or implementation.

---

## Summary

| Category | Total | Used | Unused/Partial |
|----------|-------|------|----------------|
| Repositories | 26 | 25 | 1 |
| Services (Core) | ~45 | ~42 | 3 |
| Utility Services | 6 | 4 | 2 |

---

## Repositories (Sivar.Os.Data/Repositories)

### ✅ Fully Used Repositories

| Repository | Interface | Usage |
|------------|-----------|-------|
| `ActivityRepository` | `IActivityRepository` | Used by `ActivityService` |
| `AdTransactionRepository` | `IAdTransactionRepository` | Used by `ProfileAdBudgetService` |
| `AgentConfigurationRepository` | `IAgentConfigurationRepository` | Used by `AgentFactory` |
| `AiModelPricingRepository` | `IAiModelPricingRepository` | Used by `AiCostService` |
| `BusinessContactInfoRepository` | `IBusinessContactInfoRepository` | Used by `ContactsController`, `ContactUrlBuilder`, `SearchResultService` |
| `ChatBotSettingsRepository` | `IChatBotSettingsRepository` | Used by `ChatBotSettingsController`, `ChatClient` |
| `ChatMessageRepository` | `IChatMessageRepository` | Used by `ChatService`, `ConversationsController` |
| `ChatTokenUsageRepository` | `IChatTokenUsageRepository` | Used by `ChatService`, `AiCostService` |
| `CommentRepository` | `ICommentRepository` | Used by multiple services |
| `ContactTypeRepository` | `IContactTypeRepository` | Used by `ContactUrlBuilder` |
| `ConversationRepository` | `IConversationRepository` | Used by `ChatService`, `SavedResultService`, controllers |
| `NotificationRepository` | `INotificationRepository` | Used by `NotificationService` |
| `PostAttachmentRepository` | `IPostAttachmentRepository` | Used by `PostService` |
| `PostRepository` | `IPostRepository` | Used extensively by services |
| `ProfileBookmarkRepository` | `IProfileBookmarkRepository` | Used by `ProfileBookmarkService` |
| `ProfileFollowerRepository` | `IProfileFollowerRepository` | Used by `ProfileFollowerService`, `ChatFunctionService` |
| `ProfileRepository` | `IProfileRepository` | Used extensively by services |
| `ProfileTypeRepository` | `IProfileTypeRepository` | Used by `ProfileService`, `ProfileTypeService` |
| `ReactionRepository` | `IReactionRepository` | Used by `ReactionService`, `PostService` |
| `ResourceBookingRepository` | `IResourceBookingRepository` | Used by `ResourceBookingService`, `BookingFunctions` |
| `SavedResultRepository` | `ISavedResultRepository` | Used by `SavedResultService` |
| `ScheduleEventRepository` | `IScheduleEventRepository` | Used by `ScheduleEventService` |
| `UserRepository` | `IUserRepository` | Used by `UserService`, `UserAuthenticationService` |
| `UserSearchBehaviorRepository` | `IUserSearchBehaviorRepository` | Used by `RankingService` |
| `RankingConfigurationRepository` | `IRankingConfigurationRepository` | Used by `RankingService` |

### ⚠️ Potentially Underutilized

| Repository | Status | Notes |
|------------|--------|-------|
| `AnalyticsRepository` | **Registered but not interface-based** | Registered as `builder.Services.AddScoped<AnalyticsRepository>()` directly, used only by `AnalyticsController`. Consider extracting interface. |

---

## Services (Sivar.Os/Services)

### ✅ Fully Used Services

| Service | Interface | Consumers |
|---------|-----------|-----------|
| `ActivityService` | `IActivityService` | `ActivitiesClient`, `PostService` |
| `AgentFactory` | `IAgentFactory` | `ChatService` |
| `AiCostService` | `IAiCostService` | `ChatService` |
| `AzureBlobStorageService` | `IFileStorageService` | `FilesController`, `ProfileService`, `PostService` |
| `CategoryNormalizer` | `ICategoryNormalizer` | `ChatFunctionService` |
| `ChatFunctionService` | (scoped) | `ChatService`, `AgentFactory` |
| `ChatService` | `IChatService` | `ChatMessagesController`, `ChatClient` |
| `ClientEmbeddingService` | `IClientEmbeddingService` | `PostService` |
| `ClientSentimentAnalysisService` | `IClientSentimentAnalysisService` | `SentimentAnalysisService` |
| `CommentService` | `ICommentService` | `CommentsClient`, Controllers |
| `ContactUrlBuilder` | `IContactUrlBuilder` | `SearchResultService`, `ContactsClient` |
| `ContentExtractionService` | (scoped) | `PostService` |
| `FileUploadValidator` | `IFileUploadValidator` | `FilesController` |
| `IntentClassifier` | `IIntentClassifier` | `ChatService` |
| `NotificationService` | `INotificationService` | Controllers, various services |
| `PostService` | `IPostService` | `PostsClient`, Controllers |
| `ProfileAdBudgetService` | `IProfileAdBudgetService` | `ProfileService`, `ChatFunctionService` |
| `ProfileAdSelector` | `IProfileAdSelector` | `ChatFunctionService` |
| `ProfileBookmarkService` | `IProfileBookmarkService` | `BookmarksController` |
| `ProfileFollowerService` | `IProfileFollowerService` | `FollowersClient` |
| `ProfileMetadataValidator` | `IProfileMetadataValidator` | `ProfileService` |
| `ProfileService` | `IProfileService` | `ProfilesClient`, Controllers |
| `ProfileTypeService` | `IProfileTypeService` | `ProfileTypesClient` |
| `RankingService` | `IRankingService` | Ranking pipeline |
| `RateLimitingService` | `IRateLimitingService` | `PostsController` |
| `ReactionService` | `IReactionService` | `ReactionsClient` |
| `ResourceBookingService` | `IResourceBookingService` | `ResourceBookingsController`, `BookingFunctions` |
| `SavedResultService` | `ISavedResultService` | `SavedResultsController` |
| `ScheduleEventService` | `IScheduleEventService` | `ScheduleEventsController` |
| `SearchResultService` | `ISearchResultService` | `ChatService`, `BusinessSearchAgent` |
| `SentimentAnalysisService` | `ISentimentAnalysisService` | `PostService`, `CommentService` |
| `ServerAuthenticationService` | `IAuthenticationService` | Registered in Program.cs for server-side auth |
| `ServerSentimentAnalysisService` | `IServerSentimentAnalysisService` | `SentimentAnalysisService` |
| `ServerWeatherService` | `IWeatherService` | `WeatherController` |
| `UserAuthenticationService` | `IUserAuthenticationService` | OIDC token validation |
| `UserService` | `IUserService` | `UsersClient`, Controllers |
| `VectorEmbeddingService` | `IVectorEmbeddingService` | `PostService`, `SearchResultService`, `SearchController` |

### ❌ Unused Services (Not Registered)

| Service | Interface | Status | Recommendation |
|---------|-----------|--------|----------------|
| `ValidationService` | `IValidationService` | **UNUSED** - Defined but never registered in DI or consumed | **DELETE** or implement usage |
| `ErrorHandler` | `IErrorHandler` | **UNUSED** - Defined but never registered in DI or consumed | **DELETE** or implement usage |

### ⚠️ Potentially Redundant

| Service | Notes |
|---------|-------|
| `BlobStorageCorsConfigurator` | Only used in Development mode for Azurite CORS configuration. Not a service, but a startup utility. Keep. |

---

## Client Services (Sivar.Os/Services/Clients)

### ✅ All Client Wrappers Are Used

| Client | Interface | Purpose |
|--------|-----------|---------|
| `AuthClient` | `IAuthClient` | Authentication |
| `UsersClient` | `IUsersClient` | User management |
| `ProfilesClient` | `IProfilesClient` | Profile CRUD |
| `ProfileTypesClient` | `IProfileTypesClient` | Profile type management |
| `PostsClient` | `IPostsClient` | Post CRUD |
| `CommentsClient` | `ICommentsClient` | Comment management |
| `ReactionsClient` | `IReactionsClient` | Reaction management |
| `FollowersClient` | `IFollowersClient` | Follower management |
| `NotificationsClient` | `INotificationsClient` | Notification management |
| `ChatClient` | `ISivarChatClient` | AI Chat |
| `FilesClient` | `IFilesClient` | File uploads |
| `ActivitiesClient` | `IActivitiesClient` | Activity feed |
| `ContactsClient` | `IContactsClient` | Business contacts |
| `ResourceBookingsClient` | `IResourceBookingsClient` | Resource booking |
| `PublicClient` | `IPublicClient` | Public profile access |
| `ProfileSwitcherClient` | `IProfileSwitcherService` | Profile switching |
| `SivarClient` | `ISivarClient` | Aggregate client (facade) |

---

## Shared Services (Sivar.Os.Shared/Services)

### ✅ Fully Used

| Service | Interface | Notes |
|---------|-----------|-------|
| `MemoryCacheService` | `ICacheService` | Default caching |
| `RedisCacheService` | `ICacheService` | Optional Redis caching |
| `NominatimLocationService` | `ILocationService` | Location services |
| `ImageCompressionService` | `IImageCompressionService` | Image processing |

---

## Recommendations

### 🔴 Immediate Action: Delete Unused Services

1. **`Sivar.Os/Services/ValidationService.cs`** - Contains `IValidationService` and `ValidationService`. Never registered or used. Delete.

2. **`Sivar.Os/Services/ErrorHandler.cs`** - Contains `IErrorHandler` and `ErrorHandler`. Never registered or used. Delete.

### 🟡 Low Priority: Consider Improvements

1. **`AnalyticsRepository`** - Extract interface `IAnalyticsRepository` for consistency with other repositories.

2. **`ContentExtractionService`** - Consider extracting interface for testability.

---

## Decision Matrix

| Item | Action | Priority | Effort |
|------|--------|----------|--------|
| `ValidationService` | DELETE | High | Low (simple delete) |
| `ErrorHandler` | DELETE | High | Low (simple delete) |
| `AnalyticsRepository` interface | REFACTOR | Low | Low |
| `ContentExtractionService` interface | REFACTOR | Low | Low |

---

## Notes

- All repositories have corresponding interfaces and are properly registered in DI
- All major services follow the interface-based pattern
- The XAF module (`OsModule`) properly exports all Sivar.Os.Shared entities for the admin backend
- The caching system supports both in-memory and Redis configurations
- The location services are extensible with Azure Maps and Google Maps providers (marked as TODO)
