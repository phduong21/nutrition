---
workflowStatus: completed
epic: product-endpoints-edge-cases
scope: GET /products/{barcode}, GET /products/{barcode}/alternatives
date: 2026-06-27
author: pham
---

# Test Design: Product Endpoints — Edge Cases

**Date:** 2026-06-27  
**Author:** pham  
**Status:** Draft  
**Scope:** Focused edge-case coverage for `GET /products/{barcode}` and `GET /products/{barcode}/alternatives`

---

## Executive Summary

This document maps **observed implementation behavior** (from `FoodFetcher`, `ProductService`, `NutritionScoringEngine`, `ProductEndpoints`) to test scenarios across barcode validation, sparse OFF payloads, Nutri-Score gaps, category handling, alternatives degradation, and partial upstream data.

**Coverage summary**

| Priority | Scenarios | Est. effort |
|----------|-----------|-------------|
| P0 | 8 | ~6–10 h |
| P1 | 14 | ~8–14 h |
| P2 | 9 | ~4–8 h |
| **Total** | **31** | **~18–32 h** |

**Highest risks**

| ID | Score | Finding |
|----|-------|---------|
| R-001 | 6 (DATA) | All-null nutriments → score 100 / `Healthy` — misleading band |
| R-002 | 6 (TECH) | OFF product missing `code` → mapper throws → unhandled 500 |
| R-003 | 4 (DATA) | PRD says malformed barcode → 404; whitespace → 400 (spec drift) |

---

## Implementation Baseline (Evidence)

| Area | Current behavior | Source |
|------|------------------|--------|
| Barcode empty/whitespace | `InvalidInput` → HTTP **400** | `FoodFetcher.GetProductAsync` L18–19 |
| Barcode otherwise | Forwarded verbatim to OFF v2 | `FoodFetcher` L21 |
| OFF `status != 1` or null product | **404** | `FoodFetcher` L33–34 |
| OFF 5xx on product read | **502** | `FoodFetcher` L26–29 |
| Category averages on product GET | **Always `null`** (FR4 removed) | `ProductService.BuildAnalysis` L42 |
| Alternatives search failure | **200 + `alternatives: []`** | `ProductService` L30–35 |
| Source Nutri-Score `a` / null / invalid | **200 + empty alternatives** | `FoodFetcher.GetBetterGrades` L127–131 |
| No `categories_tags` | **200 + empty alternatives** | `FoodFetcher.BuildSearchQuery` L86–87 |
| Null nutriments fields | Treated as 0 penalty/bonus; baseline score **100** | `NutritionScoringEngine.Penalty/Bonus` |

---

## Edge Case Catalog

### 1. Barcode Formats

| ID | Input | Route | Expected (current impl.) | Level | Pri | Covered? |
|----|-------|-------|--------------------------|-------|-----|----------|
| BC-01 | *(empty segment)* `GET /products/` | product | Route mismatch → **404** (no handler) | API | P1 | ❌ |
| BC-02 | Whitespace only `%20` / `%09%09` | both | **400** problem+json, detail mentions barcode required | Unit + API | P0 | ❌ |
| BC-03 | `invalid` (letters) | both | OFF `status:0` → **404** | API | P0 | ✅ partial |
| BC-04 | `1` or `12` (too short) | both | OFF not found → **404** | API | P1 | ❌ |
| BC-05 | 14-digit valid EAN `5000159407236` | both | **200** | API | P0 | ❌ |
| BC-06 | 20+ digit string | both | OFF not found or upstream error → **404** or **502** | API | P2 | ❌ |
| BC-07 | `3017620422003!` or `abc-123` (special chars) | both | URL-encoded path; OFF **404** (no local format check) | API | P1 | ❌ |
| BC-08 | Path traversal attempt `..%2F..` | both | Framework routing rejection or **404**; must not escape app root | API | P1 | ❌ |
| BC-09 | Leading/trailing spaces in barcode ` %203017620422003%20` | both | **Likely 404** (OFF lookup with spaces) — document actual behavior | API | P2 | ❌ |
| BC-10 | Unicode in barcode `café123` | both | Encoded path; OFF **404** or **502** | API | P3 | ❌ |

**Spec note:** PRD FR1 groups "unknown **or malformed**" barcode as **404**. Only whitespace is rejected locally as **400**. Decide whether to align code or update PRD.

**Suggested test names**

- `GetProduct_WhitespaceBarcode_Returns400`
- `GetProduct_EmptyPath_Returns404`
- `GetProduct_ShortNumericBarcode_Returns404`
- `GetAlternatives_SourceGradeA_Returns200EmptyArray` *(see ALT section)*

---

### 2. Missing / Null Nutriments Fields

| ID | OFF payload shape | Expected product response | Level | Pri |
|----|-------------------|---------------------------|-------|-----|
| NM-01 | `nutriments` key absent | **200**; all nutriments JSON fields `null`; `nutritionScore` **100**; `healthBand` **Healthy** | Unit + API | P0 |
| NM-02 | `nutriments: {}` (all fields absent) | Same as NM-01 | Unit | P0 |
| NM-03 | Partial: only `sugars_100g` + `salt_100g` | **200**; score penalized only on present fields; missing fields ignored | Unit | P1 |
| NM-04 | `fiber_100g: null` (Nutella fixture) | **200**; no fiber positive flag; score unchanged for fiber | Unit | ✅ |
| NM-05 | Extreme present values with rest null | Concerns fire for present highs; positives only for present goods | Unit | P1 |
| NM-06 | All null via engine | `concerns` = "No significant…"; `positives` = "No standout…"; disclaimer present | Unit | ✅ |

**Risk R-001:** Empty nutriments yield **Healthy** band — product looks healthy with no data. Consider acceptance criteria: `healthBand: Unknown` when insufficient nutriments (PRD v3 mentions this; current `ClassifyHealthBand` has no Unknown path).

**Fixture:** `off-partial-nutriments.json`, `off-empty-nutriments.json`

---

### 3. Products Without `nutriscore_grade`

| ID | Scenario | `GET /products` | `GET /alternatives` | Level | Pri |
|----|----------|-----------------|---------------------|-------|-----|
| NS-01 | OFF omits `nutriscore_grade` | **200**; `nutriscoreGrade: null`; scoring unchanged | **200**; `alternatives: []` (`GetBetterGrades(null)` → no better grades) | Unit + API | P0 |
| NS-02 | `nutriscore_grade: ""` | Treated as null rank | Empty alternatives | Unit | P1 |
| NS-03 | `nutriscore_grade: "unknown"` / `"f"` | Invalid rank → null-equivalent for alternatives | Empty alternatives; grade echoed in product response | Unit | P1 |
| NS-04 | Source `e`, search hits lack grade | Hits filtered by `IsStrictlyBetterGrade` → excluded | Unit | P1 |

**Suggested tests**

- `GetProduct_MissingNutriScoreGrade_Returns200WithNullGrade`
- `GetAlternatives_MissingSourceNutriScore_Returns200EmptyArray`
- `FoodFetcher_GetBetterGrades_NullOrA_ReturnsEmpty`

---

### 4. Category Comparison When Category Not Found

| ID | Scenario | Expected on `GET /products` | Level | Pri |
|----|----------|----------------------------|-------|-----|
| CC-01 | Normal product (categories present) | `nutritionInsights.summary` contains *"based on per-100g nutriments"* (no category compare) | Unit | ✅ partial |
| CC-02 | `categories_tags: []` | Same generic summary; **no error** | Unit + API | P1 |
| CC-03 | Non-English-only tags e.g. `fr:…` | Summary unchanged; alternatives query uses fallback last tag | Unit | ✅ partial |
| CC-04 | Engine with `CategoryAverages` all null | Summary: *"category comparison data unavailable"* | Unit | P2 |
| CC-05 | Engine with averages, product field null | That nutrient skipped in comparison | Unit | P2 |

**Important:** Category peer fetch was **removed from the product endpoint** (FR4). "Category not found" is **not** an HTTP failure — it is the **default path**. Tests should assert **degraded copy**, not 404/502.

**Alternatives linkage**

| ID | `categories_tags` | Expected `GET /alternatives` |
|----|-------------------|------------------------------|
| CC-A1 | `[]` | **200**, `alternatives: []`, no search call |
| CC-A2 | tags with no `en:` ascii slug | Search uses fallback query; may return 0 or N results |

---

### 5. Alternatives When No Better Nutri-Score Exists

| ID | Source grade | Search results | Expected | Level | Pri |
|----|--------------|----------------|----------|-------|-----|
| ALT-01 | `a` (best) | *(search skipped)* | **200**, `alternatives: []` | Unit + API | P0 |
| ALT-02 | `e` | Only `e`/`d` candidates | **200**, `alternatives: []` | Unit | P1 |
| ALT-03 | `c` | Mix of `c`, `d`, `e` | **200**, only `a`/`b` in array | Unit | P1 |
| ALT-04 | `e` | Better grades but same barcode as source | Excluded (`code != source`) | Unit | P2 |
| ALT-05 | `e` | Better grades, ranked | Ordered by grade then `nutritionScore` desc | Unit | ✅ partial |

**Suggested tests**

- `GetAlternatives_SourceGradeA_Returns200EmptyArray`
- `SearchAlternativesAsync_NoBetterGradesInResults_ReturnsEmpty`
- `GetAlternatives_SearchReturnsOnlyWorseGrades_Returns200EmptyArray`

---

### 6. OFF API Returns Partial Data

#### Product read (`GET /products`)

| ID | OFF response | Expected | Level | Pri |
|----|--------------|----------|-------|-----|
| PD-01 | HTTP 200, `status: 0` | **404** | Unit | ✅ partial |
| PD-02 | HTTP 200, `product: null` | **404** | Unit | P1 |
| PD-03 | HTTP 404 | **404** | Unit | ❌ explicit |
| PD-04 | HTTP 500/503 | **502** | Unit | ✅ |
| PD-05 | HTTP 200, valid status, **missing `code`** | Mapper `InvalidOperationException` → **500** ⚠️ | Unit + API | P0 |
| PD-06 | Missing `product_name`, `brands` | **200**; empty strings | Unit | P1 |
| PD-07 | Missing `ingredients_text` | **200**; `ingredientsText: null` | Unit | P2 |
| PD-08 | Malformed JSON / empty body | Deserialization failure → **500** ⚠️ | Unit | P1 |
| PD-09 | Extra unknown fields | Ignored; **200** | Unit | P3 |

#### Search (alternatives only)

| ID | Search response | Expected | Level | Pri |
|----|-----------------|----------|-------|-----|
| PD-S1 | HTTP 503 then 503 | Retry once → **200** product path unaffected; alternatives **[]** | Unit | P1 |
| PD-S2 | HTTP 500 | **200**, `alternatives: []` | Unit | P1 |
| PD-S3 | Network exception | **200**, `alternatives: []` (catch swallows) | Unit | P1 |
| PD-S4 | `hits: []` | **200**, `alternatives: []` | Unit | P1 |
| PD-S5 | Hits missing `nutriments` / `nutriscore_grade` | Mapped; filtered or ranked with nulls | Unit | P1 |
| PD-S6 | Hit missing `code` | Excluded from results | Unit | P2 |
| PD-S7 | `hits: null` | Treated as empty list | Unit | P2 |

**Risk R-002:** PD-05 and PD-08 are **unhandled failures** today — P0 whether to fix (map to 404/502) or document as known limitation.

**Fixtures needed:** `off-missing-code.json`, `off-status-zero.json`, `off-search-503.json`, `off-search-empty-hits.json`, `off-search-same-grade-only.json`

---

## Coverage Matrix (Condensed)

| Scenario group | Unit | API integration | Playwright |
|----------------|------|-----------------|------------|
| Barcode formats | FoodFetcher + ProductService | WebApplicationFactory | Smoke on deployed |
| Null nutriments | Mapper + ScoringEngine | Stubbed OFF | Optional |
| No nutriscore | FoodFetcher + ScoringEngine | Stubbed OFF | Optional |
| Category (degraded) | ScoringEngine + BuildSearchQuery | Assert summary text | N/A |
| No better alternatives | FoodFetcher + ProductService | Stubbed search | Live OFF flaky |
| Partial OFF | FoodFetcher + Mapper | Stubbed handlers | N/A |

**Avoid duplication:** Barcode → API once; scoring math → unit only; OFF wire format → unit with fixtures.

---

## Risk Register

| ID | Cat | Description | P | I | Score | Mitigation |
|----|-----|-------------|---|---|-------|------------|
| R-001 | DATA | Null nutriments → score 100 / Healthy | 2 | 3 | 6 | Add `Unknown` band when &lt;N fields present; test NM-01 |
| R-002 | TECH | Missing barcode in OFF payload → 500 | 2 | 3 | 6 | Catch in mapper/fetcher → 404; test PD-05 |
| R-003 | BUS | 400 vs 404 for invalid barcode | 2 | 2 | 4 | Align PRD or add format validation |
| R-004 | DATA | Alternatives silently empty on search failure | 3 | 2 | 6 | Document contract; optional `warnings` field |
| R-005 | PERF | Live search flaky in CI | 2 | 2 | 4 | Stub search in PR; live in nightly only |

---

## Execution Strategy

| Suite | When | Contents |
|-------|------|----------|
| **PR** | Every push | All unit edge cases + API tests with `TestFixtures` stubs (&lt;5 min) |
| **Nightly** | Scheduled | 1–2 live OFF smoke tests (valid barcode + alternatives) |
| **Manual** | Pre-release | BC-08 security spot-check, PD-05 if not automated |

**Quality gates:** P0 pass 100%; P1 ≥ 95%; no unhandled 500 on documented OFF partial payloads before release.

---

## Gap Analysis vs Existing Tests

| Existing test | Gap |
|---------------|-----|
| `GetProduct_InvalidBarcode_Returns404` | Only covers `invalid`; not whitespace, short numeric, special chars |
| `GetProductAsync_OffServerError_ReturnsUpstreamFailure` | Product client only; not malformed JSON |
| `NutritionInsights_AlwaysContainsDisclaimerField` | All-null nutriments — does not assert score/band anomaly |
| `ScoringEngine_WithoutCategoryAverages_StillReturnsInsights` | Does not assert exact summary template |
| `GetAlternatives_ValidBarcode_Returns200WithAlternatives` | Does not cover empty alternatives paths |
| No test for `GetBetterGrades` / grade `a` / null source | ALT-01, NS-01 |

---

## Recommended Implementation Order

1. **P0 fixtures + unit tests** — NM-01, NS-01, ALT-01, PD-05, BC-02  
2. **API integration** — BC-02, NS-01, ALT-01 with stubbed handlers  
3. **Decide R-001/R-002** — product fix or accept with documentation  
4. **P1 expansion** — partial nutriments, search degradation matrix  
5. **PRD alignment** — BC-02 400 vs 404 wording
