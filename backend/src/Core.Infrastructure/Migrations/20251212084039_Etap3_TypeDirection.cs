using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Etap3_TypeDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "direction",
                schema: "requests",
                table: "request_types",
                type: "text",
                nullable: false,

                // Не допускаем пустых значений, т.к. маппинг в доменный enum.
                defaultValue: "Incoming");

            // Минимальный seed для уже существующих типов.
            // На текущей итерации гарантированно существует SupplyRequest.
            migrationBuilder.Sql(@"
UPDATE requests.request_types
SET direction = 'Outgoing'
WHERE code = 'SupplyRequest';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "direction",
                schema: "requests",
                table: "request_types");
        }
    }
}
