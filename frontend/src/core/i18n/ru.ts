export const ru = {
  // Common
  "common.error.unknownNetwork": "Неизвестная ошибка сети",
  "common.actions.refresh": "Обновить",
  "common.actions.cancel": "Отмена",
  "common.actions.back": "Назад",
  "common.actions.retry": "Повторить попытку",
  "common.actions.add": "Добавить",
  "common.actions.edit": "Редактировать",
  "common.actions.save": "Сохранить",
  "common.actions.create": "Создать",

  // AppShell / navigation
  "nav.home": "Главная",
  "nav.requests": "Заявки",
  "nav.requests.incoming": "Входящие",
  "nav.requests.outgoing": "Исходящие",
  "nav.customers": "Клиенты",
  "nav.procurement": "Закупки",
  "nav.production": "Производство",
  "nav.warehouse": "Склад",
  "nav.engineering": "Конструктор",
  "nav.technology": "Технолог",
  "nav.settings": "Настройки",
  "nav.settings.requests": "Заявки",
  "nav.settings.security": "Безопасность",
  "nav.settings.requests.types": "Типы",
  "nav.settings.requests.statuses": "Статусы",
  "nav.settings.requests.workflow": "Workflow",
  "nav.settings.security.employees": "Сотрудники",
  "nav.settings.security.users": "Пользователи",
  "nav.settings.security.roles": "Роли",
  "nav.logout": "Выйти",
  "nav.user.unknown": "Неизвестный пользователь",
  "nav.roles.none": "Нет ролей",

  // Home
  "home.title": "Добро пожаловать в MyIS",
  "home.subtitle":
    "Выберите раздел в меню слева, чтобы перейти к соответствующему домену системы.",

  // Login
  "auth.login.title": "Вход в MyIS",
  "auth.login.form.login.label": "Логин",
  "auth.login.form.login.required": "Введите логин",
  "auth.login.form.password.label": "Пароль",
  "auth.login.form.password.required": "Введите пароль",
  "auth.login.form.submit": "Войти",
  "auth.login.error.invalidCredentials": "Неверный логин или пароль",
  "auth.login.error.userBlocked": "Учетная запись заблокирована",
  "auth.login.error.server": "Ошибка сервера, повторите попытку позже",
  "auth.login.error.network": "Ошибка сети, попробуйте позже",
  "auth.login.goToDbSetup": "Настроить подключение к БД",

  // Guards
  "auth.check.loading": "Проверка аутентификации...",
  "auth.check.error.title": "Ошибка аутентификации",
  "auth.check.error.http": "Ошибка при проверке аутентификации (HTTP {status})",
  "auth.check.error.failed": "Не удалось проверить аутентификацию: {message}",
  "auth.check.forbidden.title": "Недостаточно прав",
  "auth.check.forbidden.subtitle":
    "У вас нет доступа к этому разделу. Обратитесь к администратору системы.",
  "auth.check.forbidden.goToLogin": "Вернуться ко входу",

  "db.status.loading": "Проверка подключения к базе данных...",
  "db.status.error.title": "Ошибка при проверке статуса базы данных",
  "db.status.error.http": "Не удалось получить статус БД: HTTP {status} {statusText}",
  "db.status.error.failed": "Не удалось получить статус БД: {message}",
  "db.status.error.expectedJson":
    "Ожидался JSON, но получен иной формат ответа от сервера (content-type: {contentType}).{details}",
  "db.status.error.responseSnippet": " Фрагмент ответа: {snippet}",

  // DB setup
  "db.setup.title": "Настройка подключения к базе данных",
  "db.setup.description":
    "Укажите параметры подключения к PostgreSQL. Эти настройки будут сохранены в appsettings.Local.json (в режиме Development) и будут использоваться backend-сервисом MyIS.",
  "db.setup.description.part1":
    "Укажите параметры подключения к PostgreSQL. Эти настройки будут сохранены в",
  "db.setup.description.part2":
    "(в режиме Development) и будут использоваться backend-сервисом MyIS.",
  "db.setup.currentStatus.loading": "Проверка текущего состояния базы данных...",
  "db.setup.currentStatus.error.title": "Ошибка статуса базы данных",
  "db.setup.currentStatus.notConfigured.title": "База данных не сконфигурирована",
  "db.setup.currentStatus.notConfigured.description":
    "Строка подключения не настроена. Заполните форму ниже и сохраните конфигурацию.",
  "db.setup.currentStatus.cannotConnect.title": "Не удается подключиться к базе данных",
  "db.setup.currentStatus.cannotConnect.descriptionFallback":
    "Система не может подключиться к базе данных по текущей конфигурации.",
  "db.setup.currentStatus.ok.title": "Подключение к базе данных успешно",
  "db.setup.currentStatus.ok.description":
    "Окружение: {environment}. Источник строки подключения: {connectionStringSource}.",
  "db.setup.form.host.label": "Хост",
  "db.setup.form.host.required": "Укажите хост БД",
  "db.setup.form.host.placeholder": "localhost",
  "db.setup.form.port.label": "Порт",
  "db.setup.form.port.required": "Укажите порт БД",
  "db.setup.form.database.label": "База данных",
  "db.setup.form.database.required": "Укажите имя базы данных",
  "db.setup.form.database.placeholder": "myis",
  "db.setup.form.username.label": "Пользователь",
  "db.setup.form.username.required": "Укажите пользователя БД",
  "db.setup.form.password.label": "Пароль",
  "db.setup.form.password.required": "Укажите пароль",
  "db.setup.form.runMigrations": "Запустить миграции после сохранения",
  "db.setup.actions.test": "Проверить подключение",
  "db.setup.actions.apply": "Сохранить и применить",
  "db.setup.errors.loadStatus": "Ошибка при загрузке статуса базы данных (HTTP {status})",
  "db.setup.errors.cannotGetStatus": "Не удалось получить статус базы данных: {message}",
  "db.setup.errors.testHttp": "Ошибка при проверке подключения (HTTP {status})",
  "db.setup.errors.testFailed": "Не удалось подключиться к базе данных.",
  "db.setup.errors.testFailedWithDetails": "Не удалось подключиться: {lastError}",
  "db.setup.errors.testUnexpected": "Ошибка при проверке подключения: {message}",
  "db.setup.errors.applyForbidden":
    "Сохранение настроек базы данных запрещено в этом окружении (Production).",
  "db.setup.errors.applyHttp": "Ошибка при сохранении настроек (HTTP {status})",
  "db.setup.errors.applyFailed": "Не удалось сохранить конфигурацию базы данных.",
  "db.setup.errors.applyFailedWithDetails": "Не удалось сохранить конфигурацию: {lastError}",
  "db.setup.warnings.appliedButCannotConnect":
    "Конфигурация сохранена, но подключиться к БД не удалось.",
  "db.setup.warnings.appliedButCannotConnectWithDetails":
    "Конфигурация сохранена, но подключиться не удалось: {lastError}",
  "db.setup.warnings.connectOkMigrationsNotApplied":
    "Подключение успешно, но миграции не были применены.",
  "db.setup.warnings.connectOkMigrationsNotAppliedWithDetails":
    "Подключение успешно, но миграции не были применены: {lastError}",
  "db.setup.success.testOk": "Подключение успешно установлено.",
  "db.setup.success.applyOk": "Конфигурация сохранена и миграции успешно применены.",
  "db.setup.errors.applyUnexpected": "Ошибка при сохранении настроек: {message}",

  // Requests module
  "requests.api.error.http": "Ошибка при обращении к API ({status} {statusText})",
  "requests.api.error.expectedJson":
    "Ожидался JSON-ответ от сервера, но получен иной формат (content-type: {contentType}). {snippet}",

  "requests.list.title": "Заявки",
  "requests.list.create": "Создать заявку",
  "requests.list.create.selectTypeHint": "Выберите тип заявки",
  "requests.list.error.title": "Не удалось загрузить список заявок",
  "requests.list.error.unknown": "Неизвестная ошибка при загрузке заявок",

  "requests.list.tabs.incoming": "Входящие",
  "requests.list.tabs.outgoing": "Исходящие",

  "requests.list.typeTabs.all": "Все",

  "requests.edit.loading.edit": "Загрузка заявки...",
  "requests.edit.loading.create": "Подготовка формы...",
  "requests.edit.error.load.title": "Ошибка загрузки заявки",
  "requests.edit.error.prepare.title": "Ошибка подготовки формы",
  "requests.edit.error.notFound.title": "Заявка не найдена",
  "requests.edit.error.notFound.description":
    "Невозможно отредактировать несуществующую заявку.",
  "requests.edit.title.edit": "Редактирование заявки",
  "requests.edit.title.create": "Создание заявки",
  "requests.edit.error.loadFormData": "Не удалось загрузить данные для формы заявки",
  "requests.edit.error.save": "Не удалось сохранить заявку",

  "requests.edit.createContext.selectType": "Для создания заявки выберите тип",

  "requests.details.notFound.title": "Заявка не найдена",
  "requests.details.notFound.subtitle": "Заявка не существует или была удалена.",
  "requests.details.notFound.back": "Вернуться к списку заявок",
  "requests.details.loading": "Загрузка заявки...",
  "requests.details.error.load.title": "Не удалось загрузить заявку",
  "requests.details.error.load.unknown": "Неизвестная ошибка при загрузке заявки",
  "requests.details.actions.backToList": "К списку заявок",
  "requests.details.fields.id": "ID",
  "requests.details.fields.type": "Тип",
  "requests.details.fields.status": "Статус",
  "requests.details.fields.initiator": "Инициатор",
  "requests.details.fields.createdAt": "Создана",
  "requests.details.fields.updatedAt": "Обновлена",
  "requests.details.fields.dueDate": "Срок",
  "requests.details.fields.externalId": "Внешний ID",
  "requests.details.fields.relatedType": "Связанный объект — тип",
  "requests.details.fields.relatedId": "Связанный объект — ID",
  "requests.details.fields.description": "Описание",
  "requests.details.value.notSet": "Не задан",
  "requests.details.value.noDescription": "Нет описания",
  "requests.details.tabs.details": "Детали",
  "requests.details.tabs.history": "История",
  "requests.details.tabs.comments": "Комментарии",
  "requests.details.history.error.title": "Ошибка при загрузке истории заявки",
  "requests.details.history.error.unknown": "Не удалось загрузить историю заявки",
  "requests.details.comments.error.unknown": "Не удалось загрузить комментарии",
  "requests.details.comments.add.error": "Не удалось добавить комментарий",

  "requests.form.type.label": "Тип заявки",
  "requests.form.type.required": "Выберите тип заявки",
  "requests.form.type.placeholder": "Выберите тип",
  "requests.form.title.label": "Заголовок",
  "requests.form.title.required": "Введите заголовок",
  "requests.form.description.label": "Описание",
  "requests.form.dueDate.label": "Срок",
  "requests.form.relatedType.label": "Связанный объект — тип",
  "requests.form.relatedId.label": "Связанный объект — идентификатор",
  "requests.form.relatedId.invalidGuid": "Введите корректный GUID (например: 3f2504e0-4f89-11d3-9a0c-0305e82c3301)",
  "requests.form.externalId.label": "Внешний идентификатор",

  // Type Profiles
  "requests.typeProfile.default.title": "Описание",
  "requests.typeProfile.supply.title": "Заявка на обеспечение",

  // SupplyRequest UI
  "requests.supply.tabs.lines": "Состав",
  "requests.supply.tabs.description": "Описание",

  "requests.supply.lines.actions.add": "Добавить строку",
  "requests.supply.lines.actions.remove": "Удалить",
  "requests.supply.lines.card.title": "Позиция №{no}",

  "requests.supply.lines.columns.description": "Описание / позиция",
  "requests.supply.lines.columns.quantity": "Кол-во",
  "requests.supply.lines.columns.needByDate": "Нужно к",
  "requests.supply.lines.columns.supplierName": "Поставщик",
  "requests.supply.lines.columns.supplierContact": "Контакт",

  "requests.supply.lines.fields.description": "Описание / позиция",
  "requests.supply.lines.fields.quantity": "Количество",
  "requests.supply.lines.fields.needByDate": "Нужно к",
  "requests.supply.lines.fields.supplierName": "Поставщик",
  "requests.supply.lines.fields.supplierContact": "Контакт",

  "requests.supply.lines.placeholders.description": "Например: резистор 10 кОм",

  "requests.supply.validation.linesOrDescription":
    "Заполните либо состав (хотя бы одну строку), либо описание.",
  "requests.supply.validation.quantityRequired": "Укажите количество",
  "requests.supply.validation.quantityPositive": "Количество должно быть больше 0",

  "requests.table.columns.title": "Заголовок",
  "requests.table.columns.type": "Тип",
  "requests.table.columns.status": "Статус",
  "requests.table.columns.initiator": "Инициатор",
  "requests.table.columns.createdAt": "Создана",
  "requests.table.columns.dueDate": "Срок",
  "requests.table.filters.type": "Тип заявки",
  "requests.table.filters.status": "Статус",
  "requests.table.filters.onlyMine.all": "Все",
  "requests.table.filters.onlyMine.mine": "Мои",
  "requests.table.value.unknownInitiator": "Неизвестно",

  "requests.comments.error.title": "Не удалось загрузить комментарии",
  "requests.comments.empty": "Комментариев пока нет",
  "requests.comments.placeholder": "Добавить комментарий...",

  "requests.history.field.user": "Пользователь:",
  "requests.history.field.was": "Было:",
  "requests.history.field.became": "Стало:",
  "requests.history.field.comment": "Комментарий:",

  // Settings (Requests dictionaries)
  "settings.forbidden": "Недостаточно прав",
  
  // Settings (Security)
  "settings.security.employees.title": "Безопасность — Сотрудники",
  "settings.security.users.title": "Безопасность — Пользователи",
  "settings.security.roles.title": "Безопасность — Роли",

  "settings.security.common.columns.active": "Активен",
  "settings.security.common.columns.actions": "Действия",

  "settings.security.employees.columns.fullName": "ФИО",
  "settings.security.employees.columns.email": "Email",
  "settings.security.employees.columns.phone": "Телефон",

  "settings.security.users.columns.login": "Логин",
  "settings.security.users.columns.employee": "Сотрудник",
  "settings.security.users.columns.roles": "Роли",

  "settings.security.roles.columns.code": "Код",
  "settings.security.roles.columns.name": "Название",

  "settings.security.confirm.deactivate": "Деактивировать выбранную запись?",
  "settings.security.confirm.activate": "Активировать выбранную запись?",
  "settings.security.confirm.resetPassword": "Сбросить пароль пользователю?",
  "settings.requests.types.title": "Настройки заявок — Типы",
  "settings.requests.statuses.title": "Настройки заявок — Статусы",
  "settings.requests.workflow.title": "Настройки заявок — Workflow",

  "settings.requests.types.columns.code": "Код",
  "settings.requests.types.columns.name": "Название",
  "settings.requests.types.columns.direction": "Направление",
  "settings.requests.types.columns.isActive": "Активен",
  "settings.requests.types.columns.actions": "Действия",

  "settings.requests.statuses.columns.code": "Код",
  "settings.requests.statuses.columns.name": "Название",
  "settings.requests.statuses.columns.isFinal": "Финальный",
  "settings.requests.statuses.columns.isActive": "Активен",
  "settings.requests.statuses.columns.actions": "Действия",

  "settings.requests.workflow.filters.type": "Тип заявки",
  "settings.requests.workflow.columns.from": "Из статуса",
  "settings.requests.workflow.columns.to": "В статус",
  "settings.requests.workflow.columns.action": "ActionCode",
  "settings.requests.workflow.columns.permission": "RequiredPermission",
  "settings.requests.workflow.columns.enabled": "Включён",
  "settings.requests.workflow.columns.actions": "Действия",

  "settings.requests.form.code": "Код",
  "settings.requests.form.name": "Название",
  "settings.requests.form.direction": "Направление",
  "settings.requests.form.description": "Описание",
  "settings.requests.form.isActive": "Активен",
  "settings.requests.form.isFinal": "Финальный",
  "settings.requests.form.fromStatus": "Из статуса",
  "settings.requests.form.toStatus": "В статус",
  "settings.requests.form.actionCode": "ActionCode",
  "settings.requests.form.requiredPermission": "RequiredPermission",
  "settings.requests.form.isEnabled": "Включён",

  "settings.requests.confirm.archive": "Архивировать выбранную запись?",
} as const;

export type I18nKey = keyof typeof ru;

