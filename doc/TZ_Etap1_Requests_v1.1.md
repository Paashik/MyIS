# Техническое задание  
## Этап 1 — Модуль Requests  
### Версия v1.1 (очищенная)

> Каноничные коды типов и статусов — в `Requests_Canonical_Definitions.md`. Все ссылки на статусы/типы в этом ТЗ должны использовать эти коды.

---

# 1. Введение

## 1.1. Назначение
Определить требования к реализации модуля Requests на Этапе 1: доменная модель, REST API, UI, жизненный цикл заявок и базовая интеграция с Component2020.

## 1.2. Основание
Документ опирается на:
- Концепцию MyIS (Этап 0);
- `Requests_Concept.md`;
- `MyIS_Conceptual_Data_Model_v0.3.md`;
- каноничный справочник статусов/типов `Requests_Canonical_Definitions.md`.

## 1.3. Область
- Backend: Domain/Application/Infrastructure/WebApi.
- Frontend: React/TS/AntD + AppShell.
- БД: PostgreSQL, схема `requests`.

---

# 2. Цели и задачи

## 2.1. Главная цель
Единый модуль заявок с прозрачным жизненным циклом, историей и интеграцией с заказами/справочниками Component2020.

## 2.2. Основные задачи
1. Реализовать доменную модель `requests` по канону.
2. CRUD заявок, справочников типов/статусов.
3. Жизненный цикл с историями и комментариями.
4. UI: журнал, карточка, создание/редактирование.
5. Привязка к внешним объектам через `RelatedEntity` и `ExternalReference`.
6. Базовая интеграция чтением из Component2020.

## 2.3. Ограничения
- Закупка/производство/склад — последующие этапы.
- BPM — только задел (события), без конструктора.
- Вложения — не входят (модуль Attachments позже).
- Запись в Component2020 — нет, только чтение.

---

# 3. Модель данных Requests

- Схема `requests`.
- Таблицы:
  - `requests` — заявки: `Id`, `RequestTypeId`, `RequestStatusId`, `InitiatorId`, `Title`, `BodyText`, `Priority?`, `RelatedEntityType/Id`, `ExternalReferenceId`, `DueDate`, `CreatedAt/UpdatedAt`.
  - `request_types` — коды из канона, `Name`, `Description`, `Direction (Incoming|Outgoing)`, `IsActive`.
  - `request_statuses` — коды из канона, `Name`, `IsFinal`, `IsActive`.
  - `request_history` — события: действие, от/до статуса, пользователь, время, комментарий.
  - `request_comments` — комментарии: автор, текст, время.
  - `request_lines` — задел для Stage 2 (позиционное тело SupplyRequest).

Все идентификаторы — Guid. Код статуса/типа стабилен, меняются только названия/локализация.

---

# 4. Типы и статусы

## 4.1. Типы (Stage 1)
Коды: `CustomerDevelopment`, `InternalProductionRequest`, `SupplyRequest`, `ExternalTechStageRequest`, `ChangeRequest`. Настраиваются через справочник, названия — локализация.

## 4.2. Статусы и workflow
Коды: `Draft`, `Submitted`, `InReview`, `Approved`, `Rejected`, `InWork`, `Done`, `Closed`.

Базовый маршрут: Draft → Submitted → InReview → Approved → InWork → Done → Closed, ветка Rejected из InReview (опционально из InWork). Переходы конфигурируются, но коды неизменны.

История фиксирует кто/когда/из/в + комментарий.

---

# 5. API

- `POST /api/requests`, `GET /api/requests`, `GET /api/requests/{id}`, `PUT /api/requests/{id}`.
- Переходы по статусам: специализированные действия (`/submit`, `/approve`, `/reject`, `/start-work`, `/complete`, `/close`) или `/status/{code}` — все через use-case.
- Admin: `/api/admin/requests/types`, `/api/admin/requests/statuses`.
- Авторизация: минимум роль ADMIN для админских эндпоинтов; прикладные — по политикам/ролям (`Requests.Create/Edit/Workflow.*`).

---

# 6. UI

- AppShell: журнал (фильтры тип/статус/инициатор/даты), карточка (шапка, история, комментарии), создание/редактирование.
- Фронт не содержит бизнес-логики статусов, только вызывает доступные действия, возвращённые backend.
- Локализация: RU, тексты централизованно.

---

# 7. Интеграция с Component2020

- Только чтение заказов клиентов/справочников для подстановки в заявки.
- Доступ через модуль Integration.Component2020 (Infrastructure), без прямых обращений Domain/Application к Access.
- Связи — через `ExternalReferenceId` и `RelatedEntityType/Id`.

---

# 8. Права

- Admin Settings: `Admin.Settings.Access`, `Admin.Requests.EditTypes`, `Admin.Requests.EditStatuses`.
- Операции с заявками: базово ADMIN; при детализации — `Requests.Create/Edit/Workflow.*`.

---

# 9. DoD

- Модель данных и миграции применены, справочники типов/статусов содержат коды из канона.
- CRUD заявок работает, статусы управляются через use-case, история/комментарии пишутся.
- UI покрывает журнал/карточку/создание, кнопки действий — от backend.
- Интеграция с Component2020 — чтение через инфраструктурный модуль.
- Админка типов/статусов в Settings, защищена политиками.
