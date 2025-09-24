using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITAssetManager.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftwareLicenseTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeatsAssigned",
                table: "SoftwareProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SeatsPurchased",
                table: "SoftwareProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "AssignedOn",
                table: "LicenseAssignments",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedDate",
                table: "LicenseAssignments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "LicenseAssignments",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "LicenseAssignments",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SoftwareLicenseId",
                table: "LicenseAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnassignedOn",
                table: "LicenseAssignments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SoftwareLicenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SoftwareProductId = table.Column<int>(type: "int", nullable: false),
                    LicenseKey = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PurchaseDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SeatsPurchased = table.Column<int>(type: "int", nullable: true),
                    SeatsAssigned = table.Column<int>(type: "int", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Vendor = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareLicenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoftwareLicenses_SoftwareProducts_SoftwareProductId",
                        column: x => x.SoftwareProductId,
                        principalTable: "SoftwareProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LicenseAssignments_SoftwareLicenseId",
                table: "LicenseAssignments",
                column: "SoftwareLicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenses_SoftwareProductId",
                table: "SoftwareLicenses",
                column: "SoftwareProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_LicenseAssignments_SoftwareLicenses_SoftwareLicenseId",
                table: "LicenseAssignments",
                column: "SoftwareLicenseId",
                principalTable: "SoftwareLicenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LicenseAssignments_SoftwareLicenses_SoftwareLicenseId",
                table: "LicenseAssignments");

            migrationBuilder.DropTable(
                name: "SoftwareLicenses");

            migrationBuilder.DropIndex(
                name: "IX_LicenseAssignments_SoftwareLicenseId",
                table: "LicenseAssignments");

            migrationBuilder.DropColumn(
                name: "SeatsAssigned",
                table: "SoftwareProducts");

            migrationBuilder.DropColumn(
                name: "SeatsPurchased",
                table: "SoftwareProducts");

            migrationBuilder.DropColumn(
                name: "AssignedDate",
                table: "LicenseAssignments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "LicenseAssignments");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "LicenseAssignments");

            migrationBuilder.DropColumn(
                name: "SoftwareLicenseId",
                table: "LicenseAssignments");

            migrationBuilder.DropColumn(
                name: "UnassignedOn",
                table: "LicenseAssignments");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AssignedOn",
                table: "LicenseAssignments",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);
        }
    }
}
