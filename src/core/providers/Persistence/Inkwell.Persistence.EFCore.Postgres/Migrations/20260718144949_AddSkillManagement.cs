// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inkwell.Persistence.EFCore.Postgres.Migrations;

/// <inheritdoc />
public partial class AddSkillManagement : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "owner_user_id",
            table: "agent_skills",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "row_version",
            table: "agent_skills",
            type: "bytea",
            maxLength: 16,
            nullable: false,
            defaultValue: new byte[16]);

        migrationBuilder.AddColumn<string>(
            name: "script_file_uris",
            table: "agent_skills",
            type: "jsonb",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.Sql(
            "UPDATE agent_skills SET owner_user_id = (SELECT id FROM users ORDER BY created_time LIMIT 1) WHERE owner_user_id IS NULL");

        migrationBuilder.AlterColumn<Guid>(
            name: "owner_user_id",
            table: "agent_skills",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_agent_skills_owner_user_id",
            table: "agent_skills",
            column: "owner_user_id");

        migrationBuilder.AddForeignKey(
            name: "fk_agent_skills_users_owner_user_id",
            table: "agent_skills",
            column: "owner_user_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_agent_skills_users_owner_user_id",
            table: "agent_skills");

        migrationBuilder.DropIndex(
            name: "ix_agent_skills_owner_user_id",
            table: "agent_skills");

        migrationBuilder.DropColumn(
            name: "owner_user_id",
            table: "agent_skills");

        migrationBuilder.DropColumn(
            name: "row_version",
            table: "agent_skills");

        migrationBuilder.DropColumn(
            name: "script_file_uris",
            table: "agent_skills");
    }
}
