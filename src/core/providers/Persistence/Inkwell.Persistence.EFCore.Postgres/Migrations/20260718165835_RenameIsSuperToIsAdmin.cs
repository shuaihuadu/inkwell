using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inkwell.Persistence.EFCore.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class RenameIsSuperToIsAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "is_super",
                table: "users",
                newName: "is_admin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "is_admin",
                table: "users",
                newName: "is_super");
        }
    }
}
