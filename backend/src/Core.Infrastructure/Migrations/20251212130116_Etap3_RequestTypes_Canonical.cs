using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Etap3_RequestTypes_Canonical : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Канонический справочник типов заявок (этап 3.x).
            // Требования:
            // - при чистой БД типы создаются;
            // - при существующей БД отсутствующие добавляются, существующие обновляются по name/direction.
            // Id фиксированы (как в Etap2 seed), но при ON CONFLICT по code не переопределяем существующий id.

            migrationBuilder.Sql(@"
INSERT INTO requests.request_types (id, code, name, description, direction)
VALUES ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'CustomerDevelopment', 'Заявка заказчика', NULL, 'Incoming')
ON CONFLICT (code) DO UPDATE SET
  name = EXCLUDED.name,
  description = EXCLUDED.description,
  direction = EXCLUDED.direction;

INSERT INTO requests.request_types (id, code, name, description, direction)
VALUES ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'InternalProductionRequest', 'Внутренняя производственная заявка', NULL, 'Incoming')
ON CONFLICT (code) DO UPDATE SET
  name = EXCLUDED.name,
  description = EXCLUDED.description,
  direction = EXCLUDED.direction;

INSERT INTO requests.request_types (id, code, name, description, direction)
VALUES ('dddddddd-dddd-dddd-dddd-dddddddddddd', 'ChangeRequest', 'Заявка на изменение (ECR/ECO-light)', NULL, 'Incoming')
ON CONFLICT (code) DO UPDATE SET
  name = EXCLUDED.name,
  description = EXCLUDED.description,
  direction = EXCLUDED.direction;

INSERT INTO requests.request_types (id, code, name, description, direction)
VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'SupplyRequest', 'Заявка на обеспечение/закупку', NULL, 'Outgoing')
ON CONFLICT (code) DO UPDATE SET
  name = EXCLUDED.name,
  description = EXCLUDED.description,
  direction = EXCLUDED.direction;

INSERT INTO requests.request_types (id, code, name, description, direction)
VALUES ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 'ExternalTechStageRequest', 'Заявка на внешний технологический этап', NULL, 'Outgoing')
ON CONFLICT (code) DO UPDATE SET
  name = EXCLUDED.name,
  description = EXCLUDED.description,
  direction = EXCLUDED.direction;

-- Приводим в корректное состояние возможные исторические значения (на случай кастомных/ручных правок)
UPDATE requests.request_types
SET direction = 'Outgoing'
WHERE code IN ('SupplyRequest', 'ExternalTechStageRequest')
  AND direction <> 'Outgoing';

UPDATE requests.request_types
SET direction = 'Incoming'
WHERE code IN ('CustomerDevelopment', 'InternalProductionRequest', 'ChangeRequest')
  AND direction <> 'Incoming';

-- Канонизация: удаляем любые «лишние» типы заявок (single source of truth — 5 кодов).
-- Важно: предполагается, что до этапа Requests (Iteration 3.x) пользовательские данные
-- ещё не создавались для неканонических типов.
DELETE FROM requests.request_types
WHERE code NOT IN (
  'CustomerDevelopment',
  'InternalProductionRequest',
  'ChangeRequest',
  'SupplyRequest',
  'ExternalTechStageRequest'
);

-- Жёсткая гарантия на уровне данных: запрещаем добавление неканонических кодов.
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'ck_request_types_canonical_codes'
  ) THEN
    ALTER TABLE requests.request_types
      ADD CONSTRAINT ck_request_types_canonical_codes
      CHECK (
        code IN (
          'CustomerDevelopment',
          'InternalProductionRequest',
          'ChangeRequest',
          'SupplyRequest',
          'ExternalTechStageRequest'
        )
      );
  END IF;
END $$;
" );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: справочник является частью данных системы.
        }
    }
}
