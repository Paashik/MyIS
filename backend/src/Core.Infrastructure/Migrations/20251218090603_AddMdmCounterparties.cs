using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMdmCounterparties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "counterparties",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Inn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Kpp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    City = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Site = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_counterparties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "counterparty_external_links",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CounterpartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalSystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalEntity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: true),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_counterparty_external_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_counterparty_external_links_counterparties_CounterpartyId",
                        column: x => x.CounterpartyId,
                        principalSchema: "mdm",
                        principalTable: "counterparties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "counterparty_roles",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CounterpartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleType = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_counterparty_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_counterparty_roles_counterparties_CounterpartyId",
                        column: x => x.CounterpartyId,
                        principalSchema: "mdm",
                        principalTable: "counterparties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_counterparties_Code",
                schema: "mdm",
                table: "counterparties",
                column: "Code",
                unique: true,
                filter: "\"Code\" IS NOT NULL");

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

            migrationBuilder.CreateIndex(
                name: "IX_counterparty_roles_CounterpartyId_RoleType",
                schema: "mdm",
                table: "counterparty_roles",
                columns: new[] { "CounterpartyId", "RoleType" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO mdm.counterparties ("Id", "Code", "Name", "FullName", "Inn", "Kpp", "Email", "Phone", "City", "Address", "Site", "Note", "IsActive", "CreatedAt", "UpdatedAt")
                SELECT
                    s."Id",
                    s."Code",
                    s."Name",
                    s."FullName",
                    s."Inn",
                    s."Kpp",
                    s."Email",
                    s."Phone",
                    s."City",
                    s."Address",
                    s."Site",
                    s."Note",
                    s."IsActive",
                    s."CreatedAt",
                    s."UpdatedAt"
                FROM mdm.suppliers s
                WHERE s."Inn" IS NULL
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO mdm.counterparties ("Id", "Code", "Name", "FullName", "Inn", "Kpp", "Email", "Phone", "City", "Address", "Site", "Note", "IsActive", "CreatedAt", "UpdatedAt")
                SELECT DISTINCT ON (s."Inn", COALESCE(s."Kpp", ''))
                    s."Id",
                    s."Code",
                    s."Name",
                    s."FullName",
                    s."Inn",
                    s."Kpp",
                    s."Email",
                    s."Phone",
                    s."City",
                    s."Address",
                    s."Site",
                    s."Note",
                    s."IsActive",
                    s."CreatedAt",
                    s."UpdatedAt"
                FROM mdm.suppliers s
                WHERE s."Inn" IS NOT NULL
                ORDER BY s."Inn", COALESCE(s."Kpp", ''), s."UpdatedAt" DESC, s."Id";
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO mdm.counterparty_roles
                    ("Id", "CounterpartyId", "RoleType", "IsActive", "CreatedAt", "UpdatedAt")
                SELECT
                    (
                        substring(md5('CounterpartyRole:Supplier:' || c."Id"::text) from 1 for 8) || '-' ||
                        substring(md5('CounterpartyRole:Supplier:' || c."Id"::text) from 9 for 4) || '-' ||
                        substring(md5('CounterpartyRole:Supplier:' || c."Id"::text) from 13 for 4) || '-' ||
                        substring(md5('CounterpartyRole:Supplier:' || c."Id"::text) from 17 for 4) || '-' ||
                        substring(md5('CounterpartyRole:Supplier:' || c."Id"::text) from 21 for 12)
                    )::uuid,
                    c."Id",
                    1,
                    s."IsActive",
                    s."CreatedAt",
                    s."UpdatedAt"
                FROM mdm.suppliers s
                JOIN mdm.counterparties c ON
                    (s."Inn" IS NULL AND c."Id" = s."Id")
                    OR (s."Inn" IS NOT NULL AND c."Inn" = s."Inn" AND COALESCE(c."Kpp", '') = COALESCE(s."Kpp", ''))
                ON CONFLICT ("CounterpartyId", "RoleType") DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO mdm.counterparty_external_links
                    ("Id", "CounterpartyId", "ExternalSystem", "ExternalEntity", "ExternalId", "SourceType", "SyncedAt", "CreatedAt", "UpdatedAt")
                SELECT
                    (
                        substring(md5('CounterpartyExternalLink:Component2020:Providers:' || s."ExternalId") from 1 for 8) || '-' ||
                        substring(md5('CounterpartyExternalLink:Component2020:Providers:' || s."ExternalId") from 9 for 4) || '-' ||
                        substring(md5('CounterpartyExternalLink:Component2020:Providers:' || s."ExternalId") from 13 for 4) || '-' ||
                        substring(md5('CounterpartyExternalLink:Component2020:Providers:' || s."ExternalId") from 17 for 4) || '-' ||
                        substring(md5('CounterpartyExternalLink:Component2020:Providers:' || s."ExternalId") from 21 for 12)
                    )::uuid,
                    c."Id",
                    s."ExternalSystem",
                    'Providers',
                    s."ExternalId",
                    s."ProviderType",
                    s."SyncedAt",
                    s."CreatedAt",
                    s."UpdatedAt"
                FROM mdm.suppliers s
                JOIN mdm.counterparties c ON
                    (s."Inn" IS NULL AND c."Id" = s."Id")
                    OR (s."Inn" IS NOT NULL AND c."Inn" = s."Inn" AND COALESCE(c."Kpp", '') = COALESCE(s."Kpp", ''))
                WHERE s."ExternalSystem" IS NOT NULL AND s."ExternalId" IS NOT NULL
                ON CONFLICT ("ExternalSystem", "ExternalEntity", "ExternalId") DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_counterparties_Inn_KppCoalesced"
                ON mdm.counterparties ("Inn", COALESCE("Kpp", ''))
                WHERE "Inn" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS mdm."IX_counterparties_Inn_KppCoalesced";""");

            migrationBuilder.DropTable(
                name: "counterparty_external_links",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "counterparty_roles",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "counterparties",
                schema: "mdm");
        }
    }
}
