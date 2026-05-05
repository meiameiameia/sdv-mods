using Microsoft.Xna.Framework;

namespace Meiameiameia.PowerGrid.Integrations;

/// <summary>
/// Compatibility shim for older integrations still compiled against PowerGrid's local API namespace.
/// </summary>
public interface IPowerGridApi
{
    int ApiVersion { get; }

    void RegisterConsumer(string qualifiedItemId, int demandPerTick, float maxSpeedupFraction, int priority, string displayName);

    void UnregisterConsumer(string qualifiedItemId);

    bool IsTilePowered(string locationName, Vector2 tile);

    float GetSpeedupAtTile(string locationName, Vector2 tile);

    int GetTotalStoredEU(string locationName);
}
