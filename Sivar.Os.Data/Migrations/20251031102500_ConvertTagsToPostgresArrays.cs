using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sivar.Os.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertTagsToPostgresArrays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sivar_ProfileTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    MaxProfilesPerUser = table.Column<int>(type: "integer", nullable: false),
                    AllowedFeatures = table.Column<string>(type: "text", nullable: false),
                    FeatureFlags = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_ProfileTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_Activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Verb = table.Column<int>(type: "integer", nullable: false),
                    ObjectType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ObjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "en"),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EngagementScore = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_Activities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    StructuredResponse = table.Column<string>(type: "text", nullable: true),
                    MessageOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_ChatMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    IsEdited = table.Column<bool>(type: "boolean", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sivar_Comments_Sivar_Comments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "Sivar_Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TriggeredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_PostAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttachmentType = table.Column<int>(type: "integer", nullable: false),
                    FileId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    LinkMetadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_PostAttachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_Posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostType = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Location_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Location_State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Location_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Location_Latitude = table.Column<double>(type: "double precision", precision: 18, scale: 10, nullable: true),
                    Location_Longitude = table.Column<double>(type: "double precision", precision: 18, scale: 10, nullable: true),
                    PricingInfo = table.Column<string>(type: "jsonb", maxLength: 1000, nullable: true),
                    BusinessMetadata = table.Column<string>(type: "jsonb", maxLength: 5000, nullable: true),
                    AvailabilityStatus = table.Column<string>(type: "text", nullable: true),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ShareCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ContentEmbedding = table.Column<string>(type: "text", nullable: true),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    IsEdited = table.Column<bool>(type: "boolean", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_Posts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_ProfileFollowers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowedProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_ProfileFollowers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_Profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Handle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AvatarFileId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Avatar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LocationCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationLatitude = table.Column<double>(type: "double precision", nullable: true),
                    LocationLongitude = table.Column<double>(type: "double precision", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    VisibilityLevel = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Metadata = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    ViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    SocialMediaLinks = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShowContactInfo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AllowedViewers = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_Profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sivar_Profiles_Sivar_ProfileTypes_ProfileTypeId",
                        column: x => x.ProfileTypeId,
                        principalTable: "Sivar_ProfileTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_Reactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: true),
                    CommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReactionType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_Reactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sivar_Reactions_Sivar_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Sivar_Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sivar_Reactions_Sivar_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Sivar_Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sivar_Reactions_Sivar_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Sivar_Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_SavedResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResultType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResultData = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_SavedResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sivar_SavedResults_Sivar_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Sivar_Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sivar_SavedResults_Sivar_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Sivar_Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    KeycloakId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PreferredLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    TimeZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "UTC"),
                    ActiveProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sivar_Users_Sivar_Profiles_ActiveProfileId",
                        column: x => x.ActiveProfileId,
                        principalTable: "Sivar_Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ActorId",
                table: "Sivar_Activities",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ActorId_PublishedAt",
                table: "Sivar_Activities",
                columns: new[] { "ActorId", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_EngagementScore",
                table: "Sivar_Activities",
                column: "EngagementScore");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Feed_Query",
                table: "Sivar_Activities",
                columns: new[] { "Visibility", "IsPublished", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Metadata_Gin",
                table: "Sivar_Activities",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Object",
                table: "Sivar_Activities",
                columns: new[] { "ObjectType", "ObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_PublishedAt",
                table: "Sivar_Activities",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Verb",
                table: "Sivar_Activities",
                column: "Verb");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Visibility",
                table: "Sivar_Activities",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_ChatMessages_ConversationId",
                table: "Sivar_ChatMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_ChatMessages_ConversationId_MessageOrder",
                table: "Sivar_ChatMessages",
                columns: new[] { "ConversationId", "MessageOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Comments_CreatedAt",
                table: "Sivar_Comments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Comments_ParentCommentId",
                table: "Sivar_Comments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Comments_PostId",
                table: "Sivar_Comments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Comments_PostId_CreatedAt",
                table: "Sivar_Comments",
                columns: new[] { "PostId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Comments_ProfileId",
                table: "Sivar_Comments",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Conversations_LastMessageAt",
                table: "Sivar_Conversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Conversations_ProfileId",
                table: "Sivar_Conversations",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Conversations_ProfileId_IsActive",
                table: "Sivar_Conversations",
                columns: new[] { "ProfileId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Sivar_Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DuplicateCheck",
                table: "Sivar_Notifications",
                columns: new[] { "UserId", "Type", "RelatedEntityId", "TriggeredByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RelatedEntity",
                table: "Sivar_Notifications",
                columns: new[] { "RelatedEntityId", "RelatedEntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TriggeredByUserId",
                table: "Sivar_Notifications",
                column: "TriggeredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Sivar_Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Sivar_Notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Sivar_Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_Type",
                table: "Sivar_Notifications",
                columns: new[] { "UserId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_PostAttachments_AttachmentType",
                table: "Sivar_PostAttachments",
                column: "AttachmentType");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_PostAttachments_PostId",
                table: "Sivar_PostAttachments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_PostAttachments_PostId_DisplayOrder",
                table: "Sivar_PostAttachments",
                columns: new[] { "PostId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_BusinessMetadata_Gin",
                table: "Sivar_Posts",
                column: "BusinessMetadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_PricingInfo_Gin",
                table: "Sivar_Posts",
                column: "PricingInfo")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Tags_Gin",
                table: "Sivar_Posts",
                column: "Tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Posts_CreatedAt",
                table: "Sivar_Posts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Posts_PostType",
                table: "Sivar_Posts",
                column: "PostType");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Posts_ProfileId",
                table: "Sivar_Posts",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Posts_ProfileId_PostType",
                table: "Sivar_Posts",
                columns: new[] { "ProfileId", "PostType" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileFollowers_FollowedAt",
                table: "Sivar_ProfileFollowers",
                column: "FollowedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileFollowers_FollowedProfileId",
                table: "Sivar_ProfileFollowers",
                column: "FollowedProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileFollowers_FollowerProfile_FollowedProfile",
                table: "Sivar_ProfileFollowers",
                columns: new[] { "FollowerProfileId", "FollowedProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileFollowers_FollowerProfileId",
                table: "Sivar_ProfileFollowers",
                column: "FollowerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileFollowers_IsActive",
                table: "Sivar_ProfileFollowers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_CreatedAt",
                table: "Sivar_Profiles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_DisplayName",
                table: "Sivar_Profiles",
                column: "DisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_Handle",
                table: "Sivar_Profiles",
                column: "Handle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_ProfileTypeId",
                table: "Sivar_Profiles",
                column: "ProfileTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId",
                table: "Sivar_Profiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId_IsActive",
                table: "Sivar_Profiles",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId_ProfileTypeId",
                table: "Sivar_Profiles",
                columns: new[] { "UserId", "ProfileTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_VisibilityLevel",
                table: "Sivar_Profiles",
                column: "VisibilityLevel");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileTypes_IsActive",
                table: "Sivar_ProfileTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileTypes_Name",
                table: "Sivar_ProfileTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileTypes_SortOrder",
                table: "Sivar_ProfileTypes",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_ProfileId_CommentId",
                table: "Sivar_Reactions",
                columns: new[] { "ProfileId", "CommentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_ProfileId_PostId",
                table: "Sivar_Reactions",
                columns: new[] { "ProfileId", "PostId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Reactions_CommentId",
                table: "Sivar_Reactions",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Reactions_PostId",
                table: "Sivar_Reactions",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Reactions_ProfileId",
                table: "Sivar_Reactions",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Reactions_ReactionType",
                table: "Sivar_Reactions",
                column: "ReactionType");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_SavedResults_ConversationId",
                table: "Sivar_SavedResults",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_SavedResults_CreatedAt",
                table: "Sivar_SavedResults",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_SavedResults_ProfileId",
                table: "Sivar_SavedResults",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_SavedResults_ResultType",
                table: "Sivar_SavedResults",
                column: "ResultType");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_Users_ActiveProfileId",
                table: "Sivar_Users",
                column: "ActiveProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Sivar_Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Sivar_Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Sivar_Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_KeycloakId",
                table: "Sivar_Users",
                column: "KeycloakId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Sivar_Users",
                column: "Role");

            migrationBuilder.AddForeignKey(
                name: "FK_Sivar_Activities_Sivar_Profiles_ActorId",
                table: "Sivar_Activities",
                column: "ActorId",
                principalTable: "Sivar_Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sivar_ChatMessages_Sivar_Conversations_ConversationId",
                table: "Sivar_ChatMessages",
                column: "ConversationId",
                principalTable: "Sivar_Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sivar_Comments_Sivar_Posts_PostId",
                table: "Sivar_Comments",
                column: "PostId",
                principalTable: "Sivar_Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sivar_Comments_Sivar_Profiles_ProfileId",
                table: "Sivar_Comments",
                column: "ProfileId",
                principalTable: "Sivar_Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sivar_Conversations_Sivar_Profiles_ProfileId",
                table: "Sivar_Conversations",
                column: "ProfileId",
                principalTable: "Sivar_Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sivar_Notifications_Sivar_Users_TriggeredByUserId",
                table: "Sivar_Notifications",
                column: "TriggeredByUserId",
                principalTable: "Sivar_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Sivar_Notifications_Sivar_Users_UserId",
                table: "Sivar_Notifications",
                column: "UserId",
                principalTable: "Sivar_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sivar_PostAttachments_Sivar_Posts_PostId",
                table: "Sivar_PostAttachments",
                column: "PostId",
                principalTable: "Sivar_Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sivar_Posts_Sivar_Profiles_ProfileId",
                table: "Sivar_Posts",
                column: "ProfileId",
                principalTable: "Sivar_Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sivar_ProfileFollowers_Sivar_Profiles_FollowedProfileId",
                table: "Sivar_ProfileFollowers",
                column: "FollowedProfileId",
                principalTable: "Sivar_Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sivar_ProfileFollowers_Sivar_Profiles_FollowerProfileId",
                table: "Sivar_ProfileFollowers",
                column: "FollowerProfileId",
                principalTable: "Sivar_Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sivar_Profiles_Sivar_Users_UserId",
                table: "Sivar_Profiles",
                column: "UserId",
                principalTable: "Sivar_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sivar_Users_Sivar_Profiles_ActiveProfileId",
                table: "Sivar_Users");

            migrationBuilder.DropTable(
                name: "Sivar_Activities");

            migrationBuilder.DropTable(
                name: "Sivar_ChatMessages");

            migrationBuilder.DropTable(
                name: "Sivar_Notifications");

            migrationBuilder.DropTable(
                name: "Sivar_PostAttachments");

            migrationBuilder.DropTable(
                name: "Sivar_ProfileFollowers");

            migrationBuilder.DropTable(
                name: "Sivar_Reactions");

            migrationBuilder.DropTable(
                name: "Sivar_SavedResults");

            migrationBuilder.DropTable(
                name: "Sivar_Comments");

            migrationBuilder.DropTable(
                name: "Sivar_Conversations");

            migrationBuilder.DropTable(
                name: "Sivar_Posts");

            migrationBuilder.DropTable(
                name: "Sivar_Profiles");

            migrationBuilder.DropTable(
                name: "Sivar_ProfileTypes");

            migrationBuilder.DropTable(
                name: "Sivar_Users");
        }
    }
}
