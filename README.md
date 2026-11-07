\# BarberFlow



\*\*BarberFlow\*\* — это MVP-экосистема для автоматизации записи в барбершоп, состоящая из трех независимых модулей, работающих с единым источником данных.



Проект демонстрирует архитектуру с разделением ответственности (SoC): клиентский веб-интерфейс, Telegram-бот и административная панель общаются с единым REST API.



\## Архитектура



```mermaid

graph TD

&#x20;   WebClient\[Клиент: Web] -->|REST API| API\[BarberFlow.API]

&#x20;   Bot\[Клиент/Админ: Telegram Bot] -->|HTTP + X-Api-Key| API

&#x20;   Admin\[Админ: Web Panel] -->|JWT Token| API

&#x20;   API --> DB\[(SQLite Database)]

Модули решения

BarberFlow.API

Ядро системы (ASP.NET Core Web API). Содержит бизнес-логику, Entity Framework Core (Code-First), JWT-авторизацию и валидацию данных.



BarberFlow.Client

Клиентский веб-интерфейс (Vanilla JS, CSS Grid/Flexbox). Разработан без использования тяжелых SPA-фреймворков для минимизации размера бандла и сложности поддержки. Реализован интерфейс администратора и форма записи для клиентов.



BarberFlow.Bot

Telegram-бот (Background Worker Service на C#). Работает в фоне, не имеет собственной базы данных и общается с основным API по HTTP.



Ключевые технические решения

1\. State Machine в Telegram-боте

Для записи через бота реализован конечный автомат (State Machine). Бот хранит состояние текущего диалога пользователя (выбор услуги -> мастера -> даты -> времени) в памяти. Это позволяет избежать ручного ввода текста и минимизировать ошибки пользователя. На финальном шаге используется нативная кнопка Telegram RequestContact для автоматического получения номера телефона.



2\. Защита от Race Condition (Овербукинга)

При создании записи в AppointmentsController выполняется проверка пересечения временных интервалов (StartTime и EndTime) для конкретного мастера. Если слот уже занят, API возвращает статус 409 Conflict, что предотвращает двойную запись на одно и то же время.



3\. Разграничение прав доступа (Два уровня безопасности)

Пользователи сайта: Авторизация построена на ASP.NET Core Identity с выдачей JWT-токенов. Токен передается в заголовке Authorization и проверяется через стандартные механизмы ASP.NET Core.



Telegram-бот: Так как бот является машинным клиентом, ему не подходит JWT-авторизация. Для доступа к админским методам API (чтение записей и смена статуса) бот передает секретный ключ в заголовке X-Api-Key. На стороне API создан кастомный фильтр \[BotApiKeyAuth], который пропускает запрос либо по JWT, либо по валидному API-ключу.



Технологический стек

Backend: C# 12, ASP.NET Core 8, Entity Framework Core 8.



Database: SQLite (с возможностью быстрой миграции на PostgreSQL).



Frontend: Vanilla JavaScript (ES6+), CSS (Custom Properties, Flexbox/Grid).



Telegram: Telegram.Bot (v22+).



Auth: JWT (JSON Web Tokens), ASP.NET Core Identity.



Локальный запуск

1\. Настройка окружения

В целях безопасности реальные токены и пароли не хранятся в репозитории.

Скопируйте файлы конфигурации-шаблоны и заполните их своими данными:



BarberFlow.API/appsettings.example.json -> appsettings.json



BarberFlow.Bot/appsettings.example.json -> appsettings.json



В конфигурации API укажите Jwt:Key и BotApi:ApiKey. В конфигурации бота укажите Token (от BotFather), AdminUsername (ваш ник без @) и тот же ApiKey.



2\. Применение миграций и запуск

Убедитесь, что у вас установлен .NET 8 SDK и инструмент dotnet-ef.



bash

\# Применение миграций к базе данных (создаст файл barberflow.db и заполнит его тестовыми данными)

dotnet ef database update --project BarberFlow.API



\# Запуск API

dotnet run --project BarberFlow.API



\# Запуск клиента (в отдельном терминале)

dotnet run --project BarberFlow.Client



\# Запуск бота (в отдельном терминале)

dotnet run --project BarberFlow.Bot

После запуска API и Client:



Веб-интерфейс клиента: https://localhost:5001 (порт зависит от настроек launchSettings).



Swagger API: https://localhost:5212/swagger (или порт из вашей конфигурации).



Данные администратора по умолчанию: admin@barberflow.com / Admin123!.



Направления для развития (Roadmap)

Интеграция с внешними календарями: Синхронизация расписания мастеров с Google Calendar / Outlook.



Онлайн-оплата: Интеграция платежных шлюзов (ЮKassa, Stripe) для внесения предоплаты.



Автоматические уведомления: Фоновые задачи для отправки напоминаний клиентам за 24 и 2 часа до визита.



CRM-модуль: Расширение базы клиентов, отслеживание истории визитов и внедрение программы лояльности.

