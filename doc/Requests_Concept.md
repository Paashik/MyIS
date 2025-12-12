# MyIS — Концепция модуля заявок (Requests) v1.0

Файл: `Doc/Requests_Concept.md`  
Связан с документами:
- *MyIS_Conceptual_Data_Model_v0.x*
- *MyIS_Architecture_Core_Requirements.md*
- *Coding_Guidelines_MyIS.md*

## 1. Назначение и место в архитектуре

Модуль **Requests** реализует единый механизм работы с заявками в MyIS.

Заявка (Request) — это универсальный управленческий объект, который:
- инициирует или требует выполнение работы;
- связывает домены (Customers, Engineering, Technology, Production, Warehouse, Procurement);
- даёт управляемый жизненный цикл (workflow);
- обеспечивает историю, комментарии и связи с внешними системами.

Requests — отдельный домен в концептуальной модели данных MyIS и реализуется как агрегат уровня `requests.Request`.

## 2. Основные оси классификации заявок

### 2.1. Тип заявки (RequestType) — «что это за заявка»

Тип определяет семантику, набор полей и сценарии использования заявки.

Примеры типов (минимальный набор v1.0):

- **CustomerDevelopment** — заявка заказчика / на разработку.
- **InternalProductionRequest** — внутренняя производственная заявка.
- **ChangeRequest** — заявка на изменение КД/ТД/состава/маршрута.
- **SupplyRequest** — заявка на обеспечение/закупку (к внешней стороне).
- **ExternalTechStageRequest** — заявка на внешний технологический этап.
- **ExternalProductionRequest** — изготовление у контрагентов.
- **InternalServiceRequest** — внутренняя заявка на услуги (резерв).

Тип заявки хранится в справочнике `RequestType` и используется в UI для:
- фильтрации;
- выбора формы (тела) заявки;
- выбора маршрута обработки (workflow).

### 2.2. Направление заявки (RequestDirection) — «куда адресована»

Отдельная ось классификации:

```csharp
public enum RequestDirection
{
    Incoming = 1, // к нам (внутренние и от заказчика)
    Outgoing = 2  // от нас (к головному, поставщикам, контрагентам)
}
```

На уровне данных это свойство `Direction` у `RequestType`.

- **Incoming** — заявки, которые инициируют работу *внутри* подразделения/организации:
  - заявки заказчика;
  - внутренние производственные;
  - заявки на изменение;
  - внутренние сервисные.

- **Outgoing** — заявки, адресованные *наружу*:
  - заявки на закупку/обеспечение (Supply/Procurement);
  - заявки на внешний техэтап;
  - заявки на изготовление у контрагентов;
  - внешние заявки на изменение.

Примеры матрицы тип ↔ направление:

| RequestType                | Direction |
|---------------------------|-----------|
| CustomerDevelopment       | Incoming  |
| InternalProductionRequest | Incoming  |
| ChangeRequest             | Incoming  |
| InternalServiceRequest    | Incoming  |
| SupplyRequest             | Outgoing  |
| ExternalTechStageRequest  | Outgoing  |
| ExternalProductionRequest | Outgoing  |

## 3. Агрегат Request: структура

Агрегат `Request` включает:

1. **Шапку** (Header).
2. **Тело** (Body): текст и/или строки (RequestLines).
3. **Историю** (RequestHistory).
4. **Комментарии** (RequestComment).

Вложения (файлы) будут подключены через общий модуль Attachments и в этом документе не детализируются.

### 3.1. Шапка заявки (Header)

Обязательные поля:

- `Id` — Guid, первичный ключ.
- `RequestTypeId` — FK на `RequestType`.
- `RequestStatusId` — FK на `RequestStatus`.
- `InitiatorId` — FK на core.users.
- `Title` — краткое наименование.
- `CreatedAt`, `UpdatedAt`.
- `Priority` — приоритет (enum/справочник, v0.1 — nullable).
- `DueDate` — желаемая дата исполнения (опционально).

Связи с другими сущностями:

- `RelatedEntityType` / `RelatedEntityId` — ссылка на объект MyIS (CustomerOrder, ProductionOrder, ProductRevision и т.п.).
- `ExternalReferenceId` — ссылка на внешний объект (Component-2020, внешняя BPM, внешняя ERP).

Направление заявки определяется через `RequestType.Direction`.

### 3.2. Тело заявки (Body)

Тело бывает трёх видов:

1. **Текстовое тело** — `BodyText` (Markdown/простой текст).
   - Используется, например, для ChangeRequest и внутренних сервисных заявок.

2. **Параметрическое тело** — набор дополнительных полей (через профиль типа заявки).
   - Пример: InternalProductionRequest — изделие/ревизия, объём, тип партии.

3. **Позиционное тело** — набор строк `RequestLine`.
   - Используется для заявок с перечнем позиций (номенклатура, материалы, услуги).
   - Ключевой случай — **SupplyRequest**.

### 3.3. Строки заявки (RequestLine) — v0.1

Для v0.1 вводится унифицированная сущность строк:

```csharp
public class RequestLine
{
    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public int LineNo { get; private set; }

    // Номенклатура
    public Guid? ItemId { get; private set; }             // mdm.Item, когда MyIS станет мастер-справочником
    public Guid? ItemRevisionId { get; private set; }     // mdm.ItemRevision (опционально)
    public string? ExternalItemCode { get; private set; } // код из Component-2020/др. систем
    public string? Description { get; private set; }      // текст, если нет ItemId или нужен override

    // Количество
    public decimal Quantity { get; private set; }
    public Guid? UnitOfMeasureId { get; private set; }

    // Срок и место
    public DateTime? NeedByDate { get; private set; }
    public Guid? TargetWarehouseId { get; private set; }

    // Поставщик / аналог (актуально для SupplyRequest)
    public Guid? SupplierId { get; private set; }
    public string? SupplierName { get; private set; }
    public string? SupplierContact { get; private set; }
    public Guid? PreferredAnalogItemId { get; private set; }
    public string? AllowedAnalogsDescription { get; private set; }

    // Интеграция
    public Guid? ExternalRowReferenceId { get; private set; } // связь с позицией в Component-2020
    public Guid? PurchaseOrderId { get; private set; }        // связь с заказом поставщика (в будущем)
    public Guid? PurchaseOrderLineId { get; private set; }
}
```

В v0.1 `RequestLine` используется в первую очередь для **SupplyRequest** и, при необходимости, для внутренних производственных заявок.

### 3.4. История и комментарии

- `RequestHistory` — события изменения заявки (создание, смена статуса, изменение тела, системные действия).
- `RequestComment` — пользовательские комментарии.

Эти сущности уже реализованы в каркасе и используются для аудита и аналитики.

## 4. SupplyRequest — заявка на обеспечение/закупку

**SupplyRequest** — ключевой тип исходящей заявки.

Роль в цепочке:

1. Входящая заявка/заказ клиента → CustomerOrder.
2. На основании CustomerOrder / плана / внутренних заявок формируется `SupplyRequest`:
   - привязка к заказу клиента или другому основанию;
   - перечень позиций для закупки (RequestLine).
3. На основании согласованной `SupplyRequest` создаются `PurchaseOrder`(ы):
   - сначала в Component-2020;
   - позже в домене Procurement MyIS.

Ключевые свойства SupplyRequest:

- `RequestType.Code = "SupplyRequest"`.
- `Direction = Outgoing`.
- `RelatedEntityType = "CustomerOrder"` (или другой тип основания).
- Тело — **позиционный список** (RequestLine) + текстовое описание (`BodyText`).

## 5. ER-диаграмма домена Requests (v0.1, без вложений)

```mermaid
erDiagram
    REQUEST ||--o{ REQUEST_HISTORY : has
    REQUEST ||--o{ REQUEST_COMMENT : has
    REQUEST ||--o{ REQUEST_LINE : has
    REQUEST }o--|| REQUEST_TYPE : "has type"
    REQUEST }o--|| REQUEST_STATUS : "has status"

    REQUEST {
        guid Id
        guid RequestTypeId
        guid RequestStatusId
        guid InitiatorId
        string Title
        string? BodyText
        string? RelatedEntityType
        guid? RelatedEntityId
        guid? ExternalReferenceId
        datetime CreatedAt
        datetime UpdatedAt
        datetime? DueDate
        int? Priority
    }

    REQUEST_TYPE {
        guid Id
        string Code
        string Name
        string? Description
        int Direction  // Incoming / Outgoing
    }

    REQUEST_STATUS {
        guid Id
        string Code
        string Name
        boolean IsFinal
    }

    REQUEST_HISTORY {
        guid Id
        guid RequestId
        string Action
        guid PerformedBy
        datetime Timestamp
        string? OldValue
        string? NewValue
        string? Comment
    }

    REQUEST_COMMENT {
        guid Id
        guid RequestId
        guid AuthorId
        string Text
        datetime CreatedAt
    }

    REQUEST_LINE {
        guid Id
        guid RequestId
        int LineNo
        guid? ItemId
        guid? ItemRevisionId
        string? ExternalItemCode
        string? Description
        decimal Quantity
        guid? UnitOfMeasureId
        datetime? NeedByDate
        guid? TargetWarehouseId
        guid? SupplierId
        string? SupplierName
        string? SupplierContact
        guid? PreferredAnalogItemId
        string? AllowedAnalogsDescription
        guid? ExternalRowReferenceId
        guid? PurchaseOrderId
        guid? PurchaseOrderLineId
    }
```

## 6. Структура таблиц для миграций (DDL-скелет)

Ниже приведён ориентировочный DDL для PostgreSQL. Фактические имена и типы могут быть уточнены на этапе реализации миграций.

```sql
-- Схема домена заявок
CREATE SCHEMA IF NOT EXISTS requests;

-- Справочник типов заявок
CREATE TABLE requests.request_types (
    id              uuid PRIMARY KEY,
    code            text NOT NULL UNIQUE,
    name            text NOT NULL,
    description     text,
    direction       integer NOT NULL, -- 1 = Incoming, 2 = Outgoing
    created_at      timestamp with time zone NOT NULL DEFAULT now()
);

-- Справочник статусов заявок
CREATE TABLE requests.request_statuses (
    id          uuid PRIMARY KEY,
    code        text NOT NULL UNIQUE,
    name        text NOT NULL,
    is_final    boolean NOT NULL DEFAULT false
);

-- Заявки
CREATE TABLE requests.requests (
    id                  uuid PRIMARY KEY,
    request_type_id     uuid NOT NULL REFERENCES requests.request_types(id),
    request_status_id   uuid NOT NULL REFERENCES requests.request_statuses(id),
    initiator_id        uuid NOT NULL REFERENCES core.users(id),
    title               text NOT NULL,
    body_text           text,
    related_entity_type text,
    related_entity_id   uuid,
    external_reference_id uuid,
    due_date            timestamp with time zone,
    priority            integer,
    created_at          timestamp with time zone NOT NULL DEFAULT now(),
    updated_at          timestamp with time zone NOT NULL DEFAULT now()
);

CREATE INDEX ix_requests_type ON requests.requests(request_type_id);
CREATE INDEX ix_requests_status ON requests.requests(request_status_id);
CREATE INDEX ix_requests_initiator ON requests.requests(initiator_id);

-- Строки заявок (универсальный формат, v0.1 — для SupplyRequest и производственных заявок)
CREATE TABLE requests.request_lines (
    id                      uuid PRIMARY KEY,
    request_id              uuid NOT NULL REFERENCES requests.requests(id) ON DELETE CASCADE,
    line_no                 integer NOT NULL,
    item_id                 uuid,
    item_revision_id        uuid,
    external_item_code      text,
    description             text,
    quantity                numeric(18,6) NOT NULL,
    unit_of_measure_id      uuid,
    need_by_date            timestamp with time zone,
    target_warehouse_id     uuid,
    supplier_id             uuid,
    supplier_name           text,
    supplier_contact        text,
    preferred_analog_item_id uuid,
    allowed_analogs_description text,
    external_row_reference_id uuid,
    purchase_order_id       uuid,
    purchase_order_line_id  uuid
);

CREATE INDEX ix_request_lines_request ON requests.request_lines(request_id);
CREATE INDEX ix_request_lines_item ON requests.request_lines(item_id);
CREATE INDEX ix_request_lines_external_row ON requests.request_lines(external_row_reference_id);

-- История заявок
CREATE TABLE requests.request_history (
    id          uuid PRIMARY KEY,
    request_id  uuid NOT NULL REFERENCES requests.requests(id) ON DELETE CASCADE,
    action      text NOT NULL,
    performed_by uuid NOT NULL REFERENCES core.users(id),
    timestamp   timestamp with time zone NOT NULL DEFAULT now(),
    old_value   text,
    new_value   text,
    comment     text
);

CREATE INDEX ix_request_history_request ON requests.request_history(request_id);

-- Комментарии к заявкам
CREATE TABLE requests.request_comments (
    id          uuid PRIMARY KEY,
    request_id  uuid NOT NULL REFERENCES requests.requests(id) ON DELETE CASCADE,
    author_id   uuid NOT NULL REFERENCES core.users(id),
    text        text NOT NULL,
    created_at  timestamp with time zone NOT NULL DEFAULT now()
);

CREATE INDEX ix_request_comments_request ON requests.request_comments(request_id);
```

## 7. Связь с общей концепцией данных MyIS

- `requests.requests` — агрегат верхнего уровня, связывающий:
  - `customers.customer_orders`,
  - `production.production_orders`,
  - `procurement.purchase_orders`,
  - `engineering.items/item_revisions`,
  - `technology.routes/operations`,
  - `warehouse.documents`.

- `requests.request_lines` — точка соприкосновения с:
  - `mdm.items` / `mdm.item_revisions`,
  - `warehouse` (складские операции),
  - `procurement` (строки заказов поставщикам).

- `ExternalReferenceId` и `ExternalRowReferenceId` позволяют интегрировать MyIS с Компонент-2020 и другими внешними системами без нарушения внутренней нормализации модели.

Данный документ является базой для:
- ТЗ Этапа 2 по Requests (workflow + строки);
- уточнения концептуальной модели данных MyIS (v0.3+);
- настройки правил проектирования кода MyIS для домена Requests.
