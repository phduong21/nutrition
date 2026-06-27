# Validation Delta — v2 → v3

How **PRD edits** (between `v2/prd-snapshot.md` and `v3/prd-snapshot.md`) caused **validation outcome** changes after **removing AI/LLM**.

## Grade summary

| | v2 (AI/SK) | v3 (rule-based) |
|---|------------|-----------------|
| **Grade** | Excellent | **Excellent** |
| **Build-ready** | Yes | **Yes** |
| Critical | 0 | 0 |
| High | 0 | 0 |
| Medium | 0 | **2** (new) |
| Low | 5 | 4 |

Grade holds at **Excellent** — new mediums are threshold detail gaps, not decision blockers. v2 low on Nutri-Score case sensitivity dropped (FR-2 rewritten). v2 lows on field types / demo barcode persist.

## Dimension shifts

| Dimension | v2 | v3 | Driver |
|-----------|----|----|--------|
| Decision-readiness | adequate | adequate | LLM removed; no new OQs |
| Done-ness clarity | adequate | adequate | New FR logic; thresholds deferred to arch |
| Strategic coherence | strong | strong | Thesis pivot consistent |
| Scope honesty | strong | strong | LLM explicitly in Non-Goals |
| Others | unchanged | unchanged | — |

---

## Change → finding → outcome

### 1. Remove Semantic Kernel / OpenAI

**PRD change**

| v2 | v3 |
|----|-----|
| SK agent + `OPENAI_API_KEY` | Nutrition Scoring Engine, no API keys |
| AI-first engineering thesis | Test-driven + deterministic rules |

**Outcome:** Removes cost/API-key dependency; SM-2 simplified. No validation regression.

---

### 2. `aiInsights` → `nutritionInsights`

**PRD change:** Renamed field; glossary + UJ-1 + FR-3 + tests updated.

**Outcome:** Downstream docs (architecture, workflow, epics) must use new name — aligned in repo.

---

### 3. FR-2: Nutri-Score map → calculated nutrition score

**PRD change**

| v2 | v3 |
|----|-----|
| Map `nutriscoreGrade` → `healthBand` | Compute `nutritionScore` (0–100) from nutriments → `healthBand` |
| Response: `healthBand` only | Response: `nutritionScore` + `healthBand` |

**New v3 medium:** Numeric band cutoffs not in PRD (in ARCHITECTURE only).

---

### 4. FR-4: Agent tool → category comparison

**PRD change**

| v2 | v3 |
|----|-----|
| Auto-invoke `RecommendAlternatives` tool on poor scores | Compare nutriments to category averages in `nutritionInsights` |
| Tool observable in tests | Category reference in summary when category present |

**Outcome:** AD-6 unchanged — still no inline `alternatives` on product response.

---

### 5. FR-3: Agent insights → rule-based flags

**PRD change:** Insights from engine rules (high sugar, sat fat, protein, fiber) not LLM.

**New v3 medium:** Per-100g threshold values not in PRD text.

---

## Folder layout (updated)

```
prd-Nutrition-2026-06-27/
├── prd.md                         ← live (v3)
├── validation-delta.md            ← v1 → v2
├── validation-delta-v2-v3.md      ← this file
├── v1/  … pre-fix AI draft
├── v2/  … post-fix AI (SK + OpenAI)
└── v3/  … rule-based engine (current)
```
