# Architectural Audit

## Scope
This audit covers:
- `PowerGrid`
- `Metal Kegs`
- `FishSmoker Recipe`
- `Farm Terminal`

It uses the current repository plus the generated `mod-context` knowledge layer as the baseline.

## Executive Summary
The repository already has a workable ecosystem direction:
- `PowerGrid` is the strategic core because it already exposes an API and runtime telemetry.
- `Metal Kegs` is structurally sound as a machine-definition mod, but it is still too isolated.
- `FishSmoker Recipe` is intentionally small, but in a CP-heavy pack it is the most conflict-prone asset because it hard-overwrites one recipe entry.

The highest-value next step is not adding more content. It is standardizing how your mods describe machines, energy, and telemetry so external systems can consume them consistently.

## Current Strengths
- `PowerGrid` already exposes a public API at [IPowerGridApi.cs](c:\Users\darth\Projects\sdv-mod\[SMAPI] PowerGrid\Integrations\IPowerGridApi.cs:9).
- `PowerGrid` already writes machine and network telemetry into `modData`, which is the right interoperability direction.
- `Metal Kegs` uses `Data/Machines`, so it behaves like a normal machine pack instead of a custom simulation silo.
- `Metal Kegs` already supports GOF-aware fallback behavior, which is good modpack discipline.
- `FishSmoker Recipe` is small and safe in implementation scope.

## Audit Findings

### Architecture
- `PowerGrid` is still too monolithic. Asset registration, simulation, UI, commands, unlocks, and compatibility all originate from [ModEntry.cs](c:\Users\darth\Projects\sdv-mod\[SMAPI] PowerGrid\ModEntry.cs).
- `PowerGrid` hard-codes `Metal Kegs` consumer support in [GraphBuilder.cs](c:\Users\darth\Projects\sdv-mod\[SMAPI] PowerGrid\Core\GraphBuilder.cs:139) and [GraphBuilder.cs](c:\Users\darth\Projects\sdv-mod\[SMAPI] PowerGrid\Core\GraphBuilder.cs:150). That works, but it couples the energy layer to a specific machine mod.
- `Metal Kegs` also concentrates all behavior in [ModEntry.cs](c:\Users\darth\Projects\sdv-mod\[SMAPI] Metal Kegs\ModEntry.cs), including GOF detection, machine registration, recipe generation, sprite generation, and progression logic.

### Performance
- `PowerGrid` scans all loaded locations and building interiors every simulation tick in [PowerManager.cs](c:\Users\darth\Projects\sdv-mod\[SMAPI] PowerGrid\Core\PowerManager.cs:154) and [PowerManager.cs](c:\Users\darth\Projects\sdv-mod\[SMAPI] PowerGrid\Core\PowerManager.cs:165). In the current pack this is acceptable, but it will become the main scaling limit once you add more powered machine families.
- `PowerGrid` also rewrites telemetry for every tracked object each tick, then clears stale metadata later. This is useful for interoperability but expensive if the node count grows.

### UI / UX
- `PowerGrid` has a real control surface, but it is text-heavy and menu-driven rather than system-driven.
- The Power Tab only collects `Game1.locations` in [PowerTabMenu.cs](c:\Users\darth\Projects\sdv-mod\[SMAPI] PowerGrid\UI\PowerTabMenu.cs:54), while the simulator includes building interiors. That means the overview can under-report or omit shed/cellar-style networks.
- `PowerGrid` UI is still `IClickableMenu`-based. That is fine for the current scope, but it is not the right long-term base for a `Farm Terminal`.

### Interoperability
- `IPowerGridApi` is consumer-only in [IPowerGridApi.cs](c:\Users\darth\Projects\sdv-mod\[SMAPI] PowerGrid\Integrations\IPowerGridApi.cs:9). It does not model producers, storage, grid snapshots, or terminal-friendly telemetry queries.
- `FishSmoker Recipe` directly overwrites `Data/CraftingRecipes/Fish Smoker` in [content.json](c:\Users\darth\Projects\sdv-mod\[CP] FishSmoker Recipe\content.json:5) through [content.json](c:\Users\darth\Projects\sdv-mod\[CP] FishSmoker Recipe\content.json:8). In a pack with many CP edits, this is the right amount of code but not the right amount of compatibility strategy.

## Ecosystem Integration Plan

### Automate
- `Metal Kegs` already fits the Automate model because it registers normal machine entries in `Data/Machines`.
- `PowerGrid` should remain orthogonal to IO automation. The right integration is not custom Automate connectors. The right integration is a predictable speed contract: powered machines process faster, but input and output rules remain unchanged.
- Best next feature: expose a `CanPower(Object machine)` style query or machine-trait registry so power-aware overlays can describe why a machine is or is not accelerated.

### ExtraMachineConfig
- `Metal Kegs` should evolve toward EMC compatibility instead of only template cloning.
- Best fit:
  - keep current fallback cloning,
  - add explicit external machine trait metadata,
  - let EMC-altered machines register as `PowerGrid` consumers through adapters.
- Recommended feature: a compatibility layer that maps EMC machine definitions to energy demand profiles.

### Machine Terrain Framework
- MTF is the obvious path for non-standard powered production later.
- `PowerGrid` should not special-case MTF objects in the graph builder. Instead, MTF-compatible mods should register energy consumers through a shared API or adapter.
- This becomes important if you later add powered pumps, irrigation, fish systems, or terrain processors.

### Machine Control Panel
- MCP is a control-surface opportunity, not a logic dependency.
- `PowerGrid` already has the right primitive: `modData` telemetry.
- Recommended integration:
  - publish a stable telemetry schema,
  - add a snapshot query API,
  - let control mods display power state, demand, and allocation without owning power logic.

### Better Junimos
- Junimo labor and `PowerGrid` acceleration are complementary.
- The main risk is balance inflation when large Junimo-fed machine arrays are all accelerated.
- Recommended integration:
  - preserve deterministic consumer priority,
  - optionally add configurable power classes such as `artisan`, `agriculture`, `infrastructure`,
  - allow users to prioritize which categories get power first.

### StardewUI
- `StardewUI` should be the long-term UI base for `Farm Terminal`.
- `PowerGrid` should not be fully migrated immediately. Use a staged approach:
  - keep current menus,
  - build a read-only terminal dashboard first,
  - migrate conduit linking and network drill-down later.

### Generic Mod Config Menu
- `PowerGrid` already integrates with GMCM.
- `Metal Kegs` should reach config parity next. Its current config is simple enough that GMCM support is low risk and high value.
- `FishSmoker Recipe` only needs config support if you want recipe variants or pack-specific balance presets.

## Internal Architecture Improvements

### Recommended Shared Library Split
Create two shared projects:

```text
shared/
  DarthMods.API/
    Energy/
      IEnergyNode.cs
      IEnergyConsumer.cs
      IEnergyProducer.cs
      IEnergyStorage.cs
      IEnergyNetworkSnapshot.cs
    Machines/
      IMachineTelemetryProvider.cs
      IMachineTraitProvider.cs
    Terminal/
      ITerminalModule.cs
      ITerminalDataSource.cs
  DarthMods.Core/
    Energy/
      EnergyDemandProfile.cs
      EnergyAllocationPolicy.cs
      EnergyTelemetrySnapshot.cs
    Machines/
      MachineDescriptor.cs
      MachineTelemetrySnapshot.cs
    Integration/
      OptionalApiLoader.cs
      ModCompatibilityRegistry.cs
```

### Key Interface Proposal
Use small, capability-based interfaces instead of one large API:

```csharp
public interface IEnergyConsumer
{
    string QualifiedItemId { get; }
    int DemandPerTick { get; }
    float MaxSpeedupFraction { get; }
    int Priority { get; }
}

public interface IEnergyProducer
{
    string QualifiedItemId { get; }
    int MaxGenerationPerTick { get; }
}

public interface IEnergyStorage
{
    string QualifiedItemId { get; }
    int Capacity { get; }
}

public interface IMachineMonitor
{
    MachineTelemetrySnapshot? GetMachineState(string locationName, Vector2 tile);
}
```

### Why This Matters
- `PowerGrid` stops owning machine-specific knowledge.
- `Metal Kegs` can self-describe as an energy consumer instead of being hard-coded elsewhere.
- `Farm Terminal` can query stable snapshots instead of scraping multiple mods ad hoc.

## PowerGrid Expansion

### Immediate Improvements
- Invert the current `Metal Kegs` coupling:
  - remove special-casing from `GraphBuilder`,
  - let `Metal Kegs` register with `PowerGrid` through API or shared traits.
- Split `PowerGrid` into modules:
  - asset registration,
  - progression/unlocks,
  - simulation,
  - telemetry,
  - UI.
- Fix the Power Tab scope mismatch so interiors appear in the overview.

### Medium-Term Improvements
- Add real producer and storage abstractions, not just consumer registration.
- Add configurable power classes:
  - `machine`,
  - `farm infrastructure`,
  - `terminal / utility`,
  - `luxury`.
- Add network policies:
  - priority-based allocation,
  - category-based allocation,
  - reserve battery thresholds.
- Add support for external consumer registration with richer metadata:
  - display name,
  - category,
  - icon asset,
  - optional telemetry formatter.

### Long-Term Improvements
- Generator progression:
  - early: steam / wind,
  - mid: biofuel / waterwheel / solar-style passive,
  - late: iridium turbine / fusion-style fantasy generator.
- Storage progression:
  - starter batteries,
  - buffered battery banks,
  - location-level substations.
- Grid balancing:
  - brownout state instead of binary unpowered,
  - startup surge costs for heavy machines,
  - efficiency losses over long conduit hops or weak cable tiers.

### Compatibility Constraint
Any future power mechanic must stay compatible with automation mods. Do not require manual per-cycle interaction. Power should alter throughput, availability, or priority, not replace normal machine IO rules.

## Metal Kegs Improvements

### Strategic Direction
`Metal Kegs` should become a machine family, not just two cloned machines.

### Recommended Improvements
- Let `Metal Kegs` self-register as `PowerGrid` consumers instead of being recognized externally.
- Add GMCM support for:
  - unlock mode,
  - GOF fallback mode,
  - optional power demand preset when `PowerGrid` is present.
- Add a machine descriptor layer so future keg variants can share a consistent registration path.

### Should Metal Kegs Consume Energy?
Yes. That is the cleanest ecosystem fit.

The right rule is:
- base behavior remains vanilla-compatible with no hard dependency,
- when `PowerGrid` is installed, keg variants register as optional powered consumers,
- power improves throughput, not outputs.

### Should Metal Kegs Process Faster?
Yes, but via `PowerGrid`, not via standalone speed logic. Keep the acceleration in one system.

### Should They Produce Higher Quality Goods?
Not by default.

Reason:
- quality changes push the mod from infrastructure into economy rebalance,
- the pack already has multiple economy modifiers,
- speed-based identity is cleaner and easier to balance.

If you want a late-game prestige feature later, tie it to a separate advanced machine tier, not baseline power.

### Should They Integrate With EMC?
Yes, but via compatibility metadata and validation, not by rewriting the whole mod around EMC.

## FishSmoker Recipe Improvements

### Recommended Direction
Keep it small.

### Best Improvements
- Add compatibility notes or variants for CP-heavy packs.
- Add optional config if you want multiple balance presets:
  - `balanced`,
  - `hardwood-heavy`,
  - `battery-heavy`.
- Consider merging it into a broader `Darth Balance Tweaks` pack later if you plan more recipe-level changes.

### What Not To Do
- Do not convert it into a complex runtime mod unless the recipe needs dynamic conditions.
- Do not let it grow into a machine behavior mod; that belongs elsewhere.

## Farm Terminal Design

### Role
`Farm Terminal` should be a read-first orchestration mod:
- monitoring first,
- light control second,
- no direct replacement for Automate or MCP.

### Core Modules
- **Power module**: grid status, producers, consumers, storage, alerts.
- **Machine module**: active machines, idle machines, blocked machines, throughput by category.
- **Storage module**: index of important items and production buffers.
- **Crop / environment module**: forecast and readiness summaries.
- **Automation module**: monitor, not own, automation state.

### Text Architecture Diagram

```text
[Machine Mods]
  Metal Kegs
  EMC/MTF machine packs
        |
        v
[PowerGrid Runtime] <-> [DarthMods.API]
        |
        +--> telemetry snapshots
        |
        v
[Farm Terminal]
  Power
  Machines
  Storage
  Forecasts
  Alerts
        |
        v
[StardewUI]
```

### Terminal Responsibilities
- show network summaries,
- show per-location power usage,
- show top consumers,
- show battery reserves,
- show machine queues and blocked states,
- show \"needs attention\" alerts,
- show storage counts for tracked resources,
- provide quick navigation between modules.

### Terminal Non-Responsibilities
- do not own automation routing,
- do not directly rewrite machine rules,
- do not replace GMCM,
- do not become a second power simulation system.

## UI Architecture

### StardewUI Structure
Use a module-oriented UI instead of one giant screen:

```text
[SMAPI] Farm Terminal/
  UI/
    Screens/
      TerminalHomeView.sml
      PowerView.sml
      MachinesView.sml
      StorageView.sml
      ForecastView.sml
      AlertsView.sml
    ViewModels/
      TerminalViewModel.cs
      PowerViewModel.cs
      MachinesViewModel.cs
      StorageViewModel.cs
      ForecastViewModel.cs
      AlertsViewModel.cs
    Components/
      SummaryCard.sml
      MetricBar.sml
      StatusBadge.sml
      LocationList.sml
      MachineTable.sml
```

### UI Principles
- Home screen: high-level farm status only.
- Drill-down screens: one system per screen.
- Avoid freeform text walls. Use cards, tables, badges, and alerts.
- Use polling or snapshot refresh boundaries, not per-frame recomputation.

### Recommended Terminal Navigation
- `Overview`
- `Power`
- `Machines`
- `Storage`
- `Forecast`
- `Alerts`

## Sprite Needs

### PowerGrid
- Copper Cable spritesheet
- Iron Cable spritesheet
- Iridium Cable spritesheet
- Steam Generator
- Wind Generator
- Basic Battery
- Iridium Battery
- Power Conduit
- Future:
  - substation / transformer
  - battery bank
  - advanced generators

### Metal Kegs
- Metal Keg
- Hard Iridium Keg
- Future:
  - powered status overlay icon
  - optional family variants if the line expands

### Farm Terminal
- Farm Terminal cabinet
- CRT / industrial monitor variant
- compact wall terminal variant
- powered / offline indicator frames
- category icons:
  - power
  - machines
  - storage
  - crops
  - alerts

### No-Skill Sprite Workflow
1. Export vanilla template sprites from similar objects.
2. Recolor and silhouette-edit in LibreSprite or Piskel.
3. Generate higher-res concept art with AI if needed.
4. Downscale with nearest-neighbor.
5. Clean outlines manually.
6. Test in-game early instead of polishing in isolation.

This is already partly aligned with the current PowerGrid and Metal Kegs placeholder/template workflow.

## Prioritized Roadmap

### Immediate Improvements
- Add `Metal Kegs` GMCM support.
- Fix `PowerGrid` Power Tab to include building interiors.
- Extract `PowerGrid` consumer registration into adapters instead of hard-coded Metal Keg checks.
- Introduce `DarthMods.API` with the first shared energy and telemetry interfaces.
- Add a compatibility-safe snapshot layer to `PowerGrid` for future terminal use.
- Add conflict notes or optional variants to `FishSmoker Recipe`.

### Medium-Term Improvements
- Add richer `PowerGrid` API surfaces for producers, storage, and snapshot queries.
- Convert `Metal Kegs` to self-register power traits when `PowerGrid` is present.
- Add category-based and reserve-based power allocation policies.
- Build `Farm Terminal` read-only MVP with StardewUI:
  - overview,
  - power dashboard,
  - machine dashboard,
  - alerts.

### Long-Term Ecosystem Features
- advanced generator/storage progression,
- broader machine-family ecosystem beyond kegs,
- MTF/EMC-powered machine adapters,
- power-aware farm policy controls,
- terminal modules for storage indexing and forecasting,
- unified telemetry across all Darth mods.

## Proposed Folder Structures

### Shared Platform
```text
shared/
  DarthMods.API/
  DarthMods.Core/
```

### PowerGrid
```text
[SMAPI] PowerGrid/
  Assets/
  Abstractions/
  Domain/
    Energy/
    Networks/
    Telemetry/
  Application/
    Simulation/
    Allocation/
    Unlocks/
  Infrastructure/
    SaveData/
    AssetEditing/
  Integrations/
    GMCM/
    MetalKegs/
    EMC/
    MTF/
  UI/
    LegacyMenus/
    Overlays/
```

### Metal Kegs
```text
[SMAPI] Metal Kegs/
  Assets/
  Domain/
    KegVariants/
  Application/
    Registration/
    Progression/
  Integrations/
    GOF/
    PowerGrid/
    GMCM/
  Diagnostics/
```

### Farm Terminal
```text
[SMAPI] Farm Terminal/
  Assets/
  Domain/
    Snapshots/
    Alerts/
    Modules/
  Application/
    QueryServices/
    Aggregation/
    Refresh/
  Integrations/
    PowerGrid/
    Automate/
    BetterJunimos/
    ChestsAnywhere/
  UI/
    Screens/
    ViewModels/
    Components/
  TerminalModules/
    Power/
    Machines/
    Storage/
    Forecast/
    Alerts/
```

## Concrete Development Tasks

### Phase 1
- Create `DarthMods.API` project.
- Move energy-related public contracts out of `PowerGrid`.
- Add GMCM integration to `Metal Kegs`.
- Refactor `PowerGrid` hard-coded Metal Keg detection into an adapter or registration step.
- Fix `PowerTabMenu` location collection to include interiors.
- Add `PowerGrid` snapshot query service for UI consumers.

### Phase 2
- Split `PowerGrid` registrars and simulation services out of `ModEntry`.
- Create `MachineDescriptor` and `EnergyDemandProfile` models in shared code.
- Add `Metal Kegs` to the shared descriptor model.
- Add optional `FishSmoker Recipe` config variants if desired.
- Build `Farm Terminal` shell with StardewUI and a static home screen.

### Phase 3
- Implement Farm Terminal `Power` module backed by `PowerGrid` snapshots.
- Implement Farm Terminal `Machines` module with machine telemetry aggregation.
- Add terminal alert system for:
  - unpowered critical machines,
  - low battery reserve,
  - idle generator capacity,
  - blocked machine lines.
- Add external-machine registration examples for EMC/MTF consumers.

### Phase 4
- Add advanced generator tiers and storage tiers.
- Add policy-based power allocation.
- Add optional storage indexing and crop forecast modules to `Farm Terminal`.
- Add standardized icon/sprite library for the whole Darth ecosystem.

## Recommended Build Order
1. Shared API layer
2. PowerGrid refactor and API expansion
3. Metal Kegs parity and power registration
4. Farm Terminal read-only MVP
5. Advanced power/gameplay systems
