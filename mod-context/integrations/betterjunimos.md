# Better Junimos Integration Target

## What It Does
`Better Junimos` extends Junimo Hut automation (planting, fertilizing, and additional farm tasks).

## Why Integration Is Useful
- Junimo-fed machine loops are common in large farm automation setups.
- PowerGrid and Better Junimos together can raise full-farm throughput (labor automation + machine acceleration).

## Relevant Systems of Yours
- **PowerGrid**: should remain robust when machine inputs are supplied by Junimo workflows.
- **Metal Kegs**: behaves as standard machines, so Junimo-supported farm loops can feed them normally.

## Current State
- Better Junimos API is exposed in runtime (`BetterJunimos.BetterJunimosApi`).
- No direct Better Junimos API usage in your current code.

## High-Value Next Integration Steps
- Regression-test powered machine acceleration in Junimo-driven farm days.
- Evaluate optional priority policy: keep power allocation deterministic when many Junimo-fed machines run concurrently.
- Keep integration soft (runtime API detection only) to avoid hard dependency coupling.
