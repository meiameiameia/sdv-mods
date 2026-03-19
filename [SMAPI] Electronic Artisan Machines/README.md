# [SMAPI] Electronic Artisan Machines

First release of the Electronic Artisan Machines line.

Current scope:

- Adds `Industrial Preserves Jar`
- Keeps standard machine IO behavior
- Adds optional PowerGrid consumer registration for throughput speedup

Out of scope in this release:

- Powered Cheese Press
- Powered Oil Maker
- queue/batching
- hard power gating
- quality-changing outputs
- chipset UI/systems

## Config

`config.json` fields:

- `EnablePowerGridIntegration`
- `IndustrialPreservesJarEUPerMinute`
- `IndustrialPreservesJarMaxSpeedup`
- `IndustrialPreservesJarPriority`

If PowerGrid is not installed, the machine still works with baseline machine behavior.
