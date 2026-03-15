# Decision 0001: Lightweight Context Structure

## Status
Accepted

## Decision

Keep `mod-context` as the central repository knowledge area and add only a minimal routing layer inspired by Context Mesh:

- `README.md` as the context index
- `intent/` for current direction
- `decisions/` for architectural continuity
- `testing/` for sandbox and regression knowledge
- root `AGENTS.md` as the repo router

## Why

The repository already had strong ecosystem and integration documentation. A full framework migration would add process overhead without adding much value.

The missing pieces were:

- a clear starting point for future agents
- durable architectural decision tracking
- persistent testing workflow guidance

## Consequences

- Existing docs remain valid and stay where they are.
- Future AI agents get stable entry points without repo churn.
- Context remains practical instead of bureaucratic.
