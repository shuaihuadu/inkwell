using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inkwell.Persistence.EFCore.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAgentSessionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentSessionStates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentSessionStates",
                columns: table => new
                {
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastRunId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    SerializedState = table.Column<string>(type: "json", nullable: false),
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
        }
    }
}
