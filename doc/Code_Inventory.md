# Инвентарь: “Code” в проекте MyIS

**Примечание:** Поля "Code" не используются как технические идентификаторы (primary keys), поскольку все сущности имеют уникальные GUID-идентификаторы (Id). Поля "Code" служат бизнес-ключами для удобства пользователей и интеграций.

Цель документа — собрать все сущности/поля/константы/методы, где “Code” используется как:
- **бизнес‑ключ** (уникальный идентификатор в рамках справочника/роли),
- **код статуса/действия** (workflow),
- **внешний код** (инпорт/интеграции),
- **технический код ошибки** (Auth/API).

## 1) Core.Domain — доменная модель

### 1.1. Поле `Code` (бизнес‑ключ/справочник)

| Объект | Тип | Где объявлено | Примечание |
|---|---|---|---|
| Role | `string` | `backend/src/Core.Domain/Users/Role.cs:10` | Уникальный бизнес‑ключ роли (проверки/сопоставление). |
| RequestType | `string` | `backend/src/Core.Domain/Requests/Entities/RequestType.cs:10` | Уникальный бизнес‑ключ типа заявки. |
| RequestStatus | `RequestStatusCode` | `backend/src/Core.Domain/Requests/Entities/RequestStatus.cs:10` | Код статуса вынесен в VO `RequestStatusCode`. |
| BodyType | `string` | `backend/src/Core.Domain/Mdm/Entities/BodyType.cs:10` | Справочник MDM (уникален). |
| Counterparty | — | `backend/src/Core.Domain/Mdm/Entities/Counterparty.cs:9` | Поле `Code` удалено (в Access его нет, используется только `Id`). |
| Currency | `string?` | `backend/src/Core.Domain/Mdm/Entities/Currency.cs:10` | Код валюты может отсутствовать. |
| Item | `string` | `backend/src/Core.Domain/Mdm/Entities/Item.cs:11` | Уникальный код номенклатуры/позиции. |
| ItemAttribute | `string` | `backend/src/Core.Domain/Mdm/Entities/ItemAttribute.cs:10` | Код атрибута (справочник). |
| Manufacturer | — | `backend/src/Core.Domain/Mdm/Entities/Manufacturer.cs:10` | Поле `Code` удалено (в Access его нет, используется только `Id`). |
| ParameterSet | `string` | `backend/src/Core.Domain/Mdm/Entities/ParameterSet.cs:10` | Код набора параметров. |
| Symbol | `string` | `backend/src/Core.Domain/Mdm/Entities/Symbol.cs:10` | Код символа. |
| TechnicalParameter | `string` | `backend/src/Core.Domain/Mdm/Entities/TechnicalParameter.cs:10` | Код тех.параметра. |
| UnitOfMeasure | `string?` | `backend/src/Core.Domain/Mdm/Entities/UnitOfMeasure.cs:10` | Код ЕИ может отсутствовать. |

### 1.2. Кодовые поля workflow / интеграций

| Объект | Поле | Тип | Где объявлено | Назначение |
|---|---|---|---|---|
| RequestTransition | `FromStatusCode` | `RequestStatusCode` | `backend/src/Core.Domain/Requests/Entities/RequestTransition.cs:16` | Код статуса “откуда”. |
| RequestTransition | `ToStatusCode` | `RequestStatusCode` | `backend/src/Core.Domain/Requests/Entities/RequestTransition.cs:18` | Код статуса “куда”. |
| RequestTransition | `ActionCode` | `string` | `backend/src/Core.Domain/Requests/Entities/RequestTransition.cs:20` | Код действия (строковый). |
| RequestLine | `ExternalItemCode` | `string?` | `backend/src/Core.Domain/Requests/Entities/RequestLine.cs:17` | Внешний код позиции (без строгого FK, v0.1). |
| Request | `EnsureBodyIsValidForSubmit(requestTypeCode)` | метод | `backend/src/Core.Domain/Requests/Entities/Request.cs:214` | Пример “поведенческой” логики по коду типа (`"SupplyRequest"`). |

### 1.3. Value Object для кодов статусов

- `RequestStatusCode` (VO): `backend/src/Core.Domain/Requests/ValueObjects/RequestStatusCode.cs:5`
  - предопределённые значения: `Draft/Submitted/InReview/...` (`backend/src/Core.Domain/Requests/ValueObjects/RequestStatusCode.cs:19`).

### 1.4. Код‑парсинг/форматирование (MDM)

- `ItemNomenclature.TryParseComponentCode(...)`: `backend/src/Core.Domain/Mdm/Services/ItemNomenclature.cs:46`

## 2) Core.Application — прикладной слой

### 2.1. DTO/Commands с полями `*Code*`

- Auth:
  - `AuthResult.ErrorCode`: `backend/src/Core.Application/Auth/AuthResult.cs:9`
- Requests:
  - `RequestDto.RequestTypeCode/RequestStatusCode`: `backend/src/Core.Application/Requests/Dto/RequestDto.cs:20`
  - `RequestListItemDto.RequestTypeCode/RequestStatusCode`: `backend/src/Core.Application/Requests/Dto/RequestListItemDto.cs:13`
  - `RequestLineDto.ExternalItemCode`: `backend/src/Core.Application/Requests/Dto/RequestLineDto.cs:13`
  - `RequestWorkflowTransitionDto.*Code`: `backend/src/Core.Application/Requests/Dto/RequestWorkflowTransitionDto.cs:10`
  - `ReplaceAdminRequestWorkflowTransitionsCommand.TypeCode/ActionCode`: `backend/src/Core.Application/Requests/Commands/Admin/ReplaceAdminRequestWorkflowTransitionsCommand.cs:9`
  - `CreateAdminRequestTypeCommand.Code`: `backend/src/Core.Application/Requests/Commands/Admin/CreateAdminRequestTypeCommand.cs:9`
  - `CreateAdminRequestStatusCommand.Code`: `backend/src/Core.Application/Requests/Commands/Admin/CreateAdminRequestStatusCommand.cs:9`
- Security:
  - `CreateAdminRoleCommand.Code`: `backend/src/Core.Application/Security/Commands/Admin/CreateAdminRoleCommand.cs:8`
  - `RoleDto.Code`: `backend/src/Core.Application/Security/Dto/RoleDto.cs:8`
  - `UserDetailsDto.RoleCodes`: `backend/src/Core.Application/Security/Dto/UserDetailsDto.cs:15`
  - `UserListItemDto.RoleCodes`: `backend/src/Core.Application/Security/Dto/UserListItemDto.cs:15`
- MDM references (API‑read модели):
  - базовые `Code/*Code`: `backend/src/Core.Application/Mdm/References/Dto/MdmReferenceDtos.cs:8`
- Integration (Component2020 snapshot DTO):
  - `Code/QrCode`: `backend/src/Core.Application/Integration/Component2020/Services/IComponent2020SnapshotReader.cs:19`

### 2.2. Константы кодов действий workflow

- `RequestActionCodes`: `backend/src/Core.Application/Requests/Workflow/RequestActionCodes.cs:3`

### 2.3. Репозитории “по коду” (контракты)

Все методы `*ByCodeAsync` (контракты и реализации) удобно искать командой `rg -n \"ByCodeAsync\" backend/src`.

## 3) Core.Infrastructure — инфраструктура (EF/репозитории/интеграции)

### 3.1. EF Core: конфигурации полей/индексов по `Code`

- Явная маппинг‑колонка `code`:
  - Role: `backend/src/Core.Infrastructure/Data/Configurations/RoleConfiguration.cs:21`
  - RequestType: `backend/src/Core.Infrastructure/Data/Configurations/Requests/RequestTypeConfiguration.cs:25`
  - RequestStatus (VO): `backend/src/Core.Infrastructure/Data/Configurations/Requests/RequestStatusConfiguration.cs:24`
- Индексы:
  - уникальность `Code` на справочниках/ролях: см. `HasIndex(...Code).IsUnique()` в `backend/src/Core.Infrastructure/Data/Configurations` (например `backend/src/Core.Infrastructure/Data/Configurations/Mdm/ItemConfiguration.cs:78`)
  - workflow: уникальность перехода по `(RequestTypeId, FromStatusCode, ActionCode)` — `backend/src/Core.Infrastructure/Data/Configurations/Requests/RequestTransitionConfiguration.cs:58`

### 3.2. Репозитории “по коду” (реализации)

- Requests:
  - `RequestTypeRepository.GetByCodeAsync/ExistsByCodeAsync`: `backend/src/Core.Infrastructure/Requests/Repositories/RequestTypeRepository.cs:29`
  - `RequestStatusRepository.GetByCodeAsync/ExistsByCodeAsync`: `backend/src/Core.Infrastructure/Requests/Repositories/RequestStatusRepository.cs:29`
- Security:
  - `RoleRepository.ExistsByCodeAsync`: `backend/src/Core.Infrastructure/Security/Repositories/RoleRepository.cs:37`
- MDM:
  - `*Repository.FindByCodeAsync/ExistsByCodeAsync`: `backend/src/Core.Infrastructure/Mdm/Repositories` (например `backend/src/Core.Infrastructure/Mdm/Repositories/CurrencyRepository.cs:25`)

### 3.3. Query‑сервисы, где `Code` участвует в фильтрации/сортировке

- `MdmReferencesQueryService`: поиск по `Code`/`Name`/… (`backend/src/Core.Infrastructure/Mdm/Services/MdmReferencesQueryService.cs:38`)

### 3.4. Интеграция Component2020: словари/мапы “по коду”

- Component2020: `Code` сохраняется "как есть" (если есть в Access), для сопоставления используются только внешние ключи/ссылки (`ExternalEntityLink`).

## 4) Core.WebApi — контракты/контроллеры

### 4.1. Контракты запросов/ответов с `*Code*`

- Admin Requests:
  - `AdminRequestTypeCreateRequest.Code`: `backend/src/Core.WebApi/Contracts/Admin/Requests/AdminRequestTypeCreateRequest.cs:5`
  - `AdminRequestStatusCreateRequest.Code`: `backend/src/Core.WebApi/Contracts/Admin/Requests/AdminRequestStatusCreateRequest.cs:5`
  - `AdminReplaceWorkflowTransitionsRequest.TypeCode/ActionCode`: `backend/src/Core.WebApi/Contracts/Admin/Requests/AdminReplaceWorkflowTransitionsRequest.cs:7`
- Admin Security:
  - `AdminRoleCreateRequest.Code`: `backend/src/Core.WebApi/Contracts/Admin/Security/AdminRoleCreateRequest.cs:5`
- Requests:
  - `RequestLineRequest.ExternalItemCode`: `backend/src/Core.WebApi/Contracts/Requests/RequestLineRequest.cs:14`
- Auth:
  - `AuthErrorResponse.Code`: `backend/src/Core.WebApi/Contracts/Auth/AuthErrorResponse.cs:5`

## 5) Frontend (React/TS) — DTO/types/UI

Ключевые файлы, где есть `code/*Code*`:
- Requests DTO: `frontend/src/modules/requests/api/types.ts`
- Requests settings/workflow: `frontend/src/modules/settings/requests/dictionaries/api/types.ts`
- Security settings: `frontend/src/modules/settings/security/api/types.ts`
- MDM references API/types: `frontend/src/modules/references/mdm/api/adminMdmReferencesApi.ts`

Полезный поиск:
- все упоминания `code` в UI: `rg -n \"\\bcode\\b\" frontend/src`

## 6) Документация (doc/)

Файлы, где явно фигурирует “Code” как термин/поле/атрибут:
- `doc/MyIS_Conceptual_Data_Model_v0.3.md`
- `doc/20_ТЗ_Этап1_Requests.md`
- `doc/Requests_Concept.md`
- `doc/Settings_Requests_Dictionaries_S1_Implementation.md`
- `doc/TZ_Iteration_S2_Settings_Security_Employees_Users_Roles.md`
- `doc/TZ_MDM_Item_Dictionaries_Integration_Component2020_v0.1.4.md`

## 7) Быстрые команды для полного “grep‑инвентаря”

```bash
# Все упоминания Code по backend
rg -n "Code" backend/src

# Только определения свойств вида *Code* в C#
rg -n "public\\s+[^\\s]+\\s+[A-Za-z0-9_]*Code\\s*\\{" backend/src

# Репозитории “по коду”
rg -n "ByCodeAsync" backend/src

# Frontend: code‑поля в DTO/UI
rg -n "\\bcode\\b" frontend/src
```
