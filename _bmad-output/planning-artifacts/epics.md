---
stepsCompleted:
  - step-01-validate-prerequisites
  - step-02-design-epics
  - step-03-create-stories
  - step-04-final-validation
inputDocuments:
  - prds/prd-Nutrition-2026-06-27/prd.md
  - architecture/architecture-Nutrition-2026-06-27/ARCHITECTURE.md
  - architecture/architecture-Nutrition-2026-06-27/ARCHITECTURE-SPINE.md
  - .cursor/rules/workflow.mdc
---

# Nutrition - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Nutrition, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: Integrator can retrieve a Product by barcode via `GET /products/{barcode}` — HTTP 200 with `productName`, `brands`, `nutriments`, `nutriscoreGrade`, `ingredientsText`, `nutritionScore`, `healthBand`; unknown/malformed barcode → 404 problem details; OFF timeout/5xx → 502.

FR2: Nutrition Scoring Engine computes `nutritionScore` (0–100) from per-100g nutriments (sugar, fat, saturated fat, protein, fiber, salt) and maps to `healthBand` (`Healthy` / `Moderate` / `Poor` / `Unknown`); deterministic and unit-testable.

FR3: Product response includes `nutritionInsights` as `{ summary, concerns, positives, disclaimer }` — all non-empty on success; rule-based flags for high sugar, high saturated fat, good protein/fiber; disclaimer is informational-only (not medical advice).

FR4: Engine compares product nutriments to same-category averages (from OFF) and surfaces comparison in `nutritionInsights.summary`; graceful omit when category missing; `GET /products/{barcode}` must NOT include `alternatives` array.

FR5: Integrator can call `GET /products/{barcode}/alternatives` — ranked alternatives with `barcode`, `productName`, `nutriscoreGrade`, rule-generated `rationale`; each alternative strictly better Nutri-Score than source; invalid barcode → 404.

FR6: Service exposes OpenAPI (dev), README with optional env vars (OFF user-agent only), curl examples; integration tests cover valid product + `nutritionInsights`, 404, alternatives for demo barcode.

### NonFunctional Requirements

NFR1: Stateless — no database or cross-request cache; live OFF fetch per request (AD-2).

NFR2: Deterministic scoring — same nutriments input → same score, band, and insights; no LLM or randomness (AD-4, AD-5).

NFR3: Layered dependency direction — Endpoints → Services → Domain; Infrastructure → Domain only; no reverse refs (AD-1).

NFR4: Error handling via Result pattern; RFC 7807 problem details for API errors; no bare try/catch at boundaries (AD-8).

NFR5: External HTTP via `IHttpClientFactory` named client `"OpenFoodFacts"` (AD-9).

NFR6: Immutable public models as C# `record` types (AD-11).

NFR7: Separate test project `NutritionAgent.Tests/` at solution root (AD-10).

NFR8: No authentication on API for MVP demo (PRD §9).

NFR9: English-only response text (PRD assumption).

NFR10: Local `dotnet run` deployment sufficient; no production SLA (PRD assumption).

NFR11: Counter-metric — do not sacrifice test quality or real OFF fetches for sub-second latency (SM-C1).

NFR12: Reviewer setup under 15 minutes on clean machine, no API keys (SM-2).

### Additional Requirements

- **Stack:** .NET 10, ASP.NET Core Minimal API, Microsoft.AspNetCore.OpenApi 10.0.8, xunit + WebApplicationFactory.
- **Open Food Facts v2 only:** product `GET /api/v2/product/{barcode}.json`; search `GET /api/v2/search` for alternatives and category peers (AD-3).
- **Layered folders:** `Endpoints/`, `Services/`, `Domain/`, `Infrastructure/` per structural seed.
- **Core components:** `NutritionScoringEngine`, `ScoringThresholds`, `FoodFetcher`, `ProductService`, `ProductEndpoints`, `Result.cs`.
- **Health band thresholds (seed):** score ≥ 70 → Healthy; 40–69 → Moderate; < 40 → Poor (Architecture §4.3).
- **Phase 2 prerequisite:** Verify OFF JSON shape for demo barcodes `3017620422003`, `5000159407236` before modeling — map `sugars_100g`, `fat_100g`, `saturated-fat_100g`, `proteins_100g`, `fiber_100g`, `salt_100g`, `categories_tags`.
- **TDD mandatory:** RED tests before implementation per workflow.mdc Phase 3.
- **Mandatory test names (workflow):** GetProduct_ValidBarcode_Returns200, GetProduct_InvalidBarcode_Returns404, ScoringEngine_LowSugarHighFiber_ReturnsHigherScoreThanHighSugarLowFiber, ScoringEngine_KnownFixture_ReturnsExpectedHealthBand, GetProduct_ResponseContainsNutritionInsights, ScoringEngine_HighSugar_FlagsConcernInInsights, ScoringEngine_GoodProteinOrFiber_FlagsPositiveInInsights, NutritionInsights_AlwaysContainsDisclaimerField, GetAlternatives_PoorProduct_ReturnsBetterNutriScore, GetProduct_DoesNotIncludeAlternativesArray.
- **Implementation order:** Domain → FoodFetcher → ProductService → Endpoints → Program.cs DI.
- **Playwright integration checks** after `dotnet run` (workflow Phase 6).
- **OpenAPI export** to `docs/openapi.json` via openspec (workflow Phase 7).
- **README** with architecture rationale (rule-based engine, OFF v2, layered) — workflow Phase 8.
- **Explicit non-goals:** LLM/SK/OpenAI, UI, DB/cache, auth, OFF writes, K8s.

### UX Design Requirements

N/A — backend-only API; no UI or UX design contract.

### FR Coverage Map

FR1: Epic 1 — Product lookup by barcode with OFF integration
FR2: Epic 1 — Calculated nutrition score and health band
FR3: Epic 1 — Rule-based nutritionInsights on product response
FR4: Epic 1 — Category average comparison in insights
FR5: Epic 2 — Healthier alternatives endpoint
FR6: Epic 3 — OpenAPI, README, integration verification

## Epic List

### Epic 1: Product Nutrition Analysis
Integrator can look up any barcode and receive nutrition score, health band, and rule-based insights with category comparison — no API keys required.
**FRs covered:** FR1, FR2, FR3, FR4

### Epic 2: Healthier Alternatives
Integrator can discover better Nutri-Score products in the same category with rule-generated rationale.
**FRs covered:** FR5

### Epic 3: Developer Experience & Verification
Reviewer can clone, run, test, and integrate the API in under 15 minutes with documented endpoints and OpenAPI.
**FRs covered:** FR6

---

## Epic 1: Product Nutrition Analysis

Integrator can look up any barcode and receive nutrition score, health band, and rule-based insights with category comparison.

### Story 1.1: Verify OFF API Shape and Scaffold Projects

As a **builder**,
I want domain records mapped from real Open Food Facts JSON and a test project scaffolded,
So that implementation is grounded in actual API data and TDD can begin.

**Acceptance Criteria:**

**Given** demo barcodes `3017620422003` and `5000159407236`
**When** OFF v2 product JSON is fetched and analyzed
**Then** models account for `sugars_100g`, `fat_100g`, `saturated-fat_100g`, `proteins_100g`, `fiber_100g`, `salt_100g`, `nutriscore_grade`, `categories_tags`, `product_name`, `brands`, `ingredients_text`
**And** nullable vs required fields are documented in code comments or a short mapping note
**And** `NutritionAgent.Tests/` project exists referencing `NutritionAgent` (NFR7)
**And** layered folders `Domain/`, `Infrastructure/`, `Services/`, `Endpoints/` exist per AD-1

### Story 1.2: Nutrition Scoring Engine (TDD)

As an **integrator**,
I want a deterministic scoring engine that calculates nutrition score and health band from nutriments,
So that product quality is assessed consistently without external AI.

**Acceptance Criteria:**

**Given** unit tests written first (RED then GREEN)
**When** `NutritionScoringEngine` scores nutriments fixtures
**Then** `ScoringEngine_LowSugarHighFiber_ReturnsHigherScoreThanHighSugarLowFiber` passes
**And** `ScoringEngine_KnownFixture_ReturnsExpectedHealthBand` passes
**And** score is 0–100; band mapping uses thresholds ≥70 Healthy, 40–69 Moderate, <40 Poor (FR2, NFR2)
**And** engine is pure domain — no HTTP or I/O (AD-4)
**And** `ScoringThresholds.cs` holds constants; models are `record` types (NFR6)

### Story 1.3: Rule-Based Insights and Category Comparison (TDD)

As an **integrator**,
I want rule-generated nutritionInsights including nutrient flags and category comparison,
So that I understand product strengths and weaknesses in context.

**Acceptance Criteria:**

**Given** unit tests written first
**When** engine processes products with high sugar, high saturated fat, or good protein/fiber
**Then** `ScoringEngine_HighSugar_FlagsConcernInInsights` passes
**And** `ScoringEngine_GoodProteinOrFiber_FlagsPositiveInInsights` passes
**And** `NutritionInsights_AlwaysContainsDisclaimerField` passes
**And** `nutritionInsights` shape is `{ summary, concerns, positives, disclaimer }` — all non-empty strings (FR3, AD-7)
**And** when category averages provided, summary references above/below category average (FR4)
**And** when category missing, insights still returned without comparison text (FR4)

### Story 1.4: FoodFetcher and ProductService

As an **integrator**,
I want live product and category data fetched from Open Food Facts,
So that scores and insights reflect real catalog data.

**Acceptance Criteria:**

**Given** `FoodFetcher` registered with `IHttpClientFactory` client `"OpenFoodFacts"` (NFR5)
**When** `GetProductAsync(barcode)` is called with valid/invalid barcodes
**Then** valid returns mapped `Product`; unknown returns failure via `Result` pattern (FR1, NFR4)
**And** OFF 5xx/timeout maps to failure result (not empty product)
**And** `GetCategoryAveragesAsync(category)` returns peer averages for FR4
**And** `ProductService` orchestrates fetch → score → insights without violating layer rules (AD-1, NFR3)
**And** no database or cache introduced (NFR1)

### Story 1.5: GET /products/{barcode} Endpoint

As an **integrator**,
I want a product lookup HTTP endpoint,
So that I can integrate nutrition analysis into my applications.

**Acceptance Criteria:**

**Given** integration tests written first
**When** `GET /products/3017620422003` is called on running API
**Then** `GetProduct_ValidBarcode_Returns200` passes with `nutritionScore`, `healthBand`, `nutriscoreGrade`, `nutritionInsights`
**And** `GetProduct_ResponseContainsNutritionInsights` passes
**And** `GetProduct_DoesNotIncludeAlternativesArray` passes (FR4, AD-6)
**When** `GET /products/invalid` is called
**Then** `GetProduct_InvalidBarcode_Returns404` passes with problem details (FR1)
**And** errors use Result → HTTP mapping, not bare try/catch (NFR4)

---

## Epic 2: Healthier Alternatives

Integrator can discover better Nutri-Score products in the same category with rule-generated rationale.

### Story 2.1: Alternatives Search and Ranking (TDD)

As an **integrator**,
I want alternatives ranked by better Nutri-Score with rule-based rationale,
So that I can recommend healthier products programmatically.

**Acceptance Criteria:**

**Given** `FoodFetcher.SearchAlternativesAsync` uses OFF v2 search same category, better grade (AD-3)
**When** source product has poor Nutri-Score
**Then** `GetAlternatives_PoorProduct_ReturnsBetterNutriScore` passes
**And** each alternative has strictly better `nutriscoreGrade` than source (FR5)
**And** `rationale` is rule-generated (e.g. grade comparison + sugar delta) — no LLM (AD-4)
**And** ranking logic lives in `NutritionScoringEngine.RankAlternatives` or `ProductService`

### Story 2.2: GET /products/{barcode}/alternatives Endpoint

As an **integrator**,
I want a dedicated alternatives HTTP endpoint,
So that healthier options are available without cluttering the product response.

**Acceptance Criteria:**

**Given** Epic 1 product endpoint is functional
**When** `GET /products/3017620422003/alternatives` is called
**Then** HTTP 200 with non-empty `alternatives` array containing `barcode`, `productName`, `nutriscoreGrade`, `rationale` (FR5)
**When** invalid barcode is used
**Then** HTTP 404 with problem details
**And** alternatives appear only on this route, never on `GET /products/{barcode}` (AD-6)

---

## Epic 3: Developer Experience & Verification

Reviewer can clone, run, test, and integrate the API in under 15 minutes.

### Story 3.1: README and Development OpenAPI

As a **reviewer (interviewer)**,
I want clear setup and usage documentation,
So that I can evaluate the project without guesswork.

**Acceptance Criteria:**

**Given** API implements Epics 1–2
**When** README is followed on a clean machine
**Then** `dotnet run` starts the service with no API keys (NFR12, FR6)
**And** README includes what it does, optional `OpenFoodFacts__UserAgent`, curl examples for both endpoints, sample JSON response
**And** architecture rationale covers rule-based engine, OFF v2, layered design
**And** OpenAPI mapped in Development environment (FR6)

### Story 3.2: Playwright Integration Verification

As a **builder**,
I want end-to-end HTTP verification via Playwright MCP,
So that demo behavior is proven before marking stories done.

**Acceptance Criteria:**

**Given** service running locally
**When** Playwright MCP checks execute per workflow Phase 6
**Then** `GET /products/3017620422003` → 200 + non-empty `nutritionInsights` + `disclaimer`
**And** `GET /products/invalid` → 404
**And** `GET /products/3017620422003/alternatives` → 200 + non-empty array
**And** response includes `nutritionScore` and `healthBand`

### Story 3.3: OpenAPI Export

As an **integrator**,
I want a committed OpenAPI specification,
So that clients can generate SDKs or explore the contract offline.

**Acceptance Criteria:**

**Given** all endpoints pass integration tests
**When** openspec generates API documentation
**Then** `docs/openapi.json` is created/updated (FR6, workflow Phase 7)
**And** spec reflects both product and alternatives routes with `nutritionInsights` schema
