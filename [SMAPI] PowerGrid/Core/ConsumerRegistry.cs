namespace Darth.PowerGrid.Core;

internal sealed class ConsumerDefinition
{
    public string QualifiedItemId { get; init; } = "";
    public int DemandPerTick { get; init; }
    public float MaxSpeedupFraction { get; init; }
    public int Priority { get; init; }
    public string DisplayName { get; init; } = "";
}

internal sealed class ConsumerRegistry
{
    public static ConsumerRegistry Instance { get; } = new();

    private readonly Dictionary<string, ConsumerDefinition> consumers = new(StringComparer.OrdinalIgnoreCase);

    private ConsumerRegistry() { }

    public void Register(ConsumerDefinition def)
    {
        consumers[def.QualifiedItemId] = def;
    }

    public void Unregister(string qualifiedItemId)
    {
        consumers.Remove(qualifiedItemId);
    }

    public ConsumerDefinition? GetConsumerDef(string qualifiedItemId)
    {
        return consumers.TryGetValue(qualifiedItemId, out var def) ? def : null;
    }

    public IReadOnlyDictionary<string, ConsumerDefinition> GetAll() => consumers;
}
