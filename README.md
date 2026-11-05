# Haskimail.NET

.NET SDK для REST API [Haskimail](https://haskimail.ru) — сервиса транзакционных писем.
Собран по публичной документации [haskimail.ru/developer](https://haskimail.ru/developer) и сверен с ответами живого API.

- **Все 62 метода API** в 11 разделах, плюс разбор и проверка подписи всех вебхуков.
- **Работает везде:** .NET Framework 4.6.2+, .NET Core / .NET 5–7 (через netstandard2.0), .NET 8 и .NET 10. Совместим с Native AOT и триммингом: сериализация на source generator, без рефлексии.
- **Безопасные повторы:** чтение повторяется при сбоях сети и 5xx; отправка письма — только когда API точно её не принял, поэтому дублей нет.
- **Понятные ошибки:** отдельный тип исключения на каждый класс ошибок, код Haskimail в `ErrorCode`, все коды — в `HaskimailErrorCodes`.
- **Проверка до отправки:** документированные лимиты (длина полей, 50 получателей, запрещённые вложения, 10 МБ) — с понятным текстом ошибки, без лишнего запроса.
- **Автопагинация** через `IAsyncEnumerable`, **интерфейсы** для моков, **DI** через `IHttpClientFactory`.
- **Устойчив к изменениям API:** новые поля игнорируются, новые значения enum не ломают разбор.

## Пакеты

| Пакет | Назначение |
|---|---|
| `Haskimail.NET` | Клиент API и разбор вебхуков. Зависимостей нет (для netstandard2.0 — `System.Text.Json` и `Microsoft.Bcl.AsyncInterfaces`). |
| `Haskimail.NET.Extensions.DependencyInjection` | `services.AddHaskimail(...)` для ASP.NET Core и Generic Host. |

```bash
dotnet pack -c Release -o ./nupkg
dotnet add package Haskimail.NET --source ./nupkg
dotnet add package Haskimail.NET.Extensions.DependencyInjection --source ./nupkg
```

## Быстрый старт

```csharp
using Haskimail;
using Haskimail.Models;

using var client = new HaskimailClient("ключ-сервера");

var result = await client.Email.SendAsync(new EmailMessage
{
    From = "Магазин <shop@example.com>",
    To = "ivan@example.com",
    Subject = "Заказ №1042 оформлен",
    HtmlBody = "<p>Здравствуйте! Заказ №1042 подтверждён.</p>",
    TextBody = "Здравствуйте! Заказ №1042 подтверждён.",
    Tag = "orders",
    Metadata = new() { ["order_id"] = "1042" },
});

Console.WriteLine(result.MessageId);
```

Клиент потокобезопасен: создайте один на приложение или возьмите из DI.

**Проверка без доставки.** Ключ `HaskimailClient.TestServerToken` (`HASKIMAIL_API_TEST`) проверяет запрос и отвечает как настоящая
отправка, но письмо не создаётся и вебхуки не срабатывают. Работает только для методов отправки и без аккаунта.

## Ключи

| Ключ | Опция | Для чего |
|---|---|---|
| Ключ сервера | `ServerToken` | отправка, шаблоны, сообщения, возвраты, каналы, статистика, вебхуки, правила, стоп-листы |
| Ключ аккаунта | `AccountToken` | серверы, домены, перенос шаблонов |

```csharp
var client = new HaskimailClient(new HaskimailClientOptions
{
    ServerToken = "...",
    AccountToken = "...",
});
```

Методы, которые API разрешает вызывать любым ключом (отправленные письма, открытия и клики, каналы, вебхуки, статистика,
работа с шаблонами кроме отправки и переноса), берут ключ сервера, а если его нет — ключ аккаунта. С ключом аккаунта они
возвращают данные **всех** серверов. Входящие письма, правила для входящих и стоп-листы — только с ключом сервера.
Если метод требует ключ, которого нет в настройках, SDK сразу выбрасывает `InvalidOperationException` с подсказкой.

## Методы

| Свойство | Методы | Эндпоинты |
|---|---|---|
| `Email` | `SendAsync`, `SendBatchAsync` | `POST /email`, `/email/batch` |
| `Templates` | `SendAsync`, `SendBatchAsync`, `PushAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `ListAsync`, `ListAllAsync`, `DeleteAsync`, `ValidateAsync` | `/email/withTemplate`, `/email/batchWithTemplates`, `/templates…` |
| `Messages` | `SearchOutboundAsync`, `SearchOutboundAllAsync`, `GetOutboundDetailsAsync`, `ListOpensAsync`, `ListAllOpensAsync`, `GetOpensForMessageAsync`, `ListClicksAsync`, `ListAllClicksAsync`, `GetClicksForMessageAsync`, `SearchInboundAsync`, `SearchInboundAllAsync`, `GetInboundDetailsAsync`, `BypassInboundAsync`, `RetryInboundAsync` | `/messages…` |
| `Bounces` | `GetDeliveryStatsAsync`, `ListAsync`, `ListAllAsync` | `/deliverystats`, `/bounces` |
| `Servers` | `GetAsync`, `CreateAsync`, `UpdateAsync`, `ListAsync`, `ListAllAsync`, `DeleteAsync` | `/servers…` |
| `MessageStreams` | `ListAsync`, `GetAsync`, `UpdateAsync`, `CreateAsync`, `ArchiveAsync`, `UnarchiveAsync` | `/message-streams…` |
| `Domains` | `ListAsync`, `ListAllAsync`, `GetAsync`, `CreateAsync`, `DeleteAsync`, `VerifyDkimAsync`, `VerifySpfAsync` | `/domains…` |
| `Stats` | `GetOverviewAsync`, `GetSentCountsAsync`, `GetBounceCountsAsync`, `GetSpamComplaintsAsync`, `GetOpenCountsAsync`, `GetClickCountsAsync`, `GetOpenPlatformsAsync`, `GetOpenEmailClientsAsync`, `GetClickBrowsersAsync`, `GetClickPlatformsAsync` | `/stats…` |
| `Webhooks` | `ListAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` | `/webhooks…` |
| `InboundRules` | `ListAsync`, `ListAllAsync`, `CreateAsync`, `DeleteAsync` | `/triggers/inboundrules…` |
| `Suppressions` | `ListAsync`, `AddAsync`, `DeleteAsync` | `/message-streams/{id}/suppressions…` |
| `HaskimailWebhooks` | `ParseEvent(Async)`, `ParseEvents`, `ParseInbound(Async)`, `VerifySignature`, `ComputeSignature` | входящие запросы вебхуков |

У каждого метода в IntelliSense — описание, эндпоинт и особенности из документации.

## Примеры

### Пакетная отправка

API принимает пакет, даже если часть писем отклонена, — результат нужно смотреть по каждому письму:

```csharp
var results = await client.Email.SendBatchAsync(new EmailMessageBase[]
{
    new EmailMessage { From = "shop@example.com", To = "ivan@example.com", Subject = "Привет", TextBody = "..." },
    new TemplatedEmailMessage { From = "shop@example.com", To = "anna@example.com", TemplateAlias = "welcome",
                                TemplateModel = TemplatePlaceholders.From(("name", "Анна")) },
});

foreach (var (index, failed) in results.GetFailures())
    Console.WriteLine($"[{index}] {failed.To}: {failed.ErrorCode} {failed.Message}");

results.EnsureAllSucceeded(); // или так: исключение HaskimailBatchException, если отклонено хоть одно
```

### Шаблоны

Подстановка в Haskimail **буквальная**: ключ — фрагмент текста вместе с разделителями, `{"{{name}}": "Иван"}`.
`TemplatePlaceholders` добавит разделители сам:

```csharp
await client.Templates.SendAsync(new TemplatedEmailMessage
{
    TemplateAlias = "Заказ оформлен",
    TemplateModel = TemplatePlaceholders.From(("name", "Иван"), ("order", "1042")), // {{name}}, {{order}}
    From = "shop@example.com",
    To = "ivan@example.com",
});

// Посмотреть, что увидит получатель, ничего не отправляя
var check = await client.Templates.ValidateAsync(new ValidateTemplateRequest
{
    Subject = "Заказ {{order}}",
    TestRenderModel = TemplatePlaceholders.From(("order", "1042")),
});

// API перезаписывает шаблон целиком; эта перегрузка читает его и меняет только нужное
await client.Templates.UpdateAsync("Заказ оформлен", t => t.Subject = "Заказ №{{order}} принят");
```

`PushAsync` **перемещает** все шаблоны на другой сервер, а не копирует. Сначала вызовите его с `PerformChanges = false`.

Название и тема шаблона могут прийти с сервера HTML-экранированными (`&amp;`, `&laquo;`) — так бывает у шаблонов,
созданных через API старыми версиями сервера. Текст без экранирования — в `DecodedName`
и `DecodedSubject`. `UpdateAsync` с функцией-правкой и `UpdateTemplateRequest.FromTemplate` раскодируют их сами, поэтому
повторные правки не накапливают экранирование.

### Вложения и картинки в письме

```csharp
var message = new EmailMessage
{
    From = "shop@example.com",
    To = "ivan@example.com",
    HtmlBody = "<img src=\"cid:logo\"> Счёт во вложении",
    Attachments = new()
    {
        Attachment.FromFile("invoice.pdf", "application/pdf"),
        Attachment.FromFile("logo.png", "image/png", contentId: "logo"),
    },
};
```

Исполняемые файлы (`exe`, `bat`, `msi`, `vbs` и другие из `Attachment.ForbiddenExtensions`) Haskimail не принимает: SDK скажет об этом до отправки.

### Поиск с автопагинацией

```csharp
await foreach (var message in client.Messages.SearchOutboundAllAsync(new OutboundMessageSearch
{
    Tag = "orders",
    FromDate = DateTime.Today.AddDays(-7), // московское время
}))
{
    Console.WriteLine($"{message.ReceivedAt:dd.MM HH:mm} {message.Recipient} {message.Subject}");
}
```

Страницы запрашиваются лениво, по мере перебора. Для отправленных писем, открытий и кликов API отдаёт не дальше 10 000-й записи —
для больших выборок сужайте период.

### Статистика

```csharp
var overview = await client.Stats.GetOverviewAsync(new StatsFilter { MessageStream = "2480" });
Console.WriteLine($"Отправлено {overview.Sent}, открываемость {overview.OpenRate}%");

var clients = await client.Stats.GetOpenEmailClientsAsync(); // ключи — названия клиентов
foreach (var (name, count) in clients.Totals) Console.WriteLine($"{name}: {count}");
```

### Приём вебхуков (ASP.NET Core)

```csharp
using Haskimail.Webhooks;

app.MapPost("/haskimail/events", async (HttpRequest request) =>
{
    var e = await HaskimailWebhooks.ParseEventAsync(request.Body);

    if (!HaskimailWebhooks.VerifySignature(e, signingKey))
        return Results.StatusCode(403);

    switch (e.EventType)
    {
        case WebhookEventType.Delivered: /* ... */ break;
        case WebhookEventType.Bounced: /* e.BounceCategory, e.Code, e.Error */ break;
        case WebhookEventType.Clicked: /* e.UrlOriginal */ break;
        case WebhookEventType.SubscriptionChange: /* e.SuppressSending, e.SuppressionReason */ break;
    }
    return Results.Ok(); // отвечайте быстро: Haskimail ждёт не больше 10 секунд
});
```

- **Подпись** — MD5 от `recipient + message_id + ключ`. Ключ находится во вкладке «Вебхуки» канала и один на весь аккаунт.
- **Повторы.** На ответ 5xx Haskimail повторяет примерно раз в минуту, до 60 раз; на 4xx не повторяет.
- **Дубли.** Одно и то же событие может прийти дважды: дедуплицируйте по `e.DeduplicationKey`.
- **Тест без настоящих писем.** Отправьте письмо на `HaskimailTestAddresses.HardBounce` (или `SoftBounce`, `Blocked`, `SpamComplaint`) — событие придёт сразу.

Полный пример — в `samples/Haskimail.Samples.WebhookReceiver`.

### DI и конфигурация

```csharp
builder.Services.AddHaskimail(builder.Configuration.GetSection("Haskimail"));
// или: builder.Services.AddHaskimail(o => o.ServerToken = "...");

app.MapPost("/order", async (IHaskimailClient haskimail) => { /* ... */ });
```

```json
{
  "Haskimail": {
    "ServerToken": "...",
    "AccountToken": "...",
    "Timeout": "00:00:30",
    "Retry": { "MaxRetries": 3, "BaseDelay": "00:00:00.500", "MaxDelay": "00:00:10" }
  }
}
```

Привязка конфигурации явная, без рефлексии — работает и в Native AOT. `AddHaskimail` возвращает `IHttpClientBuilder`:
к нему можно добавить свои обработчики. Если подключаете свою политику повторов (например, Microsoft.Extensions.Http.Resilience),
выключите встроенную: `Retry = HaskimailRetryOptions.None`.

## Надёжность

### Ошибки

| Исключение | Когда |
|---|---|
| `ArgumentException` и наследники | запрос не прошёл проверку до отправки — в API ничего не ушло |
| `HaskimailAuthenticationException` | HTTP 401: ключ не передан, неверен или не подходит методу |
| `HaskimailForbiddenException` | HTTP 403: IP не в списке разрешённых |
| `HaskimailNotFoundException` | HTTP 404: нет метода или объекта |
| `HaskimailRequestTooLargeException` | HTTP 413: запрос больше 50 МБ |
| `HaskimailValidationException` | HTTP 422: API отклонил данные; причина в `ErrorCode` |
| `HaskimailRateLimitException` | HTTP 429 |
| `HaskimailServerException` | HTTP 5xx |
| `HaskimailTimeoutException` | нет ответа за `Timeout` |
| `HaskimailConnectionException` | DNS, соединение, TLS |
| `HaskimailBatchException` | только из `EnsureAllSucceeded()` |

Все наследуются от `HaskimailException`, ошибки API — от `HaskimailApiException`. У последних есть `StatusCode`, `ErrorCode`,
`ApiMessage`, `ResponseBody`, `RequestMethod`, `RequestPath` и `IsTransient`. Ключи в текст исключений не попадают.

```csharp
catch (HaskimailValidationException ex) when (ex.ErrorCode == HaskimailErrorCodes.SenderAddressNotConfirmed) { … }
```

### Повторы и таймауты

| Запрос | Повторяется при |
|---|---|
| Чтение, изменение, удаление, архивация, стоп-листы | сбое сети, таймауте, HTTP 408, 429, 500, 502, 503, 504 |
| Отправка писем, создание объектов, перенос шаблонов, обход и повтор входящих | HTTP 429 и 503 (API точно не принял запрос) и сбое соединения до отправки (.NET 8+) |

По умолчанию — 2 повтора, задержка растёт экспоненциально (0,5 с → 1 с …, не больше 10 с) со случайным разбросом.
Заголовок `Retry-After` учитывается. `Timeout` (100 с по умолчанию) действует на каждую попытку, в том числе с внешним `HttpClient`.
Отмена через `CancellationToken` прерывает запрос сразу и не повторяется.

### Проверка до отправки

| Что | Лимит |
|---|---|
| Пакет | 1–500 писем |
| Получатели в To + Cc + Bcc | до 50 (запятые в кавычках и `<…>` учитываются) |
| From / Subject / Tag | 255 / 2000 / 1000 символов UTF-16 — так же считает Haskimail |
| HtmlBody, TextBody | до 5 МБ каждый |
| Вложения | до 10 МБ после Base64, без запрещённых расширений |
| `count` / `count + offset` | 1–500 / до 10 000 (сообщения, открытия, клики) |

Обязательные поля проверяются всегда; лимиты можно выключить: `ValidateRequests = false`.

## Особенности API, учтённые в SDK

Документация в нескольких местах расходится сама с собой. SDK принимает оба варианта:

- `custom_vars` в одних вебхуках приходит JSON-строкой, в других объектом. В SDK это всегда словарь.
- `stream_id`, `server_id`, `code` в вебхуках бывают и строкой, и числом.
- `SuppressSending` приходит строкой `"true"`/`"false"`, в SDK это `bool?`.
- Тип возврата называется `BlkBounce`/`SpmBounce` в списке и `BulkBounce`/`SpamBlock` в статистике. Свойства `BounceType.IsBlock` и `IsSpam` понимают оба написания.
- Тип канала приходит в нижнем регистре (`transactional`), а при создании пишется с заглавной (`Transactional`). Сравнение не зависит от регистра.
- Время в ответах московское и без смещения (`2026-10-07 14:30:12`), у входящих писем — ISO 8601. В SDK оно всегда приводится к `DateTimeOffset` с правильным смещением.
- «Нулевые» даты `0000-00-00 00:00:00` читаются как `null`, а не ломают разбор всего ответа.
- `Unsubsribed` (с опечаткой) в сводной статистике — легаси-дубль поля `Unsubscribed`.
- В справочнике `TrackLinks` письма — `bool`, а в статье про ссылки — строка. SDK следует справочнику API.

## Разработка

```
src/Haskimail                                 клиент, модели, вебхуки
src/Haskimail.Extensions.DependencyInjection  AddHaskimail
tests/Haskimail.Tests                         xUnit: все эндпоинты, контракты на реальных ответах, повторы, валидация, вебхуки, DI
tests/Haskimail.AotSmoke                      проверка Native AOT
samples/                                      консольный пример и приёмник вебхуков
```

```bash
dotnet build -c Release                                              # netstandard2.0, net8.0, net10.0 — без предупреждений
dotnet test                                                          # net8.0 и net10.0
dotnet test -f net10.0 -p:HaskimailTestTarget=netstandard2.0         # те же тесты против сборки netstandard2.0
dotnet publish tests/Haskimail.AotSmoke -c Release && ./tests/Haskimail.AotSmoke/bin/Release/net10.0/linux-x64/publish/Haskimail.AotSmoke
HASKIMAIL_LIVE=1 dotnet test --filter Category=Live                 # живой API с тестовым ключом (без доставки)
dotnet pack -c Release -o ./nupkg                                    # с проверкой единообразия API по платформам
```

`tests/Haskimail.Tests/EndpointTests.cs` сверяет каждый публичный метод с эндпоинтом из документации. Новый метод без строки
в этой таблице уронит тесты.

## Лицензия

MIT — см. [LICENSE](LICENSE).
