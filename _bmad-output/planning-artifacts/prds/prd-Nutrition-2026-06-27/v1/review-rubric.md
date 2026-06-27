# PRD Quality Review — Nutrition Intelligence Agent

## Overall verdict

This is a well-scoped hobby/interview PRD with a clear thesis (AI-first backend demo), honest non-goals, and mostly testable FRs. It is **not yet build-ready** because three Open Questions and one FR contract gap leave implementers guessing on response shapes and external API version. For a 1–2 day assignment the lean shape is appropriate; resolving §8 items and clarifying FR-4 vs FR-5 would move this to strong decision-readiness without bloating the doc.

## Decision-readiness — thin

Open Questions §8.1–8.3 are genuinely unresolved and block architecture choices (LLM provider, `aiInsights` schema, OFF API version). Assumptions are tagged and indexed well, but the PRD still asks the implementer to decide at code time. FR-4 ("auto-invoke `RecommendAlternatives`") vs FR-5 (separate `/alternatives` endpoint) creates ambiguity about what the main `GET /products/{barcode}` response contains when score is poor.

### Findings
- **high** FR-4 vs FR-5 response contract unclear (§4.2 FR-4, §4.3 FR-5) — FR-4 requires the agent to call `RecommendAlternatives` on the main flow for D/E products, but FR-5 defines a separate endpoint for alternatives. Unclear whether poor-score products return `alternatives` inline on `GET /products`, only on `/alternatives`, or both. *Fix:* Add one sentence to FR-4 or FR-5 stating whether inline `alternatives` appear on the product response, or explicitly decouple ("FR-4 validates agent tool invocation only; alternatives payload is FR-5 only").
- **medium** LLM provider undecided (§8 OQ-1, §4.2 assumption) — OpenAI vs Azure OpenAI affects Semantic Kernel wiring and env var names reviewers will look for. *Fix:* Pick one default for MVP and move the other to out-of-scope or v2.
- **medium** OFF API version unresolved (§8 OQ-3, §4.1 assumption) — v2 is deprecated upstream; v3 lacks structured search needed for FR-5. *Fix:* State explicit decision: "MVP uses v2 read + v2 search; document migration risk in addendum" OR narrow FR-5 to category-less fallback.

## Substance over theater — strong

Content is earned for the stated stakes. Three JTBD roles map to real actors (integrator, builder, reviewer) without persona bloat. Vision is specific to Semantic Kernel tool loops and interview demo—not generic "health app" copy. No scalability/security NFR boilerplate. Success metrics tie to tests and reviewer setup time, not vanity DAU.

### Findings
- **low** Interview context name-drop (§0, §1) — "Nordic B2B SaaS (non-profit CRM)" frames audience but does not affect requirements; harmless at this length. *Fix:* Optional one-line note that CRM domain is portfolio context only, not integration scope.

## Strategic coherence — strong

Thesis is explicit: demonstrate AI-first discipline with real OFF data and agent tool use under time pressure. Features chain logically (fetch → normalize → analyze → alternatives). MVP in/out mirrors non-goals. SM-1/SM-2 validate the thesis; SM-C1 correctly deprioritizes latency for demo.

### Findings
- *(none)*

## Done-ness clarity — adequate

Most FRs have concrete, testable consequences aligned with `workflow.mdc` test names. Gaps remain where response contracts are fuzzy (`aiInsights` shape) or behavioral requirements lack observable acceptance (disclaimer, FR-4 output).

### Findings
- **medium** `aiInsights` schema undecided (§4.2 FR-3, §8 OQ-2) — FR-3 allows "string or structured object," which prevents stable OpenAPI and integration tests. *Fix:* Lock MVP to one shape (recommend structured: `{ summary, concerns, positives, disclaimer }`).
- **medium** Disclaimer not testable (§4.2 Notes) — "should include a disclaimer" is in Notes, not Consequences. *Fix:* Add consequence: "`aiInsights` includes non-empty `disclaimer` field with informational-only language."
- **low** Nutri-Score case sensitivity unspecified (§3 Glossary `a`–`e`, §4.1 FR-2) — FR-2 examples use lowercase `a`/`d`; OFF may return mixed case. *Fix:* One line in FR-2: normalization is case-insensitive.

## Scope honesty — strong

Non-goals §5 are explicit and load-bearing. Seven assumptions indexed in §9 with roundtrip to inline tags. MVP out-of-scope items include reasons (cache, i18n, k8s). Open question count is appropriate for hobby stakes—not a green-light blocker once acknowledged.

### Findings
- *(none)*

## Downstream usability — adequate

Glossary terms used consistently. FR-1–6 and UJ-1 IDs are contiguous. UJ-1 has named protagonist (Alex). `workflow.mdc` mandatory tests are referenced in SM-1 but not mapped to FR IDs—story creation will need a manual bridge.

### Findings
- **low** Test-to-FR traceability gap (§7 SM-1 vs §4) — workflow lists six named tests; PRD does not map them to FR-1–6. *Fix:* Add a short table in §4.4 or §7: test name → FR(s).
- **low** Second demo barcode underused (§6.1) — `5000159407236` listed in scope but only `3017620422003` appears in UJ-1 and FR examples. *Fix:* Note expected Nutri-Score/behavior for second barcode or drop from scope.

## Shape fit — strong

Lean ~2-page hobby PRD with single-sentence UJ is correct for backend API + interview demo. No over-formalized persona sections. Developer-product concerns (endpoints, OpenAPI, env vars) appropriately emphasized without enterprise bloat.

### Findings
- *(none)*

## Mechanical notes

- Assumptions Index §9 roundtrips all seven inline `[ASSUMPTION]` tags — complete.
- Glossary uses lowercase Nutri-Score grades; FR-2 consequences use lowercase — consistent.
- No `addendum.md` present — acceptable for this scope.
- PRD `status: draft` — validation is pre-finalize; expected.
