using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeGameBot.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AlertEnabled",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlertEnabled",
                table: "Users");
        }
    }
}
