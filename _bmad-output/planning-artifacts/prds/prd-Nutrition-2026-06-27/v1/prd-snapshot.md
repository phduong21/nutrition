---
title: Nutrition Intelligence Agent
status: draft
created: 2026-06-27
updated: 2026-06-27
snapshot: v1-pre-fix
note: Frozen PRD as validated in v1 review (before 2026-06-27 fix pass)
---

# PRD: Nutrition Intelligence Agent

## 0. Document Purpose

Lean requirements for a **1–2 day portfolio assignment** demonstrating AI-first backend development. Audience: implementer (you) and technical interview reviewers at a Nordic B2B SaaS company. Structured for downstream `bmad-architecture`, `STORIES.md`, and TDD. Assumptions tagged inline and indexed in §9.

**Inputs:** `.cursor/rules/workflow.mdc` (implementation workflow and acceptance tests).

## 1. Vision

**Nutrition Intelligence Agent** is a C# .NET minimal-API backend that looks up packaged food products by barcode via the public **Open Food Facts** API, normalizes nutrition signals (Nutri-Score, key nutriments, ingredients), and uses a **Semantic Kernel** agent to produce plain-language nutrition insights and—when appropriate—healthier product alternatives.

The product exists to show **AI-first engineering discipline**: real external data, tests written before code, and an agent that **uses tools in a loop** (not a single prompt-and-return). It is a backend-only service consumed by other developers or services; there is no UI.

For the interview context, success means a reviewer can clone, run, hit documented endpoints, and see credible agent behavior on known barcodes within minutes—not production-scale coverage or clinical accuracy.

## 2. Target User

### 2.1 Jobs To Be Done

- **Integrator (developer):** Submit a product barcode and receive structured nutrition data plus AI-generated insights without building Open Food Facts parsing or LLM orchestration myself.
- **Builder (you):** Prove I can ship a small, test-driven, agentic API under time pressure with clear docs and observable tool use.
- **Reviewer (interviewer):** Quickly assess code quality, testing habits, API design, and whether the AI layer adds real value vs. a thin wrapper.

### 2.2 Non-Users (v1)

- End consumers scanning products in a mobile app (no client UI in scope).
- Nutritionists or patients relying on this for medical or dietary treatment decisions.

### 2.3 Key User Journeys

- **UJ-1.** Alex, a backend developer evaluating the repo, runs the service locally, calls `GET /products/3017620422003`, receives Nutri-Score, normalized health band, and `aiInsights`, then calls `/alternatives` when the score is poor and gets a ranked list with brief rationale.

## 3. Glossary

- **Barcode** — EAN/UPC product identifier passed by the client (e.g. `3017620422003`).
- **Product** — Food item record returned from Open Food Facts, including name, brand, nutriments, Nutri-Score grade, and ingredients text.
- **Nutri-Score grade** — Open Food Facts letter grade `a`–`e` for overall nutritional quality.
- **Health band** — Normalized enum derived from Nutri-Score: `Healthy` (A/B), `Moderate` (C), `Poor` (D/E).
- **AI insights** — Agent-generated summary of nutrition strengths, concerns, and ingredient flags for a Product.
- **Alternative** — Another Product in the same category with a better Nutri-Score, selected and explained by the agent.
- **Agent** — Semantic Kernel orchestrator that invokes registered tools (`AnalyzeNutritionScore`, `RecommendAlternatives`) in a multi-step loop until a final answer is produced.

## 4. Features

### 4.1 Product Lookup & Normalization

**Description:** Client supplies a barcode; the service fetches live product data from Open Food Facts, maps it to internal DTOs, and derives a Health band. Invalid or unknown barcodes return a clear HTTP error. Realizes UJ-1. `[ASSUMPTION: Open Food Facts API v2 for product read—GET /api/v2/product/{barcode}.json—matching existing workflow verification URLs.]`

#### FR-1: Get product by barcode

Integrator can retrieve a Product by barcode. Realizes UJ-1.

**Consequences (testable):**
- Valid barcode (e.g. `3017620422003`) → HTTP 200 with `productName`, `brands`, `nutriments`, `nutriscoreGrade`, `ingredientsText`, `healthBand`.
- Unknown or malformed barcode → HTTP 404 with problem-details-style error body.
- Open Food Facts timeout or 5xx → HTTP 502 with message indicating upstream failure (no silent empty product).

#### FR-2: Normalize Nutri-Score to health band

System maps `nutriscoreGrade` to `healthBand`: A/B → `Healthy`, C → `Moderate`, D/E → `Poor`; missing grade → `Unknown`.

**Consequences (testable):**
- Grade `a` → `Healthy`; grade `d` → `Poor` (unit tests on normalizer, no HTTP).

**Out of Scope:** Caching product data across requests (v2).

### 4.2 AI Nutrition Analysis

**Description:** After Product fetch, the Agent analyzes nutrition using the `AnalyzeNutritionScore` tool and returns `aiInsights` on the product response. The Agent must run a **tool-use loop** (plan → tool call → observe → synthesize), not a single completion. Realizes UJ-1. `[ASSUMPTION: LLM provider is OpenAI or Azure OpenAI, configured via environment variables.]`

#### FR-3: AI insights on product response

Integrator receives `aiInsights` (string or structured object with `summary`, `concerns`, `positives`) on successful product lookup.

**Consequences (testable):**
- Response for valid barcode includes non-empty `aiInsights`.
- Integration test confirms agent invoked `AnalyzeNutritionScore` (observable via logs or test double).
- Insights reference actual product fields (name, grade, or ingredients)—not generic placeholder text.

#### FR-4: Auto-recommend alternatives for poor products

When `healthBand` is `Poor`, the Agent automatically invokes `RecommendAlternatives` during the same agent session (no separate client trigger required on the main product endpoint).

**Consequences (testable):**
- Product with Nutri-Score D/E → agent calls `RecommendAlternatives` (unit/integration test with mock kernel).
- Product with A/B → `RecommendAlternatives` not required on main flow.

**Notes:** Nutrition output is **informational only**; responses should include a disclaimer that this is not medical advice.

### 4.3 Healthier Alternatives Endpoint

**Description:** Dedicated endpoint returns a list of Alternative products for a given barcode, using Open Food Facts search (same category, better Nutri-Score) refined by the Agent. Realizes UJ-1. `[ASSUMPTION: v2 structured search (/api/v2/search) for alternatives until v3 search is available.]`

#### FR-5: List healthier alternatives

Integrator can call `GET /products/{barcode}/alternatives` and receive ranked alternatives with `barcode`, `productName`, `nutriscoreGrade`, and `rationale`.

**Consequences (testable):**
- Valid barcode with known poor product → HTTP 200 with non-empty `alternatives` array.
- Each alternative has Nutri-Score strictly better than source (e.g. source D → alternatives A/B/C only).
- Invalid barcode → HTTP 404.

### 4.4 API Surface & Developer Experience

**Description:** Minimal API with OpenAPI document, README, and sample requests so a reviewer can run without guesswork. `[ASSUMPTION: No API authentication in v1; service runs on localhost or single deploy target.]`

#### FR-6: Documented HTTP API

Service exposes OpenAPI (development) and README with setup, env vars, and curl examples for both endpoints.

**Consequences (testable):**
- README documents required env vars (LLM key, optional Open Food Facts user-agent).
- Playwright or HTTP integration tests cover: valid product 200 + `aiInsights`, invalid 404, alternatives list present for demo barcode.

## 5. Non-Goals (Explicit)

- Mobile or web UI, barcode scanning, user accounts.
- Production multi-tenant auth, rate limiting, or billing.
- Medical-grade dietary prescriptions or allergen guarantees.
- Writing or mutating Open Food Facts data.
- Full global product catalog completeness or offline mode.

## 6. MVP Scope

### 6.1 In Scope

- .NET minimal API (`NutritionAgent` project).
- Open Food Facts product fetch + Nutri-Score normalization.
- Semantic Kernel agent with `AnalyzeNutritionScore` and `RecommendAlternatives` tools.
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
- **SM-2:** Reviewer can follow README and get a successful `aiInsights` response in under 15 minutes on a clean machine. Validates FR-6.

**Counter-metrics (do not optimize)**
- **SM-C1:** Response latency — demo may be slow; do not sacrifice test quality or real API/agent behavior for sub-second p99.

## 8. Open Questions

1. Preferred LLM: OpenAI vs Azure OpenAI for Semantic Kernel? (Currently assumed either via env.)
2. Should `aiInsights` be a plain string or structured JSON for downstream services?
3. Open Food Facts v2 vs v3 for product read—v3 recommended upstream but workflow verifies v2; confirm for implementation.

## 9. Assumptions Index

- §4.1 — Open Food Facts API **v2** for product lookup.
- §4.2 — LLM via **OpenAI or Azure OpenAI** from environment config.
- §4.3 — **v2 search** for alternatives discovery.
- §4.4 — **No authentication** on API for v1 demo.
- §6.2 — **English-only** agent output.
- §6.2 — **Local/single-instance** deployment; no production SLA.
- §4.2 — Insights are **informational, not medical advice**.
