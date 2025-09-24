using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITAssetManager.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddLabNumberToLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LabNumber",
                table: "Locations",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LabNumber",
                table: "Locations");
        }
    }
}
