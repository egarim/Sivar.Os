using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sivar.Os.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkingHoursJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContentEmbedding",
                table: "Sivar_Posts",
                newName: "BlogContent");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<string>(
                name: "GeoLocationSource",
                table: "Sivar_Profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<DateTime>(
                name: "GeoLocationUpdatedAt",
                table: "Sivar_Profiles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "Sivar_Profiles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnalyzedAt",
                table: "Sivar_Posts",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AngerScore",
                table: "Sivar_Posts",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanonicalUrl",
                table: "Sivar_Posts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageFileId",
                table: "Sivar_Posts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "Sivar_Posts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EmotionScore",
                table: "Sivar_Posts",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FearScore",
                table: "Sivar_Posts",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeoLocationSource",
                table: "Sivar_Posts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<DateTime>(
                name: "GeoLocationUpdatedAt",
                table: "Sivar_Posts",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAnger",
                table: "Sivar_Posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "Sivar_Posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "JoyScore",
                table: "Sivar_Posts",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsReview",
                table: "Sivar_Posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryEmotion",
                table: "Sivar_Posts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Sivar_Posts",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReadTimeMinutes",
                table: "Sivar_Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SadnessScore",
                table: "Sivar_Posts",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SentimentPolarity",
                table: "Sivar_Posts",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subtitle",
                table: "Sivar_Posts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnalyzedAt",
                table: "Sivar_Comments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AngerScore",
                table: "Sivar_Comments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EmotionScore",
                table: "Sivar_Comments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FearScore",
                table: "Sivar_Comments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAnger",
                table: "Sivar_Comments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "JoyScore",
                table: "Sivar_Comments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsReview",
                table: "Sivar_Comments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryEmotion",
                table: "Sivar_Comments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SadnessScore",
                table: "Sivar_Comments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SentimentPolarity",
                table: "Sivar_Comments",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Sivar_AgentCapabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FunctionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExampleQueriesJson = table.Column<string>(type: "text", maxLength: 2000, nullable: true),
                    UsageInstructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_AgentCapabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_ChatBotSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Culture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    WelcomeMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    HeaderTagline = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BotName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Sivar AI Assistant"),
                    QuickActionsJson = table.Column<string>(type: "text", nullable: true),
                    SystemPrompt = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    RegionCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ThinkingMessage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_ChatBotSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_ContactTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MudBlazorIcon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UrlTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "other"),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RegionalPopularity = table.Column<string>(type: "jsonb", nullable: true),
                    ValidationRegex = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Placeholder = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OpenInNewTab = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    MobileOnly = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_ContactTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_ProfileEmotionSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeWindow = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TotalPosts = table.Column<int>(type: "integer", nullable: false),
                    TotalComments = table.Column<int>(type: "integer", nullable: false),
                    AvgJoyScore = table.Column<decimal>(type: "numeric", nullable: false),
                    AvgSadnessScore = table.Column<decimal>(type: "numeric", nullable: false),
                    AvgAngerScore = table.Column<decimal>(type: "numeric", nullable: false),
                    AvgFearScore = table.Column<decimal>(type: "numeric", nullable: false),
                    DominantEmotion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    JoyCount = table.Column<int>(type: "integer", nullable: false),
                    SadnessCount = table.Column<int>(type: "integer", nullable: false),
                    AngerCount = table.Column<int>(type: "integer", nullable: false),
                    FearCount = table.Column<int>(type: "integer", nullable: false),
                    NeutralCount = table.Column<int>(type: "integer", nullable: false),
                    FlaggedCount = table.Column<int>(type: "integer", nullable: false),
                    OverallPolarity = table.Column<decimal>(type: "numeric", nullable: false),
                    IsAutomated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_ProfileEmotionSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sivar_ProfileEmotionSummaries_Sivar_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Sivar_Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_SearchResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResultType = table.Column<int>(type: "integer", nullable: false),
                    MatchSource = table.Column<int>(type: "integer", nullable: false),
                    RelevanceScore = table.Column<double>(type: "double precision", nullable: false),
                    SemanticScore = table.Column<double>(type: "double precision", nullable: true),
                    FullTextRank = table.Column<double>(type: "double precision", nullable: true),
                    DistanceKm = table.Column<double>(type: "double precision", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Handle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SubCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    WorkingHours = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WorkingHoursJson = table.Column<string>(type: "text", nullable: true),
                    PriceRange = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Rating = table.Column<double>(type: "double precision", nullable: true),
                    ReviewCount = table.Column<int>(type: "integer", nullable: true),
                    EventDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EventEndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Venue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TicketPrice = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Requirements = table.Column<string>(type: "jsonb", nullable: true),
                    ProcessingTime = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Cost = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WhereToGo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OnlineUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Tags = table.Column<string[]>(type: "text[]", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_SearchResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sivar_SearchResults_Sivar_ChatMessages_ChatMessageId",
                        column: x => x.ChatMessageId,
                        principalTable: "Sivar_ChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sivar_SearchResults_Sivar_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Sivar_Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Sivar_SearchResults_Sivar_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Sivar_Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_CapabilityParameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CapabilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DataType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "string"),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DefaultValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AllowedValuesJson = table.Column<string>(type: "text", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_CapabilityParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sivar_CapabilityParameters_Sivar_AgentCapabilities_Capabili~",
                        column: x => x.CapabilityId,
                        principalTable: "Sivar_AgentCapabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_QuickActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatBotSettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapabilityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MudBlazorIcon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DefaultQuery = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContextHint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RequiresLocation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_QuickActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sivar_QuickActions_Sivar_AgentCapabilities_CapabilityId",
                        column: x => x.CapabilityId,
                        principalTable: "Sivar_AgentCapabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Sivar_QuickActions_Sivar_ChatBotSettings_ChatBotSettingsId",
                        column: x => x.ChatBotSettingsId,
                        principalTable: "Sivar_ChatBotSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_BusinessContactInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AvailableHours = table.Column<string>(type: "jsonb", nullable: true),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_BusinessContactInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sivar_BusinessContactInfos_Sivar_ContactTypes_ContactTypeId",
                        column: x => x.ContactTypeId,
                        principalTable: "Sivar_ContactTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sivar_BusinessContactInfos_Sivar_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Sivar_Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Blog_Drafts",
                table: "Sivar_Posts",
                columns: new[] { "IsDraft", "ProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Blog_PublishedAt",
                table: "Sivar_Posts",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCapabilities_Category",
                table: "Sivar_AgentCapabilities",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCapabilities_FunctionName",
                table: "Sivar_AgentCapabilities",
                column: "FunctionName");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCapabilities_IsEnabled",
                table: "Sivar_AgentCapabilities",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCapabilities_Key",
                table: "Sivar_AgentCapabilities",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessContactInfos_ContactTypeId",
                table: "Sivar_BusinessContactInfos",
                column: "ContactTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessContactInfos_IsActive",
                table: "Sivar_BusinessContactInfos",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessContactInfos_Profile_ContactType",
                table: "Sivar_BusinessContactInfos",
                columns: new[] { "ProfileId", "ContactTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessContactInfos_Profile_Primary",
                table: "Sivar_BusinessContactInfos",
                columns: new[] { "ProfileId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessContactInfos_ProfileId",
                table: "Sivar_BusinessContactInfos",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityParameters_Capability_Name",
                table: "Sivar_CapabilityParameters",
                columns: new[] { "CapabilityId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityParameters_CapabilityId",
                table: "Sivar_CapabilityParameters",
                column: "CapabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatBotSettings_Culture_Region_Active",
                table: "Sivar_ChatBotSettings",
                columns: new[] { "Culture", "RegionCode", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatBotSettings_IsActive",
                table: "Sivar_ChatBotSettings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ChatBotSettings_Key",
                table: "Sivar_ChatBotSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactTypes_Category",
                table: "Sivar_ContactTypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ContactTypes_Category_SortOrder",
                table: "Sivar_ContactTypes",
                columns: new[] { "Category", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactTypes_IsActive",
                table: "Sivar_ContactTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ContactTypes_Key",
                table: "Sivar_ContactTypes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileEmotionSummaries_Dates",
                table: "Sivar_ProfileEmotionSummaries",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileEmotionSummaries_ProfileId",
                table: "Sivar_ProfileEmotionSummaries",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileEmotionSummaries_TimeWindow",
                table: "Sivar_ProfileEmotionSummaries",
                column: "TimeWindow");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileEmotionSummaries_Unique",
                table: "Sivar_ProfileEmotionSummaries",
                columns: new[] { "ProfileId", "TimeWindow", "StartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuickActions_CapabilityId",
                table: "Sivar_QuickActions",
                column: "CapabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickActions_ChatBotSettingsId",
                table: "Sivar_QuickActions",
                column: "ChatBotSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickActions_IsActive",
                table: "Sivar_QuickActions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QuickActions_Settings_SortOrder",
                table: "Sivar_QuickActions",
                columns: new[] { "ChatBotSettingsId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_SearchResults_ChatMessageId",
                table: "Sivar_SearchResults",
                column: "ChatMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_SearchResults_ChatMessageId_DisplayOrder",
                table: "Sivar_SearchResults",
                columns: new[] { "ChatMessageId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_SearchResults_PostId",
                table: "Sivar_SearchResults",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_SearchResults_ProfileId",
                table: "Sivar_SearchResults",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Sivar_SearchResults_ResultType",
                table: "Sivar_SearchResults",
                column: "ResultType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sivar_BusinessContactInfos");

            migrationBuilder.DropTable(
                name: "Sivar_CapabilityParameters");

            migrationBuilder.DropTable(
                name: "Sivar_ProfileEmotionSummaries");

            migrationBuilder.DropTable(
                name: "Sivar_QuickActions");

            migrationBuilder.DropTable(
                name: "Sivar_SearchResults");

            migrationBuilder.DropTable(
                name: "Sivar_ContactTypes");

            migrationBuilder.DropTable(
                name: "Sivar_AgentCapabilities");

            migrationBuilder.DropTable(
                name: "Sivar_ChatBotSettings");

            migrationBuilder.DropIndex(
                name: "IX_Posts_Blog_Drafts",
                table: "Sivar_Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_Blog_PublishedAt",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "GeoLocationSource",
                table: "Sivar_Profiles");

            migrationBuilder.DropColumn(
                name: "GeoLocationUpdatedAt",
                table: "Sivar_Profiles");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "Sivar_Profiles");

            migrationBuilder.DropColumn(
                name: "AnalyzedAt",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "AngerScore",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "CanonicalUrl",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "CoverImageFileId",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "EmotionScore",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "FearScore",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "GeoLocationSource",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "GeoLocationUpdatedAt",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "HasAnger",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "JoyScore",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "NeedsReview",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "PrimaryEmotion",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "ReadTimeMinutes",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "SadnessScore",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "SentimentPolarity",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "Subtitle",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "AnalyzedAt",
                table: "Sivar_Comments");

            migrationBuilder.DropColumn(
                name: "AngerScore",
                table: "Sivar_Comments");

            migrationBuilder.DropColumn(
                name: "EmotionScore",
                table: "Sivar_Comments");

            migrationBuilder.DropColumn(
                name: "FearScore",
                table: "Sivar_Comments");

            migrationBuilder.DropColumn(
                name: "HasAnger",
                table: "Sivar_Comments");

            migrationBuilder.DropColumn(
                name: "JoyScore",
                table: "Sivar_Comments");

            migrationBuilder.DropColumn(
                name: "NeedsReview",
                table: "Sivar_Comments");

            migrationBuilder.DropColumn(
                name: "PrimaryEmotion",
                table: "Sivar_Comments");

            migrationBuilder.DropColumn(
                name: "SadnessScore",
                table: "Sivar_Comments");

            migrationBuilder.DropColumn(
                name: "SentimentPolarity",
                table: "Sivar_Comments");

            migrationBuilder.RenameColumn(
                name: "BlogContent",
                table: "Sivar_Posts",
                newName: "ContentEmbedding");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
