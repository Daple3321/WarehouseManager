# WarehouseManager

REST API для управления складскими запасами, построенный на ASP.NET Core (.NET 10). Предназначен для отслеживания физических товаров через многозонный складской процесс - от приёмки до проверки, оформления дефектов и перевода в статус готовности к продаже.

**Live demo размещён на [Render](https://warehousemanager-u0va.onrender.com) — смотри раздел [Live Demo](#live-demo) ниже.**

[Как запустить](#запуск-локально)

---

## Возможности

- **Управление жизненным циклом товара** — товары проходят через заданные состояния: `InTransit → Received → Inspection → ReadyForSale`, с отдельным потоком для `Defected`
- **Хранение по зонам** — товары назначаются в именованные складские зоны, каждая из которых имеет настраиваемый лимит вместимости. Перемещение отклоняется, если целевая зона заполнена
- **Оформление дефектов** — отправка отчёта о дефекте с изображением и причиной. Событие публикуется в Kafka topic и обрабатывается фоновым сервисом
- **Массовая приёмка товаров** — принять несколько товаров за один API-вызов
- **Загрузка изображений** — прикрепление изображений к товарам и отчётам о дефектах (`.jpg`, `.jpeg`, `.png`, максимум 5 МБ)
- **Аналитика** — запрос статистики по поставкам и дефектам за настраиваемый временной промежуток
- **Redis кэширование** — зоны и категории кэшируются
- **API versioning** — все маршруты версионированы под `api/v1/`
- **Интерактивная документация** — автогенерируемый [Scalar](https://scalar.com) UI доступен по `/scalar/v1`
- **Автоматические миграции БД** — EF Core миграции применяются при запуске; база данных и seed-данные создаются автоматически

---

## Стек технологий

| Слой | Технология |
|---|---|
| Runtime | .NET 10 / ASP.NET Core Web API |
| База данных | PostgreSQL 16 + EF Core 10 (Npgsql) |
| Кэш | Redis 8 (StackExchange.Redis) |
| Очередь сообщений | Apache Kafka (Confluent.Kafka) |
| Документация API | OpenAPI + Scalar |
| Контейнеризация | Docker + Docker Compose |
| Тестирование | xUnit, Moq, EF Core InMemory |

---

## Обзор API

Базовый путь: `api/v1/`

### Items

| Метод | Маршрут | Описание |
|---|---|---|
| `GET` | `/items` | Список товаров с пагинацией, фильтр по `categoryId` |
| `GET` | `/items/{id}` | Получить товар по ID |
| `GET` | `/items/{id}/image` | Скачать изображение товара |
| `GET` | `/items/zone/{zoneId}` | Список товаров в зоне с пагинацией |
| `GET` | `/items/categories` | Список всех категорий (из кэша) |
| `POST` | `/items` | Добавить один товар (multipart/form-data) |
| `POST` | `/items/receive` | Массовая приёмка товаров |
| `PUT` | `/items/move` | Переместить товар в другую зону |
| `PUT` | `/items/{id}/state/{state}` | Изменить состояние товара |
| `POST` | `/items/{id}/defect` | Отправить отчёт о дефекте с изображением |
| `GET` | `/items/{reportId}/defect` | Получить отчёт о дефекте |
| `GET` | `/items/{reportId}/defect/image` | Скачать изображение из отчёта о дефекте |
| `DELETE` | `/items/{id}` | Удалить товар |

### Zones

| Метод | Маршрут | Описание |
|---|---|---|
| `GET` | `/zones` | Список всех зон (из кэша) |
| `GET` | `/zones/{id}` | Получить зону по ID (из кэша) |
| `POST` | `/zones` | Создать новую зону |

### Analytics

| Метод | Маршрут | Описание |
|---|---|---|
| `GET` | `/analytics/deliveries/{days}` | Принятые товары + топ-категория за последние N дней |
| `GET` | `/analytics/defects/{days}` | Количество дефектов + самая дефектная категория за последние N дней |

---

## Live Demo

API размещён на **Render.com**:

- **Base URL:** `https://warehousemanager-u0va.onrender.com`
- **Документация (Scalar):** `https://warehousemanager-u0va.onrender.com/scalar/v1`

> Примечание: Первый запрос после простоя может занять до 60 секунд.

---

## Запуск локально

### Необходимые компоненты

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Вариант 1 — Docker Compose (рекомендуется)

Клонируй репозиторий и запусти все сервисы одной командой:

```bash
git clone https://github.com/your-username/WarehouseManager.git
cd WarehouseManager/WarehouseManager
docker compose up --build
```

Запустятся:
- PostgreSQL 16
- Redis 8
- Apache Kafka (KRaft mode, без Zookeeper)
- API

API будет доступен по адресам:
- **API:** `http://localhost:8080`
- **Документация:** `http://localhost:8080/scalar/v1`

Миграции и seed-данные (зоны, категории) применяются автоматически при первом запуске.

Остановить все сервисы:

```bash
docker compose down
```

Остановить и удалить все data volumes:

```bash
docker compose down -v
```

---

### Вариант 2 — Локальная разработка (нужен SDK)

**Необходимые компоненты:**

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Docker Desktop

**Шаги:**

1. Запустить только инфраструктуру:

```bash
cd WarehouseManager/WarehouseManager
docker compose up warehouse-db warehouse-redis warehouse-kafka -d
```

2. Запустить API:

```bash
dotnet run --project WarehouseManager
```

API будет доступен по адресам:
- **HTTP:** `http://localhost:5264`
- **HTTPS:** `https://localhost:7213`
- **Документация:** `http://localhost:5264/scalar/v1`

---

## Структура проекта

```
WarehouseManager/
├── WarehouseManager/           # Основной проект API
│   ├── Controllers/            # Items, Zones, Analytics
│   ├── Services/               # Бизнес-логика + Kafka consumer
│   ├── Models/
│   │   ├── Entities/           # Domain models (Item, Zone, Category, DefectReport)
│   │   └── DTOs/               # Request/response DTOs
│   ├── Data/                   # EF Core DbContext
│   ├── Migrations/             # История EF Core миграций
│   ├── Middleware/             # Global exception handler, логирование запросов
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── appsettings.json
└── WarehouseManager.Tests/     # xUnit тесты
```

---

## Модель данных

### Состояния товара

```
InTransit → Received → Inspection → ReadyForSale
                                  ↘ Defected
```

### Предустановленные зоны

| Зона | Вместимость |
|---|---|
| Sorting Zone | 25 |
| Inspection Zone | 100 |
| Defection Zone | 5 |
| Ready for Sale Zone | 25 |

### Предустановленные категории

Electronic Parts, Books, PC Parts

---

## Архитектура

```
┌─────────────┐     HTTP      ┌──────────────────┐
│   Client    │ ────────────▶ │ ASP.NET Core API │
└─────────────┘               └────────┬─────────┘
                                       │
              ┌────────────────────────┼────────────────────┐
              │                        │                    │
              ▼                        ▼                    ▼
       ┌────────────┐          ┌──────────────┐    ┌──────────────┐
       │ PostgreSQL │          │    Redis      │    │    Kafka     │
       │  (storage) │          │   (cache)     │    │  (events)    │
       └────────────┘          └──────────────┘    └──────┬───────┘
                                                          │
                                                          ▼
                                                  ┌───────────────┐
                                                  │  Background   │
                                                  │  Consumer     │
                                                  └───────────────┘
```

При отправке отчёта о дефекте API публикует событие в Kafka topic `defectsTopic`. Фоновый сервис `DefectReportConsumer` подписан на этот topic и обрабатывает входящие события.
