using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    public partial class FixStatusGroupForeignKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS mdm.statuses
    DROP CONSTRAINT IF EXISTS ""FK_statuses_status_groups_GroupId"";");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_statuses_statuses_GroupId'
    ) THEN
        ALTER TABLE mdm.statuses
            ADD CONSTRAINT ""FK_statuses_statuses_GroupId""
            FOREIGN KEY (""GroupId"") REFERENCES mdm.statuses (""Id"") ON DELETE RESTRICT;
    END IF;
END$$;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS mdm.statuses
    DROP CONSTRAINT IF EXISTS ""FK_statuses_statuses_GroupId"";");
        }
    }
}
