# Powered Machine Family Validation

Run this when adding or materially changing a powered machine-family mod.

This is the minimum validation matrix, not a full release process.

## 1. Base Machine Behavior

1. Place the machine normally.
2. Confirm it processes correctly without `PowerGrid`.
3. Confirm output collection works.
4. Save, reload, and confirm persistence.

## 2. PowerGrid Integration

1. Confirm the machine self-registers correctly with `PowerGrid`.
2. Confirm powered vs unpowered behavior is distinguishable.
3. Confirm acceleration or other declared power behavior actually applies.
4. Confirm behavior remains graceful when `PowerGrid` is absent.

## 3. Automate-Compatible IO

1. Confirm standard input works.
2. Confirm standard output works.
3. Confirm no custom interaction path breaks Automate-style IO expectations.

## 4. Terminal Observability

1. Confirm the machine appears in snapshots/status surfaces.
2. Confirm `ProgressText` is truthful and readable.
3. Confirm powered/unpowered/processing status is understandable in `Farm Terminal`.

## 5. Render Parity

Run this only if custom runtime art or stateful art exists.

1. Confirm idle draw.
2. Confirm processing draw.
3. Confirm ready indicator draw.
4. Confirm fallback behavior when a state sprite is missing or invalid.

## 6. Config / GMCM

1. Confirm config loads safely with defaults.
2. Confirm meaningful player-facing settings behave correctly.
3. If the mod has meaningful config, confirm GMCM parity is implemented or explicitly deferred.
