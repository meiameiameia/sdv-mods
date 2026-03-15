# Decision 0002: PowerGrid and Metal Kegs Ownership Boundary

## Status
Accepted

## Decision

`PowerGrid` must not own `Metal Kegs` machine-specific energy tuning.

The ownership model is:

- `PowerGrid` owns energy infrastructure and consumer registration
- `Metal Kegs` owns its own machine tuning values
- the shared `DarthMods.API` contract is used only to register consumer traits

## Why

This removes a brittle coupling where `PowerGrid` had to know about `Metal Kegs` item IDs and settings directly.

It also aligns config ownership with gameplay ownership:

- machine settings live with the machine mod
- infrastructure settings live with the infrastructure mod

## Consequences

- `Metal Kegs` can tune its own PowerGrid behavior through config and GMCM.
- `PowerGrid` only consumes registration and warns about deprecated legacy settings.
- legacy `PowerGrid` Metal Keg config entries are deprecated and ignored after migration.
