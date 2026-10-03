# Архитектура Мини-PDM

Нормативные решения и статусы требований — в [PDM_RULES.md](PDM_RULES.md). Здесь приведена согласованная структура и назначение компонентов; новые правила здесь не вводятся.

## Контекст

.NET 10 LTS. Desktop на Avalonia/MVVM общается по HTTP с ASP.NET Core API Controllers. API диспетчеризует сценарии через MediatR. PostgreSQL доступен через EF Core в `MiniPdm.Storage`; тесты — xUnit. Стандартный DI ASP.NET Core собирается в одной точке API.

## Структура решения

```text
MiniPdm/
├── MiniPdm.sln
├── src/
│   ├── MiniPdm.Common/                    # типизированные исключения и текстовые ресурсы
│   │   ├── Exceptions/
│   │   └── Resources/                     # .resx и strongly typed wrappers
│   ├── MiniPdm.Api/
│   │   ├── Configuration/
│   │   ├── Extensions/                  # подключение модулей
│   │   └── Program.cs
│   ├── MiniPdm.Desktop/
│   │   ├── Modules/
│   │   │   ├── Import/ViewModels/
│   │   │   ├── Composition/ViewModels/
│   │   │   └── BackgroundTasks/{Views,ViewModels}/
│   │   ├── Services/                    # HTTP API client
│   │   ├── Views/                      # application shell
│   │   ├── ViewModels/                 # application shell and shared commands
│   │   ├── App.axaml(.cs)
│   │   └── Program.cs
│   ├── MiniPdm.Contracts/
│   │   └── Modules/<Module>/DtoModels/   # публичные HTTP DTO, без дублей
│   ├── MiniPdm.Domain/
│   │   ├── Objects/
│   │   ├── Versions/
│   │   ├── Composition/
│   │   └── Mass/
│   ├── MiniPdm.Storage/
│   │   ├── PdmDbContext.cs
│   │   ├── Configurations/                   # EF entity mappings
│   │   ├── Concurrency/                      # graph lock + shared graph SQL helper
│   │   ├── Migrations/
│   │   └── Extensions/
│   └── Modules/
│       ├── MiniPdm.Modules.Import/
│       ├── MiniPdm.Modules.Objects/
│       ├── MiniPdm.Modules.Versions/
│       ├── MiniPdm.Modules.Composition/
│       ├── MiniPdm.Modules.Calculations/
│       └── MiniPdm.Modules.BackgroundTasks/
└── tests/
    ├── MiniPdm.Domain.Tests/
    ├── MiniPdm.Modules.Tests/
    ├── MiniPdm.Storage.Tests/
    ├── MiniPdm.Api.Tests/
    └── MiniPdm.Postgres.Tests/             # PostgreSQL; вне solution
```

Каждый серверный модуль по необходимости использует одинаковые папки одного уровня:

```text
MiniPdm.Modules.<Module>/
├── Controllers/                         # HTTP endpoints модуля
├── Features/                            # CQRS use cases
│   ├── Commands/<Scenario>/<Scenario>Command.cs, [Validators]/
│   └── Queries/<Scenario>/<Scenario>Query.cs, [Validators]/
├── DtoModels/                           # внутренние DTO при отличии от HTTP DTO
├── Services/                            # сервисы модуля
├── Abstractions/                        # порты к внешним ресурсам
├── Infrastructure/                     # файловые/CAD реализации модуля
└── Extensions/                          # регистрация модуля и контроллеров
```

Папки создаются по мере появления кода. HTTP DTO объявляются только в `MiniPdm.Contracts/Modules/<Module>/DtoModels`; клиент и сервер используют одни типы. Внутренние `DtoModels` существуют только если модель действительно отличается. Все DTO в обеих разновидностях `DtoModels` имеют суффикс `Dto` и форму `sealed record` без параметров в заголовке, с явными документированными `init`-свойствами; создавать их следует object initializer-ами. Перечисления остаются перечислениями без обязательного суффикса. Эти требования не распространяются на доменные типы, MediatR-сообщения, сервисные проекции и конфигурации вне `DtoModels`.

## XML-документация и форматирование C#

Правила XML-документации типов, членов, параметров и возвращаемых значений, а также пробельного форматирования C# определяет раздел «Документация проекта» в [PDM_RULES.md](PDM_RULES.md). Каждое объявленное свойство документируется непосредственно над свойством; `<param>` positional record документирует конструктор и не заменяет summary его свойства. Для positional records вне DTO сохраняют исходный конструктор и явно объявляют соответствующее свойство с `init`, если нужно дать свойству собственное описание. DTO positional-конструкторов не имеют: параметры методов и конструкторов описываются отдельно от свойств. `.editorconfig` задаёт параметры форматирования исходников.

В текущем проходе преобразованы все 46 DTO в папках `DtoModels` и их 223 свойства; XML-аудит подтвердил summary у всех 617 из 617 свойств в `src` и `tests` и отсутствие дублирующихся или некорректных XML-блоков. Новые внутренние DTO расположены в `src/Modules/MiniPdm.Modules.Composition/DtoModels/CompositionOccurrenceDto.cs`, `src/Modules/MiniPdm.Modules.Import/DtoModels/Cad/*Dto.cs` и `src/Modules/MiniPdm.Modules.Versions/DtoModels/VersionWriteRequestDto.cs`. Solution build прошёл без предупреждений и ошибок; прошли 131 solution-тест и 11 PostgreSQL-тестов на свежей PostgreSQL 17. Все три миграции применены, EF не сообщает о pending model changes. Реальные HTTP-проверки импорта, повторов, версионных конфликтов/циклов и фоновых задач описаны в `PROJECT_STATE.md`. Форматирование solution и отдельного PostgreSQL-проекта применено и проверено `--verify-no-changes`; все четыре команды завершились с кодом 0. Сгенерированные EF migrations и model snapshot не получают ручные XML-комментарии.

## Модули и границы

| Модуль | Сценарии |
| --- | --- |
| Import | Загрузка пакета, проверка, отчёт, версии при повторном импорте, Factory, CAD-адаптер и Saga файлов |
| Objects | Чтение поиска и карточки (`GET /api/objects`, `GET /api/objects/{id}`) |
| Versions | История, смена состояния, новая версия из выбранной, команды атрибутов |
| Composition | Редактирование прямого состава выбранной версии и получение действующего дерева одним CTE; `GET /api/objects/{objectId}/composition` |
| Calculations | Масса и сводная спецификация из одного CTE; `GET /api/objects/{objectId}/calculations` |
| BackgroundTasks | Расписание, запуск и отчёт задач очистки/восстановления |

Domain хранит общие правила объектов, состояний, версий, состава, количества и массы. `MiniPdm.Storage` предоставляет scoped `PdmDbContext`, EF entity mapping, миграции и регистрацию EF Core. Scoped services функциональных модулей получают общий контекст напрямую через DI и строят запросы/записи в контексте своих сценариев; Storage не является обязательным репозиторным слоем. Узкие внутренние интерфейсы допустимы по конкретной причине (локальная граница/тестовый seam), но не должны копировать API контекста. Один scoped `PdmDbContext` на операцию является единицей отслеживания и транзакции (Unit of Work). Ручной Unit of Work не вводится. Рекурсивный PostgreSQL CTE для дерева остаётся общей DB-specific инфраструктурной функцией, используемой модулем Composition; результат материализуется через EF Core без миграции.

## Зависимости и вызов

```text
Desktop → Contracts
Api → Modules.*, Storage
Modules.* → Domain, Contracts, Storage (PdmDbContext/EF registration)
Storage → Domain
Domain → без зависимостей от UI, API, EF Core
```

API подключает контроллеры модулей и регистрации через module `Extensions`. Все endpoints каждого модуля собраны в его `Controllers`. Контроллер переводит транспортный запрос в MediatR command/query и возвращает HTTP-ответ. Команды находятся в `Features/Commands`, запросы в `Features/Queries`. Handler вызывает scoped service этого модуля; сервис применяет доменные правила и напрямую использует injected `PdmDbContext` там, где нужен доступ к БД. Общие конкурентные механизмы и DB-specific helpers остаются инфраструктурой Storage. Модули напрямую не вызывают контроллеры друг друга.

`MiniPdm.Common` — независимая от остальных проектов библиотека для готовых текстов ошибок и двух типов прикладных исключений. `Api/Errors/LogicExceptionHandler` регистрируется через `AddExceptionHandler`/`UseExceptionHandler` и формирует `application/problem+json`: входная ошибка — HTTP 400, бизнес-ошибка — HTTP 409. DataAnnotations берут тексты из соответствующих `.resx` ресурсов. Остальные доменные результаты и их коды остаются в сценариях модулей; ошибка импорта с неопределённым исходом Saga по-прежнему использует прежний статус 503. Описание согласованного поведения — в [PDM_RULES.md](PDM_RULES.md).

Команды и запросы лежат в отдельных папках `Features/Commands` и `Features/Queries`; тип сообщения и его MediatR handler находятся вместе в одном `Command.cs` или `Query.cs` в папке сценария. Построение EF `IQueryable` не обращается к БД; `ToListAsync`, `SingleAsync` и другие операции материализации выполняют SELECT. Запросы проецируют нужные поля через `Select` в DTO и используют `AsNoTracking` для entity-чтения; `Include` используется, когда сценарию нужны связанные сущности. Дерево — отдельный рекурсивный CTE, а не цепочка `Include` или N+1 запросов. CTE выдаёт плоские occurrence rows с `ObjectPath`, `ParentPath`, локальным количеством и защитным `IsCycle`; повторяющиеся объекты на разных путях сохраняются. Расчёт получает эти строки одним чтением и передаёт их в чистую доменную логику. Количество перемножается по пути, затем агрегируется по объекту для спецификации. Неизвестная масса листа оставляет известное количество, но масса этой строки неизвестна; переполнение количества делает количество неизвестным. Отсутствующая текущая версия, пустая масса, цикл и переполнение дают диагностики с объектом и путём. Любая диагностика приводит к `TotalMassKg = null` и `IsComplete = false`, при этом известные строки спецификации сохраняются. `SaveChangesAsync` отправляет отслеженные изменения; один вызов может выполнить несколько SQL-команд. Многошаговые согласованные сценарии явно сохраняют транзакционные границы и блокировки.

PostgreSQL-интеграционные тесты размещаются в `tests/MiniPdm.Postgres.Tests`, не включённом в основной solution. Они используют отдельную тестовую базу, заданную `PDM_TEST_POSTGRES_CONNECTION`; транзакционные тесты чтения откатывают работу, а тесты конкурентной записи создают изолированные случайные fixtures и удаляют только свои объекты, версии, связи и журнал импорта. Они не очищают таблицы, не удаляют чужие данные и не запускают миграции. Все 11 PostgreSQL-тестов после рефакторинга прошли. Обычный `dotnet test MiniPdm.sln` не требует PostgreSQL.

`MiniPdm.Modules.Objects.Services.ObjectReadService` использует `AsNoTracking` и проецирует только значения для результата. Поиск считает введённую строку буквальной подстрокой, с регистронезависимым сравнением обозначения, имени текущей действующей версии и исходного имени StandardPart; SQL LIKE wildcard characters экранируются. Сортировка по идентичности и `Id` поддерживает стабильную offset-пагинацию; `limit + 1` определяет `HasMore` без отдельного `COUNT`. Карточка выбирает current version либо явно запрошенную версию, а список истории проецирует только ID, номер и состояние. Файловая система в этих read queries не используется.

## CAD и хранение файлов

`POST /api/imports/{importId}` принимает `multipart/form-data` с отдельными `.a3d`/`.m3d` JSON-файлами; `GET /api/imports/{importId}` читает сохранённый отчёт. По умолчанию действуют пределы 1000 файлов, 8 MiB на файл и 64 MiB суммарно/на запрос. `ImportStorage:DataRoot` настраивает корень источников. Серверный файловый источник выдаёт документы итератором; JSON-reader преобразует содержимое в CAD-модели. `ICadSourceFactory` выбирает зарегистрированный файловый источник. ZIP, S3 и API САПР не реализованы.

Import module service проверяет входные документы и состав, оценивает допустимость компонентов/циклов, формирует отчёт и план записи. `Services/Database/ImportDatabaseService` получает scoped `PdmDbContext` для snapshot-ов и атомарной записи пакета: новые объекты, версии, текущие версии, удалённые BOM-связи и JSON-отчёт в журнале импорта. Узкий `IImportDatabaseService` оставлен внутри Import как граница тестирования/обработки неопределённого commit. Для транзакции берётся общий PostgreSQL advisory lock, затем используется optimistic token объектов. Повтор того же `importId` воспроизводит сохранённый отчёт без повторной записи. Отдельный пользовательский Unit of Work не добавлен.

Файловая Saga использует временные upload-попытки и папки продвижения к постоянным исходникам. Подтверждённый rollback компенсируется; при неясном результате commit журнал сверяется свежим контекстом через `IDbContextFactory<PdmDbContext>` внутри `ImportDatabaseService` под блокировкой. `ImportSourceRecovery` возвращает число реально удалённых папок и диагностические ошибки, сохраняет файлы при неизвестном исходе сверки и пропускает активные lease. API регистрирует его как задачу `import-source-recovery` с начальным интервалом 1440 минут. `BackgroundTaskCoordinator` — singleton `IHostedService`; для операций с БД он создаёт DI scopes и вызывает scoped `BackgroundTaskDatabaseService`, не удерживая DbContext. Состояние и расписание хранятся в PostgreSQL. `GET /api/background-tasks`, `PUT /api/background-tasks/{taskId}/schedule` и `POST /api/background-tasks/{taskId}/run` предоставляют управление. Успешные исходники и историю автоматически не удаляют.

## Локальный запуск контейнеров

Корневой `Dockerfile` имеет отдельные цели API, EF Core migration bundle и Avalonia Desktop. Образы строятся под архитектуру текущего Docker Engine; migration bundle использует установленную в runtime .NET 10 без фиксированного RID. `docker-compose.yml` задаёт последовательность: PostgreSQL проходит health check, одноразовый migration service применяет все ожидающие миграции, API становится healthy, после чего запускается Desktop.

Desktop сохраняет Avalonia-приложение, а в Docker headless display формируется Xvfb и отображается через noVNC. Открытие корневого URL сразу показывает единственное окно Avalonia. Nginx в Desktop-контейнере обслуживает noVNC, проксирует WebSocket к websockify и перенаправляет только `/pdm-picker/*` на loopback bridge Avalonia; ASP.NET Core API доступен Desktop по Compose network, но не публикует отдельный host port. PostgreSQL и файлы исходников сохраняются в named volumes. API не раздаёт статические страницы.

Кнопка выбора папки остаётся в Avalonia. В headless Docker стандартный Avalonia file picker видит только файловую систему контейнера, поэтому включённое Compose расширение noVNC обнаруживает ожидающий запрос выбора и показывает небольшой системно-браузерный диалог поверх того же окна. Пользователь нажимает «Выбрать папку на компьютере» — браузер открывает native directory picker; подходящие `.a3d`/`.m3d` файлы, включая вложенные каталоги, отправляются в Avalonia bridge и затем обрабатываются обычным Desktop import flow/API, где остаются отчёт и безопасный повтор с тем же ID. Это не отдельная страница импорта и не отдельная логика бизнес-импорта. Из-за браузерной защиты открытие системного выбора требует явного нажатия в диалоге после запроса из Avalonia. Расширение ограничивает пакет 1000 файлами, 8 MiB на файл и 64 MiB с учётом служебных данных; одинаковые basenames запрещены. Корневой `Grid` вкладки отчёта задаёт собственный `DataContext` к `ImportViewModel`: Avalonia `TabControl` может назначить самому `TabItem` контекст элемента, поэтому binding на `TabItem` не гарантировал нужный контекст его содержимого. Headless проверки подтверждают повторные импорты, отмену выбора, строки отчёта и повтор запроса с тем же ID после неопределённого ответа. После пересборки Desktop на пользовательском Mac через noVNC отображается отчёт прежнего пакета (35 принятых, 10 отклонённых, одно предупреждение, 45 строк); один запрос отмены папочного picker завершился HTTP 204. Вторую успешную отправку пакета в браузере завершить не удалось: macOS заблокировала компьютерное управление во время системного выбора папки.

На проверке Compose конфигурация разобрана успешно; framework-dependent migration bundle построен, применил три миграции к свежей PostgreSQL 17 и повторный запуск не изменил схему. Предыдущая полная сборка всех Compose образов была остановлена registry с HTTP 403; в текущей проверке отдельный Desktop image успешно пересобран и контейнер Desktop пересоздан без остановки API/DB и без изменения named volumes.

## EF Core и конкурентность

EF Core маппит модель через Fluent API в `MiniPdm.Storage/Configurations`; `PdmDbContext` и `Migrations` расположены в корне проекта Storage. Сервисам модулей он предоставляется стандартным scoped DI; регистрация контекста и `IDbContextFactory<PdmDbContext>` находится в `MiniPdm.Storage/Extensions/StorageServiceCollectionExtensions.cs`. Общий PostgreSQL graph lock и запрос действующего графа находятся в `MiniPdm.Storage/Concurrency/GraphWriteLock.cs` и `ActiveCompositionGraphQuery.cs`. Миграция создаётся сравнением модели с snapshot (`dotnet ef migrations add`), ревьюится и применяется через `dotnet ef database update`. В репозитории находятся `InitialCreate`, `AddImportJournalAndStandardName` и `AddBackgroundTasks`. Физическое имя PostgreSQL-схемы ещё не выбрано; используется техническая схема `public` по умолчанию.

Оптимистический токен хранится на объекте и меняется при любом изменении атрибутов версии, состава или `current_version_id`; номер версии PDM — отдельный бизнес-номер. Импорт и все ручные команды записи используют одну транзакционную PostgreSQL advisory lock. Ручные команды после блокировки перечитывают объект и сравнивают клиентский `ExpectedConcurrencyToken`; импорт использует EF optimistic token на загруженных данных, поскольку отдельный клиентский token у него отсутствует. Все операции проверяют будущий действующий граф там, где он может измениться, затем сохраняют и commit/rollback. Это включает команды изменения состава и только атрибутов, поэтому устаревшие запросы не обходят сериализацию.

## Ручные команды версий

Контракты HTTP находятся в `MiniPdm.Contracts`; контроллеры и MediatR handlers принадлежат соответствующим модулям Versions, Objects и Composition. Поддерживаются создание новой версии из выбранной (`POST /api/objects/{id}/versions`), переход состояния (`PUT /api/objects/{id}/versions/{version}/state`), изменение атрибутов (`PUT /api/objects/{id}/versions/{version}/attributes`) и полная замена состава (`PUT /api/objects/{id}/versions/{version}/composition`). Каждый запрос передаёт `ExpectedConcurrencyToken`, а ответ возвращает новое значение токена.

Все четыре операции вызывают методы `CloneAsync`, `ChangeStateAsync`, `UpdateAttributesAsync` или `ReplaceCompositionAsync` сервиса `MiniPdm.Modules.Versions.Services.VersionMutationService`; реализации находятся в `Versions/Services`, CQRS types разделены между `Features/Commands` и `Features/Queries`. Сервис получает `PdmDbContext` напрямую. Общий доменный `VersionMutationPlanner` переиспользует правила атрибутов, нормализацию состава и поиск цикла. Сервис берёт общую блокировку графа, проверяет token после блокировки, загружает необходимые tracked versions и плоскую проекцию действующих рёбер, затем применяет доменный план в одной транзакции. Отдельный Unit of Work не добавлен.

Клонирование создаёт номер `max(history) + 1` в состоянии InWork, копирует атрибуты, состав и SourceReference без копирования файла, не изменяя историю источника. Клонировать можно и Cancelled-версию. Атрибуты и BOM разрешено редактировать только у выбранной InWork версии, включая нетекущую историческую; Approved и Cancelled версии неизменяемы. Наименование StandardPart задаёт идентичность объекта и командой атрибутов не переименовывается. При аннулировании текущей версии указатель переходит к максимальной неаннулированной версии или null; если получившийся граф циклический, вся команда отклоняется.

Невалидный запрос возвращает 400, отсутствующий объект/версия — 404, stale token, запрещённое состояние и цикл — 409. При неясном результате commit контроллер отвечает 503 и предлагает прочитать карточку до повтора команды. После переноса EF операций в module services solution собирается с 0 warnings/errors; прошли 130 solution tests и 11 PostgreSQL tests. Все три миграции применены к свежей временной PostgreSQL 17, EF Core сообщает, что новых миграций не требуется. HTTP smoke текущей версии подтвердил import/version сценарии, конкурентное клонирование (201/409), редактирование атрибутов и состава, циклы и отмену версии. BackgroundTasks smoke подтвердил получение списка, изменение расписания и ручной запуск `202`/`Succeeded`. Временная база не затрагивала постоянную пользовательскую базу.

## Прямой BOM выбранной версии

`GET /api/objects/{id}/versions/{version}/composition` возвращает прямые строки состава именно запрошенной версии, включая исторический или пустой BOM, номер версии и concurrency token объекта. `VersionCompositionReadService` читает эту версию одной проекцией без recursive CTE. HTTP проверка подтвердила A1 с одним ребёнком и пустой BOM A2; токен совпал с токеном карточки, а current tree и история остались без изменений. Этот результат предназначен для редактора версии; дерево и расчёты продолжают читать действующий граф текущих версий через отдельные endpoints.

## Desktop и управление фоновыми задачами

Desktop реализует Avalonia shell, HTTP-клиент с базовым URL из `PDM_API_BASE_URL` (по умолчанию `http://localhost:5000`) и модульные папки для Import, Composition и BackgroundTasks. В контейнерном запуске расширение выбора файлов noVNC соединяется только с loopback picker bridge в Desktop-процессе; бизнес-отчёт и повтор запроса остаются в Avalonia. `MainWindowViewModel` собирает соответствующие ViewModels, API-клиент разделяется между ними. Headless capture проверил реальные control bindings: InWork editor включён, Approved editor выключен, quantity editor включён; VM тесты проверили stale selection, конфликт 409 без потери ввода и import retry. Native display server не использовался. SourceReference отображается как серверная ссылка/строка; загрузка/открытие исходника из Desktop пока отсутствует. Контракт фоновых задач находится в Contracts; controller и `BackgroundTaskCoordinator` — в `MiniPdm.Modules.BackgroundTasks`; его EF-запросы и записи выполняет scoped `BackgroundTaskDatabaseService`; entity/configuration/migration — в Storage. Recovery возвращает диагностические ошибки для непроверяемых исходов; успешные завершения и активные leases не считаются ошибками.

## Паттерны, подтверждённые обсуждением

- **Adapter:** изоляция формата/источника CAD. Компромисс — конверсия моделей и контракт ошибок.
- **Factory:** выбор подключённого CAD-источника. Компромисс — фабрика и descriptor даже при одной текущей реализации.
- **Iterator:** потоковое чтение документов через `IAsyncEnumerable<T>`; отдельная базовая иерархия не нужна. Компромисс — отмена и освобождение источника.
- **Repository:** обязательного слоя между module services и `PdmDbContext` нет; сервис строит EF Core-запросы напрямую. Узкие внутренние границы возможны только при конкретной потребности. Компромисс репозиторного слоя — контракты и риск дублировать API `DbContext`.
- **CQRS/Mediator:** развязка HTTP и сценариев через сообщения/handlers. Компромисс — больше типов и косвенная навигация.
- **MVVM:** UI и состояние представления отделены от API и бизнес-правил. Компромисс — ViewModels и команды.
- **Saga:** компенсация файловых шагов вокруг БД-транзакции. Компромисс — идемпотентность и восстановление после прерывания.

До кода применение или изменение применения каждого паттерна согласуется согласно PDM_RULES. Обязательное наследование, навязанное фреймворком, также обсуждается до реализации. Strategy пока не вводится без нескольких самостоятельных алгоритмов; State-классы не нужны для централизованных переходов; Template Method не используется для повторного бизнес-кода, поток строится композицией. Паттерны SOLID/DRY/KISS применяются к конкретным обязанностям, без универсальных базовых классов.
