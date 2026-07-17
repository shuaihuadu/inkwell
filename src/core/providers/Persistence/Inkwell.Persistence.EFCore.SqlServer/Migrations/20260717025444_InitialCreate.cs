// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inkwell.Persistence.EFCore.SqlServer.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AgentSkills",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ContentMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ReferenceFileUris = table.Column<string>(type: "json", nullable: false),
                AssetFileUris = table.Column<string>(type: "json", nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgentSkills", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AgentTools",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ParametersJsonSchema = table.Column<string>(type: "json", nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgentTools", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Agents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                AvatarUri = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                BuildOptions = table.Column<string>(type: "json", nullable: false),
                CurrentPublishedVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LatestPublishedVersionNumber = table.Column<int>(type: "int", nullable: false),
                IsShared = table.Column<bool>(type: "bit", nullable: false),
                SharedRevokedByAdminTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Agents", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsSuper = table.Column<bool>(type: "bit", nullable: false),
                IsLocked = table.Column<bool>(type: "bit", nullable: false),
                FailedUnlockAttempts = table.Column<int>(type: "int", nullable: false),
                LastLoginTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AgentVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VersionNumber = table.Column<int>(type: "int", nullable: false),
                Snapshot = table.Column<string>(type: "json", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ChangeSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                PublishedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgentVersions", x => x.Id);
                table.UniqueConstraint("AK_AgentVersions_AgentId_Id", x => new { x.AgentId, x.Id });
                table.ForeignKey(
                    name: "FK_AgentVersions_Agents_AgentId",
                    column: x => x.AgentId,
                    principalTable: "Agents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AgentConversations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SessionKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AgentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                LastCommittedRunId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                LastActivityTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgentConversations", x => x.Id);
                table.ForeignKey(
                    name: "FK_AgentConversations_AgentVersions_AgentId_AgentVersionId",
                    columns: x => new { x.AgentId, x.AgentVersionId },
                    principalTable: "AgentVersions",
                    principalColumns: new[] { "AgentId", "Id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AgentChatMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RunId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                RunMessageIndex = table.Column<int>(type: "int", nullable: true),
                Message = table.Column<string>(type: "json", nullable: false),
                SequenceNumber = table.Column<int>(type: "int", nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgentChatMessages", x => x.Id);
                table.ForeignKey(
                    name: "FK_AgentChatMessages_AgentConversations_ConversationId",
                    column: x => x.ConversationId,
                    principalTable: "AgentConversations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AgentSessionStates",
            columns: table => new
            {
                ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SerializedState = table.Column<string>(type: "json", nullable: false),
                Revision = table.Column<long>(type: "bigint", nullable: false),
                LastRunId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                UpdatedTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgentSessionStates", x => x.ConversationId);
                table.ForeignKey(
                    name: "FK_AgentSessionStates_AgentConversations_ConversationId",
                    column: x => x.ConversationId,
                    principalTable: "AgentConversations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AgentChatMessages_ConversationId_SequenceNumber",
            table: "AgentChatMessages",
            columns: new[] { "ConversationId", "SequenceNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AgentConversations_AgentId_AgentVersionId",
            table: "AgentConversations",
            columns: new[] { "AgentId", "AgentVersionId" });

        migrationBuilder.CreateIndex(
            name: "IX_AgentConversations_AgentId_OwnerUserId_LastActivityTime",
            table: "AgentConversations",
            columns: new[] { "AgentId", "OwnerUserId", "LastActivityTime" });

        migrationBuilder.CreateIndex(
            name: "IX_AgentConversations_AgentVersionId",
            table: "AgentConversations",
            column: "AgentVersionId");

        migrationBuilder.CreateIndex(
            name: "IX_AgentConversations_OwnerUserId",
            table: "AgentConversations",
            column: "OwnerUserId");

        migrationBuilder.CreateIndex(
            name: "IX_AgentConversations_SessionKey",
            table: "AgentConversations",
            column: "SessionKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AgentTools_Name",
            table: "AgentTools",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AgentVersions_AgentId_VersionNumber",
            table: "AgentVersions",
            columns: new[] { "AgentId", "VersionNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Agents_CurrentPublishedVersionId",
            table: "Agents",
            column: "CurrentPublishedVersionId");

        migrationBuilder.CreateIndex(
            name: "IX_Agents_IsShared",
            table: "Agents",
            column: "IsShared");

        migrationBuilder.CreateIndex(
            name: "IX_Agents_OwnerUserId",
            table: "Agents",
            column: "OwnerUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_IsLocked",
            table: "Users",
            column: "IsLocked");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Username",
            table: "Users",
            column: "Username",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AgentChatMessages");

        migrationBuilder.DropTable(
            name: "AgentSessionStates");

        migrationBuilder.DropTable(
            name: "AgentSkills");

        migrationBuilder.DropTable(
            name: "AgentTools");

        migrationBuilder.DropTable(
            name: "Users");

        migrationBuilder.DropTable(
            name: "AgentConversations");

        migrationBuilder.DropTable(
            name: "AgentVersions");

        migrationBuilder.DropTable(
            name: "Agents");
    }
}
