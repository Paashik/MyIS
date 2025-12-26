using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusDictionary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ref");

            migrationBuilder.CreateTable(
                name: "status_groups",
                schema: "ref",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "statuses",
                schema: "ref",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Color = table.Column<int>(type: "integer", nullable: true),
                    Flags = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_statuses_status_groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "ref",
                        principalTable: "status_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_status_groups_Code",
                schema: "ref",
                table: "status_groups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_statuses_GroupId_Code",
                schema: "ref",
                table: "statuses",
                columns: new[] { "GroupId", "Code" },
                unique: true);

            migrationBuilder.Sql(
                "INSERT INTO ref.status_groups (\"Id\", \"Code\", \"Name\", \"IsActive\", \"CreatedAt\", \"UpdatedAt\") " +
                "VALUES (uuid_generate_v4(), 'Requests', 'Статусы заявок', TRUE, NOW(), NOW());");

            migrationBuilder.Sql(
                "INSERT INTO ref.statuses (\"Id\", \"GroupId\", \"Code\", \"Name\", \"Flags\", \"IsActive\", \"CreatedAt\", \"UpdatedAt\") " +
                "SELECT uuid_generate_v4(), g.\"Id\", rs.code, rs.name, " +
                "CASE WHEN rs.is_final THEN 1 ELSE 0 END, rs.is_active, NOW(), NOW() " +
                "FROM requests.request_statuses rs " +
                "JOIN ref.status_groups g ON g.\"Code\" = 'Requests';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "statuses",
                schema: "ref");

            migrationBuilder.DropTable(
                name: "status_groups",
                schema: "ref");
        }
    }
}
