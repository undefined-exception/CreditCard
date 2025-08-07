using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditCard.Data.Migrations
{
    /// <inheritdoc />
    public partial class addexternalIdcolum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalApplicationId",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalApplicationId",
                table: "Applications");
        }
    }
}
