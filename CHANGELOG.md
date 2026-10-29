# Изменения

Формат — [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/), версии — [SemVer](https://semver.org/lang/ru/).

## [1.0.0] — 2026-10-29

Первый выпуск.

### Добавлено
- Все 61 метод справочника API Haskimail: отправка, шаблоны, сообщения, возвраты, серверы, каналы, домены,
  статистика, вебхуки, правила для входящих, стоп-листы.
- Разбор и проверка подписи всех вебхуков: доставка, возврат, открытие, клик, жалоба, изменение подписки, входящие письма.
- Платформы: netstandard2.0 (.NET Framework 4.6.2+), net8.0, net10.0. Совместимость с Native AOT и триммингом.
- Повторы при временных ошибках без риска дублей отправки, таймауты, учёт `Retry-After`.
- Иерархия исключений по типам ошибок, все коды ошибок API в `HaskimailErrorCodes`.
- Проверка документированных лимитов до отправки.
- Автопагинация через `IAsyncEnumerable`, интерфейсы для моков, расширяемые enum.
- Пакет `Haskimail.NET.Extensions.DependencyInjection` с `AddHaskimail`.
- `TemplatePlaceholders`, `HaskimailTestAddresses`, `SendResultExtensions`.
