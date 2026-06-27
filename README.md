# Nutrition Intelligence Agent

A stateless .NET 10 minimal API that looks up packaged food by barcode via [Open Food Facts v2](https://world.openfoodfacts.org), runs a **rule-based Nutrition Scoring Engine** (no LLM, no API keys), and returns nutrition score, health band, insights, and healthier alternatives.

## What it does

- **`GET /products/{barcode}`** — fetch product data, compute a 0–100 nutrition score, classify a health band, and return rule-generated `nutritionInsights` with optional category comparison.
- **`GET /products/{barcode}/alternatives`** — find same-category products with a strictly better Nutri-Score and rule-generated rationale.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- No API keys required (optional Open Food Facts user-agent for etiquette)

## How to run

```bash
cd NutritionAgent
dotnet run
```

The API listens on `http://localhost:5163` (see `Properties/launchSettings.json`).

### Swagger UI (Development)

Chạy app rồi mở trình duyệt:

```bash
cd NutritionAgent
dotnet run
```

- **Swagger UI:** [http://localhost:5163/swagger](http://localhost:5163/swagger) — giao diện thử API trực tiếp
- **OpenAPI JSON:** [http://localhost:5163/openapi/v1.json](http://localhost:5163/openapi/v1.json) — spec để import Postman/Insomnia

Swagger chỉ bật khi `ASPNETCORE_ENVIRONMENT=Development` (mặc định trong `launchSettings.json`).

## API reference

| Method | Endpoint | Mô tả |
| --- | --- | --- |
| `GET` | `/products/{barcode}` | Tra cứu sản phẩm + điểm dinh dưỡng + insights |
| `GET` | `/products/{barcode}/alternatives` | Gợi ý sản phẩm cùng loại có Nutri-Score tốt hơn |

### `GET /products/{barcode}`

**Tham số:** `barcode` — mã vạch EAN/UPC (ví dụ `3017620422003` = Nutella).

**Response 200** — JSON gồm:

| Field | Kiểu | Ý nghĩa |
| --- | --- | --- |
| `productName` | string | Tên sản phẩm |
| `brands` | string | Thương hiệu |
| `nutriments` | object | Đường, mỡ, protein, chất xơ, muối (g/100g) |
| `nutriscoreGrade` | string? | Nutri-Score OFF (`a`–`e`) |
| `ingredientsText` | string? | Danh sách thành phần |
| `nutritionScore` | int | Điểm 0–100 (rule-based) |
| `healthBand` | string | `Excellent` / `Good` / `Fair` / `Poor` |
| `nutritionInsights` | object | `summary`, `concerns`, `positives`, `disclaimer` |

**Lỗi:**

| Status | Khi nào |
| --- | --- |
| `404` | Barcode không tồn tại trên Open Food Facts |
| `502` | Open Food Facts không phản hồi |

### `GET /products/{barcode}/alternatives`

**Response 200** — JSON gồm `sourceBarcode` và mảng `alternatives`. Mỗi phần tử có `barcode`, `productName`, `nutriscoreGrade`, `rationale`.

**Lưu ý:** Endpoint này **không** nằm trong response của `GET /products/{barcode}` — gọi riêng khi cần gợi ý thay thế.

Optional configuration via environment variable or `appsettings.json`:

```json
{
  "OpenFoodFacts": {
    "UserAgent": "NutritionAgent/1.0 (your-contact)"
  }
}
```

## Sample requests

**Product lookup (Nutella demo barcode):**

```bash
curl -s http://localhost:5163/products/3017620422003 | jq
```

Example response (abbreviated):

```json
{
  "productName": "Nutella",
  "brands": "Ferrero, Nutella, Yum yum",
  "nutriments": {
    "sugars100g": 56.3,
    "fat100g": 30.9,
    "saturatedFat100g": 10.6,
    "proteins100g": 6.3,
    "fiber100g": null,
    "salt100g": 0.107
  },
  "nutriscoreGrade": "e",
  "ingredientsText": "…",
  "nutritionScore": 23,
  "healthBand": "Poor",
  "nutritionInsights": {
    "summary": "Compared to category average: sugar is above category average; …",
    "concerns": "High sugar (56.3/100g). High saturated fat (10.6/100g).",
    "positives": "No standout positive nutrient highlights.",
    "disclaimer": "This information is for educational purposes only and is not medical advice."
  }
}
```

**Healthier alternatives:**

```bash
curl -s http://localhost:5163/products/3017620422003/alternatives | jq
```

**Unknown barcode (404):**

```bash
curl -s -o /dev/null -w "%{http_code}" http://localhost:5163/products/invalid
```

## Architecture decisions

| Decision | Why |
| --- | --- |
| **Rule-based scoring engine** | Deterministic, fully unit-testable, zero AI cost; demonstrates domain logic clearly |
| **Open Food Facts v2** | Public product + search API with no key; v2 supports category search needed for alternatives |
| **Layered design** | `Endpoints` → `Services` → `Domain`; `Infrastructure` → `Domain` only — clear separation without hexagonal ceremony |

## Project structure

```
NutritionAgent/
  Domain/           NutritionScoringEngine, records, thresholds
  Infrastructure/   FoodFetcher (OFF HTTP client)
  Services/         ProductService orchestration, Result pattern
  Endpoints/        Minimal API routes
NutritionAgent.Tests/
  Unit/             Scoring engine, fetcher, service tests
  Integration/      WebApplicationFactory endpoint tests
```

## Tests

```bash
dotnet test
```

## Demo barcodes

| Barcode | Product | Nutri-Score |
| --- | --- | --- |
| `3017620422003` | Nutella | E |
| `5000159407236` | Mars bar | E |
