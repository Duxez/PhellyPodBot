using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeGameBot.Migrations
{
    /// <inheritdoc />
    public partial class ManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Pods_PodId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PodId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PodId",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "PodUser",
                columns: table => new
                {
                    PodsId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PodUser", x => new { x.PodsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_PodUser_Pods_PodsId",
                        column: x => x.PodsId,
                        principalTable: "Pods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PodUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PodUser_UsersId",
                table: "PodUser",
                column: "UsersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PodUser");

            migrationBuilder.AddColumn<int>(
                name: "PodId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PodId",
                table: "Users",
                column: "PodId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Pods_PodId",
                table: "Users",
                column: "PodId",
                principalTable: "Pods",
                principalColumn: "Id");
        }
    }
}
