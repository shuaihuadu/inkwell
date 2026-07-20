using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inkwell.Persistence.EFCore.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAgentSessionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_session_states");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_session_states",
                columns: table => new
                {
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_run_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    revision = table.Column<long>(type: "bigint", nullable: false),
                    serialized_state = table.Column<string>(type: "jsonb", nullable: false),
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
        }
    }
}
