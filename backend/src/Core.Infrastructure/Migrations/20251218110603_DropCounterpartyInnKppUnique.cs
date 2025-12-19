using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropCounterpartyInnKppUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS mdm."IX_counterparties_Inn_KppCoalesced";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_counterparties_Inn_KppCoalesced"
                ON mdm.counterparties ("Inn", COALESCE("Kpp", ''))
                WHERE "Inn" IS NOT NULL;
                """);
        }
    }
}
