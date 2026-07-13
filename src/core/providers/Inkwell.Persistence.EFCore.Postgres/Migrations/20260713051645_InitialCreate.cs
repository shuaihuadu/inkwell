using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inkwell.Persistence.EFCore.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    content_markdown = table.Column<string>(type: "text", nullable: false),
                    reference_file_uris = table.Column<string>(type: "jsonb", nullable: false),
                    asset_file_uris = table.Column<string>(type: "jsonb", nullable: false),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_skills", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agent_tools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    parameters_json_schema = table.Column<string>(type: "jsonb", nullable: false),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_tools", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_published_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    draft_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    latest_published_version_number = table.Column<int>(type: "integer", nullable: false),
                    is_shared = table.Column<bool>(type: "boolean", nullable: false),
                    shared_revoked_by_admin_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    is_super = table.Column<bool>(type: "boolean", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    failed_unlock_attempts = table.Column<int>(type: "integer", nullable: false),
                    last_login_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agent_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    change_summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_versions_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_session",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    session_state = table.Column<string>(type: "jsonb", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_session", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_session_agent_versions_agent_version_id",
                        column: x => x.agent_version_id,
                        principalTable: "agent_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agent_chat_message",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message = table.Column<string>(type: "jsonb", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_chat_message", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_chat_message_agent_session_session_id",
                        column: x => x.session_id,
                        principalTable: "agent_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_chat_message_session_id_sequence_number",
                table: "agent_chat_message",
                columns: new[] { "session_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_agent_session_agent_id_owner_user_id",
                table: "agent_session",
                columns: new[] { "agent_id", "owner_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_session_agent_version_id",
                table: "agent_session",
                column: "agent_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_session_owner_user_id",
                table: "agent_session",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_tools_name",
                table: "agent_tools",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_agent_versions_agent_id_version_number",
                table: "agent_versions",
                columns: new[] { "agent_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_agents_current_published_version_id",
                table: "agents",
                column: "current_published_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_agents_draft_version_id",
                table: "agents",
                column: "draft_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_agents_is_shared",
                table: "agents",
                column: "is_shared");

            migrationBuilder.CreateIndex(
                name: "ix_agents_owner_user_id",
                table: "agents",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_is_locked",
                table: "users",
                column: "is_locked");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_chat_message");

            migrationBuilder.DropTable(
                name: "agent_skills");

            migrationBuilder.DropTable(
                name: "agent_tools");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "agent_session");

            migrationBuilder.DropTable(
                name: "agent_versions");

            migrationBuilder.DropTable(
                name: "agents");
        }
    }
}
