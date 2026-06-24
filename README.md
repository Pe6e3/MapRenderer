# TrackMapRenderer

Сервис генерации PNG-изображений маршрутов на карте из географических координат.

Входные данные — список точек с координатами. На выходе — PNG-изображение карты с отмеченным маршрутом, маркерами и подписями.

Используется внешними системами для визуализации маршрутов: логистика, мониторинг, отслеживание перемещений.

---

## Стек

- ASP.NET Core 9 Web API
- Leaflet.js — отрисовка карты
- Playwright (Chromium) — рендеринг HTML → PNG
- OpenStreetMap — тайлы (через локальный прокси)

---

## API

### Генерация карты маршрута

```http
POST /api/map/render
Content-Type: application/json
```

#### Тело запроса

```json
{
  "title": "Seal 8252595211",
  "subtitle": "Russia → Mozambique",
  "width": 900,
  "height": 600,
  "points": [
    {
      "lat": 51.563571,
      "lon": 38.396241,
      "label": "Russia",
      "type": "start"
    },
    {
      "lat": -14.083181,
      "lon": 36.419055,
      "label": "Mozambique",
      "type": "finish"
    }
  ],
  "options": {
    "showTrack": true,
    "showMarkers": true,
    "showLabels": true,
    "autoFit": true,
    "padding": 80,
    "mapTheme": "light"
  }
}
```

#### Поля

| Поле | Тип | По умолчанию | Описание |
|------|-----|-------------|----------|
| `title` | string | — | Заголовок (макс. 300 символов) |
| `subtitle` | string | — | Подзаголовок (макс. 200 символов) |
| `width` | int | 900 | Ширина изображения (300–2000) |
| `height` | int | 600 | Высота изображения (300–2000) |
| `points` | array | — | Массив точек маршрута (2–100) |

#### Точка маршрута

| Поле | Тип | Обязательно | Описание |
|------|-----|-------------|----------|
| `lat` | double | Да | Широта (−90..90) |
| `lon` | double | Да | Долгота (−180..180) |
| `label` | string | Нет | Подпись (макс. 100 символов) |
| `type` | string | Нет | Тип точки: `start`, `middle`, `finish` (по умолчанию `middle`) |

#### Типы точек и цвета маркеров

| Тип | Цвет маркера |
|-----|-------------|
| `start` | Зелёный |
| `middle` | Синий |
| `finish` | Красный |

#### Опции рендеринга

| Поле | Тип | По умолчанию | Описание |
|------|-----|-------------|----------|
| `showTrack` | bool | `true` | Показывать линию маршрута |
| `showMarkers` | bool | `true` | Показывать маркеры |
| `showLabels` | bool | `true` | Показывать подписи к маркерам |
| `autoFit` | bool | `true` | Автоматически масштабировать карту |
| `padding` | int | 80 | Отступ от краёв точек (пиксели) |
| `mapTheme` | string | `"light"` | Тема карты: `light`, `dark` |

#### Ответ

```
200 OK
Content-Type: image/png

<PNG binary>
```

#### Ошибки

```
400 Bad Request
Content-Type: application/json

{ "error": "At least 2 points are required" }
```

```
500 Internal Server Error
Content-Type: application/json

{ "error": "Map rendering failed" }
```

---

### Тестовый рендер

```http
POST /api/map/render/test
```

Генерирует предустановленный маршрут (Россия → Мозамбик) для проверки работоспособности.

```
200 OK
Content-Type: image/png
```

---

### Прокси тайлов

```http
GET /api/tiles/{z}/{x}/{y}.png
```

Возвращает тайл OpenStreetMap. Тайлы кешируются на диске.

| Параметр | Диапазон |
|----------|----------|
| `z` | 0–19 |
| `x` | 0..2^z − 1 |
| `y` | 0..2^z − 1 |

При ошибке загрузки возвращается прозрачный PNG 1×1 (fallback).

---

### Проверка здоровья

```http
GET /health
```

```json
{ "status": "ok" }
```

---

## Конфигурация

Секция `MapRender` в `appsettings.json`:

```json
{
  "MapRender": {
    "TileUrl": "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
    "DarkTileUrl": "https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png",
    "TileCachePath": "./tile-cache",
    "TileCacheTtlDays": 30,
    "EnableTileCache": true
  }
}
```

| Параметр | Описание | По умолчанию |
|----------|----------|-------------|
| `TileUrl` | URL-шаблон тайлов OSM | `https://tile.openstreetmap.org/{z}/{x}/{y}.png` |
| `DarkTileUrl` | URL-шаблон тёмных тайлов | CartoDB Dark |
| `TileCachePath` | Путь к кешу тайлов | `./tile-cache` |
| `TileCacheTtlDays` | Время жизни кеша (дни) | 30 |
| `EnableTileCache` | Включить кеширование | `true` |

---

## Запуск

### Локально

```bash
dotnet run
```

Сервис запускается на `http://localhost:5000` (или порт из `launchSettings.json`).

### Установка Playwright (первый запуск)

```bash
dotnet build
powershell -ExecutionPolicy Bypass -File "bin\Debug\net9.0\playwright.ps1" install chromium
```

### Публикация

```bash
dotnet publish -c Release -o ./publish
```

---

## Деплой на Linux (systemd)

### 1. Собрать и скопировать

```bash
dotnet publish -c Release -o ./publish
scp -r ./publish/* user@server:/opt/trackmaprenderer/
```

### 2. Установить Playwright на сервере

```bash
cd /opt/trackmaprenderer
dotnet Microsoft.Playwright.dll install chromium
# или
pwsh playwright.ps1 install chromium
```

### 3. Настроить systemd

Скопировать файл `trackmaprenderer.service`:

```bash
sudo cp trackmaprenderer.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now trackmaprenderer
```

### 4. Проверить

```bash
sudo systemctl status trackmaprenderer
curl http://localhost:5000/health
```

### Файл `trackmaprenderer.service`

```ini
[Unit]
Description=TrackMapRenderer Service
After=network.target

[Service]
WorkingDirectory=/opt/trackmaprenderer
ExecStart=/usr/bin/dotnet /opt/trackmaprenderer/TrackMapRenderer.dll
Restart=always
RestartSec=10
SyslogIdentifier=trackmaprenderer
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=multi-user.target
```

---

## Docker (опционально)

```bash
docker-compose up -d
```

Сервис будет доступен на `http://localhost:5000`.

---

## Обратный прокси (Nginx)

```nginx
server {
    listen 80;
    server_name map.example.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## Примеры вызовов

### curl

```bash
curl -X POST http://localhost:5000/api/map/render \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Доставка #12345",
    "subtitle": "Москва → Санкт-Петербург",
    "width": 800,
    "height": 500,
    "points": [
      { "lat": 55.7558, "lon": 37.6173, "label": "Москва", "type": "start" },
      { "lat": 59.9343, "lon": 30.3351, "label": "Санкт-Петербург", "type": "finish" }
    ],
    "options": {
      "showTrack": true,
      "showMarkers": true,
      "showLabels": true,
      "mapTheme": "light"
    }
  }' \
  --output route.png
```

### PowerShell

```powershell
$body = @{
    title = "Доставка #12345"
    subtitle = "Москва → Санкт-Петербург"
    width = 800
    height = 500
    points = @(
        @{ lat = 55.7558; lon = 37.6173; label = "Москва"; type = "start" },
        @{ lat = 59.9343; lon = 30.3351; label = "Санкт-Петербург"; type = "finish" }
    )
    options = @{
        showTrack = $true
        showMarkers = $true
        showLabels = $true
        mapTheme = "light"
    }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Uri "http://localhost:5000/api/map/render" `
  -Method Post -ContentType "application/json" -Body $body `
  -OutFile route.png
```

---

## Ограничения

| Параметр | Значение |
|----------|----------|
| Минимум точек | 2 |
| Максимум точек | 100 |
| Ширина изображения | 300–2000 px |
| Высота изображения | 300–2000 px |
| Широта | −90..90 |
| Долгота | −180..180 |
| Заголовок | 300 символов |
| Подзаголовок | 200 символов |
| Подпись точки | 100 символов |
| Параллельных рендеров | 4 |
| Таймаут рендера | 60 сек |
