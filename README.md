# Nutrition Intelligence Agent

A stateless .NET 10 minimal API that looks up packaged food by barcode via [Open Food Facts v2](https://world.openfoodfacts.org), runs a **rule-based Nutrition Scoring Engine** (no LLM, no API keys), and returns nutrition score, health band, insights, and healthier alternatives.


## What it does

- **`GET /products/{barcode}`** — fetch product data, compute a 0–100 nutrition score, classify a health band, and return rule-generated `nutritionInsights`.
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

With the app running, open in a browser:

- **Swagger UI:** [http://localhost:5163/swagger](http://localhost:5163/swagger) — interactive API explorer
- **OpenAPI JSON:** [http://localhost:5163/openapi/v1.json](http://localhost:5163/openapi/v1.json) — spec for Postman/Insomnia import

Swagger is enabled only when `ASPNETCORE_ENVIRONMENT=Development` (the default in `launchSettings.json`).

## API reference

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/products/{barcode}` | Product lookup with nutrition score and insights |
| `GET` | `/products/{barcode}/alternatives` | Same-category alternatives with a better Nutri-Score |

### `GET /products/{barcode}`

**Parameter:** `barcode` — EAN/UPC barcode (e.g. `3017620422003` for Nutella).

**Response 200** — JSON fields:

| Field | Type | Meaning |
| --- | --- | --- |
| `productName` | string | Product name |
| `brands` | string | Brand(s) |
| `nutriments` | object | Sugar, fat, protein, fiber, salt (g/100g) |
| `nutriscoreGrade` | string? | Open Food Facts Nutri-Score (`a`–`e`) |
| `ingredientsText` | string? | Ingredient list |
| `nutritionScore` | int | Rule-based score 0–100 |
| `healthBand` | string | `Excellent` / `Good` / `Fair` / `Poor` |
| `nutritionInsights` | object | `summary`, `concerns`, `positives`, `disclaimer` |

**Errors:**

| Status | When |
| --- | --- |
| `404` | Barcode not found on Open Food Facts |
| `502` | Open Food Facts did not respond |

### `GET /products/{barcode}/alternatives`

**Response 200** — JSON with `sourceBarcode` and an `alternatives` array. Each item includes `barcode`, `productName`, `nutriScoreGrade`, and `rationale`.

**Note:** Alternatives are **not** included in the `GET /products/{barcode}` response — call this endpoint separately when you need swap suggestions.

Optional configuration via environment variable or `appsettings.json`:

```json
{
  "OpenFoodFacts": {
    "BaseUrl": "https://world.openfoodfacts.org",
    "SearchBaseUrl": "https://search.openfoodfacts.org",
    "UserAgent": "NutritionAgent/1.0 (your-contact)"
  }
}
```

- **Product lookup** uses `BaseUrl` — Open Food Facts v2 (`/api/v2/product/{barcode}.json`).
- **Alternatives search** uses `SearchBaseUrl` — [Search-a-licious](https://search.openfoodfacts.org) (`/search?q=…`). Nutri-Score filtering is done client-side (the `nutrition_grades` query param is unreliable on Search-a-licious).

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
    "summary": "Nutrition profile for Nutella based on per-100g nutriments.",
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

Example response:

```json
{
  "sourceBarcode": "3017620422003",
  "alternatives": [
    {
      "barcode": "7613035539679",
      "productName": "Weetabix",
      "nutriScoreGrade": "b",
      "rationale": "Nutri-Score B vs E — significantly lower sugar and saturated fat"
    }
  ]
}
```

**Unknown barcode (404):**

```bash
curl -s -o /dev/null -w "%{http_code}" http://localhost:5163/products/invalid
```

## Architecture decisions

| Decision | Why |
| --- | --- |
| **Stateless design** | No DB, no cache — every request fetches live; keeps scope honest and infra-free |
| **TDD** | Tests written before implementation; unit + integration coverage per story acceptance criteria |
| **Rule-based scoring engine** | Deterministic, fully unit-testable, zero AI cost; demonstrates domain logic clearly |
| **Open Food Facts v2 + Search-a-licious** | Product read via v2 (no key); alternatives via Search-a-licious full-text search (~1s vs v2 search timeouts) |
| **Layered design** | `Endpoints` → `Services` → `Domain`; `Infrastructure` → `Domain` only — clear separation without hexagonal ceremony |

## Project structure

```
NutritionAgent/
  Domain/           NutritionScoringEngine, records, thresholds
  Infrastructure/   FoodFetcher (OFF product + Search-a-licious HTTP clients)
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
