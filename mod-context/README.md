# Mod Context

This repository uses a lightweight Context Mesh-inspired structure.

The goal is not framework adoption. The goal is to make long-term AI guidance more stable by giving future agents a small set of reliable entry points for:

- roadmap
- architecture
- decisions
- testing
- integration routing

`mod-context` remains the main context area.

## Context Map

### Roadmap
- `roadmap.md`

### Architecture and Ecosystem
- `architecture.md`
- `architectural-audit.md`
- `art/`
- `versioning.md`
- `modpack-overview.md`
- `mod-list.md`
- `api-surface.md`
- `frameworks.md`
- `my-mods/`
- `systems/`
- `integrations/`

### Decisions
- `decisions/`

### Testing
- `testing/sandbox-profiles.md`
- `testing/regression-checklist.md`

## How To Use This Folder

For most work, future agents should read in this order:

1. `roadmap.md`
2. `architecture.md`
3. `architectural-audit.md`
4. the relevant file in `my-mods/`
5. the relevant file in `integrations/` or `systems/`
6. the relevant file in `decisions/`
7. the relevant file in `testing/`

## Lightweight Conventions

- Keep existing ecosystem docs in place.
- Add a decision note only when an implementation or ownership boundary changes.
- Add or update testing docs only when the sandbox workflow or regression expectations change.
- Prefer extending existing files over creating new folders unless the new file clearly improves routing.
