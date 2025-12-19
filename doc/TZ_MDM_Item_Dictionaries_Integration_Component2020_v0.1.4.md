# ТЗ для ИИ-агента: MDM Item + Справочники + Интеграция с Компонент-2020 (зеркалирование Access) — v0.1

## Принятые уточнения (зафиксировано)

- **Формат базы Компонент-2020:** Microsoft Access **.mdb**.
- **Источник структуры (таблицы/поля):** файл `.kilocode/rules/Component2020_Access_schema_mermaid.md` (источник истины в этом репозитории).
- **Режим номенклатуры (Iteration 1):** **ExternalMaster** — в MyIS **нельзя создавать** номенклатуру; все `Item` зеркалируются из Компонента-2020. Поддержка локальных позиций допускается в будущем **без изменения модели данных** (через поле `Source` и `ExternalId`).
- **Критичные поля для закупки (MVP):** `Name` + `ManufacturerPN` (MPN).
  - В Компонент-2020 MPN соответствует полю `PartNumber` в таблице `Component` (и аналогично `PartNumber` в `Product`). fileciteturn12file6L62-L76


> Назначение: этот документ — **единый “источник правды”** для ИИ/разработки MyIS по трём темам:
> 1) единая номенклатурная сущность **Item (MDM Item)**,
> 2) система управления **справочниками/настройками**,
> 3) интеграция с **Компонент-2020 (Access, пароль)** через периодическое **зеркалирование**.  
>
> Контекст: MyIS — модульный монолит, доступ к БД Компонента только через модуль `Integration.Component2020`. fileciteturn11file3L6-L15  
> Справочники должны быть расширяемыми “через данные и конфигурацию”. fileciteturn11file12L15-L19

---

## 1. Цели и границы

### 1.1 Цели
1. Ввести в MyIS единый мастер-справочник номенклатуры **MDM Item**, покрывающий:
   - материалы,
   - стандартные изделия,
   - покупные компоненты (РЭК),
   - изготавливаемые детали,
   - сборочные единицы,
   - услуги/работы (для закупок и кооперации).
2. Ввести **унифицированную систему справочников/настроек** (Settings Module), где:
   - простые справочники редактируются через типовой CRUD UI,
   - сложные каталоги (Components/Items) управляются через гибридную модель (скелет + атрибуты),
   - есть режим владения данными (ExternalMaster / Hybrid / MyISMaster).
3. Реализовать модуль `Integration.Component2020` для **подключения к запароленной Access** и **зеркалирования** справочников в PostgreSQL MyIS:
   - “по кнопке Sync” (первый релиз),
   - с логированием прогонов,
   - с безопасным хранением пароля/строки подключения.

### 1.2 Не входит в v0.1
- Полный двунаправленный обмен “MyIS ↔ Компонент” (с записью в Access) — отдельно.
- Полная реализация заказов (CustomerOrder / PurchaseOrder) как доменов MyIS — отдельно (в рамках последующих этапов).
- Полная модель денег/цен, складские движения, партии — отдельно.

---

## 2. Принципы архитектуры (обязательные)

1. **Компонент-2020 = внешний мир**: домены MyIS не подключаются к Access напрямую; только через `Integration.Component2020`. fileciteturn11file3L6-L15  
2. **Один владелец данных**: у каждой сущности есть “домашний” домен (MDM, Customers, Procurement…). Остальные читают или взаимодействуют через сервисы. fileciteturn11file5L22-L26  
3. **Постепенная смена источника правды**: сначала владелец — Компонент (ExternalMaster), позже — MyIS (MyISMaster), при необходимости — Hybrid. fileciteturn11file5L37-L40  
4. **Зеркало всегда локально**: MyIS хранит зеркальные данные в PostgreSQL, UI работает с локальной БД (не с Access напрямую).

---

## 3. Система справочников MyIS

### 3.1 Классы справочников
**A) Простые справочники (SimpleDictionary)**  
Типичные поля:
- `id: uuid` (PK)
- `code: text` (unique, not null)
- `name: text` (not null)
- `is_active: bool` (default true)
- `sort_order: int` (опционально)
- `external_system: text` (опционально)
- `external_id: text` (опционально, unique вместе с external_system)

**B) Сложные каталоги (ComplexCatalog)**  
Напр.: Components/Items (50+ полей в Компоненте).  
Храним гибридно:
- “скелет” — фиксированные поля,
- “хвост” — атрибуты (EAV) для вариативных параметров.

### 3.2 Режим владения данными (DataOwnership)
Для каждой сущности-справочника фиксируется режим:
- **ExternalMaster** — данные приходят только из Компонента; UI только просмотр.
- **Hybrid** — данные приходят из Компонента + допускаются локальные добавления/правки в MyIS.
- **MyISMaster** — данные ведутся в MyIS; импорт из Компонента возможен как разовая загрузка.

> Для этапа “зеркалирования” по умолчанию: справочники Komponent2020 = ExternalMaster.

### 3.3 Где живёт управление справочниками
В модуле **Settings** (единая точка админки):
- `Settings → Integrations → Component-2020` (подключение, тест, синк, журнал)
- `Settings → Dictionaries → ...` (просмотр/редактирование локальных справочников)

---

## 4. MDM Item (единая номенклатура)

### 4.1 Главная сущность
**`mdm.item`** — единый справочник “номенклатурная позиция” для всех категорий.

#### Обязательные поля (скелет)
- `id: uuid` (PK)
- `kind: item_kind` (enum/lookup)
- `code: text` (внешний/внутренний код)
- `name: text`
- `uom_id: uuid` (FK → `mdm.uom`)
- `group_id: uuid` (FK → `mdm.item_group`, иерархия)
- `is_active: bool`
- `search_text: text` (денормализация для поиска; формируется)
- `external_system: text` (например, `Component2020`)
- `external_id: text` (ID записи в Компоненте)
- `synced_at: timestamptz` (когда обновлялось из источника)
- `created_at / updated_at: timestamptz`

#### ItemKind (минимум)
- `Material`
- `PurchasedComponent`
- `StandardPart`
- `ManufacturedPart`
- `Assembly`
- `ServiceWork`

> Не делаем отдельные таблицы “materials/standard/parts/assemblies” на старте.

### 4.2 Атрибуты (гибридная часть)
- `mdm.item_attribute`
  - `id, code(unique), name`
  - `data_type` (string/number/bool/date/ref)
  - `ref_dictionary_code` (если data_type = ref)
  - `is_filterable`, `is_searchable`, `sort_order`, `is_active`
- `mdm.item_attribute_value`
  - `item_id, attribute_id`
  - `value_string / value_number / value_bool / value_date / value_ref_id`
  - уникальность: `(item_id, attribute_id)` (если одинарный атрибут) **или** поддержка multi-value (опционально)
- Индексы по фильтруемым атрибутам (минимально: attribute_id + value_*)

### 4.3 Иерархия номенклатуры
- `mdm.item_group` (дерево)
  - `id, code, name, parent_id?, is_active, sort_order`

### 4.4 Политика “строгого выбора из справочника”
Во всех формах (Requests/Orders/Lines):
- выбор Item выполняется **только** из `mdm.item` (автокомплит/поиск).
- при отсутствии позиции — либо (а) Sync из Компонента, либо (б) разрешение Hybrid и создание локально (не в v0.1).

---

## 5. Интеграция `Integration.Component2020` (Access + пароль)

### 5.1 Общие требования
1. Подключение к базе Компонента выполняется **только сервером**. fileciteturn11file1L11-L14  
2. Внешняя БД — **MS Access**, файл запаролен, доступ по паролю обязателен.
3. Никаких прямых запросов к Access из доменных модулей (`Requests`, `MDM`, …).

### 5.2 Настройки подключения (Settings)
Сущность MyIS (PostgreSQL):
- `integration.component2020_connection`
  - `id`
  - `file_path`
  - `password_secret` (не хранить в git; хранить безопасно)
  - `provider` (`OLEDB` / `ODBC`) — на случай разных окружений
  - `is_active`
  - `created_at`, `updated_at`

API:
- `POST /api/admin/integrations/component2020/test`
- `POST /api/admin/integrations/component2020/save`
- `POST /api/admin/integrations/component2020/sync` (параметр `scope`: какие справочники)

### 5.3 Механика зеркалирования (Sync)
**Режим: snapshot-upsert**
- читаем набор записей из Access (справочник)
- upsert в MyIS по `(external_system='Component2020', external_id)`
- отсутствующие в новом снимке → `is_active=false` (soft-deactivate), НЕ delete
- логируем прогон

Журнал:
- `integration.component2020_sync_run`
  - `id, scope, started_at, finished_at, status, summary, started_by_user_id`
- `integration.component2020_sync_error` (опционально)
  - `sync_run_id, entity, external_id, message`

### 5.4 Первая волна справочников для синка (минимум)
Должны быть синхронизированы до того, как появится закупочная заявка с позициями:
- `Unit` → `mdm.uom` (единицы измерения) fileciteturn11file7L56-L63  
- `Manufact`/`Providers`/контрагенты (поставщики) (источник уточняется)
- `Components/Comp`/номенклатура (источник уточняется)
- группы/классификаторы номенклатуры (если есть в Access)

> Точные таблицы Компонента и маппинг полей фиксируются отдельным приложением “Integration Mapping Component2020 → MyIS”.

---

## 6. Требования к качеству и безопасности

1. Пароль Access/строки подключения:
   - не хранить в git,
   - хранить как секрет (env/secret store),
   - в UI отображать только “есть пароль” без значения.
2. Sync должен быть:
   - идемпотентным (повторный запуск не ломает данные),
   - устойчивым к частичным ошибкам (логировать ошибки, но не падать всем приложением).
3. Все изменения справочников через Settings должны попадать в аудит (кто/когда/что).

---

## 7. Acceptance Criteria (минимальные)

1. В Settings есть экран Component-2020:
   - задать `file_path` + пароль,
   - `Test` возвращает OK/ошибку с понятным сообщением.
2. Sync справочника Unit (и ещё хотя бы одного справочника) создаёт локальные записи в PostgreSQL.
3. В MyIS есть `mdm.item` + `mdm.uom` + `mdm.item_group` + `mdm.item_attribute*` (таблицы и миграции).
4. В UI Requests (SupplyRequestLines) выбор номенклатуры выполняется из локального `mdm.item`.

---

## 8. Открытые вопросы (нужно уточнить у владельца продукта)

> Эти вопросы **обязательны** для фиксации ТЗ и последующей реализации без переделок.

### 8.1 Access / Component-2020
1. Какой формат файла: `.mdb` или `.accdb`?
2. Где физически хранится файл (локально на сервере / сетевой ресурс)? Нужна ли поддержка UNC-path?
3. Требуется ли поддержка **нескольких** подключений (несколько баз Компонента) или достаточно одного активного?
4. Какие ограничения по установке драйверов (ACE OLEDB/ODBC) на сервере?

### 8.2 Что именно “зеркалим первым”
5. Какие справочники обязаны быть в первой волне (кроме Unit):
   - поставщики/контрагенты,
   - номенклатура компонентов,
   - группы/классификаторы?
6. В Компоненте “компоненты” — это одна таблица или несколько (например, Comp + доп. таблицы)?  
   (По схеме видно `OrderPos.CompID` и `UnitKoef.CompID` — нужен первичный справочник Comp/Components. fileciteturn11file9L107-L115)

### 8.3 Модель Item / атрибутов
7. Нужны ли **локальные (MyIS)** позиции, которых нет в Компоненте, уже сейчас? (Hybrid)  
8. Какие атрибуты критично нужны для закупок “сразу” (например, MPN, производитель, корпус, допуски, аналоги)?
9. Атрибуты должны быть:
   - одиночные или могут быть multi-value (например, “аналоги” несколько)?
10. Как искать номенклатуру:
   - по коду,
   - по наименованию,
   - по “сквозному тексту” (SearchText),
   - по атрибутам?

### 8.4 Связи с заявками и дальнейшими заказами
11. Для SupplyRequestLine `ItemId` должен быть обязательным всегда, или допускается строка “текстом” (без номенклатуры) в исключительных случаях?
12. Нужно ли фиксировать “оригинальное значение из Компонента” (например, исходный код/название на момент заявки), чтобы не терять историчность?

---

## 9. Правила для ИИ-агента (как работать по этому ТЗ)

- Не создавать разрозненные сущности “Material/StandardPart/Assembly” как отдельные таблицы на старте — использовать `mdm.item + item_kind`.
- Любая интеграция с Компонентом должна происходить через `Integration.Component2020` (никаких прямых подключений из Requests/MDM).
- Любая настроечная/справочная информация должна добавляться в Settings структурировано (не “в коде”, а “через данные/конфигурацию”), если это возможно. fileciteturn11file12L15-L19


## Дополнение: единая модель `Item` (Component + Product) и правила именования/ЕСКД

### Термины (фиксируем)

- **NomenclatureNo** — внутренний номенклатурный номер / артикул / учётный код (обязателен для *всех* позиций, ЕСКД и не‑ЕСКД).
- **Name** — наименование (обязателен для *всех* позиций).
- **Designation** — обозначение по ЕСКД (обязателен **только** для ЕСКД‑типов: детали/сборки/изделия).
- **ManufacturerPN (MPN)** — код производителя (опционально, чаще для покупных/материалов/компонентов).

### Единый `Item` вместо двух таблиц источника

В Iteration 1 мы **объединяем** источники Component-2020 `Component` и `Product` в единый справочник MyIS `mdm.items`.
Различия фиксируются через `ItemKind` (в коде), группы (`mdm.item_groups`) и флаги (`IsEskd`, …).

### Текущая реализация `Item` в репозитории (актуально)

`mdm.items`:

- `Id` (GUID, PK)
- `Code` (NOT NULL, unique) — текущий бизнес-код позиции
- `Name` (NOT NULL)
- `ItemKind` (NOT NULL, enum) — тип/вид позиции (минимум: `Component`, `Product`, …)
- `UnitOfMeasureId` (NOT NULL) — FK на `mdm.unit_of_measures`
- `ItemGroupId` (NULL) — FK на `mdm.item_groups` (дерево групп)
- `IsActive` (NOT NULL)
- `IsEskd` (NOT NULL)
- `IsEskdDocument` (NULL)
- `ManufacturerPartNumber` (NULL) — поле есть в модели, но маппинг из Access ещё не завершён
- `ExternalSystem`, `ExternalId`, `SyncedAt` — legacy-поля для обратной совместимости (часть синков их использует)

Внешние ключи/идентичность (основной механизм синхронизаций):
- таблица `integration.external_entity_links` (сущность `ExternalEntityLink`) с уникальным ключом `(EntityType, ExternalSystem, ExternalEntity, ExternalId)`.

> Важное: поля `NomenclatureNo` и `Designation` как отдельные колонки в `mdm.items` в текущем коде **не реализованы**. Сейчас роль “устойчивого кода” играет `Item.Code`.

### Маппинг из Component-2020 → MyIS Item (как реализовано сейчас)

**Источник `Component` → Item**
- `ItemKind` = `Component`
- `Code` = `Component.Code` (используется как бизнес-код)
- `Name` = `Component.Name`
- `ExternalSystem` = `Component2020`
- `ExternalId` = `Component.Code` (в текущей реализации синк читает/использует `Code`, а не `ID`)
- `ManufacturerPartNumber`: поле в модели есть, но **пока не маппится** из `Component.PartNumber`

**Источник `Product` → Item**
- `ItemKind` = `Product`
- `Code` = `Product.PartNumber` (если заполнен) иначе `Product.ID`
- `Name` = `Product.Name` (текущее поведение синка; может быть уточнено позже в сторону `Description`)
- `ExternalSystem` = `Component2020Product`
- `ExternalId` = `Product.ID`
- `IsEskd`: флаг есть в модели, но текущий синк не выводит его из Access-данных автоматически

### Правила валидации (MUST)

1) `Code` и `Name` — **NOT NULL** для всех Item.
2) Внешняя идентичность при синхронизации должна храниться в `integration.external_entity_links` (upsert по `(ExternalEntity, ExternalId)`), а не только в legacy-полях `ExternalSystem/ExternalId`.
3) Поля `NomenclatureNo`/`Designation` и правила ЕСКД в расширенном виде — см. следующий раздел как целевую модель.

### Синхронизация (уточнение)

- Режим: **инкрементальный** (upsert), без truncate.
- Контрагенты берём из `Providers` (зеркало в `mdm.counterparties`, роли `Supplier/Customer`).




---

## Целевая модель карточки Item (UI/UX) и хранения (план)

### UI: «шапка + вкладки по доменам»

**Шапка Item** содержит только общие поля, нужные всегда (и для поиска/идентификации):
- код (`Code` / в целевой модели — `NomenclatureNo`) — обязателен
- наименование (`Name`) — обязателен
- бизнес‑класс/тип (`ItemType`: Material / Purchased / Part / Assembly / Product / Service / Other)
- (опц.) обозначение ЕСКД (`Designation`) — только для ЕСКД‑типов
- группа (`ItemGroupId`) и принадлежность к «категории» (как корневой узел дерева групп)
- единица измерения (`UnitOfMeasureId`)
- статус жизненного цикла (например, Active/Archived; целевой LCM — отдельный справочник)
- (опц.) основной поставщик/производитель
- флаги (активность, срок годности, средство производства и т.п.)

**Вкладки** строятся по контекстам использования (а не по таблицам Access):
- `ECAD`, `MCAD`, `Закупка`, `Склад`, `Бухгалтерия` (если нужно), `Аналоги и замены`, `Документы`, `История`.

Правило: в шапке не должно быть полей «только для одного модуля» — такие поля живут во вкладках/расширениях.

### Хранение: базовый Item + расширения 1:1 + параметры

Рекомендуемый подход: комбинировать:

**A) 1:1 таблицы‑расширения** на доменные вкладки:
- `mdm.item_ecad` (`item_id` PK/FK, symbol/footprint/library refs, …)
- `mdm.item_mcad` (`item_id`, габариты/масса/материал/3D‑ссылки, …)
- `mdm.item_procurement` (`item_id`, preferred supplier, MOQ, multiple, lead time, …)
- `mdm.item_storage` (`item_id`, min/max, shelf life, условия хранения, …)
- `mdm.item_accounting` (`item_id`, счета/НДС/статьи, …) — при необходимости

**B) параметрические наборы** (если у компонентов сильно разные наборы свойств):
- `mdm.param_sets`, `mdm.param_definitions`, `mdm.item_param_values`
- применимость наборов задаётся через `ItemGroup`/«категорию».

### Аналоги и замены (план)

Аналоги — это связь many‑to‑many, а не «атрибут»:
- `mdm.item_substitutes` (`item_id`, `substitute_item_id`, `type`, `priority`, `notes`, `constraints`)

---

## Целевая модель карточки Item (UI/UX) и хранения (план)

### UI: «шапка + вкладки по доменам»

**Шапка Item** содержит только общие поля, нужные всегда (и для поиска/идентификации):
- код (`Code` / в целевой модели — `NomenclatureNo`) — обязателен
- наименование (`Name`) — обязателен
- бизнес‑класс/тип (`ItemType`: Material / Purchased / Part / Assembly / Product / Service / Other)
- (опц.) обозначение ЕСКД (`Designation`) — только для ЕСКД‑типов
- группа (`ItemGroupId`) и принадлежность к «категории» (как корневой узел дерева групп)
- единица измерения (`UnitOfMeasureId`)
- статус жизненного цикла (например, Active/Archived; целевой LCM — отдельный справочник)
- (опц.) основной поставщик/производитель
- флаги (активность, срок годности, средство производства и т.п.)

**Вкладки** строятся по контекстам использования (а не по таблицам Access):
- `ECAD`, `MCAD`, `Закупка`, `Склад`, `Бухгалтерия` (если нужно), `Аналоги и замены`, `Документы`, `История`.

Правило: в шапке не должно быть полей «только для одного модуля» — такие поля живут во вкладках/расширениях.

### Хранение: базовый Item + расширения 1:1 + параметры

Рекомендуемый подход: комбинировать:

**A) 1:1 таблицы‑расширения** на доменные вкладки:
- `mdm.item_ecad` (`item_id` PK/FK, symbol/footprint/library refs, …)
- `mdm.item_mcad` (`item_id`, габариты/масса/материал/3D‑ссылки, …)
- `mdm.item_procurement` (`item_id`, preferred supplier, MOQ, multiple, lead time, …)
- `mdm.item_storage` (`item_id`, min/max, shelf life, условия хранения, …)
- `mdm.item_accounting` (`item_id`, счета/НДС/статьи, …) — при необходимости

**B) параметрические наборы** (если у компонентов сильно разные наборы свойств):
- `mdm.param_sets`, `mdm.param_definitions`, `mdm.item_param_values`
- применимость наборов задаётся через `ItemGroup`/«категорию».

### Аналоги и замены (план)

Аналоги — это связь many‑to‑many, а не «атрибут»:
- `mdm.item_substitutes` (`item_id`, `substitute_item_id`, `type`, `priority`, `notes`, `constraints`)

## Целевая модель (не реализовано в коде): формат `NomenclatureNo` и правила нормализации/генерации

### Алгоритм склейки `Product` + `Component` → единый `Item` (спецификация)

Цель: получить единый справочник `Item` (“шапка”), а доменные детали хранить во вкладках/расширениях, при этом сохранить трассируемость «из какой записи Access приехало».

#### A. Инварианты (MUST)

1) **Трассируемость:** каждая импортированная строка Access должна оставлять след в `integration.external_entity_links`:
- `EntityType = Item`
- `ExternalSystem = Component2020`
- `ExternalEntity ∈ {Component, Product}`
- `ExternalId = <ID из Access>`

2) **Детерминированность:** один и тот же входной снимок Access должен давать одинаковый результат (без “рандома”).

3) **Безопасность данных ЕСКД:** при склейке нельзя перетирать `Designation`/основное `Name` у ЕСКД‑позиции данными из “компонентной” записи.

#### B. Нормализация (MUST)

`Normalize(s)`:
- trim
- collapse multiple spaces → single space
- uppercase (ru/lat)
- `ё → е`
- убрать пробелы вокруг `.` и `-`

#### C. Ключи сопоставления (MUST)

1) **ExternalKey** (первичный ключ синка): `(ExternalEntity, ExternalId)`

2) **DesignationKey** (ЕСКД‑ключ): `Normalize(Designation)`
- для `Product`: `Designation = Product.Name`
- для `Component`: `Designation` извлекаем из `PartNumber/Name/Description` по паттерну

3) **NomenKey** (номенклатурный ключ):
- для `Component`: `Component.Code` (если заполнен)
- если `Component.Code` пустой → позиция получает сгенерированный `NomenclatureNo` (см. раздел про sequences)

#### D. Извлечение обозначения из `Component` (SHOULD)

Так как часть ЕСКД‑позиций физически лежит в `Component`, нужно попытаться извлечь обозначение.

1) Порядок поиска:
- `Component.PartNumber`
- `Component.Name`
- `Component.Description`

2) Паттерн ЕСКД (первое приближение, расширяемый):
- `<PREFIX>.<6digits>.<3digits>` (например `ТГРС.758789.001`, `ЕЯИК.685671.026`)

Если нашли несколько кандидатов — это неоднозначность → в review.

#### E. Порядок обработки (MUST)

1) Импортируем все `Product`:
- создаём/обновляем `Item` по `ExternalKey(Product, Product.ID)`
- заполняем `Designation=Product.Name`, `Name=Product.Description` (fallback на `Product.Name`)
- вычисляем `ItemType` по `Product.Kind/Goods/Own`
- индексируем `DesignationKey → item_id`

2) Импортируем все `Component`:
- создаём/обновляем `Item` по `ExternalKey(Component, Component.ID)` только если **не приклеили** к `Product`
- извлекаем `component_designation` (если удалось)
  - если `component_designation` совпал ровно с одним `DesignationKey` из `Product` → приклеиваем `Component` к этому `Item`
  - если совпало с несколькими → в review
  - если не совпало → создаём отдельный `Item` (Purchased/Material/Service по `BOMSection`+группа)

#### F. Merge‑правила (MUST)

При склейке `Component → Item(Product)`:
- **не изменять** `Designation`
- `Name`: не перетирать “основное” имя; при необходимости хранить `PurchaseName`/`AltName` во вкладке “Закупка”
- закупочные поля можно брать из `Component` (MPN/производитель/datasheet/MOQ/multiple/min qty/…)
- группа/категория: если конфликтуют, приоритет задаётся правилами по `Groups` (в спорных случаях → review)

#### G. Review очередь (SHOULD)

Если автосклейка не однозначна, создаём запись на ручную проверку (проектируемая таблица):
- `integration.component2020_item_match_review`:
  - `id`, `created_at`
  - `component_id`, `product_id` (nullable, если несколько кандидатов)
  - `designation_candidate`
  - `score`/`reason`
  - `status` (`New/Resolved/Ignored`)

#### H. Связи `Product.MaterialID` / `DetailID` (SHOULD)

После импорта восстанавливаем ссылки через `ExternalEntityLink` и сохраняем как отдельные отношения `Item ↔ Item` (“материал/заготовка/деталь‑заготовка”), не как поля в `Item`.

### Формат

`NomenclatureNo` хранится в формате:

`XXX-000000`

где:
- `XXX` — префикс (3 буквы), **зависит от категории/типа** `ItemType`;
- `000000` — числовая часть (6 знаков), автонумерация с ведущими нулями.

### Нормализация для позиций из `Component` (есть `Component.Code`)

- `Component.Code` в источнике — число (обычно 1..9999).
- При синхронизации MyIS приводит его к `000000` (6 знаков, leading zeros).
- Итоговый `NomenclatureNo` формируется как:  
  `Prefix(ItemType) + "-" + Pad6(Component.Code)`.

Пример: `CMP-000123`.

> Примечание: `ItemType` для `Component` в Iteration 1 по умолчанию `PurchasedComponent`, поэтому дефолтный префикс для таких позиций будет соответствовать этой категории. Уточнение `ItemType→Prefix` возможно позже (через группы/атрибуты), но **NomenclatureNo не переименовывается** автоматически после изменения категории (чтобы не ломать ссылки и документы).

### Генерация для позиций из `Product` (нет `Code`, есть только ЕСКД `Designation`)

Для `Product` MyIS генерирует числовую часть **локально**:

- `NomenclatureNo = Prefix(ItemType) + "-" + NextSequence(prefix)`
- `Designation = Product.Name` (ЕСКД обозначение)
- `Name = Product.Description` (наименование)

### Таблица последовательностей (MUST)

Для поддержки генерации вводится таблица MyIS:

`mdm.item_sequences`:
- `Prefix` (PK)
- `NextValue` (int)
- `UpdatedAt`

Правила:
- при старте синхронизации для каждого префикса `NextValue` инициализируется как `(max уже занятый номер по этому префиксу) + 1`;
- генерация должна быть транзакционной и защищённой от гонок (SELECT FOR UPDATE / concurrency token).

### Справочник соответствия ItemType → Prefix (MUST, настраиваемый)

В Iteration 1 соответствие фиксируется в конфигурации (или в отдельном простом справочнике Settings):

Пример (можно править):
- `PurchasedComponent` → `CMP`
- `StandardPart` → `STD`
- `Material` → `MAT`
- `ManufacturedPart` → `PRT`
- `Assembly` → `ASM`
- `Product` → `PRD`
- `ServiceWork` → `SRV`

### Уникальность (MUST)

- `NomenclatureNo` — уникален в MyIS (unique index).
- При коллизии на импортируемом `Component.Code`:
  - синхронизация должна завершаться ошибкой с понятным сообщением и записью в sync-log (это означает несогласованность исходных данных или правил префикса).




## Дополнение: дефолтный маппинг ItemType → Prefix и правило неизменности кода

### Дефолтный маппинг ItemType → Prefix (FIXED DEFAULT)

В Iteration 1 используется следующий **дефолтный** маппинг категорий номенклатуры к префиксам `NomenclatureNo`:

| ItemType              | Prefix |
|-----------------------|--------|
| PurchasedComponent    | CMP    |
| Material              | MAT    |
| StandardPart          | STD    |
| ManufacturedPart      | PRT    |
| Assembly              | ASM    |
| Product               | PRD    |
| ServiceWork           | SRV    |

- Маппинг хранится централизованно (Settings / Configuration).
- В будущем допускается расширение списка ItemType и добавление новых префиксов **без изменения существующих кодов**.

### Правило неизменности `NomenclatureNo` (MUST)

После первичного присвоения `NomenclatureNo` для Item действует **строгое правило**:

> **Изменение `ItemType`, атрибутов, групп или классификации НЕ приводит к переименованию `NomenclatureNo`.**

Причины:
- `NomenclatureNo` используется как устойчивый бизнес‑идентификатор;
- код может фигурировать в:
  - заявках;
  - заказах клиента;
  - заказах поставщикам;
  - складских документах;
  - печатных формах и переписке;
- автоматическое переименование привело бы к потере связей и исторической целостности данных.

### Следствие правила

- Префикс в `NomenclatureNo` отражает **категорию на момент первичного заведения позиции**.
- При последующей реклассификации Item (например, `PurchasedComponent → StandardPart`) код **сохраняется как есть**.
- Если требуется новый код с другим префиксом:
  - создаётся **новый Item**;
  - старый переводится в `IsActive = false`;
  - взаимосвязь фиксируется через механизм аналогов/замен (будущий этап).

### Проверки (SHOULD)

- Система **должна предупреждать** администратора, если ItemType меняется на тип с другим “логическим” префиксом.
- Но операция **разрешена**, т.к. не влияет на `NomenclatureNo`.
