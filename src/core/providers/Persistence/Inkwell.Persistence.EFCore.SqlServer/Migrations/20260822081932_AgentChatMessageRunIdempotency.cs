using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inkwell.Persistence.EFCore.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AgentChatMessageRunIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AgentChatMessages_ConversationId_RunId_RunMessageIndex",
                table: "AgentChatMessages",
                columns: new[] { "ConversationId", "RunId", "RunMessageIndex" },
                unique: true,
                filter: "[RunId] IS NOT NULL AND [RunMessageIndex] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentChatMessages_ConversationId_RunId_RunMessageIndex",
                table: "AgentChatMessages");
        }
    }
}
