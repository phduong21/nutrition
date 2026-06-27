# Open Food Facts v2 → Domain Field Mapping

Verified against live API responses for barcodes `3017620422003` (Nutella) and `5000159407236` (Mars).

## Response envelope

| OFF field | Domain | Required | Notes |
| --- | --- | --- | --- |
| `code` | `Product.Barcode` | Yes | Fallback to `product.code` when top-level missing |
| `status` | — | — | `1` = found; not mapped to domain (handled by fetch layer in Story 1.4) |
| `product` | entire `Product` | Yes | Mapping throws if null |

## Product fields

| OFF field | Domain property | Required in OFF | Nullable in domain |
| --- | --- | --- | --- |
| `product_name` | `ProductName` | No | No — defaults to `""` |
| `brands` | `Brands` | No | No — defaults to `""` |
| `nutriscore_grade` | `NutriScoreGrade` | No | Yes |
| `categories_tags` | `CategoriesTags` | No | No — defaults to empty list |
| `ingredients_text` | `IngredientsText` | No | Yes |
| `nutriments` | `Nutriments` | No | No — defaults to all-null nutriments record |

## Nutriments (per 100g)

| OFF field | Domain property | Required in OFF | Nullable in domain |
| --- | --- | --- | --- |
| `sugars_100g` | `Sugars100g` | No | Yes |
| `fat_100g` | `Fat100g` | No | Yes |
| `saturated-fat_100g` | `SaturatedFat100g` | No | Yes — JSON key uses hyphen |
| `proteins_100g` | `Proteins100g` | No | Yes |
| `fiber_100g` | `Fiber100g` | No | Yes — often absent (e.g. Nutella returns `null`) |
| `salt_100g` | `Salt100g` | No | Yes |

## Demo barcode snapshots

**3017620422003 (Nutella):** high sugar (56.3g), null fiber, nutriscore `e`.

**5000159407236 (Mars):** high sugar (61.6g), fiber 1.24g, nutriscore `e`.

Fixtures: `NutritionAgent.Tests/Fixtures/off-*.json`
