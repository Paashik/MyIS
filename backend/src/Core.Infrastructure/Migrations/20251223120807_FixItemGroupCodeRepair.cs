using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixItemGroupCodeRepair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Repair migration: some environments may have schema drift (e.g. old columns still exist even though the model no longer uses them).
            // 1) Backfill data into integration.external_entity_links before dropping legacy columns/tables.
            migrationBuilder.Sql(@"
INSERT INTO integration.external_entity_links (""Id"", ""EntityType"", ""EntityId"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"", ""SourceType"", ""SyncedAt"", ""CreatedAt"", ""UpdatedAt"")
SELECT uuid_generate_v4(), 'Counterparty', ""CounterpartyId"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"", ""SourceType"", ""SyncedAt"", ""CreatedAt"", ""UpdatedAt""
FROM mdm.counterparty_external_links
ON CONFLICT (""EntityType"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"") DO NOTHING;
");

            migrationBuilder.Sql(@"
INSERT INTO integration.external_entity_links (""Id"", ""EntityType"", ""EntityId"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"", ""SourceType"", ""SyncedAt"", ""CreatedAt"", ""UpdatedAt"")
SELECT uuid_generate_v4(), 'UnitOfMeasure', ""Id"", ""ExternalSystem"", 'Unit', ""ExternalId"", NULL, ""SyncedAt"", ""CreatedAt"", ""UpdatedAt""
FROM mdm.unit_of_measures
WHERE ""ExternalSystem"" IS NOT NULL AND ""ExternalId"" IS NOT NULL
ON CONFLICT (""EntityType"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"") DO NOTHING;
");

            migrationBuilder.Sql(@"
INSERT INTO integration.external_entity_links (""Id"", ""EntityType"", ""EntityId"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"", ""SourceType"", ""SyncedAt"", ""CreatedAt"", ""UpdatedAt"")
SELECT uuid_generate_v4(), 'Item', ""Id"", ""ExternalSystem"", CASE
    WHEN ""ExternalSystem"" = 'Component2020Product' THEN 'Product'
    WHEN ""ItemKind"" = 3 THEN 'Product'
    ELSE 'Component'
END, ""ExternalId"", NULL, ""SyncedAt"", ""CreatedAt"", ""UpdatedAt""
FROM mdm.items
WHERE ""ExternalSystem"" IS NOT NULL AND ""ExternalId"" IS NOT NULL
ON CONFLICT (""EntityType"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"") DO NOTHING;
");

            migrationBuilder.Sql(@"
INSERT INTO integration.external_entity_links (""Id"", ""EntityType"", ""EntityId"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"", ""SourceType"", ""SyncedAt"", ""CreatedAt"", ""UpdatedAt"")
SELECT uuid_generate_v4(), 'Manufacturer', ""Id"", ""ExternalSystem"", 'Manufact', ""ExternalId"", NULL, ""SyncedAt"", ""CreatedAt"", ""UpdatedAt""
FROM mdm.manufacturers
WHERE ""ExternalSystem"" IS NOT NULL AND ""ExternalId"" IS NOT NULL
ON CONFLICT (""EntityType"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"") DO NOTHING;
");

            migrationBuilder.Sql(@"
INSERT INTO integration.external_entity_links (""Id"", ""EntityType"", ""EntityId"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"", ""SourceType"", ""SyncedAt"", ""CreatedAt"", ""UpdatedAt"")
SELECT uuid_generate_v4(), 'BodyType', ""Id"", ""ExternalSystem"", 'Body', ""ExternalId"", NULL, ""SyncedAt"", ""CreatedAt"", ""UpdatedAt""
FROM mdm.body_types
WHERE ""ExternalSystem"" IS NOT NULL AND ""ExternalId"" IS NOT NULL
ON CONFLICT (""EntityType"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"") DO NOTHING;
");

            migrationBuilder.Sql(@"
INSERT INTO integration.external_entity_links (""Id"", ""EntityType"", ""EntityId"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"", ""SourceType"", ""SyncedAt"", ""CreatedAt"", ""UpdatedAt"")
SELECT uuid_generate_v4(), 'Currency', ""Id"", ""ExternalSystem"", 'Curr', ""ExternalId"", NULL, ""SyncedAt"", ""CreatedAt"", ""UpdatedAt""
FROM mdm.currencies
WHERE ""ExternalSystem"" IS NOT NULL AND ""ExternalId"" IS NOT NULL
ON CONFLICT (""EntityType"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"") DO NOTHING;
");

            migrationBuilder.Sql(@"
INSERT INTO integration.external_entity_links (""Id"", ""EntityType"", ""EntityId"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"", ""SourceType"", ""SyncedAt"", ""CreatedAt"", ""UpdatedAt"")
SELECT uuid_generate_v4(), 'TechnicalParameter', ""Id"", ""ExternalSystem"", 'NPar', ""ExternalId"", NULL, ""SyncedAt"", ""CreatedAt"", ""UpdatedAt""
FROM mdm.technical_parameters
WHERE ""ExternalSystem"" IS NOT NULL AND ""ExternalId"" IS NOT NULL
ON CONFLICT (""EntityType"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"") DO NOTHING;
");

            migrationBuilder.Sql(@"
INSERT INTO integration.external_entity_links (""Id"", ""EntityType"", ""EntityId"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"", ""SourceType"", ""SyncedAt"", ""CreatedAt"", ""UpdatedAt"")
SELECT uuid_generate_v4(), 'ParameterSet', ""Id"", ""ExternalSystem"", 'SPar', ""ExternalId"", NULL, ""SyncedAt"", ""CreatedAt"", ""UpdatedAt""
FROM mdm.parameter_sets
WHERE ""ExternalSystem"" IS NOT NULL AND ""ExternalId"" IS NOT NULL
ON CONFLICT (""EntityType"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"") DO NOTHING;
");

            migrationBuilder.Sql(@"
INSERT INTO integration.external_entity_links (""Id"", ""EntityType"", ""EntityId"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"", ""SourceType"", ""SyncedAt"", ""CreatedAt"", ""UpdatedAt"")
SELECT uuid_generate_v4(), 'Symbol', ""Id"", ""ExternalSystem"", 'Symbol', ""ExternalId"", NULL, ""SyncedAt"", ""CreatedAt"", ""UpdatedAt""
FROM mdm.symbols
WHERE ""ExternalSystem"" IS NOT NULL AND ""ExternalId"" IS NOT NULL
ON CONFLICT (""EntityType"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"") DO NOTHING;
");

            // 2) Repair legacy ItemGroup "Code" column that can still exist (NOT NULL) in drifted DBs.
            // The domain model no longer uses it, and it breaks imports with `NULL violates NOT NULL`.
            migrationBuilder.Sql(@"ALTER TABLE mdm.item_groups DROP COLUMN IF EXISTS ""Code"";");

            migrationBuilder.DropTable(
                name: "counterparty_external_links",
                schema: "mdm");

            migrationBuilder.DropIndex(
                name: "IX_unit_of_measures_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "unit_of_measures");

            migrationBuilder.DropIndex(
                name: "IX_technical_parameters_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "technical_parameters");

            migrationBuilder.DropIndex(
                name: "IX_symbols_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "symbols");

            migrationBuilder.DropIndex(
                name: "IX_parameter_sets_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "parameter_sets");

            migrationBuilder.DropIndex(
                name: "IX_manufacturers_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "manufacturers");

            migrationBuilder.DropIndex(
                name: "IX_items_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_currencies_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "currencies");

            migrationBuilder.DropIndex(
                name: "IX_body_types_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "body_types");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "mdm",
                table: "unit_of_measures");

            migrationBuilder.DropColumn(
                name: "ExternalSystem",
                schema: "mdm",
                table: "unit_of_measures");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                schema: "mdm",
                table: "unit_of_measures");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "mdm",
                table: "technical_parameters");

            migrationBuilder.DropColumn(
                name: "ExternalSystem",
                schema: "mdm",
                table: "technical_parameters");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                schema: "mdm",
                table: "technical_parameters");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "mdm",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "ExternalSystem",
                schema: "mdm",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                schema: "mdm",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "mdm",
                table: "parameter_sets");

            migrationBuilder.DropColumn(
                name: "ExternalSystem",
                schema: "mdm",
                table: "parameter_sets");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                schema: "mdm",
                table: "parameter_sets");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "mdm",
                table: "manufacturers");

            migrationBuilder.DropColumn(
                name: "ExternalSystem",
                schema: "mdm",
                table: "manufacturers");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                schema: "mdm",
                table: "manufacturers");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "mdm",
                table: "items");

            migrationBuilder.DropColumn(
                name: "ExternalSystem",
                schema: "mdm",
                table: "items");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                schema: "mdm",
                table: "items");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "mdm",
                table: "currencies");

            migrationBuilder.DropColumn(
                name: "ExternalSystem",
                schema: "mdm",
                table: "currencies");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                schema: "mdm",
                table: "currencies");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "mdm",
                table: "body_types");

            migrationBuilder.DropColumn(
                name: "ExternalSystem",
                schema: "mdm",
                table: "body_types");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                schema: "mdm",
                table: "body_types");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "mdm",
                table: "unit_of_measures",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSystem",
                schema: "mdm",
                table: "unit_of_measures",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SyncedAt",
                schema: "mdm",
                table: "unit_of_measures",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "mdm",
                table: "technical_parameters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSystem",
                schema: "mdm",
                table: "technical_parameters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SyncedAt",
                schema: "mdm",
                table: "technical_parameters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "mdm",
                table: "symbols",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSystem",
                schema: "mdm",
                table: "symbols",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SyncedAt",
                schema: "mdm",
                table: "symbols",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "mdm",
                table: "parameter_sets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSystem",
                schema: "mdm",
                table: "parameter_sets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SyncedAt",
                schema: "mdm",
                table: "parameter_sets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "mdm",
                table: "manufacturers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSystem",
                schema: "mdm",
                table: "manufacturers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SyncedAt",
                schema: "mdm",
                table: "manufacturers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "mdm",
                table: "items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSystem",
                schema: "mdm",
                table: "items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SyncedAt",
                schema: "mdm",
                table: "items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "mdm",
                table: "currencies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSystem",
                schema: "mdm",
                table: "currencies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SyncedAt",
                schema: "mdm",
                table: "currencies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "mdm",
                table: "body_types",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSystem",
                schema: "mdm",
                table: "body_types",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SyncedAt",
                schema: "mdm",
                table: "body_types",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "counterparty_external_links",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CounterpartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExternalEntity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalSystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: true),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_counterparty_external_links", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_unit_of_measures_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "unit_of_measures",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_technical_parameters_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "technical_parameters",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true,
                filter: "[ExternalSystem] IS NOT NULL AND [ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_symbols_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "symbols",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true,
                filter: "[ExternalSystem] IS NOT NULL AND [ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_parameter_sets_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "parameter_sets",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true,
                filter: "[ExternalSystem] IS NOT NULL AND [ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_manufacturers_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "manufacturers",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "items",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true,
                filter: "ExternalSystem IS NOT NULL AND ExternalId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_currencies_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "currencies",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_body_types_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "body_types",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true,
                filter: "[ExternalSystem] IS NOT NULL AND [ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_counterparty_external_links_CounterpartyId",
                schema: "mdm",
                table: "counterparty_external_links",
                column: "CounterpartyId");

            migrationBuilder.CreateIndex(
                name: "IX_counterparty_external_links_ExternalSystem_ExternalEntity_E~",
                schema: "mdm",
                table: "counterparty_external_links",
                columns: new[] { "ExternalSystem", "ExternalEntity", "ExternalId" },
                unique: true);
        }
    }
}
