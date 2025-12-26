using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    public partial class FixStatusesMissingGroups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS mdm.statuses
    DROP CONSTRAINT IF EXISTS ""FK_statuses_statuses_GroupId"";

UPDATE mdm.statuses s
SET ""GroupId"" = NULL
WHERE s.""GroupId"" IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM mdm.statuses g
      WHERE g.""Id"" = s.""GroupId""
  );

ALTER TABLE mdm.statuses
    ADD CONSTRAINT ""FK_statuses_statuses_GroupId""
    FOREIGN KEY (""GroupId"") REFERENCES mdm.statuses (""Id"") ON DELETE RESTRICT;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS mdm.statuses
    DROP CONSTRAINT IF EXISTS ""FK_statuses_statuses_GroupId"";");
        }
    }
}
