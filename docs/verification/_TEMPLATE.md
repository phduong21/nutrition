# Story X.Y — Verification Log

**Story:** _title_  
**AC source:** [`epics.md`](../../_bmad-output/planning-artifacts/epics.md) (Story X.Y)

## Metadata

| Field | Value |
| --- | --- |
| Implement agent / model | |
| Implement date | |
| Baseline commit | `git rev-parse HEAD` at start |
| Done commit | `git rev-parse HEAD` at end |

---

## Phase 3–4 — TDD / Implement

| Check | Status | Evidence |
| --- | --- | --- |
| Tests RED then GREEN | ⬜ | |
| `dotnet test` green | ⬜ | paste output |
| AC from epics satisfied | ⬜ | |

---

## Phase 5 — Code review

| Check | Status | Reviewer | Date | Notes |
| --- | --- | --- | --- | --- |
| Tests XANH | ⬜ | | | |
| Không bare try/catch | ⬜ | | | |
| Không hardcode OFF response | ⬜ | | | |
| Scoring deterministic | ⬜ / N/A | | | |
| `nutritionInsights` disclaimer | ⬜ / N/A | | | |
| GET product không có `alternatives` | ⬜ / N/A | | | |

### Review session record

| Field | Value |
| --- | --- |
| Review requested | ⬜ |
| Reviewer model | _must differ from implementer_ |
| Review skill | `/bmad-code-review` |
| Outcome | Approve / Changes requested |
| Action items fixed | ⬜ |

---

## Phase 6 — Playwright MCP

_Set N/A if no HTTP endpoints in this story._

| Check | Status | Evidence |
| --- | --- | --- |
| GET /products/3017620422003 → 200 + insights | ⬜ / N/A | |
| GET /products/invalid → 404 | ⬜ / N/A | |
| GET /products/3017620422003/alternatives → 200 | ⬜ / N/A | |
| nutritionInsights.disclaimer non-empty | ⬜ / N/A | |
| nutritionScore + healthBand present | ⬜ / N/A | |

**Playwright run:**

```text
(paste MCP output or curl results)
```

---

## Story done gate

- [ ] Required phases complete for this story
- [ ] [`STORIES.md`](../STORIES.md) checkbox updated
- [ ] Row added/updated in [`VERIFICATION.md`](../VERIFICATION.md)
