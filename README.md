# EmployeeMonitor – мониторинг рабочей активности сотрудников

Прототип клиент-серверного приложения для мониторинга активности сотрудников в организации. Клиент — фоновый Windows-агент, который незаметно запускается при входе пользователя, собирает базовую информацию о машине и по команде сервера делает скриншот рабочего стола. Сервер — ASP.NET Core с веб-интерфейсом для администратора: список всех подключённых клиентов, время последней активности, статус online/offline и превью скриншотов.

## 📌 Назначение

**Область:** внутренний IT-мониторинг в организации.  
**Что делаем:** показываем текущую трудовую активность всех сотрудников через единый веб-интерфейс.  
**Почему это важно:** помогает руководителю и службе безопасности видеть, кто из сотрудников на связи, чем занимается, и при необходимости — получить скриншот рабочего стола для контроля или расследования инцидентов.

## 🧩 Что реализовано

| Требование | Реализация |
|------------|-----------|
| Client-Server application | Одно Visual Studio-решение, два проекта |
| Client (Windows) — C# | .NET 10, фоновый агент без окна (`OutputType=WinExe`) |
| Silent launch on logon | Автозапуск через реестр `HKCU\...\Run` |
| Work in background | Бесконечный цикл, нет UI, нет консоли |
| Communicates with server | HTTP/JSON, `HttpClient` + `System.Text.Json` |
| No third-party libraries | Только Microsoft: `System.Text.Json`, `Microsoft.Win32.Registry`, P/Invoke |
| Server — web interface | ASP.NET Core 10 Web API + статический `index.html` |
| List clients: domain/machine/ip/user | Хранится в `ConcurrentDictionary`, отображается в таблице |
| Last active time | `DateTime.UtcNow` на каждый ping, относительное время в UI |
| Screenshot from desktop | P/Invoke (GDI `BitBlt` + `GetDIBits`) → BMP → отображение в браузере |

## 🏗 Архитектура

```
┌──────────────────┐    HTTP/JSON    ┌───────────────────┐     HTTP     ┌──────────┐
│  Client (C#)     │ ──────────────► │  Server (ASP.NET) │ ◄──────────  │  Browser │
│  Windows agent   │ ◄────────────── │  in-memory store  │              │  (admin) │
│  (background)    │   ping/capture  │  + wwwroot UI     │              └──────────┘
└──────────────────┘                 └───────────────────┘
```

**Как это работает:**

1. **Клиент** каждые 5 секунд отправляет `POST /api/clients/ping` с идентификацией (домен, машина, IP, пользователь) и получает в ответ команду — `none` или `capture`.
2. **При команде `capture`** клиент делает скриншот экрана через P/Invoke (`BitBlt` + `GetDIBits`), собирает BMP вручную и загружает на сервер `POST /api/clients/screenshot`.
3. **Сервер** хранит список клиентов в памяти (`IClientRegistry` + `ConcurrentDictionary`), обновляет время последней активности на каждый ping, отдаёт HTML-страницу со списком и превью скриншотов.
4. **Администратор** открывает `http://localhost:8080`, видит всех клиентов, их статус online/offline, время последней активности и может запросить скриншот одним нажатием.

## 🔄 Алгоритм работы клиента

**Цикл мониторинга (раз в 5 секунд):**

1. Собрать текущие данные о машине (`Environment.UserDomainName`, `Environment.MachineName`, `Environment.UserName`, локальный IPv4).
2. Отправить `ping` на сервер.
3. Если в ответ пришла команда `capture`:
   - Захватить экран через GDI (`GetSystemMetrics` → `CreateCompatibleDC` → `BitBlt` → `GetDIBits`).
   - Собрать BMP-файл вручную: `BITMAPFILEHEADER` (14 байт) + `BITMAPINFOHEADER` (40 байт) + пиксели BGRA.
   - Загрузить BMP на сервер.
4. Спать до следующего интервала.

**Обработка ошибок:**
- Сетевые сбои логируются не чаще раза в минуту, чтобы не спамить `client.log`.
- `try/catch` вокруг всего цикла — клиент не падает при недоступности сервера.
- `Mutex` не даёт запустить второй экземпляр.

**Автозапуск:**
- При первом запуске опубликованного exe прописывается в `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- При запуске через `dotnet run` (`ProcessPath = dotnet.exe`) регистрация **пропускается** — чтобы в реестр не попал мусорный путь.

## 🛠 Стек технологий

| Технология | Версия | Назначение |
|------------|--------|-----------|
| .NET | 10.0 | Платформа |
| ASP.NET Core | 10.0 | Веб-сервер |
| C# | 13 | Язык |
| System.Text.Json | встроен | JSON-сериализация без сторонних библиотек |
| Microsoft.Win32.Registry | встроен | Работа с реестром для автозапуска |
| P/Invoke (user32.dll, gdi32.dll) | — | Захват экрана без `System.Drawing.Common` |
| ConcurrentDictionary | встроен | Потокобезопасное хранение клиентов |
| HttpClient | встроен | Связь клиента с сервером |

**Сторонние библиотеки не используются.** Всё, что не входит в BCL, реализовано через P/Invoke или вручную.

## 🚀 Как запустить (локально)

### Требования

- **.NET 10 SDK**
- **Windows 10/11** — для клиента (использует P/Invoke GDI и реестр)
- **Любая ОС** — для сервера (.NET кроссплатформенный)

### Шаги

**1. Клонировать репозиторий**

```bash
git clone https://github.com/Vilt01/client-server-work-monitoring.git
cd EmployeeMonitor
```

**2. Собрать решение**

```bash
dotnet build
```

**3. Запустить сервер (терминал №1)**

```bash
dotnet run --project EmployeeMonitor.Server
```

Должно появиться:
```
Now listening on: http://localhost:8080
```

**Терминал №1 не закрывать** — там крутится сервер.

**4. Открыть браузер**

```
http://localhost:8080
```

Увидите пустую таблицу — клиентов пока нет.

**5. Опубликовать и запустить клиент (терминал №2 или двойной клик)**

```bash
dotnet publish EmployeeMonitor.Client -c Release
```

Затем двойной клик по:
```
EmployeeMonitor.Client\bin\Release\net10.0-windows\publish\EmployeeMonitor.Client.exe
```

**Никаких окон не появится** — это фоновый агент.

**6. Обновить браузер**

Появится строка с вашей машиной: домен, компьютер, IP, пользователь, время последней активности, статус `● online`.

**7. Нажать «Запросить скриншот»**

Через 5–7 секунд (следующий ping) под кнопкой появится превью рабочего стола.

**8. Остановить клиент**

Диспетчер задач (`Ctrl+Shift+Esc`) → вкладка **Подробности** → `EmployeeMonitor.Client.exe` → **Снять задачу**.

Или в терминале:
```cmd
taskkill /IM EmployeeMonitor.Client.exe /F
```

**9. Остановить сервер**

В терминале №1 — `Ctrl+C`.

## ⚙️ Настройка порта

По умолчанию `8080`. Если занят — поменяйте в **двух местах**:

- `EmployeeMonitor.Server\appsettings.json` → `"Urls": "http://localhost:9090"`
- `EmployeeMonitor.Client\client.settings.json` → `"ServerUrl": "http://localhost:9090"`

После правки пересоберите оба проекта.

## 🔥 Firewall (для работы с другой машины)

Если клиент должен подключаться к серверу на другой машине, откройте порт 8080:

```cmd
netsh advfirewall firewall add rule name="EmployeeMonitor" dir=in action=allow protocol=TCP localport=8080
```

И убедитесь, что в `appsettings.json` сервер слушает `0.0.0.0`, а не только `localhost`.

## 🧱 Используемые паттерны

| Паттерн | Где | Зачем |
|---------|-----|-------|
| **Service Layer** | `MonitoringService`, `ClientRegistry` | Вся логика в сервисах, вне `Main` и контроллеров |
| **Dependency Injection (ручной)** | `Program.Main` клиента, `Program.cs` сервера | 5 сервисов собираются вручную через конструкторы |
| **Repository** | `IClientRegistry` | Абстракция над хранилищем — легко заменить на БД |
| **DTO** | `ClientRegistration`, `PingResponse`, `ClientRegistrationDto` | Сетевые контракты отделены от внутренних моделей |
| **Options** | `ClientSettings` + `client.settings.json` | Конфиг без хардкода |
| **Single Instance** | `Mutex` в `Program.Main` клиента | Не даёт запустить два экземпляра |

## 📁 Структура репозитория

```
EmployeeMonitor/
├── EmployeeMonitor.sln
├── README.md
├── .gitignore
├── Screenshots/
│   ├── main-page.png
│   ├── screenshot.png
│   └── client-log.png
│
├── EmployeeMonitor.Client/
│   ├── EmployeeMonitor.Client.csproj
│   ├── Program.cs
│   ├── client.settings.json
│   ├── Options/
│   │   └── ClientSettings.cs
│   ├── Models/
│   │   ├── ClientRegistration.cs
│   │   └── PingResponse.cs
│   ├── Infrastructure/
│   │   ├── Logger.cs
│   │   └── NativeMethods.cs
│   └── Services/
│       ├── IMonitoringService.cs
│       ├── MonitoringService.cs
│       ├── IServerApiClient.cs
│       ├── ServerApiClient.cs
│       ├── ISystemInfoService.cs
│       ├── SystemInfoService.cs
│       ├── IScreenCaptureService.cs
│       ├── ScreenCaptureService.cs
│       ├── IStartupService.cs
│       └── StartupService.cs
│
└── EmployeeMonitor.Server/
    ├── EmployeeMonitor.Server.csproj
    ├── Program.cs
    ├── appsettings.json
    ├── Properties/
    │   └── launchSettings.json
    ├── Models/
    │   ├── ClientInfo.cs
    │   ├── ClientRegistrationDto.cs
    │   └── PingResponseDto.cs
    ├── Application/
    │   ├── IClientRegistry.cs
    │   └── ClientRegistry.cs
    ├── Controllers/
    │   └── ClientsController.cs
    └── wwwroot/
        └── index.html
```

## 🧪 Как проверить работу

1. Запустите сервер: `dotnet run --project EmployeeMonitor.Server`.
2. Откройте `http://localhost:8080` — таблица пуста.
3. Опубликуйте и запустите клиент (см. шаг 5 выше) — двойной клик по exe.
4. Обновите страницу — появится строка с вашей машиной.
5. Нажмите **«Запросить скриншот»** — через 5–7 секунд появится превью.
6. Убейте клиент через Диспетчер задач — статус станет `○ offline` через 30 секунд.

## 🖼 Почему BMP, а не JPEG

Требование ТЗ — **не использовать сторонние библиотеки**. `System.Drawing.Common` формально является NuGet-пакетом (пусть и от Microsoft), а для JPEG без него нужен либо WIC через COM-интероп, либо свой JPEG-энкодер. Оба варианта нереалистичны за 1–2 дня.

**BMP** — простой формат, сериализуется вручную за 40 строк, браузеры показывают его нативно, сторонних библиотек не требуется.

Для продакшена использовался бы **WIC** (`Windows Imaging Component`) через `IWICImagingFactory` или `System.Drawing.Common` с `ImageFormat.Jpeg`.

## ⚠️ Known limitations

- **In-memory хранение** — список клиентов и скриншоты не переживают рестарт сервера.
- **BMP без сжатия** — Full HD ~6 МБ, 4K ~24 МБ.
- **Только primary monitor** — мультимониторные конфигурации не обрабатываются.
- **IP — первый IPv4** из `Dns.GetHostEntry`, может оказаться виртуальным адаптером (Hyper-V, WSL).
- **Нет аутентификации** — любой может зарегистрировать клиента, запросить скриншот или загрузить фейковый BMP.
- **Нет HTTPS** — трафик не шифруется.
- **Нет ретраев с backoff** — при недоступности сервера клиент ждёт следующий интервал.
- **Нет тестов** — покрытие отсутствует, задача ограничена 1–2 днями.
- **Автозапуск только для опубликованного exe** — через `dotnet run` не регистрируется.
