using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEngineeringModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "engineering");

            migrationBuilder.CreateTable(
                name: "products",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "text", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_products_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "mdm",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_versions",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_code = table.Column<string>(type: "text", maxLength: 50, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_bom_versions_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "engineering",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bom_lines",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bom_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    unit_of_measure = table.Column<string>(type: "text", maxLength: 20, nullable: true),
                    position_no = table.Column<string>(type: "text", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_bom_lines_bom_versions_bom_version_id",
                        column: x => x.bom_version_id,
                        principalSchema: "engineering",
                        principalTable: "bom_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bom_lines_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "mdm",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_lines_items_parent_item_id",
                        column: x => x.parent_item_id,
                        principalSchema: "mdm",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_operations",
                schema: "engineering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bom_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "text", maxLength: 200, nullable: false),
                    area_name = table.Column<string>(type: "text", maxLength: 100, nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_operations", x => x.id);
                    table.ForeignKey(
                        name: "FK_bom_operations_bom_versions_bom_version_id",
                        column: x => x.bom_version_id,
                        principalSchema: "engineering",
                        principalTable: "bom_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bom_lines_bom_version_id",
                schema: "engineering",
                table: "bom_lines",
                column: "bom_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_bom_lines_item_id",
                schema: "engineering",
                table: "bom_lines",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_bom_lines_parent_item_id",
                schema: "engineering",
                table: "bom_lines",
                column: "parent_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_bom_operations_bom_version_id",
                schema: "engineering",
                table: "bom_operations",
                column: "bom_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_bom_versions_product_id",
                schema: "engineering",
                table: "bom_versions",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_item_id",
                schema: "engineering",
                table: "products",
                column: "item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bom_lines",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "bom_operations",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "bom_versions",
                schema: "engineering");

            migrationBuilder.DropTable(
                name: "products",
                schema: "engineering");
        }
    }
}
