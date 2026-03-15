# UniqueID Migration Smoke

This is a narrow smoke checklist for the `meiameiameia.*` UniqueID migration.

Use it before any broader runtime validation work.

## Scope

Validate only:

- startup / no-red-errors behavior,
- old sandbox save continuity for `PowerGrid`,
- `Metal Kegs` re-registration against the new `PowerGrid` ID,
- old/new mod coexistence risk during zip intake.

This is not the Phase 2 query-validation pass and it is not a `Farm Terminal` gate by itself.

## Preconditions

1. Start with `SDV Sandbox Baseline`.
2. Install the repo-owned mods from `artifacts/mod-zips/`.
3. Use only the highest-version zip for each repo-owned mod.
4. Remove any older installed copies of:
   - `[SMAPI] PowerGrid`
   - `[SMAPI] Metal Kegs`
   - `[CP] FishSmoker Recipe`
5. Do not allow old and new repo-owned packages to coexist in the same `Mods` folder or mod-manager intake set.

Why this matters:

- the new manifest IDs are `meiameiameia.*`,
- older installs may still exist under previous folders or cached zip selections,
- a mod manager can treat old and new IDs as different mods and load both.

## Expected Identity Split

These are expected and should not be treated as failures:

- manifest UniqueIDs are now `meiameiameia.PowerGrid`, `meiameiameia.MetalKegs`, and `meiameiameia.FishSmokerRecipe`,
- item IDs remain `darth.PowerGrid_*` and `darth.MetalKegs_*`,
- `PowerGrid` object `modData` keys remain `darth.PowerGrid/*`,
- `PowerGrid` raw save-data key suffixes remain `darth.PowerGrid_*`.

## Acceptable Migration Behavior

On the first load of an old pre-migration sandbox save, this warning is acceptable:

```text
[PowerGrid] Imported compatibility state from persisted object modData for UniqueID migration...
```

Interpretation:

- this means the new UniqueID namespace had no save payload yet,
- `PowerGrid` reconstructed battery charge, conduit links, and generator fuel ticks from persisted `darth.PowerGrid/*` object `modData`,
- this should be treated as a pass signal, not a regression, if the restored state matches the visible sandbox setup.

## Failure Conditions

Treat the smoke pass as failed if any of the following occur:

- new red startup errors from `PowerGrid`, `Metal Kegs`, or `FishSmoker Recipe`,
- both old and new repo-owned mods appear to be loaded together,
- `PowerGrid` loses obvious old-save battery charge or conduit links on first migrated load,
- a known powered `Metal Keg` / `Hard Iridium Keg` network no longer appears as a consumer after the first 10-minute tick,
- `FishSmoker Recipe` fails to load as a content pack after installing the new zip set.

## Minimal Smoke Sequence

1. Launch with `SDV Sandbox Baseline` using only the current repo-owned zip set.
2. Confirm startup has no new red errors.
3. Confirm only the new repo-owned IDs are present in the loaded mod list.
4. Load an old sandbox save that predates the UniqueID migration.
5. Record whether the one-time `PowerGrid` migration warning appears.
6. In a location with known old `PowerGrid` infrastructure, run:

```text
powergrid_status
```

7. Check:
   - expected battery counts / visible stored charge are present,
   - expected conduit links still function,
   - no obvious state was reset to zero unexpectedly.
8. If the location includes `Metal Kegs`, wait for the first 10-minute simulation tick and run `powergrid_status` again.
9. Pass `Metal Kegs` re-registration if the powered machine appears under consumer allocations using item IDs like:
   - `darth.MetalKegs_MetalKeg`
   - `darth.MetalKegs_HardIridiumKeg`
10. Save once under the new IDs, return to title, reload the same save, and re-run `powergrid_status`.
11. Pass continuity if state remains coherent after the re-save/reload cycle.

## Notes For The Human Running It

- Older zip files can remain in `artifacts/mod-zips/`; pick the newest version for each mod.
- `FishSmoker Recipe` has no special migration behavior beyond loading under the new content-pack ID.
- Do not expand into query-harness validation during this smoke pass unless startup or save continuity already fails.
