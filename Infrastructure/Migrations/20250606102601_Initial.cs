using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermissionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeForename = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeSurname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PermissionTypeId = table.Column<int>(type: "int", nullable: false),
                    PermissionDate = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_PermissionTypes_PermissionTypeId",
                        column: x => x.PermissionTypeId,
                        principalTable: "PermissionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PermissionTypes",
                columns: new[] { "Id", "Description" },
                values: new object[] { 1, "First type" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "EmployeeForename", "EmployeeSurname", "PermissionDate", "PermissionTypeId" },
                values: new object[] { 1, "Forename", "Surname", new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1 });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_PermissionTypeId",
                table: "Permissions",
                column: "PermissionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTypes_Description",
                table: "PermissionTypes",
                column: "Description");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "PermissionTypes");
        }
    }
}
