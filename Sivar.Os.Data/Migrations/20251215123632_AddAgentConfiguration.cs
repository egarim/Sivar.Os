using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sivar.Os.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "CategoryKeys",
                table: "Sivar_Profiles",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "CategoryKeys",
                table: "Sivar_Posts",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "ProcedureMetadataJson",
                table: "Sivar_Posts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModelId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: false),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    EnabledTools = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    ProviderSettings = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    IntentPatterns = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedByProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    AbTestVariant = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    AbTestWeight = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentTools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FunctionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ParameterSchema = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredPermission = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsExternalCall = table.Column<bool>(type: "boolean", nullable: false),
                    AvgExecutionTimeMs = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentTools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_CategoryDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayNameEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayNameEs = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParentKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Synonyms = table.Column<string[]>(type: "text[]", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_CategoryDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sivar_ProfileBookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sivar_ProfileBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sivar_ProfileBookmarks_Sivar_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Sivar_Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sivar_ProfileBookmarks_Sivar_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Sivar_Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_CategoryKeys_Gin",
                table: "Sivar_Profiles",
                column: "CategoryKeys")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CategoryKeys_Gin",
                table: "Sivar_Posts",
                column: "CategoryKeys")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryDefinitions_Active_SortOrder",
                table: "Sivar_CategoryDefinitions",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryDefinitions_Key",
                table: "Sivar_CategoryDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryDefinitions_ParentKey",
                table: "Sivar_CategoryDefinitions",
                column: "ParentKey");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryDefinitions_Synonyms_GIN",
                table: "Sivar_CategoryDefinitions",
                column: "Synonyms")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileBookmarks_CreatedAt",
                table: "Sivar_ProfileBookmarks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileBookmarks_PostId",
                table: "Sivar_ProfileBookmarks",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileBookmarks_ProfileId",
                table: "Sivar_ProfileBookmarks",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileBookmarks_ProfileId_PostId",
                table: "Sivar_ProfileBookmarks",
                columns: new[] { "ProfileId", "PostId" },
                unique: true);

            // Seed default agent configuration (sivar-main)
            var defaultSystemPrompt = @"You are Sivar, a helpful AI assistant for the Sivar.Os social network platform in El Salvador.
You can help users:
- Search for profiles, posts, businesses, and places on the network
- Find nearby businesses and content using GPS location
- Get contact information (phone, email, WhatsApp) for businesses
- Get business hours and open/closed status
- Get directions and location information
- Help with government procedures and requirements (DUI, pasaporte, licencia, etc.)
- Follow and unfollow other users
- Get information about their own profile

IMPORTANT INSTRUCTIONS:
1. Always respond in Spanish when the user writes in Spanish.
2. When users ask for contact info, use GetContactInfo function.
3. When users ask about hours/schedule, use GetBusinessHours function.
4. When users ask for directions/location, use GetDirections function.
5. When users ask about procedures/requirements, use GetProcedureInfo function.
6. When showing links, always use RELATIVE URLs (starting with /) not absolute URLs.
7. Be friendly, helpful, and conversational.";

            var enabledTools = @"[""SearchProfiles"", ""SearchPosts"", ""GetPostDetails"", ""FindBusinesses"", ""FollowProfile"", ""UnfollowProfile"", ""GetMyProfile"", ""SearchNearbyProfiles"", ""SearchNearbyPosts"", ""CalculateDistance"", ""GetAddressFromCoordinates"", ""GetCoordinatesFromAddress"", ""SearchNearMe"", ""GetCurrentLocationStatus"", ""GetContactInfo"", ""GetBusinessHours"", ""GetDirections"", ""GetProcedureInfo""]";

            var intentPatterns = @"["".*""]";

            migrationBuilder.Sql($@"
                INSERT INTO ""AgentConfigurations"" (
                    ""Id"", ""AgentKey"", ""DisplayName"", ""Description"", ""SystemPrompt"",
                    ""Provider"", ""ModelId"", ""Temperature"", ""MaxTokens"",
                    ""EnabledTools"", ""IntentPatterns"", ""Priority"", ""IsActive"",
                    ""Version"", ""AbTestWeight"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted""
                ) VALUES (
                    '{Guid.NewGuid()}', 'sivar-main', 'Sivar Principal',
                    'Agente principal para todas las consultas generales en Sivar.Os',
                    '{defaultSystemPrompt.Replace("'", "''")}',
                    'ollama', 'llama3.2:latest', 0.7, 2000,
                    '{enabledTools}'::jsonb, '{intentPatterns}'::jsonb, 100, true,
                    1, 100, NOW(), NOW(), false
                );
            ");

            // Seed AgentTools registry
            migrationBuilder.Sql(@"
                INSERT INTO ""AgentTools"" (""Id"", ""FunctionName"", ""DisplayName"", ""Category"", ""Description"", ""IsActive"", ""SortOrder"", ""IsExternalCall"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"")
                VALUES
                    (gen_random_uuid(), 'SearchProfiles', 'Buscar Perfiles', 'Search', 'Busca perfiles por nombre, tipo, o palabras clave', true, 1, false, NOW(), NOW(), false),
                    (gen_random_uuid(), 'SearchPosts', 'Buscar Publicaciones', 'Search', 'Busca publicaciones por contenido', true, 2, false, NOW(), NOW(), false),
                    (gen_random_uuid(), 'GetPostDetails', 'Ver Publicación', 'Search', 'Obtiene detalles completos de una publicación', true, 3, false, NOW(), NOW(), false),
                    (gen_random_uuid(), 'FindBusinesses', 'Buscar Negocios', 'Search', 'Busca negocios por categoría y ubicación', true, 4, false, NOW(), NOW(), false),
                    (gen_random_uuid(), 'FollowProfile', 'Seguir Perfil', 'Profile', 'Sigue a un perfil', true, 10, false, NOW(), NOW(), false),
                    (gen_random_uuid(), 'UnfollowProfile', 'Dejar de Seguir', 'Profile', 'Deja de seguir a un perfil', true, 11, false, NOW(), NOW(), false),
                    (gen_random_uuid(), 'GetMyProfile', 'Mi Perfil', 'Profile', 'Obtiene información del perfil activo', true, 12, false, NOW(), NOW(), false),
                    (gen_random_uuid(), 'SearchNearbyProfiles', 'Perfiles Cercanos', 'Location', 'Busca perfiles cerca de una ubicación', true, 20, false, NOW(), NOW(), false),
                    (gen_random_uuid(), 'SearchNearbyPosts', 'Publicaciones Cercanas', 'Location', 'Busca publicaciones cerca de una ubicación', true, 21, false, NOW(), NOW(), false),
                    (gen_random_uuid(), 'CalculateDistance', 'Calcular Distancia', 'Location', 'Calcula distancia entre dos puntos', true, 22, false, NOW(), NOW(), false),
                    (gen_random_uuid(), 'GetAddressFromCoordinates', 'Geocodificación Inversa', 'Location', 'Obtiene dirección desde coordenadas GPS', true, 23, true, NOW(), NOW(), false),
                    (gen_random_uuid(), 'GetCoordinatesFromAddress', 'Geocodificación', 'Location', 'Obtiene coordenadas desde una dirección', true, 24, true, NOW(), NOW(), false),
                    (gen_random_uuid(), 'SearchNearMe', 'Buscar Cerca de Mí', 'Location', 'Busca contenido cerca del usuario', true, 25, false, NOW(), NOW(), false),
                    (gen_random_uuid(), 'GetCurrentLocationStatus', 'Estado de Ubicación', 'Location', 'Verifica el estado del GPS', true, 26, false, NOW(), NOW(), false),
                    (gen_random_uuid(), 'GetContactInfo', 'Información de Contacto', 'Business', 'Obtiene teléfono, email, WhatsApp de un negocio', true, 30, false, NOW(), NOW(), false),
                    (gen_random_uuid(), 'GetBusinessHours', 'Horarios de Atención', 'Business', 'Obtiene horarios de un negocio', true, 31, false, NOW(), NOW(), false),
                    (gen_random_uuid(), 'GetDirections', 'Direcciones', 'Business', 'Obtiene direcciones hacia un negocio', true, 32, true, NOW(), NOW(), false),
                    (gen_random_uuid(), 'GetProcedureInfo', 'Información de Trámites', 'Government', 'Obtiene requisitos y pasos para trámites gubernamentales', true, 40, false, NOW(), NOW(), false);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentConfigurations");

            migrationBuilder.DropTable(
                name: "AgentTools");

            migrationBuilder.DropTable(
                name: "Sivar_CategoryDefinitions");

            migrationBuilder.DropTable(
                name: "Sivar_ProfileBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_CategoryKeys_Gin",
                table: "Sivar_Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Posts_CategoryKeys_Gin",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "CategoryKeys",
                table: "Sivar_Profiles");

            migrationBuilder.DropColumn(
                name: "CategoryKeys",
                table: "Sivar_Posts");

            migrationBuilder.DropColumn(
                name: "ProcedureMetadataJson",
                table: "Sivar_Posts");
        }
    }
}
