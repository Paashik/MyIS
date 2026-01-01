# Отчет о маппинге данных BOM из Access Component-2020 в EBOM PostgreSQL

## 1. Обзор

Этот отчет анализирует структуру данных в базе данных Microsoft Access (Component-2020) для состава изделия и перечней элементов, и определяет, как эти данные соответствуют модели EBOM в PostgreSQL. Анализ основан на:

- Миграции `AddEngineeringModule.cs` (схема EBOM в PostgreSQL)
- Типах frontend в `frontend/src/features/ebom/api/types.ts`
- Схеме Access из `.kilocode/rules/Component2020_Access_schema_mermaid.md`
- Существующем коде интеграции Component-2020

## 2. Структура данных Access (Component-2020)

### 2.1 Ключевые таблицы BOM

#### Bom (Перечни элементов)
- **ID** (autonumber, PK): Уникальный идентификатор спецификации
- **ProductID** (FK to Product): Ссылка на изделие
- **Mod**: Модификация/версия (int)
- **Data**: Дата создания/изменения (datetime)
- **UserID**: Пользователь (FK)
- **State**: Статус (int)
- **Note**: Примечание (text)

#### Complect (Перечень элементов)
- **ID** (autonumber, PK): Уникальный идентификатор позиции
- **Product** (FK to Product): Сборочное изделие
- **Component** (FK to Component): Элемент/комплектующее
- **Position**: Позиция (text)
- **Num**: Количество (decimal)
- **Note**: Примечание (text)
- **Block**: Флаг блокировки (bool)
- **PositionEx**: Расширенная позиция (longtext)
- **RowSN**: Порядковый номер (int)
- **BomID** (FK to Bom): Ссылка на спецификацию

#### Product (Изделия)
- **ID** (autonumber, PK)
- **Parent**: Родительское изделие (для иерархии)
- **Name**: Обозначение (text)
- **Description**: Наименование (text)
- **Kind**: Вид (0=сборочная единица, 1=деталь, 2=комплекс)
- **Goods**: Назначение (0=товарная продукция, 1=полуфабрикат)
- **Own**: Способ получения (0=собственное производство, 1=покупное, etc.)
- **GroupID**: Категория
- **Hidden**: Снято с производства (bool)
- И другие поля...

#### Component (Компоненты)
- **ID** (autonumber, PK)
- **Group**: Категория
- **Name**: Наименование
- **PartNumber**: Номер по каталогу производителя
- **Code**: Номенклатурный номер
- **UnitID**: Единица измерения (FK)
- **BOMSection**: Раздел спецификации (0=Прочие, 1=Стандартные, 3=Материалы)
- **StatusID**: Статус жизненного цикла
- **Hidden**: Архивный (bool)
- И другие поля...

## 3. Структура EBOM в PostgreSQL

### 3.1 Таблицы EBOM

#### engineering.products
- **id** (uuid, PK)
- **code** (text, max 50)
- **name** (text, max 200)
- **description** (text)
- **type** (int): тип продукта (enum)
- **item_id** (uuid, FK to mdm.items)
- **created_at**, **updated_at** (timestamp)

#### engineering.bom_versions
- **id** (uuid, PK)
- **product_id** (uuid, FK to engineering.products)
- **version_code** (text, max 50)
- **status** (int): enum (Draft=0, Released=1, Archived=2)
- **source** (int): enum (Component2020=0, MyIS=1)
- **description** (text)

#### engineering.bom_lines
- **id** (uuid, PK)
- **bom_version_id** (uuid, FK)
- **parent_item_id** (uuid, FK to mdm.items): родитель в дереве
- **item_id** (uuid, FK to mdm.items): элемент
- **role** (int): enum (Component=0, Material=1, SubAssembly=2, Service=3)
- **quantity** (numeric(18,6))
- **unit_of_measure** (text, max 20): код единицы
- **position_no** (text, max 50)
- **notes** (text)
- **status** (int): enum (Valid=0, Warning=1, Error=2, Archived=3)

#### engineering.bom_operations
- **id** (uuid, PK)
- **bom_version_id** (uuid, FK)
- **code** (text, max 10)
- **name** (text, max 200)
- **area_name** (text, max 100)
- **duration_minutes** (int)
- **status** (int): enum (Active=0, Inactive=1, Draft=2)
- **description** (text)

## 4. Маппинг полей и преобразования

### 4.1 Предварительные условия
- Items из Access (Product и Component) уже импортированы в `mdm.items` с external_id = ID из Access
- Units из Access импортированы в `mdm.units`
- Созданы маппинг-таблицы для соответствия ID Access ↔ UUID PostgreSQL

### 4.2 Маппинг engineering.products
Из таблицы Product (только те, у которых есть Bom или Complect)

| Поле Access | Поле PostgreSQL | Преобразование |
|-------------|------------------|----------------|
| ID | item_id | UUID из mdm.items по external_id |
| Name | code | Если короткий, иначе оставить пустым |
| Description | name | Обрезать до 200 символов |
| Description | description | Полное описание |
| Kind/Goods | type | Маппинг: 0→Assembly, 1→Part, 2→Complex |
| - | created_at/updated_at | Текущая дата |

### 4.3 Маппинг engineering.bom_versions
Из таблицы Bom

| Поле Access | Поле PostgreSQL | Преобразование |
|-------------|------------------|----------------|
| ID | - | Не используется (новый UUID) |
| ProductID | product_id | UUID из engineering.products по item_id |
| Mod | version_code | "v" + Mod.ToString() или "1.0" |
| State | status | Маппинг: 0→Draft, 1→Released, etc. |
| - | source | Всегда 'Component2020' |
| Note | description | - |

### 4.4 Маппинг engineering.bom_lines
Из таблицы Complect

| Поле Access | Поле PostgreSQL | Преобразование |
|-------------|------------------|----------------|
| ID | - | Не используется (новый UUID) |
| BomID | bom_version_id | UUID из bom_versions по ProductID и Mod |
| Product | parent_item_id | UUID из mdm.items по external_id |
| Component | item_id | UUID из mdm.items по external_id |
| Component.BOMSection | role | 0→Component, 1→Component, 3→Material |
| Num | quantity | Преобразовать decimal → numeric |
| Component.UnitID → Unit.Code | unit_of_measure | Код единицы измерения (max 20) |
| Position | position_no | Обрезать до 50 символов |
| Note | notes | - |
| Block | status | false→Valid, true→Archived |

### 4.5 Иерархия BOM
- Простой BOM: parent_item_id = item_id продукта, item_id = item_id компонента
- Многоуровневый BOM: использовать ProductStruct для субсборок
  - ProductStruct.ParentID → parent_item_id
  - ProductStruct.ProductID → item_id с role='SubAssembly'

## 5. Необходимые преобразования данных

### 5.1 Типы данных
- **ID**: Access autonumber(int) → PostgreSQL uuid (генерировать новые)
- **Decimal**: Access decimal → PostgreSQL numeric(18,6)
- **Datetime**: Access datetime → PostgreSQL timestamp with time zone
- **Text**: Обрезать по max length (name: 200, code: 50, etc.)

### 5.2 Enum маппинги

#### BomRole
- Component.BOMSection = 0 (Прочие) → 'Component'
- Component.BOMSection = 1 (Стандартные) → 'Component'
- Component.BOMSection = 3 (Материалы) → 'Material'
- Product.Kind = 0 (сборочная) → 'SubAssembly'
- Product.Kind = 1 (деталь) → 'Component'

#### BomStatus
- Bom.State = 0 → 'Draft'
- Bom.State = 1 → 'Released'
- Иначе → 'Draft'

#### LineStatus
- Complect.Block = false → 'Valid'
- Complect.Block = true → 'Archived'

### 5.3 FK преобразования
- Все FK по ID Access заменить на UUID PostgreSQL через lookup таблицы
- Требуется предварительное создание mapping: Access_ID → PostgreSQL_UUID

### 5.4 Дерево BOM
- Корень: Product без Parent
- Первый уровень: Complect где Product = корневой Product
- Глубокие уровни: использовать ProductStruct для субпродуктов

## 6. Потенциальные несоответствия и проблемы

### 6.1 Многоуровневые BOM
- **Проблема**: Access использует плоские Complect + ProductStruct для иерархии
- **EBOM**: Требует дерево с parent_item_id
- **Решение**: Рекурсивно строить дерево из ProductStruct + Complect

### 6.2 Роли элементов
- **Проблема**: Component.BOMSection ограничен (0,1,3), нет 'SubAssembly', 'Service'
- **Решение**: Определять по Product.Kind для субпродуктов

### 6.3 Версии BOM
- **Проблема**: Bom.Mod может быть версией, но формат не стандартизирован
- **Решение**: Использовать "v{Mod}" или инкрементную нумерацию

### 6.4 Отсутствующие данные
- **Проблема**: Нет unit_of_measure в Complect, нужно брать из Component.UnitID
- **Проблема**: Нет position_no в стандарте, использовать Position
- **Решение**: Дополнять из связанных таблиц

### 6.5 Инкрементальная синхронизация
- **Проблема**: Bom.Data и Component.DT для модифицированных записей
- **Решение**: Использовать ModifiedAt паттерн как в существующей интеграции

### 6.6 Операции
- **Проблема**: Bom_operations не имеют аналогов в Access (Bom не содержит операций)
- **Решение**: Импорт операций из TechCard/OpTP в будущем расширении

### 6.7 Статусы и жизненный цикл
- **Проблема**: Статусы в Access (State, StatusID) не полностью соответствуют EBOM enum
- **Решение**: Маппинг таблица или условная логика

## 7. Рекомендации по реализации

### 7.1 Этапы миграции
1. **Подготовка**: Создать mapping таблицы для ID → UUID
2. **Import Products**: Импортировать engineering.products для Product с BOM
3. **Import BomVersions**: Импортировать bom_versions из Bom
4. **Import BomLines**: Импортировать bom_lines из Complect
5. **Build Hierarchy**: Обработать ProductStruct для многоуровневых BOM
6. **Validation**: Проверить целостность FK и логику

### 7.2 Архитектура импорта
- Использовать паттерн Component2020SyncHandler
- Добавить BomSyncHandler в Component2020IntegrationModule
- Использовать инкрементальную синхронизацию по ModifiedAt

### 7.3 Тестирование
- Dry-run для preview изменений
- Commit с транзакциями
- Валидация дерева BOM (нет циклов, все FK валидны)

### 7.4 Расширения
- Импорт операций из TechCard
- Импорт альтернативных компонентов из AltComps
- Поддержка версий BOM (Mod)

## 8. Заключение

Маппинг возможен с умеренной сложностью. Основные вызовы:
- Построение иерархической структуры из плоских данных
- Маппинг ролей и статусов
- Обработка версий BOM

Рекомендуется начать с простого одноуровневого BOM, затем добавить многоуровневую поддержку.