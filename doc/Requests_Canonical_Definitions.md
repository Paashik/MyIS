# Requests Canonical Definitions

Этот файл — **источник истины** по *стабильным кодам* типов и статусов заявок для домена `Requests`.

Правило: UI и интеграции опираются на `Code`, а не на `Id`. RU-названия могут меняться, `Code` — нет.

## 1. Request Types (RequestType.Code)

Stage 1 (используются в ТЗ Этапа 1):

| Code | Direction | RU (default) |
|------|-----------|--------------|
| `CustomerDevelopment` | `Incoming` | Заявка заказчика / на разработку |
| `InternalProductionRequest` | `Incoming` | Внутренняя производственная заявка |
| `ChangeRequest` | `Incoming` | Заявка на изменение (КД/ТД/состав/маршрут) |
| `SupplyRequest` | `Outgoing` | Заявка на обеспечение / закупку |
| `ExternalTechStageRequest` | `Outgoing` | Заявка на внешний технологический этап |

Зарезервировано (описано в `doc/Requests_Concept.md`, может появиться в следующих этапах):

| Code | Direction | RU (default) |
|------|-----------|--------------|
| `ExternalProductionRequest` | `Outgoing` | Заявка на изготовление у контрагентов |
| `InternalServiceRequest` | `Incoming` | Внутренняя заявка на услуги / вспомогательные операции |

## 2. Request Statuses (RequestStatus.Code)

Stage 1:

| Code | RU (default) | IsFinal |
|------|--------------|---------|
| `Draft` | Черновик | `false` |
| `Submitted` | Отправлена | `false` |
| `InReview` | На согласовании | `false` |
| `Approved` | Согласована | `false` |
| `Rejected` | Отклонена | `false` |
| `InWork` | В работе | `false` |
| `Done` | Выполнена | `false` |
| `Closed` | Закрыта | `true` |

## 3. Базовые переходы (workflow)

Минимальный маршрут Stage 1:

`Draft` → `Submitted` → `InReview` → `Approved` → `InWork` → `Done` → `Closed`

Ветка отклонения:

`InReview` → `Rejected` (опционально допускается `InWork` → `Rejected`, если подтверждено ТЗ следующего этапа).

