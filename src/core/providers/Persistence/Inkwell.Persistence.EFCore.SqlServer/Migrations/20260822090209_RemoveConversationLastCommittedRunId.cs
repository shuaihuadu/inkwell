using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inkwell.Persistence.EFCore.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveConversationLastCommittedRunId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCommittedRunId",
                table: "AgentConversations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastCommittedRunId",
                table: "AgentConversations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
