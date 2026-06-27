---
title: Nutrition Intelligence Agent
status: draft
created: 2026-06-27
updated: 2026-06-27T18:00
snapshot: v3-rule-based-engine
note: Frozen PRD — rule-based Nutrition Scoring Engine (no LLM/SK)
---

# PRD: Nutrition Intelligence Agent

## 0. Document Purpose

Lean requirements for a **1–2 day portfolio assignment** demonstrating test-driven backend development. Audience: implementer (you) and technical interview reviewers at a Nordic B2B SaaS company. Structured for downstream `bmad-architecture`, `STORIES.md`, and TDD. Assumptions tagged inline and indexed in §9.

**Inputs:** `.cursor/rules/workflow.mdc` (implementation workflow and acceptance tests).

## 1. Vision

**Nutrition Intelligence Agent** is a C# .NET minimal-API backend that looks up packaged food products by barcode via the public **Open Food Facts** API and runs a **rule-based Nutrition Scoring Engine** to calculate a nutrition score from raw nutriments, classify a health band, produce deterministic nutrition insights, and—via a dedicated endpoint—surface healthier alternatives in the same category.

The product exists to show **solid engineering discipline**: real external data, tests written before code, and transparent deterministic business logic (no LLM costs or API keys). It is a backend-only service consumed by other developers or services; there is no UI.

For the interview context, success means a reviewer can clone, run, hit documented endpoints, and see credible rule-based nutrition analysis on known barcodes within minutes—not production-scale coverage or clinical accuracy.

## 2. Target User

### 2.1 Jobs To Be Done

- **Integrator (developer):** Submit a product barcode and receive structured nutrition data plus rule-based insights without building Open Food Facts parsing or scoring logic myself.
- **Builder (you):** Prove I can ship a small, test-driven API under time pressure with clear docs and observable business rules.
- **Reviewer (interviewer):** Quickly assess code quality, testing habits, API design, and whether the scoring engine adds real value vs. a thin OFF wrapper.

### 2.2 Non-Users (v1)

- End consumers scanning products in a mobile app (no client UI in scope).
- Nutritionists or patients relying on this for medical or dietary treatment decisions.

### 2.3 Key User Journeys

- **UJ-1.** Alex, a backend developer evaluating the repo, runs the service locally, calls `GET /products/3017620422003`, receives Nutri-Score, calculated nutrition score, health band, and `nutritionInsights` (including category comparison), then calls `/alternatives` when the score is poor and gets a ranked list with brief rationale.

## 3. Glossary

- **Barcode** — EAN/UPC product identifier passed by the client (e.g. `3017620422003`).
- **Product** — Food item record returned from Open Food Facts, including name, brand, nutriments, Nutri-Score grade, category, and ingredients text.
- **Nutri-Score grade** — Open Food Facts letter grade `a`–`e` for overall nutritional quality.
- **Nutrition score** — Numeric score (0–100) calculated by the Nutrition Scoring Engine from per-100g nutriments: sugar, fat, saturated fat, protein, fiber, salt.
- **Health band** — Enum classified by the engine: `Healthy`, `Moderate`, `Poor`, or `Unknown` when data is insufficient.
- **Nutrition insights** — Structured object on the Product response: `{ summary, concerns, positives, disclaimer }` — rule-generated flags (e.g. high sugar, good fiber) and category comparison. Alternatives are not included here; see Alternative and FR-5.
- **Category average** — Mean per-100g values for key nutriments across products in the same OFF category, used for comparison.
- **Alternative** — Another Product in the same category with a better Nutri-Score, ranked and explained by rule-based rationale.
- **Nutrition Scoring Engine** — Pure domain component that scores nutriments, classifies health band, generates insights, and compares against category averages. No LLM or external AI.

## 4. Features

### 4.1 Product Lookup & Normalization

**Description:** Client supplies a barcode; the service fetches live product data from Open Food Facts API **v2** (`GET /api/v2/product/{barcode}.json`), maps it to internal DTOs. Invalid or unknown barcodes return a clear HTTP error. Realizes UJ-1.

**Notes:** MVP commits to OFF **v2** for product read (v3 is recommended upstream but lacks structured search needed for FR-5). Migration to v3 is a post-MVP concern — track schema and search API changes before upgrading.

#### FR-1: Get product by barcode

Integrator can retrieve a Product by barcode. Realizes UJ-1.

**Consequences (testable):**
- Valid barcode (e.g. `3017620422003`) → HTTP 200 with `productName`, `brands`, `nutriments`, `nutriscoreGrade`, `ingredientsText`, `nutritionScore`, `healthBand`.
- Unknown or malformed barcode → HTTP 404 with problem-details-style error body.
- Open Food Facts timeout or 5xx → HTTP 502 with message indicating upstream failure (no silent empty product).

**Out of Scope:** Caching product data across requests (v2).

### 4.2 Nutrition Scoring & Insights

**Description:** After Product fetch, the **Nutrition Scoring Engine** calculates `nutritionScore` from raw nutriments (sugar, fat, saturated fat, protein, fiber, salt per 100g), classifies `healthBand`, fetches category peers for averages, and returns structured `nutritionInsights` on the product response. All logic is deterministic and unit-testable. Realizes UJ-1.

#### FR-2: Calculate nutrition score and health band

Engine computes `nutritionScore` (0–100) from available nutriments and maps score to `healthBand`: high score → `Healthy`, mid → `Moderate`, low → `Poor`; insufficient nutriments → `Unknown`.

**Consequences (testable):**
- Product with low sugar/high fiber nutriments → higher `nutritionScore` than high sugar/low fiber fixture.
- Known fixture → expected `healthBand` (unit tests, no HTTP).
- Nutri-Score grade `a` product and engine `healthBand` are both present; engine band is derived from calculated score, not copied from OFF grade alone.

#### FR-3: Rule-based nutrition insights

Integrator receives `nutritionInsights` as `{ summary, concerns, positives, disclaimer }` on successful product lookup.

**Consequences (testable):**
- Response for valid barcode includes `nutritionInsights` with all four fields non-empty.
- `nutritionInsights.disclaimer` contains informational-only language (not medical advice).
- High sugar (per 100g above threshold) → `concerns` mentions high sugar.
- High saturated fat → `concerns` mentions saturated fat.
- Good protein or fiber → `positives` mentions protein or fiber as applicable.
- Insights reference actual product nutriments—not generic placeholder text.

#### FR-4: Category comparison on product response

Engine compares product nutriments against **category averages** (same OFF category) and surfaces comparison in `nutritionInsights.summary` or structured flags.

**Consequences (testable):**
- Product with category → `nutritionInsights` references category (e.g. "above/below category average for sugar").
- Product without category → comparison omitted gracefully; response still 200 with other insights.
- `GET /products/{barcode}` does **not** include an `alternatives` array — alternatives payload is FR-5 only.

**Out of Scope:** Inline alternatives on the product response.

### 4.3 Healthier Alternatives Endpoint

**Description:** Dedicated endpoint is the **only** surface that returns alternatives payload. Uses Open Food Facts **v2** structured search (`/api/v2/search`, same category, better Nutri-Score); ranks and explains alternatives with rule-based rationale (no LLM). Realizes UJ-1.

#### FR-5: List healthier alternatives

Integrator can call `GET /products/{barcode}/alternatives` and receive ranked alternatives with `barcode`, `productName`, `nutriscoreGrade`, and `rationale`.

**Consequences (testable):**
- Valid barcode with known poor product → HTTP 200 with non-empty `alternatives` array.
- Each alternative has Nutri-Score strictly better than source (e.g. source D → alternatives A/B/C only).
- `rationale` is rule-generated (e.g. "Nutri-Score B vs source D, lower sugar per 100g").
- Invalid barcode → HTTP 404.

### 4.4 API Surface & Developer Experience

**Description:** Minimal API with OpenAPI document, README, and sample requests so a reviewer can run without guesswork. `[ASSUMPTION: No API authentication in v1; service runs on localhost or single deploy target.]`

#### FR-6: Documented HTTP API

Service exposes OpenAPI (development) and README with setup, optional env vars, and curl examples for both endpoints.

**Consequences (testable):**
- README documents optional env vars (Open Food Facts user-agent only; no API keys required).
- Playwright or HTTP integration tests cover: valid product 200 + `nutritionInsights`, invalid 404, alternatives list present for demo barcode.

## 5. Non-Goals (Explicit)

- Mobile or web UI, barcode scanning, user accounts.
- LLM, OpenAI, Azure OpenAI, Semantic Kernel, or any paid AI API.
- Production multi-tenant auth, rate limiting, or billing.
- Medical-grade dietary prescriptions or allergen guarantees.
- Writing or mutating Open Food Facts data.
- Full global product catalog completeness or offline mode.

## 6. MVP Scope

### 6.1 In Scope

- .NET minimal API (`NutritionAgent` project).
- Open Food Facts **v2** product fetch + v2 structured search.
- **Nutrition Scoring Engine** — score, health band, rule-based insights, category comparison.
- Endpoints: `GET /products/{barcode}`, `GET /products/{barcode}/alternatives`.
- TDD: red tests per workflow.mdc before implementation.
- Integration verification with demo barcodes `3017620422003`, `5000159407236`.
- README + OpenAPI.

### 6.2 Out of Scope for MVP

- Persistent database or cache — live API calls only; keeps 1–2 day scope realistic.
- i18n — English responses only `[ASSUMPTION]`.
- Kubernetes / cloud IaC — local `dotnet run` sufficient for demo.

## 7. Success Metrics

**Primary**
- **SM-1:** All mandatory tests from workflow.mdc pass (unit + integration). Validates FR-1–FR-6.
- **SM-2:** Reviewer can follow README and get a successful `nutritionInsights` response in under 15 minutes on a clean machine (no API keys). Validates FR-6.

**Counter-metrics (do not optimize)**
- **SM-C1:** Response latency — demo may be slow; do not sacrifice test quality or real OFF fetches for sub-second p99.

## 8. Open Questions

*(None blocking MVP.)*

## 9. Assumptions Index

- §4.4 — **No authentication** on API for v1 demo.
- §6.2 — **English-only** response text.
- §6.2 — **Local/single-instance** deployment; no production SLA.
