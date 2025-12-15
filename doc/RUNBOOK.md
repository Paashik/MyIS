# RUNBOOK — эксплуатация и сопровождение MyIS

Документ описывает, как эксплуатировать и поддерживать стенды MyIS в режимах разработки, тестирования и промышленной эксплуатации.

## 1. Обзор окружений

| Окружение | Цель | Ключевые отличия |
|-----------|------|------------------|
| Dev | Повседневная разработка, hot-reload, возможность настройки БД через UI. | Используется скрипт [`dev.cmd`](dev.cmd:1), чтение `appsettings.Local.json`, включены `dotnet watch` и Vite HMR, разрешено применение строки подключения через [`AdminDbController`](backend/src/Core.WebApi/Controllers/AdminDbController.cs:1). |
| Test | Проверка сборок перед продом, реплика боевой конфигурации без открытого UI мастера. | Сборки `Release`, приложение запускается из артефактов `dotnet publish` + `npm run build`, переменные окружения передаются через системные секреты или менеджеры секретов. |
| Prod | Боевая эксплуатация. | Только артефакты `Release`, обязательное внешнее хранение секретов, миграции выполняются вручную (CLI) или через админ-контроллер при включённом Maintenance-моде, расширенный мониторинг (обвязка к `/api/admin/db-status`). |

## 2. Конфигурация и секреты

### 2.1. Dev

- Backend ищет локальные переопределения в [`backend/src/Core.WebApi/appsettings.Local.json`](backend/src/Core.WebApi/appsettings.Local.json:1).
- Создайте файл из шаблона [`backend/src/Core.WebApi/appsettings.Local.example.json`](backend/src/Core.WebApi/appsettings.Local.example.json:1) и заполните `ConnectionStrings:Default` (PostgreSQL).
- Файл содержит секреты, поэтому он уже присутствует в `.gitignore` и не должен коммититься.
- Через UI `/db-setup` и эндпоинты `POST /api/admin/db-config/test|apply` можно безопасно тестировать и применять строку подключения: контроллер перезапишет `appsettings.Local.json` и выполнит миграции.

### 2.2. Test/Prod

- На серверах запрещено хранить секреты в репозитории. Используйте переменные окружения (`ConnectionStrings__Default`, `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`) или внешние секрет-хранилища (Azure Key Vault, HashiCorp Vault и т.д.), откуда значения прокидываются в `appsettings.{Environment}.json` при старте.
- `appsettings.Local.json` в Test/Prod не используется; для ручной диагностики допускается временный файл за пределами репозитория с ограниченными правами.
- Никогда не копируйте рабочие строки подключения в Git, CI-логи или публичные тикеты.

## 3. Процессы развертывания

### 3.1. Dev (сводка)

Подробная инструкция находится в [`DEV_SETUP.md`](DEV_SETUP.md:1). Кратко:

1. Установить .NET SDK 8+, Node.js 18+, PostgreSQL.
2. Настроить `appsettings.Local.json` из шаблона.
3. Применить миграции (`dotnet ef database update ...`).
4. Выполнить [`dev.cmd`](dev.cmd:1) для параллельного запуска backend (`dotnet watch run`) и frontend (`npm run dev -- --host 0.0.0.0`).

### 3.2. Test/Prod (Release-сборки)

1. **Подготовка окружения**
   - Убедитесь, что на сервере установлен .NET Runtime 8 и Node.js (для однократной сборки). На CI допустимо выполнять шаги сборки и доставлять артефакты без Node.js на сервере.
   - Настройте переменные окружения: `ASPNETCORE_ENVIRONMENT=Production` (или `Staging`), `ConnectionStrings__Default=<строка подключения>`, `ASPNETCORE_URLS=http://0.0.0.0:5000` (пример).

2. **Сборка backend**

   ```cmd
   dotnet publish backend/src/Core.WebApi/MyIS.Core.WebApi.csproj \
     -c Release \
     -o publish/backend
   ```

   В каталоге `publish/backend` появится self-contained набор (DLL + зависимости). Для запуска используйте `dotnet MyIS.Core.WebApi.dll` или создайте systemd/Windows Service.

3. **Сборка frontend**

   ```cmd
   cd frontend
   npm install --include=dev
   npm run build
   ```

   Готовая SPA лежит в `frontend/dist`. Раздайте её через любой статический сервер (Reverse proxy Nginx/Apache, ASP.NET Static Files, CDN). Путь деплоя согласуйте с DevOps, чтобы `/` frontend-а проксировался на backend API `http(s)://<host>:5000`.

4. **Применение миграций** (до переключения трафика)
   - CLI-режим (рекомендуемый):

     ```cmd
     dotnet ef database update \
       --project backend/src/Core.Infrastructure/MyIS.Core.Infrastructure.csproj \
       --startup-project backend/src/Core.WebApi/MyIS.Core.WebApi.csproj \
       --configuration Release
     ```

   - Через [`AdminDbController`](backend/src/Core.WebApi/Controllers/AdminDbController.cs:1): вызов `POST /api/admin/db-config/test` → `POST /api/admin/db-config/apply`. Этот путь включён только в окружениях, где разрешено редактирование строки подключения (обычно Dev/Test). На Prod опция «apply» должна быть выключена политиками.

5. **Запуск сервисов**
   - Backend: `dotnet MyIS.Core.WebApi.dll` (или публикация как Windows Service/systemd unit). Настройте логирование в stdout + ротацию.
   - Frontend: раздайте `dist` (например, копируйте в `/var/www/myis-spa` и укажите `root` на каталог). Обновление — атомарная замена каталога + инвалидация кешей.

6. **Проксирование и TLS**
   - Используйте обратный прокси (Nginx/Apache/IIS) для TLS и объединения frontend+API: `/` → SPA, `/api` → ASP.NET.
   - Проверьте, что CORS и cookie `.MyIS.Auth` проходят через прокси (не переписывайте домены и пути).

## 4. Health & мониторинг

- Endpoint `GET /api/admin/db-status` возвращает DTO [`DbConnectionStatus`](backend/src/Core.Infrastructure/Data/DbConnectionStatus.cs:3).
- Поля ответа:
  - `configured` — обнаружена ли строка подключения.
  - `canConnect` — удалось ли выполнить короткое подключение к БД.
  - `lastError` — текст последней ошибки (без секретов).
  - `environment` — текущее значение `ASPNETCORE_ENVIRONMENT`.
  - `connectionStringSource` — источник конфигурации (`AppSettings`, `EnvironmentVariable`, `NotConfigured`, и т.д.).
  - `rawSourceDescription` — безопасное описание (например, имя файла). Используйте для диагностики, не логируйте целиком в открытых каналах.
- Рекомендуется:
  - Настроить системный мониторинг (Prometheus/Healthchecks) на периодический вызов `/api/admin/db-status`.
  - Считать инцидентом состояние `configured=true`, `canConnect=false`.

## 5. Checklists

### 5.1. После деплоя Test/Prod

1. **Миграции** — выполнить CLI-команду или проверить, что `DbMigrationHistory` содержит последнюю версию, соответствующую ветке релиза.
2. **Health** — `GET /api/admin/db-status` должен вернуть `configured=true` и `canConnect=true`, `lastError=null`.
3. **Frontend** — открыть собранную SPA (корень сайта). Должна грузиться авторизационная форма `/login` без ошибок в DevTools.
4. **Авторизация** — войти под системным администратором (`Admin/admin`, если база новая) и убедиться, что `/db-setup` не запрашивается при корректно настроенной БД.
5. **Логи** — проверить, что backend логирует старт `Now listening on: http://0.0.0.0:5000` (или настроенный URL), ошибки отсутствуют.
6. **Прокси/HTTPS** — убедиться, что публичный адрес доступен по HTTPS и прокси корректно маршрутизирует `/api/*`.

### 5.2. При расследовании инцидентов

1. Снять показатели `/api/admin/db-status` и логи backend.
2. Убедиться, что секреты не изменены и `ConnectionStrings__Default` указывает на верную БД.
3. При необходимости временно перевести трафик на резервный инстанс и повторно применить миграции.

---

Актуальные инструкции по локальной разработке находятся в [`DEV_SETUP.md`](DEV_SETUP.md:1). Все боевые изменения и операционные заметки фиксируйте в настоящем RUNBOOK, чтобы команда сопровождения имела единый источник правды.
