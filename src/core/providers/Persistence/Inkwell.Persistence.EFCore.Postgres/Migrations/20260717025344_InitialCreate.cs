// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inkwell.Persistence.EFCore.Postgres.Migrations;

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
                name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                avatar_uri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                instructions = table.Column<string>(type: "text", nullable: true),
                build_options = table.Column<string>(type: "jsonb", nullable: false),
                current_published_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                latest_published_version_number = table.Column<int>(type: "integer", nullable: false),
                is_shared = table.Column<bool>(type: "boolean", nullable: false),
                shared_revoked_by_admin_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                snapshot = table.Column<string>(type: "jsonb", nullable: false),
                created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                change_summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                published_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_agent_versions", x => x.id);
                table.UniqueConstraint("ak_agent_versions_agent_id_id", x => new { x.agent_id, x.id });
                table.ForeignKey(
                    name: "fk_agent_versions_agents_agent_id",
                    column: x => x.agent_id,
                    principalTable: "agents",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "agent_conversations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                session_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                agent_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                last_committed_run_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                last_activity_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_agent_conversations", x => x.id);
                table.ForeignKey(
                    name: "fk_agent_conversations_agent_versions_agent_id_agent_version_id",
                    columns: x => new { x.agent_id, x.agent_version_id },
                    principalTable: "agent_versions",
                    principalColumns: new[] { "agent_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "agent_chat_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                run_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                run_message_index = table.Column<int>(type: "integer", nullable: true),
                message = table.Column<string>(type: "jsonb", nullable: false),
                sequence_number = table.Column<int>(type: "integer", nullable: false),
                created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_agent_chat_messages", x => x.id);
                table.ForeignKey(
                    name: "fk_agent_chat_messages_agent_conversations_conversation_id",
                    column: x => x.conversation_id,
                    principalTable: "agent_conversations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "agent_session_states",
            columns: table => new
            {
                conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                serialized_state = table.Column<string>(type: "jsonb", nullable: false),
                revision = table.Column<long>(type: "bigint", nullable: false),
                last_run_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_agent_session_states", x => x.conversation_id);
                table.ForeignKey(
                    name: "fk_agent_session_states_agent_conversations_conversation_id",
                    column: x => x.conversation_id,
                    principalTable: "agent_conversations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_agent_chat_messages_conversation_id_sequence_number",
            table: "agent_chat_messages",
            columns: new[] { "conversation_id", "sequence_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_agent_conversations_agent_id_agent_version_id",
            table: "agent_conversations",
            columns: new[] { "agent_id", "agent_version_id" });

        migrationBuilder.CreateIndex(
            name: "ix_agent_conversations_agent_id_owner_user_id_last_activity_ti",
            table: "agent_conversations",
            columns: new[] { "agent_id", "owner_user_id", "last_activity_time" });

        migrationBuilder.CreateIndex(
            name: "ix_agent_conversations_agent_version_id",
            table: "agent_conversations",
            column: "agent_version_id");

        migrationBuilder.CreateIndex(
            name: "ix_agent_conversations_owner_user_id",
            table: "agent_conversations",
            column: "owner_user_id");

        migrationBuilder.CreateIndex(
            name: "ix_agent_conversations_session_key",
            table: "agent_conversations",
            column: "session_key",
            unique: true);

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
            name: "agent_chat_messages");

        migrationBuilder.DropTable(
            name: "agent_session_states");

        migrationBuilder.DropTable(
            name: "agent_skills");

        migrationBuilder.DropTable(
            name: "agent_tools");

        migrationBuilder.DropTable(
            name: "users");

        migrationBuilder.DropTable(
            name: "agent_conversations");

        migrationBuilder.DropTable(
            name: "agent_versions");

        migrationBuilder.DropTable(
            name: "agents");
    }
}
