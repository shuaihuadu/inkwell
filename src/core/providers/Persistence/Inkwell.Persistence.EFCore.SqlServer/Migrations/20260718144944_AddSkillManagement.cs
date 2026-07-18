// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inkwell.Persistence.EFCore.SqlServer.Migrations;

/// <inheritdoc />
public partial class AddSkillManagement : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "OwnerUserId",
            table: "AgentSkills",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "AgentSkills",
            type: "varbinary(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: new byte[16]);

        migrationBuilder.AddColumn<string>(
            name: "ScriptFileUris",
            table: "AgentSkills",
            type: "json",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.Sql(
            "UPDATE [AgentSkills] SET [OwnerUserId] = (SELECT TOP 1 [Id] FROM [Users] ORDER BY [CreatedTime]) WHERE [OwnerUserId] IS NULL");

        migrationBuilder.AlterColumn<Guid>(
            name: "OwnerUserId",
            table: "AgentSkills",
            type: "uniqueidentifier",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_AgentSkills_OwnerUserId",
            table: "AgentSkills",
            column: "OwnerUserId");

        migrationBuilder.AddForeignKey(
            name: "FK_AgentSkills_Users_OwnerUserId",
            table: "AgentSkills",
            column: "OwnerUserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AgentSkills_Users_OwnerUserId",
            table: "AgentSkills");

        migrationBuilder.DropIndex(
            name: "IX_AgentSkills_OwnerUserId",
            table: "AgentSkills");

        migrationBuilder.DropColumn(
            name: "OwnerUserId",
            table: "AgentSkills");

        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "AgentSkills");

        migrationBuilder.DropColumn(
            name: "ScriptFileUris",
            table: "AgentSkills");
    }
}
