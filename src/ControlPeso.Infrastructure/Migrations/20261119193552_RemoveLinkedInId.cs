using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlPeso.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLinkedInId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkedInId",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkedInId",
                table: "Users",
                type: "text",
                nullable: true);
        }
    }
}
