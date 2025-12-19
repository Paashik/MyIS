using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameDetailsJsonToCountersJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Older installations may have had DetailsJson; new installations already have CountersJson.
            migrationBuilder.Sql(
                """
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'integration'
          AND table_name = 'component2020_sync_run'
          AND column_name = 'DetailsJson'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'integration'
          AND table_name = 'component2020_sync_run'
          AND column_name = 'CountersJson'
    ) THEN
        ALTER TABLE integration.component2020_sync_run RENAME COLUMN "DetailsJson" TO "CountersJson";
    END IF;
END
$$;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'integration'
          AND table_name = 'component2020_sync_run'
          AND column_name = 'CountersJson'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'integration'
          AND table_name = 'component2020_sync_run'
          AND column_name = 'DetailsJson'
    ) THEN
        ALTER TABLE integration.component2020_sync_run RENAME COLUMN "CountersJson" TO "DetailsJson";
    END IF;
END
$$;
""");
        }
    }
}
