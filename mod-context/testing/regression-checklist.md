# Regression Checklist

Run the smallest relevant slice of this checklist after each meaningful gameplay or integration change.

If the change modifies manifest identity, packaged zip intake, or repo-owned mod UniqueIDs, also run `testing/uniqueid-migration-smoke.md`.

## Baseline

1. Launch with `SDV Sandbox Baseline`.
2. Load `Mod Sandbox`.
3. Run the mod status/diagnostic commands relevant to the changed mods.
4. If PowerGrid query/snapshot code changed, run `powergrid_query_dump` and compare it to `powergrid_status`.
5. Confirm item placement/removal works.
6. Confirm save/load persistence works.

## Integrations

1. Launch with `SDV Sandbox Integrations`.
2. Confirm startup is free of new red errors.
3. Re-test the changed machine or system with `Automate` and other targeted integration mods.
4. If PowerGrid query/snapshot code changed, run `powergrid_query_dump` and compare it to `powergrid_status`.
5. Confirm no obvious routing or machine-state desync appears.

## Stress

1. Launch with `SDV Sandbox Stress`.
2. Confirm startup is clean enough for testing.
3. Re-run the changed scenario under the heavy stack.
4. If PowerGrid query/snapshot code changed, run `powergrid_query_dump` and compare it to `powergrid_status`.
5. Save, reload, and confirm persistence.

## Decision Rule

If a change alters:

- ownership boundaries
- shared contracts
- mod interoperability expectations
- sandbox workflow

then update `mod-context/decisions/` or `mod-context/testing/` before closing the task.

If the change adds or modifies PowerGrid query/snapshot behavior, also run the focused checks in `testing/powergrid-query-api-validation.md`.

If the change adds or materially changes a powered machine-family mod, also run `testing/powered-machine-family-validation.md`.
