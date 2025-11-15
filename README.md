EduMaxBot — учебный бот для MAX

Бот для мессенджера MAX, который помогает:

- создавать учебные группы;
- приглашать студентов по токену;
- заводить задания с дедлайнами;
- принимать и проверять решения студентов.

(использовать можно для репетиторства и автоматизации иного учебного процесса реализуемого дистанционно)


Стек:

- .NET 8 / ASP.NET Core
- PostgreSQL
- наличие Dockerfile для контейнеризации


Структура репозитория:

EduMaxBot/  
  EduMaxBot.csproj  
  appsettings.json  
  Program.cs  
  Controllers/  
    WebhookController.cs  
  Integrations/  
    MaxApiClient.cs  
    MaxApiOptions.cs  
  Data/  
    AppDbContext.cs  
  Models/  
    User.cs  
    Group.cs  
    GroupMember.cs  
    GroupRole.cs  
    InviteToken.cs  
    Assignment.cs  
    AssignmentVariant.cs  
    Submission.cs  
    SubmissionStatus.cs  
    ReviewSession.cs  
  Services/  
    RegistrationService.cs  
    GroupService.cs  
    AssignmentService.cs  
    ReviewService.cs  
  Transport/  
    UpdateDto.cs             

Минимально нужно:
.NET SDK 8.0  
ASP.NET Core Runtime 8.0  
PostgreSQL 14+   
доступ к MAX Platform API:  
        токен бота  
        secret для вебхука  
        домен для вебхука  
Docker (или Podman с docker-совместимым CLI), если хотите запускать в контейнере  

СБОРКА И ЗАПУСК:  
Настройка PostgreSQL  

Создайте БД:  

~ sudo -u postgres psql

В консоли psql:

CREATE DATABASE edumaxbot;
\q

Убедитесь, что пользователь/пароль из строки подключения существуют и имеют доступ к этой базе.

При первом запуске на пустой БД схема создаётся автоматически через EnsureCreated().
ЛОКАЛЬНО БЕЗ ДОКЕРА:
~ git clone https://github.com/ВАШ_АККАУНТ/ВАШ_РЕПОЗИТОРИЙ.git
~ cd ВАШ_РЕПОЗИТОРИЙ/MaxEduBot

~ export POSTGRES_CONNECTION_STRING="Host=127.0.0.1;Port=5432;Database=edumaxbot;Username=postgres;Password=postgres"
~ export MaxApi__BaseUrl="https://platform-api.max.ru/"
~ export MaxApi__Token="MAX_BOT_TOKEN_HERE"
~ export MaxApi__WebhookSecret="MAX_WEBHOOK_SECRET_HERE"
~ export ASPNETCORE_ENVIRONMENT=Development

~ dotnet restore
~ dotnet build
~ dotnet run

В ДОКЕРЕ:
~ cd MaxEduBot
~ docker build -t maxedubot .

~ docker run -d --name maxedubot \
~   --net host \
~   -e ASPNETCORE_ENVIRONMENT=Production \
~   -e POSTGRES_CONNECTION_STRING="Host=127.0.0.1;Port=5432;Database=edumaxbot;Username=postgres;Password=postgres" \
~   -e MaxApi__BaseUrl="https://platform-api.max.ru/" \
~   -e MaxApi__Token="MAX_BOT_TOKEN_HERE" \
~   -e MaxApi__WebhookSecret="MAX_WEBHOOK_SECRET_HERE" \
~   maxedubot

~ docker logs -f maxedubot # проверка что все нормально запустилось

# ожидаем такое:
Now listening on: http://0.0.0.0:5000
Application started. ...
