using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMdmDictionaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "body_types",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Pins = table.Column<int>(type: "integer", nullable: true),
                    Smt = table.Column<int>(type: "integer", nullable: true),
                    Photo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FootPrintPath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FootprintRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FootprintRef2 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FootPrintRef3 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalSystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_body_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "currencies",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalSystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "manufacturers",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Site = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalSystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manufacturers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "parameter_sets",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    P0Id = table.Column<int>(type: "integer", nullable: true),
                    P1Id = table.Column<int>(type: "integer", nullable: true),
                    P2Id = table.Column<int>(type: "integer", nullable: true),
                    P3Id = table.Column<int>(type: "integer", nullable: true),
                    P4Id = table.Column<int>(type: "integer", nullable: true),
                    P5Id = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalSystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parameter_sets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "symbols",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SymbolValue = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Photo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    LibraryPath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    LibraryRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalSystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_symbols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "technical_parameters",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UnitId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalSystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_technical_parameters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_body_types_Code",
                schema: "integration",
                table: "body_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_body_types_ExternalSystem_ExternalId",
                schema: "integration",
                table: "body_types",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_currencies_Code",
                schema: "integration",
                table: "currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_currencies_ExternalSystem_ExternalId",
                schema: "integration",
                table: "currencies",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_manufacturers_Code",
                schema: "integration",
                table: "manufacturers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_manufacturers_ExternalSystem_ExternalId",
                schema: "integration",
                table: "manufacturers",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parameter_sets_Code",
                schema: "integration",
                table: "parameter_sets",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parameter_sets_ExternalSystem_ExternalId",
                schema: "integration",
                table: "parameter_sets",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_symbols_Code",
                schema: "integration",
                table: "symbols",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_symbols_ExternalSystem_ExternalId",
                schema: "integration",
                table: "symbols",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_technical_parameters_Code",
                schema: "integration",
                table: "technical_parameters",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_technical_parameters_ExternalSystem_ExternalId",
                schema: "integration",
                table: "technical_parameters",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "body_types",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "currencies",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "manufacturers",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "parameter_sets",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "symbols",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "technical_parameters",
                schema: "integration");
        }
    }
}
