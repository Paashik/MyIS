# Settings → Requests → Dictionaries (Iteration S1) — Реализация

Нормативный ТЗ: [`TZ_Iteration_S1_Settings_Module_Requests_Dictionaries.md`](doc/TZ_Iteration_S1_Settings_Module_Requests_Dictionaries.md:1).

## 1) Admin API (backend)

Все эндпойнты под префиксом `/api/admin/requests/...`.

### 1.1. Request Types

- `GET /api/admin/requests/types`
  - ответ: массив `RequestTypeDto` (включая архивные)
- `POST /api/admin/requests/types`
  - тело: `AdminRequestTypeCreateRequest`
  - ответ: `201` + созданный `RequestTypeDto`
- `PUT /api/admin/requests/types/{id}`
  - тело: `AdminRequestTypeUpdateRequest`
  - ответ: `200` + обновлённый `RequestTypeDto`
- `POST /api/admin/requests/types/{id}/archive`
  - soft-деактивация (`IsActive=false`), удаление не используется
  - ответ: `204`

### 1.2. Request Statuses

- `GET /api/admin/requests/statuses`
  - ответ: массив `RequestStatusDto` (включая архивные)
- `POST /api/admin/requests/statuses`
  - тело: `AdminRequestStatusCreateRequest`
  - ответ: `201` + созданный `RequestStatusDto`
- `PUT /api/admin/requests/statuses/{id}`
  - тело: `AdminRequestStatusUpdateRequest`
  - ответ: `200` + обновлённый `RequestStatusDto`
- `POST /api/admin/requests/statuses/{id}/archive`
  - soft-деактивация (`IsActive=false`)
  - ответ: `204`

### 1.3. Workflow / Transitions

- `GET /api/admin/requests/workflow/transitions?typeCode={code}`
  - ответ: массив `RequestWorkflowTransitionDto`
- `PUT /api/admin/requests/workflow/transitions`
  - тело: `AdminReplaceWorkflowTransitionsRequest` (bulk replace для конкретного `typeCode`)
  - ответ: `204`

## 2) Права доступа (S1)

Политики (policies) настроены в [`Program`](backend/src/Core.WebApi/Program.cs:1) и пока маппятся на роль `ADMIN`:

- `Admin.Settings.Access`
- `Admin.Requests.EditTypes`
- `Admin.Requests.EditStatuses`
- `Admin.Requests.EditWorkflow`

## 3) Frontend маршруты

Внутри приватной зоны (под guard’ами `DbStatusGuard` + `RequireAuth`):

- `/settings/requests/types` — Types
- `/settings/requests/statuses` — Statuses
- `/settings/requests/workflow` — Workflow

В левом меню (Sider) добавлен раздел **Settings → Requests**.

## 4) Миграции

Добавлены поля:

- `requests.request_types.is_active`
- `requests.request_statuses.is_active`
- `requests.request_transitions.is_enabled`

Миграция: [`S1_Settings_RequestsDictionaries`](backend/src/Core.Infrastructure/Migrations/20251212141604_S1_Settings_RequestsDictionaries.cs:1).

Дополнительно снято ограничение канонических кодов типов заявок (иначе нельзя создавать новые типы из Settings):

- миграция: [`S1_AllowCustomRequestTypes`](backend/src/Core.Infrastructure/Migrations/20251212143740_S1_AllowCustomRequestTypes.cs:1)
