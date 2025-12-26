using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveStatusesToMdmAndDropCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "mdm");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'ref' AND table_name = 'statuses'
    ) THEN
        ALTER TABLE ref.statuses SET SCHEMA mdm;
    END IF;
END$$;");

            migrationBuilder.Sql(@"
ALTER TABLE mdm.statuses
    ADD COLUMN IF NOT EXISTS ""Description"" character varying(500);

ALTER TABLE mdm.statuses
    ALTER COLUMN ""GroupId"" DROP NOT NULL;");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'ref' AND table_name = 'status_groups'
    ) THEN
        INSERT INTO mdm.statuses (""Id"", ""GroupId"", ""Name"", ""Description"", ""Color"", ""Flags"", ""SortOrder"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
        SELECT g.""Id"", NULL, g.""Name"", g.""Description"", NULL, NULL, g.""SortOrder"", g.""IsActive"", g.""CreatedAt"", g.""UpdatedAt""
        FROM ref.status_groups g
        WHERE NOT EXISTS (SELECT 1 FROM mdm.statuses s WHERE s.""Id"" = g.""Id"");

        INSERT INTO integration.external_entity_links (""Id"", ""EntityType"", ""EntityId"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"", ""SourceType"", ""SyncedAt"", ""CreatedAt"", ""UpdatedAt"")
        SELECT uuid_generate_v4(),
               'Status',
               g.""Id"",
               map.external_system,
               map.external_entity,
               map.external_id,
               NULL,
               NOW(),
               NOW(),
               NOW()
        FROM (
            SELECT 'Components' AS code, 'Component2020' AS external_system, 'StatusKind' AS external_entity, '0' AS external_id
            UNION ALL
            SELECT 'SupplierOrderLines', 'Component2020', 'StatusKind', '1'
            UNION ALL
            SELECT 'CustomerOrders', 'Component2020', 'StatusKind', '2'
            UNION ALL
            SELECT 'CustomerOrderTypes', 'Component2020', 'StatusKind', '3'
            UNION ALL
            SELECT 'Requests', 'MyIS', 'RequestStatusGroup', 'Requests'
        ) AS map
        JOIN ref.status_groups g ON g.""Code"" = map.code
        ON CONFLICT (""EntityType"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"")
        DO UPDATE SET ""EntityId"" = EXCLUDED.""EntityId"", ""UpdatedAt"" = NOW();
    ELSIF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'mdm' AND table_name = 'status_groups'
    ) THEN
        INSERT INTO mdm.statuses (""Id"", ""GroupId"", ""Name"", ""Description"", ""Color"", ""Flags"", ""SortOrder"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
        SELECT g.""Id"", NULL, g.""Name"", g.""Description"", NULL, NULL, g.""SortOrder"", g.""IsActive"", g.""CreatedAt"", g.""UpdatedAt""
        FROM mdm.status_groups g
        WHERE NOT EXISTS (SELECT 1 FROM mdm.statuses s WHERE s.""Id"" = g.""Id"");

        INSERT INTO integration.external_entity_links (""Id"", ""EntityType"", ""EntityId"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"", ""SourceType"", ""SyncedAt"", ""CreatedAt"", ""UpdatedAt"")
        SELECT uuid_generate_v4(),
               'Status',
               g.""Id"",
               map.external_system,
               map.external_entity,
               map.external_id,
               NULL,
               NOW(),
               NOW(),
               NOW()
        FROM (
            SELECT 'Components' AS code, 'Component2020' AS external_system, 'StatusKind' AS external_entity, '0' AS external_id
            UNION ALL
            SELECT 'SupplierOrderLines', 'Component2020', 'StatusKind', '1'
            UNION ALL
            SELECT 'CustomerOrders', 'Component2020', 'StatusKind', '2'
            UNION ALL
            SELECT 'CustomerOrderTypes', 'Component2020', 'StatusKind', '3'
            UNION ALL
            SELECT 'Requests', 'MyIS', 'RequestStatusGroup', 'Requests'
        ) AS map
        JOIN mdm.status_groups g ON g.""Code"" = map.code
        ON CONFLICT (""EntityType"", ""ExternalSystem"", ""ExternalEntity"", ""ExternalId"")
        DO UPDATE SET ""EntityId"" = EXCLUDED.""EntityId"", ""UpdatedAt"" = NOW();
    END IF;

    ALTER TABLE mdm.statuses DROP CONSTRAINT IF EXISTS ""FK_statuses_status_groups_GroupId"";
    DROP TABLE IF EXISTS ref.status_groups;
    DROP TABLE IF EXISTS mdm.status_groups;
END$$;");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'FK_statuses_statuses_GroupId'
    ) THEN
        ALTER TABLE mdm.statuses
            ADD CONSTRAINT ""FK_statuses_statuses_GroupId""
            FOREIGN KEY (""GroupId"") REFERENCES mdm.statuses (""Id"") ON DELETE RESTRICT;
    END IF;
END$$;");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS mdm.""IX_statuses_GroupId_Code"";");
            migrationBuilder.Sql(@"ALTER TABLE mdm.statuses DROP COLUMN IF EXISTS ""Code"";");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_statuses_GroupId_Name"" ON mdm.statuses (""GroupId"", ""Name"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ref");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ref.status_groups (
    ""Id"" uuid NOT NULL,
    ""Code"" character varying(50) NOT NULL,
    ""Name"" character varying(200) NOT NULL,
    ""Description"" character varying(500),
    ""SortOrder"" integer,
    ""IsActive"" boolean NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""UpdatedAt"" timestamp with time zone NOT NULL,
    CONSTRAINT ""PK_status_groups"" PRIMARY KEY (""Id"")
);");

            migrationBuilder.Sql(@"
INSERT INTO ref.status_groups (""Id"", ""Code"", ""Name"", ""Description"", ""SortOrder"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
SELECT s.""Id"", s.""Id""::text, s.""Name"", s.""Description"", s.""SortOrder"", s.""IsActive"", s.""CreatedAt"", s.""UpdatedAt""
FROM mdm.statuses s
WHERE s.""GroupId"" IS NULL
ON CONFLICT (""Id"") DO NOTHING;");

            migrationBuilder.Sql(@"ALTER TABLE mdm.statuses ADD COLUMN IF NOT EXISTS ""Code"" character varying(50);");
            migrationBuilder.Sql(@"UPDATE mdm.statuses SET ""Code"" = ""Id""::text WHERE ""Code"" IS NULL;");
            migrationBuilder.Sql(@"ALTER TABLE mdm.statuses ALTER COLUMN ""Code"" SET NOT NULL;");

            migrationBuilder.Sql(@"ALTER TABLE mdm.statuses DROP CONSTRAINT IF EXISTS ""FK_statuses_statuses_GroupId"";");
            migrationBuilder.Sql(@"ALTER TABLE mdm.statuses ADD CONSTRAINT ""FK_statuses_status_groups_GroupId"" FOREIGN KEY (""GroupId"") REFERENCES ref.status_groups (""Id"") ON DELETE RESTRICT;");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS mdm.""IX_statuses_GroupId_Name"";");
            migrationBuilder.Sql(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_statuses_GroupId_Code"" ON mdm.statuses (""GroupId"", ""Code"");");
        }
    }
}
