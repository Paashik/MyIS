# ТЗ Этап 2 — Расширение модуля Requests  
## Workflow и RequestLines

Версия: v0.1  
Основание: `TZ_Etap1_Requests_v1.1.md`, `Requests_Concept.md`

---

## 1. Назначение

Этап 2 развивает модуль Requests, реализуя:

1. Полноценный управляемый workflow (жизненный цикл заявок) с действиями.
2. Универсальное позиционное тело заявки (`RequestLine`) для типов с перечнем позиций, в первую очередь **SupplyRequest**.

Цель — перейти от «CRUD по заявкам» к управляемым бизнес-процессам с чёткими переходами и позиционным составом.

---

## 2. Scope Этапа 2

В scope входят:

- доменная модель workflow для заявок:
  - матрица допустимых переходов по типам заявок;
  - команды (use case’ы) для действий над заявкой;
  - учёт переходов в истории;
- реализация `RequestLine` в домене, БД, API и UI:
  - для `SupplyRequest` и, опционально, для внутренних производственных заявок.

В scope *не* входят:

- вложения/файлы (перенесены в отдельный модуль Attachments);
- интеграция с внешними BPMS;
- сложная конфигурируемость workflow через UI (v0.1 — конфигурация в коде/миграциях).

---

## 3. Workflow заявок

### 3.1. Базовые статусы (уже существуют)

- Draft  
- Submitted  
- InReview  
- Approved  
- Rejected  
- InWork  
- Done  
- Closed  

### 3.2. Действия над заявкой (Request Actions)

В рамках Этапа 2 вводятся действия:

- `Submit` — отправить на согласование.
- `Approve` — согласовать.
- `Reject` — отклонить.
- `StartWork` — перевести в работу.
- `Complete` — отметить выполнение.
- `Close` — закрыть.

Действия зависят от типа заявки и текущего статуса.

### 3.3. Модель данных workflow

Добавить сущность (упрощённо, на уровне Application/Infrastructure):

```csharp
public class RequestTransition
{
    public Guid Id { get; set; }
    public Guid RequestTypeId { get; set; }
    public string FromStatusCode { get; set; } = default!;
    public string ToStatusCode { get; set; } = default!;
    public string ActionCode { get; set; } = default!; // Submit / Approve / ...
    public string? RequiredPermission { get; set; }    // "Requests.Approve", "Requests.Complete"
}
```

В БД — таблица `requests.request_transitions`.

### 3.4. Use Case’ы (Application слой)

Добавить команды и обработчики:

- `SubmitRequestCommand`
- `ApproveRequestCommand`
- `RejectRequestCommand`
- `StartWorkOnRequestCommand`
- `CompleteRequestCommand`
- `CloseRequestCommand`

Общее поведение:

1. Загрузка заявки и её текущего статуса.
2. Проверка прав через `IRequestsAccessChecker` с учётом `RequiredPermission`.
3. Проверка, что существует допустимый `RequestTransition` From→To для данного `RequestType`.
4. Смена статуса (через метод домена `Request.ChangeStatus`).
5. Запись события в `RequestHistory`.

### 3.5. WebApi

Добавить endpoint’ы:

- `POST /api/requests/{id}/submit`
- `POST /api/requests/{id}/approve`
- `POST /api/requests/{id}/reject`
- `POST /api/requests/{id}/start-work`
- `POST /api/requests/{id}/complete`
- `POST /api/requests/{id}/close`

Тело запроса — при необходимости комментарий (`{ comment: string }`).

### 3.6. UI

В карточке заявки:

- отображать доступные действия (кнопки), загружаемые с backend:
  - `GET /api/requests/{id}/actions` → список ActionCode;
- по нажатию на кнопку:
  - вызывать соответствующий endpoint;
  - обновлять карточку заявки и историю.

Доступные действия формируются на backend (статус + тип + права), фронт доверяет ответу.

---

## 4. RequestLines (позиционное тело заявки)

### 4.1. Цели

- Для типов, содержащих перечень позиций (номенклатура, материалы, услуги), реализовать единый формат строк `RequestLine`.
- В Этапе 2 обязательный сценарий — **SupplyRequest (заявка на закупку/обеспечение)**.

### 4.2. Модель данных `RequestLine`

Используется модель из `Requests_Concept.md` (раздел 3.3), с хранением в таблице `requests.request_lines`.

Для Этапа 2 достаточно поддержать:

- ItemId / ExternalItemCode / Description;
- Quantity, UnitOfMeasureId;
- NeedByDate;
- SupplierName / SupplierContact (как текст);
- ExternalRowReferenceId (для связи с Компонент-2020).
  Детализированная схема доступа к объектам Компонент‑2020, которой следует придерживаться при работе с этими ссылками, приведена в [Component2020_Access_schema_mermaid.md](../.kilocode/rules/Component2020_Access_schema_mermaid.md).

Поля для связи с `PurchaseOrder` можно оставить на будущее (nullable, без использования).

### 4.3. API и DTO

В WebApi-контрактах `CreateRequestRequest` и `UpdateRequestRequest`:

- для типа `SupplyRequest` добавить поле:

  ```json
  {
    "lines": [
      {
        "lineNo": 10,
        "itemId": "guid|null",
        "externalItemCode": "string|null",
        "description": "string|null",
        "quantity": 10.0,
        "unitOfMeasureId": "guid|null",
        "needByDate": "2025-12-31T00:00:00Z",
        "supplierName": "string|null",
        "supplierContact": "string|null"
      }
    ]
  }
  ```

В `RequestDto`:

- добавить `BodyText` и `List<RequestLineDto> Lines`.

### 4.4. Правила для SupplyRequest

- RequestType.Code = "SupplyRequest".
- Direction = Outgoing.
- `Lines` обязательно должны быть либо непустыми, либо должно быть заполнено `BodyText`.
- Изменение `Lines` разрешено только пока статус заявки не финальный (`IsFinal = false`).

### 4.5. UI

В форме заявки:

- для SupplyRequest:
  - вкладка/секция «Состав заявки» — таблица позиций:
    - номенклатура (позже — поиск по справочнику),
    - количество,
    - единица измерения,
    - требуемая дата,
    - поставщик/контакт (текстом).
  - вкладка/поле «Описание» — текстовое поле `BodyText`.

В карточке заявки:

- показывать таблицу строк (`Lines`) и текстовое описание.

---

## 5. Роли и права (только в рамках Этапа 2)

В рамках Этапа 2 достаточно:

- доработать `IRequestsAccessChecker`:

  - `CanPerformAction(user, request, actionCode)` — с учётом `RequiredPermission` в `RequestTransition`;
  - `CanEditBody(user, request)` — изменение тела (BodyText/Lines) только в не финальных статусах.

- на фронте:
  - `can("Requests.Submit")`, `can("Requests.Approve")`, `can("Requests.StartWork")`, `can("Requests.Complete")`, `can("Requests.Close")` использовать для:
    - отображения/скрытия кнопок действий;
    - блокировки редактирования тела.

Полноценная доменная RBAC (вынесение permissions в БД, привязка к ролям) может быть запланирована отдельным этапом.

---

## 6. Требования к тестированию

- Unit-тесты:
  - доменная логика `Request.ChangeStatus` с учётом RequestTransition;
  - работа с `RequestLine` (добавление/изменение/удаление, финальный статус).

- Application-тесты:
  - обработчики действий (Submit/Approve/Reject/StartWork/Complete/Close);
  - создание/обновление SupplyRequest с линиями (валидные и невалидные сценарии).

- Интеграционные тесты WebApi:
  - сценарий: создать SupplyRequest → добавить строки → Submit → Approve → StartWork → Complete → Close;
  - проверка истории (`RequestHistory`) и актуального статуса.

---

## 7. Итог

После Этапа 2 модуль Requests:

- поддерживает осмысленный workflow с действиями;
- умеет работать с позиционным телом заявки через `RequestLine`;
- покрывает ключевой сценарий SupplyRequest (заявка на обеспечение/закупку под заказ клиента);
- остаётся согласованным с общей концепцией данных MyIS и может быть расширен для других типов заявок без пересмотра каркаса.
