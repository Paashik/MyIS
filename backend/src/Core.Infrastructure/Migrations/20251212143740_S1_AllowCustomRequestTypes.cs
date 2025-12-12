using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
public partial class S1_AllowCustomRequestTypes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Iteration S1 (Settings): справочник типов заявок должен быть управляемым.
        // На этапе 3.x был добавлен CHECK-constraint, запрещающий неканонические коды.
        // Для Settings снимаем это ограничение.
        migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'ck_request_types_canonical_codes'
  ) THEN
    ALTER TABLE requests.request_types
      DROP CONSTRAINT ck_request_types_canonical_codes;
  END IF;
END $$;
");
    }

        /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // No-op: обратное включение ограничения может сломать БД,
        // если уже добавлены пользовательские (неканонические) типы.
    }
}
}
