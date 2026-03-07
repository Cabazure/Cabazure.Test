# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|---------|
| API design & public surface | Mal | New method signatures, breaking change review, naming decisions |
| Core fixture implementation | Kaylee | SutFixture, AutoFixture customizations, ISpecimenBuilder implementations |
| xUnit 3 integration & tooling | Wash | DataAttribute wiring, module initializers, CI/CD, NuGet packaging |
| Test authoring & quality | Zoe | Writing/reviewing tests, edge case coverage, dogfooding verification |
| Code review & PR approval | Mal | All PRs require Mal's approval before merge |
| Issue triage | Mal | All `squad`-labeled issues → Mal triages, assigns `squad:{member}` |
| Async issue work (bugs, tests, small features) | @copilot 🤖 | 🟢 Good fit tasks per capability table in `team.md` |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, evaluate @copilot fit, assign `squad:{member}` label | Mal |
| `squad:mal` | Architecture, API design, PR review | Mal |
| `squad:kaylee` | SutFixture and customizations work | Kaylee |
| `squad:wash` | xUnit 3 wiring, tooling, packaging, CI | Wash |
| `squad:zoe` | Test authoring and quality verification | Zoe |
| `squad:copilot` | Assign to @copilot for autonomous work | @copilot 🤖 |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, **Mal** triages it — analyzing content, evaluating @copilot's capability profile, assigning the right `squad:{member}` label, and commenting with triage notes.
2. **@copilot evaluation:** Mal checks the capability table in `team.md`. 🟢 good-fit → `squad:copilot`. 🟡 needs review → assign with review flag. 🔴 not suitable → assign to appropriate member.
3. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
4. Members can reassign by removing their label and adding another.

### Mal's Triage Guidance for @copilot

1. **Well-defined + follows patterns?** (e.g., add a test, fix a known bug) → 🟢
2. **Medium complexity with spec?** (e.g., new customization with examples) → 🟡
3. **Design judgment required?** (new API, architectural change) → 🔴 route to Mal
4. **Affects public NuGet API?** → always 🔴

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If Kaylee implements a feature, spawn Zoe to write test cases simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied, route to that member. The `squad` base label → Mal triages.
8. **@copilot routing** — check capability profile in `team.md`. Route 🟢 tasks to `squad:copilot`, flag 🟡 for PR review, keep 🔴 with squad members.
