using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMAS.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeMetaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeneratedPassword",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPasswordChanged",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PasswordResetCount",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneratedPassword",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsPasswordChanged",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PasswordResetCount",
                table: "Employees");
        }
    }
}
