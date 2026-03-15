# Chests Anywhere Integration Target

## What It Does
`Chests Anywhere` provides remote chest access and chest organization from anywhere.

## Why Integration Is Useful
- It changes how players feed machine lines and move materials through production zones.
- In automation-heavy farms, it effectively becomes a logistics layer paired with Automate.

## Relevant Systems of Yours
- **PowerGrid**: indirectly benefits because faster machines are easier to keep supplied when logistics friction is low.
- **Metal Kegs**: chest-fed keg chains become easier to sustain.
- **FishSmoker Recipe**: material acquisition and staging become easier in late-game workflows.

## Current State
- API is present in runtime (`Pathoschild.Stardew.ChestsAnywhere.Framework.ChestsAnywhereApi`).
- Your current mods do not call this API directly.

## High-Value Next Integration Steps
- Optional QoL bridge: show power/machine context while handling remote logistics workflows.
- Validate no edge-case desync between remote chest refills and power-accelerated processing ticks.
- Keep this as optional soft integration to avoid hard dependency coupling.
