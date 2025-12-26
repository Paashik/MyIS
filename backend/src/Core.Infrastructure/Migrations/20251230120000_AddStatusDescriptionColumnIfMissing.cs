using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    public partial class AddStatusDescriptionColumnIfMissing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS mdm.statuses
    ADD COLUMN IF NOT EXISTS ""Description"" character varying(500);");

            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS mdm.statuses
    ALTER COLUMN ""GroupId"" DROP NOT NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS mdm.statuses
    DROP COLUMN IF EXISTS ""Description"";");
        }
    }
}
