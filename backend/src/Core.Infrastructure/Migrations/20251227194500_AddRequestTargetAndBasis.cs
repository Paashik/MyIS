using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyIS.Core.Infrastructure.Data;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20251227194500_AddRequestTargetAndBasis")]
    public partial class AddRequestTargetAndBasis : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "related_entity_name",
                schema: "requests",
                table: "requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_entity_name",
                schema: "requests",
                table: "requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "target_entity_id",
                schema: "requests",
                table: "requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_entity_type",
                schema: "requests",
                table: "requests",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "related_entity_name",
                schema: "requests",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "target_entity_name",
                schema: "requests",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "target_entity_id",
                schema: "requests",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "target_entity_type",
                schema: "requests",
                table: "requests");
        }
    }
}
