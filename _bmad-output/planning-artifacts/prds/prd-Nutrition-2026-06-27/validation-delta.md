# Validation Delta — v1 → v2

How **PRD edits** (between `v1/prd-snapshot.md` and `v2/prd-snapshot.md`) caused **validation outcome** changes.

## Grade summary

| | v1 | v2 |
|---|----|----|
| **Grade** | Good | **Excellent** |
| **Build-ready** | No | **Yes** |
| Critical | 0 | 0 |
| High | 1 | 0 |
| Medium | 4 | 0 |
| Low | 4 | 5 |

v2 has one additional low finding (`concerns`/`positives` field types) surfaced because FR-3 is now specific enough to review field cardinality. Net severity dropped sharply.

## Dimension shifts

| Dimension | v1 | v2 | Driver |
|-----------|----|----|--------|
| Decision-readiness | **thin** | **adequate** | 3 OQs closed; FR-4/FR-5 decoupled; OFF v2 + OpenAI decided |
| Done-ness clarity | adequate | adequate | Medium gaps fixed; minor lows remain |
| Scope honesty | strong | strong | Assumptions → decisions; Azure in Non-Goals |
| Others | unchanged | unchanged | — |

---

## Change → finding → outcome

### 1. FR-4 vs FR-5 decoupled

**PRD change (v1 → v2)**

| v1 | v2 |
|----|-----|
| FR-4: agent auto-invokes `RecommendAlternatives` on poor scores; unclear if response includes alternatives | FR-4: tool invocation only; explicit consequence: `GET /products` does **not** include `alternatives` |
| FR-5: separate endpoint only | FR-5: **only** surface that returns alternatives payload |

**v1 finding:** **High** — FR-4 vs FR-5 response contract unclear

**v2 result:** Finding **closed**. Decision-readiness no longer blocked.

---

### 2. LLM provider locked to OpenAI

**PRD change**

| v1 | v2 |
|----|-----|
| `[ASSUMPTION: OpenAI or Azure OpenAI]` | OpenAI via `OPENAI_API_KEY`; Azure in §5 Non-Goals |
| OQ-1 open | OQ-1 removed; §8 empty |

**v1 finding:** **Medium** — LLM provider undecided

**v2 result:** Finding **closed**. Architect can wire Semantic Kernel without fork.

---

### 3. Open Food Facts v2 committed

**PRD change**

| v1 | v2 |
|----|-----|
| v2 as `[ASSUMPTION]` inline | v2 as explicit MVP decision in §4.1, §4.3, §6.1 |
| OQ-3 open | OQ-3 removed |
| No migration note | §4.1 Notes: v3 migration risk documented |

**v1 finding:** **Medium** — OFF API version unresolved

**v2 result:** Finding **closed**. Trade-off named (v2 deprecated but has search).

---

### 4. `aiInsights` schema locked

**PRD change**

| v1 | v2 |
|----|-----|
| "string or structured object with summary, concerns, positives" | `{ summary, concerns, positives, disclaimer }` in Glossary + FR-3 |
| OQ-2 open | OQ-2 removed |

**v1 finding:** **Medium** — schema undecided

**v2 result:** Finding **closed**. OpenAPI and integration tests can target stable shape.

**New v2 low:** field types (string vs array) for `concerns`/`positives` — only visible once schema is locked.

---

### 5. Disclaimer made testable

**PRD change**

| v1 | v2 |
|----|-----|
| Disclaimer in FR-4 **Notes** only | FR-3 **Consequences**: non-empty `disclaimer`, informational-only language |

**v1 finding:** **Medium** — disclaimer not testable

**v2 result:** Finding **closed**. TDD can assert disclaimer presence.

---

## Unchanged low findings (carried v1 → v2)

These were **not** addressed by the fix pass; they do not affect grade at hobby stakes:

- Interview CRM context (§0, §1)
- Nutri-Score case sensitivity (FR-2)
- Test-to-FR mapping (SM-1)
- Second demo barcode role (§6.1)

---

## Folder layout

```
prd-Nutrition-2026-06-27/
├── prd.md                 ← live PRD (matches v2 snapshot)
├── validation-delta.md    ← this file
├── v1/                    ← first validation run (pre-fix PRD)
│   ├── prd-snapshot.md
│   ├── review-rubric.md
│   ├── validation-report.md
│   └── validation-report.html
└── v2/                    ← second validation run (post-fix PRD)
    ├── prd-snapshot.md
    ├── review-rubric.md
    ├── validation-report.md
    └── validation-report.html
```
