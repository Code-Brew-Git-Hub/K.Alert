# Сервис управления напоминаниями

## Проверки backend

Из корня репозитория с установленным SDK .NET 10:

```sh
dotnet test backend/ReminderService.sln --configuration Release
```

GitHub Actions проверяет пробелы и окончания строк в C#, собирает решение
и запускает NUnit-тесты для pull request и пушей в `main`, `develop` и
`dev/backend`. Имя проверки — **Backend checks**.

## Форматирование

Rider и другие редакторы читают настройки из `.editorconfig`. Исправить
форматирование C# из терминала можно командой:

```sh
dotnet format whitespace backend --folder
```

В ветках, где есть `.githooks/pre-push`, подключите локальную проверку Full Cleanup
один раз после клонирования:

```sh
dotnet tool install --global JetBrains.ReSharper.GlobalTools
git config --local core.hooksPath .githooks
```

Если CLI находит изменения, выполните очистку в Rider, проверьте diff
и закоммитьте исправления перед повторным пушем.
