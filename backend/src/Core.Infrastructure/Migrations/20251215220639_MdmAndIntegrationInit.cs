using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MdmAndIntegrationInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "integration");

            migrationBuilder.EnsureSchema(
                name: "mdm");

            migrationBuilder.CreateTable(
                name: "component2020_connection",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MdbPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EncryptedPassword = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastTestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastTestMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component2020_connection", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "component2020_sync_run",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProcessedCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    CountersJson = table.Column<string>(type: "jsonb", nullable: true),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component2020_sync_run", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "item_attributes",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_attributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "item_groups",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_item_groups_item_groups_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "mdm",
                        principalTable: "item_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_sequences",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemKind = table.Column<int>(type: "integer", nullable: false),
                    Prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NextNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_sequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "unit_of_measures",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unit_of_measures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "component2020_sync_error",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SyncRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalEntity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component2020_sync_error", x => x.Id);
                    table.ForeignKey(
                        name: "FK_component2020_sync_error_component2020_sync_run_SyncRunId",
                        column: x => x.SyncRunId,
                        principalSchema: "integration",
                        principalTable: "component2020_sync_run",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "items",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ItemKind = table.Column<int>(type: "integer", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalSystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsEskd = table.Column<bool>(type: "boolean", nullable: false),
                    IsEskdDocument = table.Column<bool>(type: "boolean", nullable: true),
                    ManufacturerPartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ItemGroupId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_items_item_groups_ItemGroupId",
                        column: x => x.ItemGroupId,
                        principalSchema: "mdm",
                        principalTable: "item_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_items_unit_of_measures_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalSchema: "mdm",
                        principalTable: "unit_of_measures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_attribute_values",
                schema: "mdm",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_attribute_values", x => new { x.ItemId, x.AttributeId });
                    table.ForeignKey(
                        name: "FK_item_attribute_values_item_attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalSchema: "mdm",
                        principalTable: "item_attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_item_attribute_values_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "mdm",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Inn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Kpp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_component2020_sync_error_SyncRunId",
                schema: "integration",
                table: "component2020_sync_error",
                column: "SyncRunId");

            migrationBuilder.CreateTable(
                name: "sync_cursors",
                schema: "integration",
                columns: table => new
                {
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEntity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastProcessedKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastSyncAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_cursors", x => new { x.ConnectionId, x.SourceEntity });
                });

            migrationBuilder.CreateTable(
                name: "component2020_sync_schedules",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CronExpression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Scope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component2020_sync_schedules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_item_attribute_values_AttributeId",
                schema: "mdm",
                table: "item_attribute_values",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_item_attributes_Code",
                schema: "mdm",
                table: "item_attributes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_groups_Code",
                schema: "mdm",
                table: "item_groups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_groups_ParentId",
                schema: "mdm",
                table: "item_groups",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_item_sequences_ItemKind",
                schema: "mdm",
                table: "item_sequences",
                column: "ItemKind",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_Code",
                schema: "mdm",
                table: "items",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_ItemGroupId",
                schema: "mdm",
                table: "items",
                column: "ItemGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_items_UnitOfMeasureId",
                schema: "mdm",
                table: "items",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_unit_of_measures_Code",
                schema: "mdm",
                table: "unit_of_measures",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_Code",
                schema: "mdm",
                table: "suppliers",
                column: "Code",
                unique: true);

            // Seed basic unit of measures - Using all lowercase column names for PostgreSQL
            migrationBuilder.Sql(@"
                INSERT INTO mdm.unit_of_measures (""Id"", ""Code"", ""Name"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
                SELECT '33333333-3333-3333-3333-333333333333'::uuid, 'EA', 'Each', TRUE, NOW(), NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM mdm.unit_of_measures WHERE ""Code"" = 'EA'
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO mdm.unit_of_measures (""Id"", ""Code"", ""Name"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
                SELECT '44444444-4444-4444-4444-444444444444'::uuid, 'шт', 'Штука', TRUE, NOW(), NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM mdm.unit_of_measures WHERE ""Code"" = 'шт'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM mdm.unit_of_measures
                WHERE code IN ('EA', 'шт');
            ");

            migrationBuilder.DropTable(
                name: "component2020_sync_schedules",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "sync_cursors",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "component2020_connection",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "component2020_sync_error",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "item_attribute_values",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "suppliers",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "item_sequences",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "component2020_sync_run",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "item_attributes",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "items",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "item_groups",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "unit_of_measures",
                schema: "mdm");
        }
    }
}
