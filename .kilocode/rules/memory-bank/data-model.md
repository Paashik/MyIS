# MyIS — Концептуальная модель данных (memory bank)

Этот файл конспектирует и фиксирует основные правила из документа [`MyIS_Conceptual_Data_Model_v0.3.md`](Doc/MyIS_Conceptual_Data_Model_v0.3.md:2).
Полная подробная модель всегда берётся из исходного документа; здесь только «оперативный» срез для ИИ.

## 1. Назначение

- Задать *единую* концептуальную модель данных для всех доменов MyIS.
- Не допустить дублирования сущностей и «самодельных» таблиц, противоречащих модели.
- Подсказывать ИИ, где размещать новые сущности и какие поля у базовых объектов.

Если при генерации кода возникает противоречие между этим файлом и исходным документом, приоритет у [`MyIS_Conceptual_Data_Model_v0.3.md`](Doc/MyIS_Conceptual_Data_Model_v0.3.md:2).

## 2. Доменная структура и схемы БД

Одна БД PostgreSQL (например, *myis_db*), схемы по доменам:

- `core` — пользователи, роли, измерения, атрибуты, файлы.
- `mdm` — номенклатура и классификация (`Item`, `ItemRevision` и т.п.).
- `engineering` — изделия, BOM, КД, ревизии.
- `technology` — ТП, маршруты, операции, рабочие центры, оборудование.
- `warehouse` — склады, ячейки, партии, остатки и движения.
- `requests` — заявки, статусы, workflow, история.
- `customers` — клиенты, контакты, заказы клиентов.
- `production` — производственные заказы, WIP, партии производства.
- `procurement` — поставщики, заказы на закупку.
- `costing` — элементы себестоимости, ставки, фактическая стоимость.
- `integration` — внешние системы, ссылки, очереди синхронизации.
- `public` — служебные объекты EF Core (миграции и т.п.).

Правила:

- Домены **фиксированы**, объединять/делить их нельзя.
- Каждая сущность принадлежит одному домену-владельцу; только этот домен меняет её состояние.
- Другие домены ссылаются по ключам или читают проекции через Application-слой, но не вносят прямых изменений.

## 3. Общие правила по идентификаторам, ревизиям и статусам

- Все сущности используют `Guid Id` как суррогатный PK.
- Дополнительно часто есть человекочитаемый `Code` (строка, уникальная в домене).
- Для версионных сущностей используются отдельные сущности `*Revision`:
  - `ItemRevision`, `ProductRevision`, `BOMRevision`, `TechProcessRevision` и др.
  - ревизии неизменяемы (меняется только статус); новые изменения → новая ревизия.
- Статусы не произвольные строки, а либо:
  - справочник в своей схеме (`RequestStatus`, `RevisionStatus`),
  - либо enum/VO в коде с маппингом в таблицу.
- Количество и единицы измерения моделируются через VO `Quantity`:
  - на физическом уровне — пара полей `...Value` (decimal) + `...UoMId` (ссылка на `core.UnitOfMeasure`).

## 4. Ключевые сущности по доменам (укороченный список)

### 4.1. core

- `core.User` — пользователи:
  - `Id`, `UserName`, `FullName`, `Email`, `IsActive`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`.
- `core.Role` — роли:
  - `Id`, `Code`, `Name`, `IsSystem`.
- `core.UnitOfMeasure` — единицы измерения:
  - `Id`, `Code`, `Name`, `Dimension`.

### 4.2. mdm (Master Data)

- `mdm.Item` — номенклатура (компонент, материал, узел, изделие, услуга):
  - `Id`, `Code`, `Name`, `ItemType`, `DefaultUoMId`, `IsStocked`, `IsActive`.
- `mdm.ItemRevision` — ревизия номенклатуры:
  - `Id`, `ItemId`, `RevisionCode`, `Status`, `EffectiveFrom`, `EffectiveTo`, `CreatedAt`, `CreatedBy`.

### 4.3. engineering

- `engineering.Product` — изделие (часто ссылка 1:1 на `Item` определённого типа).
- `engineering.ProductRevision` — ревизия изделия:
  - `Id`, `ProductId`, `ItemRevisionId`, `RevisionCode`, `Status`, `CreatedAt`, `CreatedBy`.
- `engineering.BOMHeader` / `BOMRevision` / `BOMItem` — спецификации:
  - `BOMHeader`: `Id`, `ProductRevisionId`, `Code`, `Name`.
  - `BOMRevision`: `Id`, `BOMHeaderId`, `RevisionCode`, `Status`, `CreatedAt`, `CreatedBy`.
  - `BOMItem`: `Id`, `BOMRevisionId`, `ComponentItemRevisionId`, `QuantityValue`, `QuantityUoMId`, `IsOptional`, `Notes`.

### 4.4. technology

- `TechProcess` / `TechProcessRevision` — технологические процессы и их ревизии.
- `Route` / `RouteOperation` — маршруты и операции:
  - `Route`: `Id`, `TechProcessRevisionId`, `Name`.
  - `RouteOperation`: `Id`, `RouteId`, `Sequence`, `OperationTypeId`, `WorkCenterId`, `EquipmentId`, `PlannedDurationMinutes`.
- `WorkCenter`, `Equipment` — рабочие центры и оборудование.

### 4.5. warehouse

- `Warehouse` — склады (`Id`, `Code`, `Name`).
- `Location` — ячейки/зоны склада (`Id`, `WarehouseId`, `Code`, `Name`).
- `StockItem` — агрегированная запись по запасам (`ItemRevisionId`, `WarehouseId`, количество + UoM).
- `StockBatch` — партии и серийные номера (`StockItemId`, `BatchNumber`, `SerialNumber`, даты).

### 4.6. requests

- `Request` — универсальная заявка:
  - `Id`, `RequestTypeId`, `StatusId`, `Title`, `Description`, `CreatedBy`, `CreatedAt`, `DueDate`, `RelatedEntityType`, `RelatedEntityId`.
- `RequestType` — типы заявок (`Id`, `Code`, `Name`).
- `RequestStatus` — статусы заявок (`Id`, `Code`, `Name`).

Остальные домены (`customers`, `production`, `procurement`, `costing`, `integration`) подробно раскрыты в исходном документе и должны использоваться оттуда при проектировании.

## 5. Правила для ИИ при работе с данными

ИИ, генерируя код MyIS, обязан:

- Сначала определить домен новой функциональности и использовать соответствующую схему (см. раздел 2).
- Проверять, не существует ли уже подходящей сущности в [`MyIS_Conceptual_Data_Model_v0.3.md`](Doc/MyIS_Conceptual_Data_Model_v0.3.md:382) перед созданием новой.
- Не вводить сущности, дублирующие смысл `Item`, `ItemRevision`, `ProductRevision`, `BOMItem`, `TechProcessRevision`, `Request` и т.д.
- Соблюдать общие правила:
  - GUID `Id` для всех сущностей;
  - ревизии через отдельные `*Revision`-сущности;
  - количество через пару `Value + UnitOfMeasure` / `QuantityValue + QuantityUoMId`;
  - статусы через справочники/enum/VO, а не свободный текст.
- Для спорных случаев (новые поля, связи, статусы) ссылаться на исходный документ и, при необходимости, просить уточнения в ТЗ, а не придумывать модель «с нуля».

Этот файл должен использоваться совместно с:

- архитектурными правилами [`architecture.md`](.kilocode/rules/memory-bank/architecture.md:1);
- правилами кодирования [`Coding_Guidelines_MyIS.md`](.kilocode/rules/Coding_Guidelines_MyIS.md:1);
- технологическим контекстом [`tech.md`](.kilocode/rules/memory-bank/tech.md:1).