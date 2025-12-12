# Requests — доменный конспект (memory bank)

Этот файл даёт сжатое, опорное описание домена Requests для ИИ. Полная концепция модуля описана в [`Requests_Concept.md`](doc/Requests_Concept.md:1).

## 1. Назначение модуля Requests

- Requests — отдельный домен и агрегат верхнего уровня [`Request`](doc/Requests_Concept.md:82) (сущность `requests.Request` в БД), реализующий единый механизм работы с заявками в MyIS.
- Заявка инициирует или требует выполнение работы и связывает несколько доменов (Customers, Engineering, Technology, Production, Warehouse, Procurement).
- Модуль даёт управляемый жизненный цикл заявок (workflow), историю изменений, комментарии и связи с внешними системами (в т.ч. Компонент‑2020).
- В БД используется схема `requests`; агрегат [`Request`](doc/Requests_Concept.md:204) владеет своими таблицами (например, [`requests.requests`](doc/Requests_Concept.md:304), [`requests.request_types`](doc/Requests_Concept.md:286), [`requests.request_statuses`](doc/Requests_Concept.md:296), [`requests.request_lines`](doc/Requests_Concept.md:325), [`requests.request_history`](doc/Requests_Concept.md:352), [`requests.request_comments`](doc/Requests_Concept.md:366)).
- Requests выступает единым входным контуром для всех типов заявок; за деталями по бизнес‑процессам обращаться к [`01_Бизнес-процессы.md`](Doc/01_Бизнес-процессы.md:1).

## 2. Типы заявок и их особенности

Базовая ось классификации — [`RequestType`](doc/Requests_Concept.md:220) (справочник типов заявок). Тип определяет:

- семантику заявки;
- набор обязательных полей и формат тела (текст, параметры, строки);
- применимый маршрут обработки (workflow) и участников процесса;
- направление (входящая / исходящая заявка).

Минимальный набор типов v1.0:

- [`CustomerDevelopment`](doc/Requests_Concept.md:29) — заявка заказчика / на разработку, инициирует работу по новому запросу клиента.
- [`InternalProductionRequest`](doc/Requests_Concept.md:30) — внутренняя производственная заявка, запускает производство внутри подразделения.
- [`ChangeRequest`](doc/Requests_Concept.md:31) — заявка на изменение КД/ТД/состава/маршрута, влияет на Engineering/Technology.
- [`SupplyRequest`](doc/Requests_Concept.md:173) — заявка на обеспечение/закупку; ключевой исходящий тип, формирует перечень позиций к закупке.
- [`ExternalTechStageRequest`](doc/Requests_Concept.md:33) — заявка на внешний технологический этап (отдача операций внешним контрагентам).
- [`ExternalProductionRequest`](doc/Requests_Concept.md:34) — заявка на изготовление у контрагентов (внешнее производство).
- [`InternalServiceRequest`](doc/Requests_Concept.md:35) — внутренняя заявка на услуги и вспомогательные операции.

[`RequestType`](doc/Requests_Concept.md:220) используется в UI для:

- фильтрации списка заявок;
- выбора шаблона/формы тела заявки;
- выбора маршрута обработки и ответственных.

### 2.1. Направление заявки (RequestDirection)

Для [`RequestType`](doc/Requests_Concept.md:220) задаётся направление (`Direction`), соответствующее перечислению [`RequestDirection`](doc/Requests_Concept.md:47):

- `Incoming` — заявки, инициирующие работу внутри подразделения:
  - [`CustomerDevelopment`](doc/Requests_Concept.md:29);
  - [`InternalProductionRequest`](doc/Requests_Concept.md:30);
  - [`ChangeRequest`](doc/Requests_Concept.md:31);
  - [`InternalServiceRequest`](doc/Requests_Concept.md:35).
- `Outgoing` — заявки, адресованные наружу:
  - [`SupplyRequest`](doc/Requests_Concept.md:173);
  - [`ExternalTechStageRequest`](doc/Requests_Concept.md:33);
  - [`ExternalProductionRequest`](doc/Requests_Concept.md:34).

Направление влияет на дальнейшие цепочки (создание заказов поставщикам, внешние производства и т.п.), но само по себе не определяет статусы — они задаются через [`RequestStatus`](doc/Requests_Concept.md:228) и конфигурацию workflow.

## 3. Статусы и базовый workflow

Статус заявки хранится в справочнике [`RequestStatus`](doc/Requests_Concept.md:228) и типизирован (код, имя, признак финальности).

Общая логика жизненного цикла (упрощённо, может отличаться по типам):

- Черновик — заявка создана, но ещё не запущена в процесс.
- На согласовании — проходит согласование у ответственных.
- Согласована — одобрена к исполнению.
- В работе — выполняются действия по заявке (производство, закупка, изменения и т.п.).
- Закрыта — выполнена и формально завершена.
- Отклонена — прекращена без исполнения.

Требования к workflow:

- Допустимые переходы между статусами определяются доменной логикой и/или конфигурацией; прямые произвольные смены недопустимы.
- Все изменения статуса и важных полей фиксируются в [`RequestHistory`](doc/Requests_Concept.md:235) с указанием действия, пользователя и времени.
- Нельзя реализовывать бизнес‑логику статусов на уровне WebApi или фронтенда; статусы управляются доменом Requests (Domain/Application).
- Для каждого типа заявки (особенно [`SupplyRequest`](doc/Requests_Concept.md:173)) могут задаваться свои допустимые маршруты и проверки.

## 4. Связи с другими доменами

Requests связывает заявки с объектами других доменов через универсальные ссылки:

- `RelatedEntityType` / `RelatedEntityId` — ссылка на внутреннюю сущность MyIS:
  - заказы клиентов (`customers.customer_orders`);
  - производственные заказы (`production.production_orders`);
  - заказы поставщикам (`procurement.purchase_orders`);
  - изделия, ревизии, спецификации (`engineering`);
  - маршруты и операции (`technology`);
  - складские документы (`warehouse.documents`) и др.
- `ExternalReferenceId` — ссылка на внешний объект (Компонент‑2020, внешняя BPM/ERP).
- Для строк:
  - `ExternalRowReferenceId` — связь строки заявки с внешней позицией (например, строкой заказа в Компонент‑2020);
  - `PurchaseOrderId` / `PurchaseOrderLineId` — связь с заказами поставщика в домене Procurement (текущем или будущем).

Важные принципы:

- Requests владеет только своими таблицами; он не изменяет напрямую объекты других доменов.
- Другие домены могут создавать/изменять заявки только через публичные Use Case слоя Application домена Requests.
- Интеграция с Компонент‑2020 и другими внешними системами реализуется в Infrastructure, а не в Domain/Application.

Отдельно выделяется [`SupplyRequest`](doc/Requests_Concept.md:173):

- Связан с заказами клиентов (CustomerOrder) или другими основаниями через `RelatedEntityType` / `RelatedEntityId`.
- Содержит позиционный список [`RequestLine`](doc/Requests_Concept.md:254) как базу для формирования заказов поставщикам.
- На основе согласованной [`SupplyRequest`](doc/Requests_Concept.md:173) создаются `PurchaseOrder`(ы) в Компонент‑2020, а в дальнейшем — в домене Procurement MyIS.

## 5. Ключевые сущности и поля (укороченный список)

Основной агрегат:

- [`Request`](doc/Requests_Concept.md:204) (таблица [`requests.requests`](doc/Requests_Concept.md:304)):
  - `Id` — Guid, PK.
  - `RequestTypeId` — тип заявки ([`RequestType`](doc/Requests_Concept.md:220)).
  - `RequestStatusId` — статус ([`RequestStatus`](doc/Requests_Concept.md:228)).
  - `InitiatorId` — пользователь‑инициатор (`core.users`).
  - `Title` — краткое наименование.
  - `BodyText` — текстовое тело (опционально).
  - `RelatedEntityType` / `RelatedEntityId` — связь с внутренними сущностями.
  - `ExternalReferenceId` — связь с внешними системами.
  - `DueDate` — желаемая дата исполнения (опционально).
  - `Priority` — приоритет (enum/справочник, может быть nullable в ранних версиях).
  - `CreatedAt`, `UpdatedAt`.

Справочники:

- [`RequestType`](doc/Requests_Concept.md:220) (таблица [`requests.request_types`](doc/Requests_Concept.md:286)):
  - `Id`, `Code`, `Name`, `Description`.
  - `Direction` — направление (`Incoming` / `Outgoing`).
- [`RequestStatus`](doc/Requests_Concept.md:228) (таблица [`requests.request_statuses`](doc/Requests_Concept.md:296)):
  - `Id`, `Code`, `Name`, `IsFinal`.

Строки заявки:

- [`RequestLine`](doc/Requests_Concept.md:254) (таблица [`requests.request_lines`](doc/Requests_Concept.md:325)) — унифицированные строки для [`SupplyRequest`](doc/Requests_Concept.md:173) и, при необходимости, производственных заявок:
  - `Id`, `RequestId`, `LineNo`.
  - Номенклатура: `ItemId`, `ItemRevisionId`, `ExternalItemCode`, `Description`.
  - Количество: `Quantity`, `UnitOfMeasureId` (на уровне кода следует использовать VO [`Quantity`](.kilocode/rules/memory-bank/data-model.md:47)).
  - Срок и место: `NeedByDate`, `TargetWarehouseId`.
  - Поставщик/аналоги: `SupplierId`, `SupplierName`, `SupplierContact`, `PreferredAnalogItemId`, `AllowedAnalogsDescription`.
  - Интеграция: `ExternalRowReferenceId`, `PurchaseOrderId`, `PurchaseOrderLineId`.

Аудит и комментарии:

- [`RequestHistory`](doc/Requests_Concept.md:235) (таблица [`requests.request_history`](doc/Requests_Concept.md:352)):
  - фиксирует события по заявке (создание, смена статуса, существенные изменения тела и системные действия);
  - содержит пользователя (`PerformedBy`), время (`Timestamp`), старые/новые значения и комментарий.
- [`RequestComment`](doc/Requests_Concept.md:247) (таблица [`requests.request_comments`](doc/Requests_Concept.md:366)):
  - хранит пользовательские комментарии по заявке с автором и временем создания.

## 6. Требования и ограничения для ИИ

При генерации кода для домена Requests ИИ обязан:

- Считать Requests самостоятельным доменным модулем со своей схемой БД `requests` и агрегатом [`Request`](doc/Requests_Concept.md:204).
- Не создавать сущности/таблицы, дублирующие смысл [`Request`](doc/Requests_Concept.md:204), [`RequestType`](doc/Requests_Concept.md:220), [`RequestStatus`](doc/Requests_Concept.md:228), [`RequestLine`](doc/Requests_Concept.md:254), [`RequestHistory`](doc/Requests_Concept.md:235), [`RequestComment`](doc/Requests_Concept.md:247).
- Любые изменения статуса, истории и строк заявки проводить через доменные методы и Use Case слоя Application, а не через прямые SQL/EF‑обновления из других модулей.
- Сохранять инвариант: каждая заявка имеет ровно один текущий статус и непрерывную историю изменений (`RequestHistory`).
- Для количественных полей в коде использовать VO [`Quantity`](.kilocode/rules/memory-bank/data-model.md:47) (даже если на уровне БД это пара `decimal + UnitOfMeasureId`).
- Использовать `RelatedEntityType` / `RelatedEntityId` для связи с другими доменами, а не добавлять прямые FK на «чужие» таблицы поверх концептуальной модели.
- Для интеграции с Компонент‑2020 и др. внешними системами опираться на поля `ExternalReferenceId` / `ExternalRowReferenceId` и реализовывать доступ через Infrastructure‑слой.
- При добавлении новых типов заявок или расширении структуры опираться на [`Requests_Concept.md`](doc/Requests_Concept.md:1) и общую модель данных [`MyIS_Conceptual_Data_Model_v0.3.md`](Doc/MyIS_Conceptual_Data_Model_v0.3.md:2), а не придумывать модель «с нуля».

Для деталей бизнес‑процессов, статусов и полей всегда следует обращаться к исходному документу [`Requests_Concept.md`](doc/Requests_Concept.md:1) и сопутствующим ТЗ по Этапу 1.

