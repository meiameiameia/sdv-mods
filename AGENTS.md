# AGENTS

This repository uses `mod-context/` as the main long-term context area.

Future AI agents should treat this file as the router, not as a second documentation system.

## Start Here

Read these files first:

1. `mod-context/README.md`
2. `mod-context/roadmap.md`
3. `mod-context/architecture.md`
4. `mod-context/architectural-audit.md`

## Task Routing

### If the task is about ecosystem fit or modpack compatibility
Read:

1. `mod-context/modpack-overview.md`
2. `mod-context/mod-list.md`
3. `mod-context/api-surface.md`
4. `mod-context/frameworks.md`
5. the relevant file under `mod-context/integrations/`
6. the relevant file under `mod-context/systems/`

### If the task is about one of this repo's mods
Read:

1. the relevant file under `mod-context/my-mods/`
2. `mod-context/architecture.md`
3. the relevant file under `mod-context/decisions/`

### If the task changes ownership, shared APIs, or architectural boundaries
Read:

1. `mod-context/decisions/0001-lightweight-context-structure.md`
2. `mod-context/decisions/0002-powergrid-metal-kegs-ownership.md`

Then add or update a decision file if the change materially alters architecture.

### If the task is about testing or validation
Read:

1. `mod-context/testing/sandbox-profiles.md`
2. `mod-context/testing/regression-checklist.md`

Use the sandbox progression:

1. `Baseline`
2. `Integrations`
3. `Stress`

## Working Rules

- Preserve the existing `mod-context` structure unless a new file clearly improves routing.
- Prefer lightweight additions over new frameworks.
- Do not create process-heavy documentation with no operational value.
- Keep architecture decisions and testing knowledge current when implementation behavior changes.
