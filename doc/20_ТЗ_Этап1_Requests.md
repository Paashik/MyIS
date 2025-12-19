# Техническое задание
## Этап 1 — Модуль Requests (управление заявками)

> Каноничные коды типов и статусов указаны в `Requests_Canonical_Definitions.md` и являются источником истины для всех документов и DTO.

---

# 1. Введение

## 1.1. Назначение документа
Задать требования к реализации модуля Requests на Этапе 1: доменная модель, REST API, UI на базе AppShell, базовый жизненный цикл заявок и минимальная интеграция.

## 1.2. Область применения
- backend-модуль Requests (слои Domain/Application/Infrastructure/WebApi);
- REST API для работы с заявками;
- фронтенд-модуль Requests (React/TypeScript/Ant Design);
- модель данных в схеме `requests` PostgreSQL;
- базовая интеграция с Component2020 (чтение справочников/заказов клиентов).

## 1.3. Связанные документы
- `Requests_Canonical_Definitions.md` — коды статусов и типов;
- `Requests_Concept.md` — концепция агрегата;
- `MyIS_Conceptual_Data_Model_v0.3.md` — общая модель данных;
- `TZ_Etap1_Requests_v1.1.md` — уточнения Stage 1;
- `TZ_Etap2_Requests_Workflow_RequestLines.md` — развитие workflow/строк;
- `TZ_Iteration_S1_Settings_Module_Requests_Dictionaries.md` — админка справочников.

---

# 2. Цели и Scope

## 2.1. Цели
- Ввести модуль Requests как единую точку входа для ключевых типов заявок.
- Обеспечить прозрачный жизненный цикл (статусы, согласования, история).
- Настроить базовую интеграцию с Component2020 (чтение заказов/справочников).
- Подготовить фундамент для последующих этапов (Customers, Procurement, Production, Warehouse) без ломки каркаса.

## 2.2. В Scope Этапа 1
- Доменная модель и таблицы `requests.*` (Request, RequestType, RequestStatus, History, Comments; RequestLine — опционально, задел под Stage 2).
- CRUD заявок и справочников типов/статусов.
- Базовый жизненный цикл со стандартными статусами.
- UI: журнал, карточка, создание/редактирование в AppShell.
- Простая интеграция с Component2020: чтение заказов клиентов и номенклатуры для подстановки в заявки.

## 2.3. Out of Scope
- Полный закупочный/производственный/складской контур (этапы 2–3).
- Сложный BPM/workflow-конструктор (только фиксированная матрица переходов).
- Вложения/файлы (будущий модуль Attachments).
- Полный RBAC (минимальные политики достаточно).
- Запись в Component2020 (только чтение на этом этапе).

---

# 3. Требования

## 3.1. Типы заявок (Stage 1)
Используются коды из канона (`Requests_Canonical_Definitions.md`):
- `CustomerDevelopment`
- `InternalProductionRequest`
- `SupplyRequest`
- `ExternalTechStageRequest`
- `ChangeRequest`

Типы настраиваются через справочник (админка Settings), коды стабильны, названия локализуются.

## 3.2. Статусы и workflow
Коды статусов (канон): `Draft`, `Submitted`, `InReview`, `Approved`, `Rejected`, `InWork`, `Done`, `Closed`.

Базовый маршрут: Draft → Submitted → InReview → Approved → InWork → Done → Closed, с ответвлением Rejected из InReview (опционально из InWork). Ограничения по типам допускаются конфигурацией, коды не меняются.

Требования:
- хранить статус по коду + ссылке на справочник `RequestStatus`;
- вести историю переходов (кто/когда/откуда/куда/комментарий);
- действия/переходы — через use-case слоя Application, не в контроллере/UI.

## 3.3. Модель данных (минимум Stage 1)
Схема `requests`:
- `requests.requests`: `Id (Guid)`, `RequestTypeId`, `RequestStatusId`, `InitiatorId`, `Title`, `BodyText`, `Priority (int?)`, `RelatedEntityType`, `RelatedEntityId`, `ExternalReferenceId`, `DueDate`, `CreatedAt`, `UpdatedAt`.
- `requests.request_types`: `Id`, `Code` (уникален, коды выше), `Name`, `Description`, `Direction (Incoming|Outgoing)`, `IsActive`.
- `requests.request_statuses`: `Id`, `Code` (коды выше), `Name`, `IsFinal`, `IsActive`.
- `requests.request_history`: `Id`, `RequestId`, `Action`, `FromStatusId?`, `ToStatusId`, `PerformedBy`, `Timestamp`, `Comment`.
- `requests.request_comments`: `Id`, `RequestId`, `AuthorId`, `Text`, `CreatedAt`.
- `requests.request_lines` — допускается задел под Stage 2 (позиционное тело SupplyRequest).

Ссылки на Component2020/внешние сущности — через `ExternalReferenceId` и `RelatedEntityType/Id`, без прямых FK на чужие схемы.

## 3.4. API
- CRUD заявки: `POST /api/requests`, `GET /api/requests`, `GET /api/requests/{id}`, `PUT /api/requests/{id}`.
- Статусы/переходы (минимум Stage 1): `POST /api/requests/{id}/status/{code}` или специализированные эндпоинты (`/submit`, `/approve`, ...), маппящиеся на use-case.
- Справочники (админ): `/api/admin/requests/types`, `/api/admin/requests/statuses`.
- Авторизация: защищено через policies/roles (минимум роль ADMIN для админских эндпоинтов).

## 3.5. UI
- В AppShell: журнал заявок (фильтры по типу/статусу/инициатору/дате), карточка заявки (общие поля, история, комментарии), форма создания/редактирования.
- Локализация: RU по умолчанию; тексты — в одном месте (словарь).
- Без бизнес-логики статусов на фронте: только кнопки действий, разрешения — от backend.

## 3.6. Интеграция с Component2020
- Чтение заказов клиентов и номенклатуры — через модуль Integration.Component2020 (Infrastructure), без прямых обращений Domain/Application к Access.
- Связи фиксируются через `ExternalReferenceId` / `RelatedEntityType/Id`.
- Настройки/проверка подключения — в Settings/Integration (см. интеграционное ТЗ).

## 3.7. Права доступа (минимум)
- Админка справочников: `Admin.Settings.Access`, `Admin.Requests.EditTypes`, `Admin.Requests.EditStatuses`.
- Операции по заявкам: можно маппить на роль/permission (например, `Requests.Create`, `Requests.Edit`, `Requests.Workflow.*`) — базово достаточно ADMIN для всех.

## 3.8. Нефункциональные
- Архитектура: модульный монолит, .NET 8, PostgreSQL, React/TS/AntD.
- Производительность: список < 2–3 с, карточка < 2–3 с при типовых объёмах Stage 1.
- Тесты: unit для доменной логики/handlers, интеграционные API smoke.

---

# 4. Definition of Done (Этап 1)
- В базе есть таблицы `requests.*` по модели выше, миграции применяются.
- CRUD заявок и справочников работает, статусы — каноничные коды.
- История и комментарии сохраняются.
- UI: журнал/карточка/создание; действия по статусам вызывают backend use-case.
- Админка справочников в Settings, защищена политиками.
- Интеграция с Component2020 ограничена чтением (заказы/справочники) через инфраструктурный модуль, без прямых подключений из домена.
