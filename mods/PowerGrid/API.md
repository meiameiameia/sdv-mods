# PowerGrid API

PowerGrid exposes a public SMAPI API for mod authors who want their own big craftable machines to work with PowerGrid.

If you are new to SMAPI integrations, the short version is:

1. Copy the `IPowerGridApi` interface from this file into your own mod.
2. In `GameLaunched`, call `Helper.ModRegistry.GetApi<IPowerGridApi>("meiameiameia.PowerGrid")`.
3. If the API exists and `ApiVersion >= 1`, call `RegisterConsumer(...)` for your machine's qualified item ID.
4. If your machine uses normal `MinutesUntilReady` object processing, PowerGrid can accelerate it automatically while it is active.
5. If your machine uses custom timing logic, query tile speedup and apply the bonus in your own code.

This file is for mod authors. Players do not need it.

## What API Version 1 Is For

Current PowerGrid `0.3.0-rc.1` builds expose API version `1`.

API version `1` is intentionally small and beginner-friendly. It is meant for mods that:

- add a custom big craftable machine;
- know that machine's qualified item ID;
- want PowerGrid to recognize it as a consumer;
- want to query whether a tile is powered and what speedup it received.

API version `1` does not try to expose every internal PowerGrid detail.

## Copy This Interface Into Your Mod

Keep your own local copy of this interface in your mod source. Do not reference PowerGrid's DLL unless you intentionally want a hard dependency.

```csharp
using Microsoft.Xna.Framework;

public interface IPowerGridApi
{
    int ApiVersion { get; }

    void RegisterConsumer(string qualifiedItemId, int demandPerTick, float maxSpeedupFraction, int priority, string displayName);

    void UnregisterConsumer(string qualifiedItemId);

    bool IsTilePowered(string locationName, Vector2 tile);

    float GetSpeedupAtTile(string locationName, Vector2 tile);

    int GetTotalStoredEU(string locationName);
}
```

## Minimum Working Integration

This is the smallest useful integration pattern.

For a simple integration, `RegisterConsumer(...)` is the only PowerGrid API method you need to call.

- Required for the basic path: `RegisterConsumer(...)`
- Optional for advanced/custom behavior: `IsTilePowered(...)`, `GetSpeedupAtTile(...)`, `GetTotalStoredEU(...)`

If your machine already uses normal Stardew object processing, you can stop after registration and let PowerGrid handle acceleration automatically.

```csharp
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;

internal sealed class ModEntry : Mod
{
    private const string PowerGridModId = "meiameiameia.PowerGrid";
    private const string MyMachineQualifiedItemId = "(BC)YourAuthor.YourMachine";

    private IPowerGridApi? powerGrid;

    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        powerGrid = Helper.ModRegistry.GetApi<IPowerGridApi>(PowerGridModId);
        if (powerGrid == null)
            return;

        if (powerGrid.ApiVersion < 1)
            return;

        powerGrid.RegisterConsumer(
            qualifiedItemId: MyMachineQualifiedItemId,
            demandPerTick: 20,
            maxSpeedupFraction: 0.20f,
            priority: 10,
            displayName: "Your Machine");
    }
}
```

If this is all your mod needs, you can stop there.

## Before You Register A Machine

Your machine should meet these assumptions:

- It is a big craftable object with a stable qualified item ID such as `(BC)YourAuthor.YourMachine`.
- It exists as an object in the world on a tile.
- It behaves like a machine, not like a generator, battery, or conduit.

PowerGrid works best out of the box when your machine uses normal Stardew object processing fields such as `MinutesUntilReady`.

## Registering A Consumer

Call `RegisterConsumer` once after PowerGrid is available.

```csharp
powerGrid.RegisterConsumer(
    qualifiedItemId: "(BC)YourAuthor.YourMachine",
    demandPerTick: 20,
    maxSpeedupFraction: 0.20f,
    priority: 10,
    displayName: "Your Machine");
```

### Parameter meanings

| Parameter | Meaning |
| --- | --- |
| `qualifiedItemId` | The machine's qualified item ID. This must match the placed object's qualified item ID exactly. |
| `demandPerTick` | EU requested each PowerGrid tick while the machine is actively processing. |
| `maxSpeedupFraction` | The maximum speed bonus at full power. `0.20f` means up to 20% faster. |
| `priority` | Lower numbers get power first when the network cannot satisfy every active machine. |
| `displayName` | Name shown in the PowerGrid UI. |

### Guardrails

- `qualifiedItemId` must not be blank.
- `demandPerTick` must be zero or positive.
- `maxSpeedupFraction` must be between `0` and `1`.
- `priority` must be zero or positive.
- Blank `displayName` falls back to the qualified item ID.

Calling `RegisterConsumer` again for the same qualified item ID replaces the old registration.

Use `UnregisterConsumer` only if your mod really needs to remove the registration:

```csharp
powerGrid.UnregisterConsumer("(BC)YourAuthor.YourMachine");
```

## Choosing Good Values

If you are unsure what numbers to use, start simple:

- `demandPerTick`: pick a small round number like `10`, `15`, or `20`.
- `maxSpeedupFraction`: start with `0.10f` to `0.25f`.
- `priority`: use `10` unless your mod has a strong reason to be earlier or later than other consumers.

Example:

- `demandPerTick: 15`
- `maxSpeedupFraction: 0.50f`
- `priority: 10`

This means:

- the machine asks for `15` EU each PowerGrid tick while active;
- at full power it can reach `50%` faster processing;
- if the network is short on power, lower-priority-number machines get served first.

## Units And Timing

PowerGrid simulates power on Stardew's 10-minute time ticks.

- EU values are per PowerGrid tick unless a method or UI label says otherwise.
- `demandPerTick` is the amount one active machine asks for each tick.
- `maxSpeedupFraction` is the ceiling on extra speed, not a guaranteed always-on bonus.
- Real speedup scales with real power delivered.

If a machine receives half of its requested EU, it receives half of its configured maximum speedup.

## What PowerGrid Does Automatically

If your registered machine behaves like a normal Stardew object machine, PowerGrid can accelerate it automatically while it is actively processing.

This is the beginner-friendly path.

## What PowerGrid Does Not Do Automatically

`RegisterConsumer` does not:

- create your machine;
- patch arbitrary custom timing systems;
- make an idle machine look powered;
- turn your machine into a generator, battery, or conduit;
- detect unrelated custom machines that were never registered.

If your machine has custom timing logic that does not use normal object processing fields, query tile speedup and apply the bonus yourself.

## Querying Tile Power And Speedup

Use tile queries only when your mod needs to react to the current grid state directly.

You do not need these methods for the basic registration-only path.

```csharp
bool powered = powerGrid.IsTilePowered(location.NameOrUniqueName, tile);
float speedup = powerGrid.GetSpeedupAtTile(location.NameOrUniqueName, tile);
```

`GetSpeedupAtTile` returns the last simulated speedup fraction for that tile, or `0` when no speed bonus was applied.

`IsTilePowered` returns `true` when `GetSpeedupAtTile(...) > 0`.

Important:

- These values come from PowerGrid's last simulation report.
- A registered machine that is idle can still appear in the PowerGrid UI but show `powered=false` and `speedup=0`.
- A tile that is not a registered consumer returns default values.

## Querying Stored Energy

Use `GetTotalStoredEU(locationName)` if you want a simple number for the battery energy currently stored in one loaded location.

```csharp
int storedEu = powerGrid.GetTotalStoredEU(location.NameOrUniqueName);
```

This is mainly useful for diagnostics, compatibility displays, or optional gameplay logic.

## Common Mistakes

If integration does not work, check these first:

1. Wrong mod ID. The PowerGrid SMAPI ID is exactly `meiameiameia.PowerGrid`.
2. Wrong item ID. Your `qualifiedItemId` must exactly match the placed object's qualified item ID.
3. Registering too early. Call `GetApi` and `RegisterConsumer` in `GameLaunched`, not before.
4. Expecting idle machines to report power. Idle machines can show `0` speedup because they are not currently consuming power.
5. Expecting automatic support for custom timing systems. If your machine does not use normal object processing flow, you must apply speed effects yourself.

## Compatibility Expectations

Supported in API version `1`:

1. API version discovery.
2. Registering object-based machine consumers by qualified item ID.
3. Removing a registered machine consumer.
4. Querying tile power and speedup.
5. Querying total stored EU for a loaded location.

Not supported in API version `1`:

1. Registering third-party generators.
2. Registering third-party batteries.
3. Registering third-party conduits.
4. Automatic support for arbitrary non-standard machine timing systems.
5. Public snapshot DTO queries for networks, consumers, generators, or batteries.

Future versions can expand the contract, but `0.3.0-rc.1` intentionally keeps the surface small and reliable.

## Load Order

PowerGrid does not need to be a required dependency unless your mod cannot function without it.

For optional integration:

1. Do not list PowerGrid as a required dependency.
2. In `GameLaunched`, call `Helper.ModRegistry.GetApi<IPowerGridApi>("meiameiameia.PowerGrid")`.
3. If the API is `null`, skip integration.
4. If `ApiVersion` is too low, skip integration or fall back safely.
5. Register your machines only when the API is available.

## How To Verify Your Integration

After your machine is registered:

1. Place the machine on a tile connected to a working PowerGrid network.
2. Start the machine processing a valid input.
3. Open the PowerGrid Machines tab.
4. Confirm your machine appears by name.
5. Confirm it shows EU demand and a positive speed value while active.
6. If you query the same tile through `GetSpeedupAtTile`, confirm the value is greater than `0`.

If your machine appears in the UI but shows `EU 0/x` and `speed 0%`, that usually means the network is underpowered, disconnected, or the machine is idle.
