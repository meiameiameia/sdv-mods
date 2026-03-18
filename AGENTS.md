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

### If the task changes a deployable mod, packaging flow, or release artifacts
Read:

1. `mod-context/versioning.md`
2. `scripts/release-mod.ps1`

Use the sandbox progression:

1. `Baseline`
2. `Integrations`
3. `Stress`

## Working Rules

- Preserve the existing `mod-context` structure unless a new file clearly improves routing.
- Prefer lightweight additions over new frameworks.
- Do not create process-heavy documentation with no operational value.
- Keep architecture decisions and testing knowledge current when implementation behavior changes.

## Role Discipline

- Keep architecture/review work and execution work separate unless the user explicitly asks to collapse them.
- Architecture/review role:
  - do not edit deployable mod code or assets,
  - do not build/package mods,
  - instead hand off exact implementation scope, validation target, and expected release output to the executor.
- Executor role:
  - owns file changes, build validation, packaging, and exact artifact handoff,
  - must not stop after code edits if the task changed shipped mod behavior/assets/config/UI/runtime output.
- If a role boundary was crossed accidentally, say so plainly, stop drifting further, and route the remaining work back to the correct role.

## Release Discipline

- Any change that affects a deployable mod's shipped behavior or shipped assets must end with the normal release helper flow unless the user explicitly says not to package.
- Use `scripts/release-mod.ps1` as the authoritative release path for deployable mods.
- Do not treat a mod-changing task as complete if the working tree changed but no new installable zip was produced.
- Default expectation for shipped mod changes:
  - bump the changed mod with the release helper,
  - sync manifest/csproj versions through the script,
  - build through the script's normal path,
  - produce a new zip in `artifacts/mod-zips/`.
- If a mod-changing pass intentionally does not create a new zip, the reason must be explicit in the handoff and must come from user direction, not agent omission.

## Zip Retention Discipline

- After packaging, verify that `artifacts/mod-zips/` contains only the current and immediate previous version per mod.
- Older zips must be archived outside the watched folder in the sibling archive folder (default: `artifacts/mod-zips-archive/`).
- If retention/archive behavior did not occur, treat that as an issue to report, not as a silent success.
- Handoffs for packaged work must name:
  - the exact new zip path,
  - the active retained versions in `artifacts/mod-zips/` when relevant,
  - and whether older zips were archived.

## Handoff Discipline

- Executor handoffs for deployable mod changes must include:
  - exact files changed,
  - build result,
  - exact packaged zip path,
  - version metadata result,
  - what still needs manual runtime validation.
- Do not report a mod pass as "done" when it is only code-complete but not yet packaged, unless the user explicitly asked for code-only work.

## Recovery Discipline

- If a runtime validation pass shows a packaged version is a regression, do not keep iterating on top of that failed build by assumption.
- Identify the last known good behavior/build and recover from there deliberately.
- Prefer reusing or mirroring existing vanilla/runtime behavior over inventing approximations when the game already implements the desired behavior correctly.
