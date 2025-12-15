# MyIS — краткое описание и целевая картина

## 1. Назначение и контекст

MyIS — модульная информационная система (модульный монолит) для обособленного подразделения, занимающегося разработкой и мелкосерийным производством СВЧ‑модулей.

Основные цели (сокращённо по концепции из [`00_Концепция_системы.md`](Doc/00_Концепция_системы.md)):

- поэтапно заменить функции Компонент‑2020 без остановки текущей деятельности;
- повысить управляемость и прозрачность процессов по заявкам, закупке, производству, складу, инженерному и технологическому контурам;
- дать единую основу для диспетчеризации и аналитики по подразделению.

Система строится как **модульный монолит**:

- одно backend‑приложение на .NET;
- одна основная БД PostgreSQL;
- чётко выделенные доменные модули с собственными сущностями и границами.

Компонент‑2020 рассматривается как внешняя система (источник/приёмник данных) с поэтапным переносом владения данными в MyIS.
Её детализированная структура доступов фиксируется в [Component2020_Access_schema_mermaid.md](../Component2020_Access_schema_mermaid.md) и используется как база для всех интеграционных решений.

Высокоуровневая архитектура и правила по слоям формально зафиксированы в:

- [`Coding_Guidelines_MyIS.md`](.kilocode/rules/Coding_Guidelines_MyIS.md);
- [`MyIS_Architecture_Core_Requirements.md`](.kilocode/rules/MyIS_Architecture_Core_Requirements.md).

Этот файл даёт ИИ сжатый ментальный снимок системы и этапов её развития.

## 2. Основные доменные модули

Планируемые домены (по гл. 5 в [`00_Концепция_системы.md`](Doc/00_Концепция_системы.md)):

- **Requests** — управление всеми типами заявок; единый входной контур, связывающий заявки с заказами клиентов, закупкой, производством и складом; статусы, согласования, история.
- **Customers** — клиенты и заказы клиентов; статусы исполнения заказов, связь с заявками и производственными заказами.
- **Engineering** — изделия, ревизии/версии, спецификации (BOM), конструкторская документация и файлы САПР.
- **Technology** — маршруты и технологические операции, нормы времени, технологические инструкции и карты процессов.
- **Production** — производственные заказы и партии, статусы прохождения стадий, укрупнённое планирование и диспетчеризация производства.
- **Procurement** — закупка: обработка заявок на обеспечение/закупку, коммерческие предложения и счета, заказы поставщикам и их статусы.
- **Warehouse** — склады, ячейки, остатки, резервы и движения.
- **Integration.Component2020** — интеграция с Компонент‑2020: чтение справочников и заказов, запись заказов поставщикам и производственных заказов по мере миграции.
- **Settings** — централизованная админка для справочников и параметров (Requests, безопасность, сотрудники) согласно [`TZ_Iteration_S1_Settings_Module_Requests_Dictionaries.md`](Doc/TZ_Iteration_S1_Settings_Module_Requests_Dictionaries.md:1) и последующим итерациям.

Каждый модуль владеет своими сущностями и таблицами; другие модули взаимодействуют с ним только через публичные контракты Application‑слоя (Use Case, интерфейсы сервисов).

## 3. Этапы проекта и текущий статус

Этапность (см. разд. 3.3 в [`00_Концепция_системы.md`](Doc/00_Концепция_системы.md)):

1. **Этап 1 — Requests и базовая интеграция**
   - модуль Requests как единый входной контур заявок;
   - базовый обмен с Компонент‑2020 (справочники, заказы, упрощённые заказы поставщикам).
2. **Этап 2 — Customers и Procurement**
   - управление заказами клиентов и закупочной деятельностью в MyIS;
   - Компонент‑2020 остаётся в роли учётного слоя.
3. **Этап 3 — Production и Warehouse**
   - перенос производственного и складского контура;
   - внедрение оперативной диспетчеризации.
4. **Этап 4 — Engineering, Technology и аналитика**
   - перенос инженерных и технологических данных;
   - расширенная аналитика и минимизация роли Компонент‑2020.

Фактическое состояние после итераций S1–S2 описано в [`Описание состояния после Этап 0.md`](Doc/Описание состояния после Этап 0.md) и эксплуатационном [`RUNBOOK`](Doc/RUNBOOK.md:1):

- базовый каркас backend и frontend сохранён (слои, Auth, Admin DB API) и используется как опора для новых модулей;
- **Requests** частично реализован: доменный агрегат заявки и связанных сущностей (`backend/src/Core.Domain/Requests/Entities/Request.cs:1`, `backend/src/Core.Domain/Requests/Entities/RequestHistory.cs:1`, `backend/src/Core.Domain/Requests/Entities/RequestLine.cs:1`) обслуживается use case’ами создания/обновления/аудита (`backend/src/Core.Application/Requests/Handlers/CreateRequestHandler.cs:1`, `backend/src/Core.Application/Requests/Handlers/UpdateRequestHandler.cs:1`) и workflow-командами (`backend/src/Core.Application/Requests/Handlers/Workflow/SubmitRequestHandler.cs:1`, `backend/src/Core.Application/Requests/Handlers/Workflow/ApproveRequestHandler.cs:1`); фронтенд покрывает список/карточку/редактирование через `frontend/src/modules/requests/pages/RequestsListPage.tsx:1` и связанные компоненты, а интеграции с внешними системами остаются в планах Этапа 1;
- **Settings** модуль развёрнут: Requests-справочники (типы, статусы, переходы) администрируются через `frontend/src/modules/settings/requests/dictionaries/pages/RequestTypesSettingsPage.tsx:1` и соответствующие use case’ы (`backend/src/Core.Application/Requests/Handlers/Admin/GetAdminRequestTypesHandler.cs:1`, `backend/src/Core.Application/Requests/Handlers/Admin/ReplaceAdminRequestWorkflowTransitionsHandler.cs:1`), а периметр безопасности (сотрудники, пользователи, роли) ведётся через `frontend/src/modules/settings/security/pages/EmployeesSettingsPage.tsx:1` и обработчики (`backend/src/Core.Application/Security/Handlers/Admin/GetAdminEmployeesHandler.cs:1`, `backend/src/Core.Application/Security/Handlers/Admin/CreateAdminUserHandler.cs:1`);
- эксплуатационные процедуры и on-call описаны в [`RUNBOOK`](Doc/RUNBOOK.md:1), а обязательный CI (`.github/workflows/ci.yml:1`) выполняет dotnet restore/build/test и фронтовые `npm run lint / check / build` на каждом push/pull request в `master`.

## 4. Высокоуровневый поток запросов

Логический поток для пользовательского запроса:

```mermaid
flowchart LR
  Frontend[Frontend React SPA] --> WebApi[Backend WebApi]
  WebApi --> Application[Application Use Cases]
  Application --> Domain[Domain]
  Application --> Infrastructure[Infrastructure]
  Infrastructure --> Db[PostgreSQL Core Schema]
  Infrastructure --> Component2020[Integration Component2020]
```

Ключевые моменты:

- Frontend обращается к REST API (`/api/...`) через WebApi;
- WebApi не содержит бизнес‑логики, только маппинг HTTP ↔ DTO и вызов Use Case;
- Application реализует Use Case как единственную точку изменения состояния;
- Domain содержит только предметную модель (entities, value objects, domain services);
- Infrastructure реализует доступ к PostgreSQL и внешним системам (Компонент‑2020 и др.).

## 5. Что уже можно считать «стабильным каркасом»

С учётом Этапа 0 (см. [`Описание состояния после Этап 0.md`](Doc/Описание состояния после Этап 0.md)) стабильным и опорным для последующей разработки считаем:

- слоистую архитектуру backend (Domain/Application/Infrastructure/WebApi) и их границы;
- модель users/roles/user_roles и базовый Auth‑контур (login/me/logout, cookie `.MyIS.Auth`);
- инфраструктуру настройки БД (Admin DB API + UI DB Setup) и политику «приложение стартует даже без строки подключения»;
- App Shell на фронтенде, базовую маршрутизацию и guard’ы DbStatusGuard и RequireAuth.

Все новые функции (начиная с модуля Requests) должны проектироваться и реализовываться, строго опираясь на этот каркас и архитектурные правила из `.kilocode/rules`.
