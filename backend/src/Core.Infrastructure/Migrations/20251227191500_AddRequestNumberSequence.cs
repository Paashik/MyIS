using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyIS.Core.Infrastructure.Data;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20251227191500_AddRequestNumberSequence")]
    public partial class AddRequestNumberSequence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS requests.request_number_seq START WITH 1 INCREMENT BY 1;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS requests.request_number_seq;");
        }
    }
}
