-- SQL скрипт для исправления проблемы с переименованием столбца DetailsJson в CountersJson
-- Проблема: PostgresException: 42703: столбец "DetailsJson" не существует

-- Переименование столбца в таблице component2020_sync_run
ALTER TABLE integration.component2020_sync_run 
RENAME COLUMN "DetailsJson" TO "CountersJson";

-- Проверка результата
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'component2020_sync_run' 
AND table_schema = 'integration' 
AND column_name IN ('DetailsJson', 'CountersJson');