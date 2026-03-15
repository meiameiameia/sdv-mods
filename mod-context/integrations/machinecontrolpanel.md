# Machine Control Panel Integration Target

## What It Does
`Machine Control Panel` provides machine rule control/inspection UI and management workflows.

## Why Integration Is Useful
- In a machine-dense pack, a central control plane reduces user friction.
- PowerGrid introduces runtime machine state (powered, allocated EU, speedup) that could enrich control views.

## Relevant Systems of Yours
- **PowerGrid**: primary candidate; it writes per-object `darth.PowerGrid/*` modData and exposes API queries.
- **Metal Kegs**: secondary candidate via machine identity and rule visibility.

## Current State
- MCP is loaded (`mushymato.MachineControlPanel` 2.1.1).
- No direct MCP integration calls in your current mods.

## High-Value Next Integration Steps
- Document PowerGrid modData keys as a stable contract for external UI/control mods.
- Add compatibility checks so powered machine states remain coherent in MCP-managed workflows.
- Consider a thin bridge for displaying PowerGrid power metrics in control panels.
