erDiagram
  %% Accounts — банковские реквизиты
  Accounts {
    autonumber ID PK
    int32 OrgID FK
    text(100) Name
    text(20) INN
    text(10) KPP
    text(20) BIC
    text(50) Bank
    text(20) CorrAcc
    text(20) Account
    text(100) Note
  }

  %% Address — адреса доставки
  Address {
    autonumber ID PK
    int32 ProviderID FK NOTNULL
    text(100) Adds
    text(100) Note
  }

  %% AltComps — взаимозаменяемость компонентов
  AltComps {
    autonumber ID PK
    int32 BaseComp FK
    int32 AltComp FK
    int32 Product
    int32 PosID FK
    bool AllQty
    int32 Priority
    bool Inverse
  }

  %% Batch — партии
  Batch {
    autonumber ID PK
    datetime Data
    text(12) Num
    int32 ProductID FK
    int32 Qty
    text(30) Note
    int32 CustomerID FK
    int32 State
    datetime DateP
    int32 Priority
    int32 OrderID
    int32 MapID
  }

  %% Bills — счетов
  Bills {
    autonumber ID PK
    text(40) Num
    datetime Data
    decimal Amount
    int32 ContractID FK
    text(100) Note
    text(255) Path
    decimal Tax
  }

  %% Body — справочник типоразмеров
  Body {
    autonumber ID PK
    text(50) Name
    text(100) Description
    int32 Pins
    int32 SMT
    type11 Photo
    text(100) FootPrintPath
    text(40) FootprintRef
    text(40) FootprintRef2
    text(40) FootPrintRef3
  }

  %% Bom — перечни элементов
  Bom {
    autonumber ID PK
    int32 ProductID FK NOTNULL
    int32 Mod
    datetime Data
    int32 UserID FK
    int32 State
    text(100) Note
  }

  %% ComSupLink — каталог цен поставщиков
  ComSupLink {
    autonumber ID PK
    int32 CompID FK
    int32 SuppID FK
    text(50) OrderCode
    float Price
    text(20) Note
    int32 CurrID FK
    int32 DeliveryTime
    longtext Prices
    text(255) URL
  }

  %% Comment — комментарии пользователей
  Comment {
    autonumber ID PK
    int32 CompID FK
    int32 UserID FK
    text(100) Note
  }

  %% Complect — перечень элементов
  Complect {
    autonumber ID PK
    int32 Product FK
    int32 Component FK
    text(255) Position
    decimal Num
    text(100) Note
    bool Block
    longtext PositionEx
    int32 RowSN
    int32 BomID FK
  }

  %% Component — справочник компонентов
  Component {
    autonumber ID PK %% Идентификатор
    int32 Group FK %% Категория (на справочник Groups)
    text(100) Name %% Наименование
    int32 Body FK %% Ссылка на тип корпуса
    int32 Manufact FK %% Ссылка на производителч
    int32 Store FK %% Ссылка на место хранения
    text(200) Description %% Описание компонента
    int32 PriceMethod %% Метод формирования цены: Стандарт. наценка (0), Специальная наценка (1), фиксированная цена (3) 
    float PriceValue %% Наценка
    type11 Photo %% Бинарное поле для картинки
    text(50) PartNumber %% Номер по каталогу производителя
    text(20) Code %% Номенклатурный номер (уникальный инкрементный, может быть null)
    int32 MinQty %% Минимальный запас
    int32 Multiple %% Кратность заказа
    int32 UnitID FK %% Ссылка на единицы измерения
    text(200) DataSheet %% Ссылка на DataSheet
    int32 StatusID FK %% Ссылка на статус жизненого цикла
    int32 SymbolID FK %% Ссылка на условное графическое изображений для Altium
    datetime DT %% ДАта последнего изменения
    int32 UserID FK %% Сылка на пользователя последним изменившим.
    int32 SParID FK %% Ссылка на название набора параметров
    text(30) Val0 %% Параметр 1
    text(30) Val1 %% Параметр 2
    text(30) Val2 %% Параметр 3
    text(30) Val3 %% Параметр 4
    text(30) Val4 %% Параметр 5
    text(100) Val5 %% Параметр 5
    text(30) Marking %% Варианты маркировки
    int32 BOMSection %% Раздел спецификации (по ЕСКД): Прочие (0), Стандартные (1), Материалы(3)
    text(50) QRCode %% Данные для QR-кода
    int32 Margin %% Технологический запас, %
    text(255) URL %% Интернет ссылка
    bool Hidden %% Архивный
    text(255) AltURL %% Альтернативная ссылка
    bool CanMeans %% Может служить средством производства
    bool HaveDate %% Установлен срок годности
    int32 ProviderID FK %% Приоритетный поставщик
  }

  %% Contacts — контактные лица
  Contacts {
    autonumber ID PK
    int32 OrgID FK
    text(50) Name
    text(50) Email
    text(20) Phone
    text(100) Note
    text(40) Position
  }

  %% Contract — договоров
  Contract {
    autonumber ID PK
    text(60) Num NOTNULL
    datetime Data
    datetime DataEnd
    int32 PartnerID FK
    int32 State
    text(100) Note
    text(20) Account
    text(255) Path
  }

  %% CorrInv — корректирующие счета-фактуры
  CorrInv {
    autonumber ID PK
    text(20) Num
    datetime Data
    int32 UserID FK
    int32 DocID FK
  }

  %% CorrPos — позиции счетов-фактур
  CorrPos {
    autonumber ID PK
    int32 CorrInvID FK
    int32 RecID FK
    decimal OldQty
    decimal NewQty
    float OldPrice
    float NewPrice
  }

  %% Curr — справочник валют
  Curr {
    autonumber ID PK
    text(20) Name NOTNULL
    text(3) Symbol NOTNULL
    text(3) Code NOTNULL
    decimal Rate
  }

  %% CustomerOrder — заказы покупателей
  CustomerOrder {
    autonumber ID PK
    text(50) Number
    datetime Data
    datetime DeliveryData
    int32 State
    int32 CustomerID FK
    text(100) Note
    text(30) Contract
    int32 StoreID
    int32 PersonID FK
    text(255) Path
    datetime DatePay
    datetime DateFinished
    int32 ContactID FK
    int32 Discount
    int32 Tax
    int32 Mark
    int32 PN
    int32 PaymentForm
    int32 PayMethod
    int32 PayPeriod
    int32 Prepayment
    int32 Kind
    int32 AccountID FK
  }

  %% CustomerOrderKit — комплекты в заказах покупателей
  CustomerOrderKit {
    autonumber ID PK
    int32 CustomerOrderID FK NOTNULL
    int32 DeliveryKitID FK NOTNULL
    int32 Qty NOTNULL
  }

  %% CustomerOrderPos — позиции заказов покупателей
  CustomerOrderPos {
    autonumber ID PK
    int32 OrderID FK
    int32 ProductID FK
    decimal Qty
    float Price
    int32 CompID FK
    int32 OrderKitID
  }

  %% DeliveryKitPos — составы комплектов поставки
  DeliveryKitPos {
    autonumber ID PK
    int32 DeliveryKitID FK NOTNULL
    int32 ProductID FK
    int32 CompID FK
    decimal Qty
    int32 SN
    decimal Price
  }

  %% DeliveryKits — комплекты поставки
  DeliveryKits {
    autonumber ID PK
    decimal Num
    text(100) Name
    text(100) Note
  }

  %% Depts — производственная структура
  Depts {
    autonumber ID PK
    int32 Parent
    text(30) Name NOTNULL
    int32 Level
    int32 HeadID FK
  }

  %% Doc — накладные
  Doc {
    autonumber ID PK
    int32 Kind
    datetime Data
    text(20) DocN
    int32 Provider FK
    int32 Product FK
    text(100) Note
    int32 Batch
    int32 OrderID FK
    int32 ReleasedID FK
    int32 ReceivedID FK
    int32 ProdOrderID FK
    text(30) Invoice
    text(20) ComInv
    datetime DocDate
    int32 WhomID
    int32 RequestedID
    int32 AllowedID
    int32 StoreToID FK
  }

  %% Equipment — реестр оборудования
  Equipment {
    autonumber ID PK
    int32 RefEqID FK
    text(20) Model
    text(10) Inv
    int32 PersonID FK
    int32 WorkplaceID FK
    int32 DeptID FK
  }

  %% Goods — склад готовой продукции
  Goods {
    autonumber ID PK
    int32 StoreID FK
    int32 ProductID FK
    int32 Qty
  }

  %% Groups — справочник категорий
  Groups {
    autonumber ID PK
    int32 Parent
    text(30) Name
    text(50) Description
    text(250) FullName
  }

  %% Holiday — праздничных дней
  Holiday {
    autonumber ID PK
    datetime Data NOTNULL
  }

  %% Icons — перечень значков
  Icons {
    autonumber ID PK
    text(20) ObjectName
    int32 ObjectID
    int32 IconIndex
  }

  %% Info — конфигурации
  Info {
    int32 Version
    bool Login
    bool Mode
    int32 CurrID FK
    longtext Settings
    text(100) Tag
  }

  %% Inventory — инвентаризационные описи
  Inventory {
    autonumber ID PK
    text(10) Num
    datetime Data
    int32 State
    int32 PersonID FK
    int32 StoreID FK
    text(40) Note
  }

  %% InventoryPos — позиции инвентаризационных описей
  InventoryPos {
    autonumber ID PK
    int32 InventoryID FK
    int32 StoreID FK
    int32 CompID FK
    int32 ProductID FK
    decimal Qty
    decimal QtyFact
  }

  %% Kit — производственные заказы
  Kit {
    autonumber ID PK
    text(50) Name NOTNULL
    text(100) Description
    datetime Data
    int32 ClientID FK
    int32 State
    int32 ClientOrderID FK
    datetime DataPlan
    datetime DataFact
    int32 Purpose
    int32 StoreID FK
    int32 PersonID FK
    int32 ExecPersonID FK
    int32 ParentOrderID FK
    int32 DeptID FK
  }

  %% Kits — составы производственных заказов
  Kits {
    autonumber ID PK
    int32 Kit FK
    int32 Product FK
    int32 Num
    int32 SpecID FK
  }

  %% LinkDocOrder — связей накладных и заказов покупателей
  LinkDocOrder {
    autonumber ID PK
    int32 DocID FK
    int32 CustOrderID FK
  }

  %% LinkOrder — связи заказов
  LinkOrder {
    autonumber ID PK
    int32 SuppOrderID FK
    int32 CustOrderID FK
  }

  %% LogProdMeans — журнал движения средств производства
  LogProdMeans {
    autonumber ID PK
    datetime DT
    int32 ProdMeansID FK
    int32 Action
    int32 FromStoreID FK
    int32 ToStoreID FK
    int32 FromWorkplaceID FK
    int32 ToWorkplaceID FK
    int32 FromPersonID FK
    int32 ToPersonID FK
    int32 AllowedID FK
    int32 UserID FK
    text(50) Note
  }

  %% LogProduct — выпуск готовой продукции
  LogProduct {
    autonumber ID PK
    datetime Data
    int32 ProductID FK
    int32 Qty
    int32 PersonID FK
    text(100) Note
    int32 ProdOrderID FK
    int32 BatchID FK
    text(20) DocN
    int32 ProdPersonID FK
  }

  %% LogSN — журнал движения номерных изделий
  LogSN {
    autonumber ID PK
    int32 SNoID FK NOTNULL
    int32 RecID FK
    int32 LogProductID FK
    int32 LogShipmentID FK
  }

  %% LogShipment — отпуск готовой продукции
  LogShipment {
    autonumber ID PK
    datetime Data
    int32 ProductID FK
    int32 Qty
    int32 OrderID FK
    int32 ProdOrderID FK
    int32 StoreID FK
    int32 Kind
    text(50) Note
  }

  %% ManPro — связи производителей и поставщиков
  ManPro {
    autonumber ID PK
    int32 ManID FK
    int32 ProID FK
  }

  %% Manufact — справочник производителей
  Manufact {
    autonumber ID PK
    text(40) Name
    text(80) FullName
    text(30) Site
    text(40) Note
  }

  %% Measures — средств измерения
  Measures {
    autonumber ID PK
    int32 RefMeasureID FK
    text(10) Num
    datetime DT
    int32 PlaceID FK
  }

  %% NPar — справочник технических параметров
  NPar {
    autonumber ID PK
    text(20) Name
    text(3) Symbol
    int32 UnitID FK
  }

  %% Notices — почтовые оповещения
  Notices {
    autonumber ID PK
    int32 Event
    int32 ToPersonID FK
    int32 CcPersonID FK
    text(200) Topic
    longtext Body
    text(255) Path
    int32 BccPersonID FK
    longtext CcList
    longtext ToList
  }

  %% OpTP — операции техпроцессов
  OpTP {
    autonumber ID PK
    int32 TProcID FK
    text(4) Num
    int32 OperationID FK
    longtext Content
    int32 DeptID FK
    longtext Ext
    int32 MapID
    text(255) Path
    bool Coop
    int32 PartnerID FK
  }

  %% Operation — справочник технологических операций
  Operation {
    autonumber ID PK
    text(4) Code
    text(200) Name
    int32 Parent
  }

  %% OrderPos — позиции заказов поставщикам
  OrderPos {
    autonumber ID PK
    int32 OrderID FK
    int32 CompID FK
    decimal Qty
    float Price
    int32 UnitID FK
    int32 ItemID
    int32 Status
    int32 ServID
    datetime DateD
  }

  %% Orders — заказы поставщикам
  Orders {
    autonumber ID PK
    datetime Data
    text(30) Num
    int32 Provider FK
    int32 State
    text(100) Note
    datetime DataD NOTNULL
    text(40) InvoiceNum
    text(100) InvoicePath
    int32 CurrID FK
    int32 Tax
    int32 BatchID FK
    int32 ContractID FK
  }

  %% PayOrder — платежные поручения
  PayOrder {
    autonumber ID PK
    text(20) Number
    datetime Data
    decimal Amount
    text(50) Note
    int32 OrderID FK
  }

  %% Payment — график платежей
  Payment {
    autonumber ID PK
    text(30) Name
    text(20) CostItem
    int32 Period
    datetime Data
    int32 CurrID FK
    float Amount
  }

  %% PersPos — специальности сотрудников
  PersPos {
    autonumber ID PK
    int32 PersonID FK
    int32 PositionID FK
    int32 Level
  }

  %% Person — справочник сотрудников
  Person {
    autonumber ID PK
    text(20) LastName
    text(20) FirstName
    text(20) SecondName
    text(40) Position
    int32 DeptID FK
    bool Hidden
    text(50) Email
    text(20) Phone
    text(100) Note
  }

  %% Photo — фотоархив
  Photo {
    autonumber ID PK
    int32 SNoID FK
    text(255) FilePath
    text(100) Note
    int32 ServItemID FK
    int32 ServOrderID FK
  }

  %% Positions — справочник профессий
  Positions {
    autonumber ID PK
    text(5) Code
    text(50) Name NOTNULL
  }

  %% ProdMeans — перечень средств производства
  ProdMeans {
    autonumber ID PK
    datetime DateReg
    int32 CompID FK NOTNULL
    int32 Kind
    text(10) Inv
    int32 StoreID FK
    int32 WorkplaceID FK
    int32 PersonID FK
    bool Hidden
    text(50) Note
  }

  %% ProdTask — производственных заданий
  ProdTask {
    autonumber ID PK
    datetime DT
    int32 PersonID FK
    int32 BatchID FK
    int32 OpID FK
    datetime TimeBegin
    datetime TimeEnd
    int32 State
    text(100) Note
    int32 ShiftID FK
    int32 Qty
    int32 MadeQty
    int32 Duration
  }

  %% Product — перечень изделий
  Product {
    autonumber ID PK  %% Идентификатор
    int32 Parent  %% Родитель
    text(50) Name  %% Обозначение
    text(100)  %% Description Наименование
    int32 PlateX  %% Длина
    int32 PlateY  %% Ширина
    type11 Photo  %% бинарное поле для картинки
    int32 Plan  %% План выпуска
    text(50) Project %% Проект
    int32 Height %% Высота
    text(255) Annex  %% Ссылка на файл
    int32 Qty 
    float Price %% Цена базовая
    int32 Duration %% Срок изготовления
    int32 AssemblyTime %% Время сборки
    decimal Weight %% Масса
    text(100) Note %% Примечание
    text(10) Prefix %% Префикс серийного номера
    int32 SNLen %% Число цифр серийного номера
    int32 GroupID FK %% Категория (на справочник Groups)
    int32 Kind %% Вид издели: сборочная единица (0), деталь (1), комплекс (2)
    int32 Goods %% Назначение: товарная продукция (0), полуфабрикат (1)
    int32 Own %% Способ получения: Собств. производство (0), покуп. изд. (1), покуп. с доработкой (2), покуп. с ДС и доработкой (3), Не изготавливается (4), покуп. с ДС
    int32 Blank %% Загатовка: Материал (0), деталь-загатовка(1)
    int32 MaterialID %% Ссылка на материал на справочник компонентов (component)
    decimal MaterialQty %% Кол. материала
    int32 DetailID %% Ссылка на деталь-заготовку на справочник компонентов (component)
    int32 Warranty %% Гарантийный срок
    int32 ProviderID %% Ссылка на поставщика
    text(50) QRCode %% Данные для QR-кода
    int32 NeedSN %% Нумерация изделий: Нет (0), Единая (1), Особая (2)
    bool Hidden %% Снято с производства (1)
    text(50) PartNumber %% Артикул
    longtext Prices %% Оптовые цены
    int32 MinQty %% Минимальное запас
    datetime DT %% Время последнего изменения
    int32 UserID FK %% Кто изменял
    int32 DeptID FK %% Ссылка на участок изготавления (на производственную структуру)
  }

  %% ProductStruct — структура изделий
  ProductStruct {
    autonumber ID PK
    int32 ParentID FK
    int32 ProductID FK
    int32 Qty
  }

  %% Providers — справочник контрагентов
  Providers {
    autonumber ID PK
    text(50) Name
    text(100) FullName
    text(25) City
    text(100) Address
    text(30) Site
    text(25) Login
    text(10) Password
    int32 Summa
    text(20) INN
    text(20) Bank
    text(20) Account
    text(20) KPP
    int32 Type NOTNULL
    text(30) Email
    text(30) Phone
    text(250) Note
    int32 StoreID FK
    text(100) Comment
    int32 Discount
    int32 PayMethod
    int32 PayPeriod
    int32 Prepayment
    int32 Tax
  }

  %% Rec — записи об операциях
  Rec {
    autonumber ID PK
    int32 Doc FK
    int32 Comp FK
    decimal Num
    float Price
    int32 Store FK
    datetime DT
    int32 Event
    int32 UserID FK
    int32 CurrID FK
    int32 TaxRate
    int32 UnitID FK
    int32 ItemID
    int32 StatusID FK
    int32 StoreID FK
  }

  %% RefEquipment — справочник технологического оборудования
  RefEquipment {
    autonumber ID PK
    text(30) Code
    text(60) Name
    text(40) Note
  }

  %% RefMeasures — справочник средств измерения
  RefMeasures {
    autonumber ID PK
    text(40) Name NOTNULL
    int32 Period
  }

  %% RefServices — справочник работ/услуг/тары
  RefServices {
    autonumber ID PK
    text(10) Code
    text(40) Name
    int32 Kind
    text(40) Note
  }

  %% ResPos — позиции резервирований
  ResPos {
    autonumber ID PK
    int32 ResID FK
    int32 StockID
    decimal Qty
  }

  %% Reserve — резервирования
  Reserve {
    autonumber ID PK
    datetime Data
    text(10) Num
    datetime DataEnd
    int32 State
    text(30) Note
    int32 OrderID FK
    int32 UserID FK
    int32 CustOrderID FK
  }

  %% Roles — роли пользователей
  Roles {
    autonumber ID PK
    text(20) Name
    int32 Grants
    text(40) Note
  }

  %% RouteMaps — маршрутных карт
  RouteMaps {
    autonumber ID PK
    int32 ProductID FK
    text(10) Num NOTNULL
    text(3) Version
    bool Repair
    datetime DateSign
    int32 SignUserID
    datetime DateApprove
    int32 AppUserID
    int32 State
    text(100) RepairInfo
    text(100) Note
  }

  %% Rules — правила
  Rules {
    autonumber ID PK
    int32 Rule
    int32 Position
    text(12) Attribute
    text(3) Prefix
    text(3) Suffix
  }

  %% SN — серийные номера
  SN {
    autonumber ID PK
    datetime Data
    text(20) Num
    int32 ProductID FK
    int32 PersonID FK
    int32 CustomerID FK
    text(100) Note
    datetime SaleDate
    int32 State
    int32 StoreID FK
    int32 CustOrderID FK
  }

  %% SPar — справочник наборов параметров
  SPar {
    autonumber ID PK
    text(25) Name
    int32 P0ID FK
    int32 P1ID FK
    int32 P2ID FK
    int32 P3ID FK
    int32 P4ID FK
    int32 P5ID FK
  }

  %% ServiceItems — состав сервисных заказов
  ServiceItems {
    autonumber ID PK
    int32 ServOrderID FK
    int32 SNoID FK
    text(100) Note
    int32 ProductID FK
    text(100) Name
    text(20) SN
    int32 Qty
    text(100) ComClient
    text(100) ComDiag
    text(100) ComGen
  }

  %% ServiceOrders — сервисные заказы
  ServiceOrders {
    autonumber ID PK
    text(20) Num
    datetime DateOrder
    datetime DatePlan
    datetime DateFact
    int32 PartnerID FK
    int32 PersonID FK
    int32 State
    text(100) Note
    int32 StoreID FK
    int32 ContactID FK
  }

  %% Shifts — смен
  Shifts {
    autonumber ID PK
    text(20) Des
    datetime DateBegin
    datetime DateEnd
    int32 PersonID FK
  }

  %% Spec — спецификации
  Spec {
    autonumber ID PK
    int32 ProductID FK
    datetime Data
    int32 Mod
    int32 State
    int32 UserID FK
    text(40) Note
  }

  %% SpecPos — позиции спецификаций
  SpecPos {
    autonumber ID PK
    int32 SpecID
    int32 ProductID FK
    decimal Qty
    int32 CompID FK
    int32 TechDocID FK
    text(40) Note
    int32 SN
  }

  %% Status — справочник состояний
  Status {
    autonumber ID PK
    text(30) Name
    int32 Color
    int32 Kind
    int32 Code
    int32 SN
    int32 Flags
  }

  %% Stock — остатки на складе
  Stock {
    autonumber ID
    int32 Store FK
    int32 Comp FK
    decimal Num
    float Price
    datetime Data
  }

  %% Store — структура склада
  Store {
    autonumber ID PK
    int32 Parent
    text(50) Name
    text(50) Description
    bool Block
    int32 Volume
    bool Full
    text(250) FullName
    int32 CustOrderID
  }

  %% Symbol — справочник графических обозначений
  Symbol {
    autonumber ID PK
    text(35) Name
    text(3) Symbol
    type11 Photo
    text(100) LibraryPath
    text(35) LibraryRef
  }

  %% Task — задачи
  Task {
    autonumber ID PK
    datetime Data
    text(100) Name
    int32 Duration
    text(20) Topic
    int32 Priority
    text(20) Note
    int32 State
    datetime DataEnd
    int32 PersonID FK
    int32 Author FK
    int32 Kind
  }

  %% TechCard — технологические карты
  TechCard {
    autonumber ID PK
    int32 ProductID FK
    int32 Position
    int32 OperationID FK
    int32 RefEqID FK
    float Duration
    text(255) Content
    text(100) Path
    text(4) Num
    text(20) DocDes
  }

  %% TechDoc — технические документы
  TechDoc {
    autonumber ID PK
    int32 ProductID FK
    text(30) Des
    text(50) Name
    text(4) Format
    text(20) Note
    text(255) Path
  }

  %% TechProc — техпроцессы
  TechProc {
    autonumber ID PK
    text(30) Des
    text(50) Name
    text(20) Act
    datetime ActDate
    int32 Mod
    text(20) Notice
    datetime NoticeDate
    int32 State
    bool Typical
    int32 ProductID FK
    longtext Products
    bool Archive
  }

  %% Templates — шаблоны отчетов
  Templates {
    autonumber ID PK
    int32 Report
    text(255) FilePath
    text(100) Note
    bool FlagDefault
  }

  %% Tools — справочник инструментов
  Tools {
    autonumber ID PK
    text(20) Des
    text(60) Name NOTNULL
    text(40) Note
  }

  %% ToolsPlace — наличия инструмента
  ToolsPlace {
    autonumber ID PK
    int32 ToolID FK
    int32 DeptID FK
    text(10) Inv
    int32 PersonID FK
    int32 WorkplaceID FK
  }

  %% Unit — справочник единиц измерения
  Unit {
    autonumber ID PK
    text(20) Name NOTNULL
    text(5) Symbol NOTNULL
    text(3) Code
  }

  %% UnitKoef — справочник коэффициентов пересчета
  UnitKoef {
    autonumber ID PK
    int32 BaseUnitID FK
    decimal Koef
    int32 AltUnitID FK
    int32 CompID FK
  }

  %% UserLog — журнал действий пользователей
  UserLog {
    autonumber ID PK
    datetime DT
    int32 UserID FK
    int32 Action
    int32 CompID FK
    text(100) Note
  }

  %% Users — реестр пользователей
  Users {
    autonumber ID PK
    text(20) Name NOTNULL
    text(10) Password
    bool Hidden
    int32 RoleID FK
    int32 PersonID
    longtext Roles
    longtext UI
  }

  %% Vehicle — перечень транспортных средств
  Vehicle {
    autonumber ID PK
    text(20) Model
    text(10) RegNum
    text(4) ProdYear
    int32 EnginePower
    int32 Volume
    int32 PersonID FK
    int32 ContactID FK
    text(50) Note
  }

  %% Workplaces — рабочие места
  Workplaces {
    autonumber ID PK
    text(20) Name
    int32 PersonID FK
    int32 DeptID FK
    int32 StoreID FK
  }

  %% Works — справочник видов работ
  Works {
    autonumber ID PK
    text(30) Name NOTNULL
  }

  Curr ||--o{ Info : FK_Info_Curr
