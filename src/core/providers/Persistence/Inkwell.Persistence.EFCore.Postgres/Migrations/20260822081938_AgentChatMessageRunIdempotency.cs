using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inkwell.Persistence.EFCore.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AgentChatMessageRunIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_agent_chat_messages_conversation_id_run_id_run_message_index",
                table: "agent_chat_messages",
                columns: new[] { "conversation_id", "run_id", "run_message_index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_agent_chat_messages_conversation_id_run_id_run_message_index",
                table: "agent_chat_messages");
        }
    }
}
