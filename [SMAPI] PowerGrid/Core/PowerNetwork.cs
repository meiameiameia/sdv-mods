namespace Darth.PowerGrid.Core;

internal sealed class PowerNetwork
{
    public int NetworkId { get; set; }
    public List<PowerNode> Cables { get; } = new();
    public List<PowerNode> Generators { get; } = new();
    public List<PowerNode> Batteries { get; } = new();
    public List<PowerNode> Consumers { get; } = new();
    public List<PowerNode> Conduits { get; } = new();

    public int MinCableThroughput { get; set; } = int.MaxValue;

    public void AddNode(PowerNode node)
    {
        switch (node.NodeType)
        {
            case PowerNodeType.Cable:
                Cables.Add(node);
                if (node.ThroughputCap < MinCableThroughput)
                    MinCableThroughput = node.ThroughputCap;
                break;
            case PowerNodeType.Generator:
                Generators.Add(node);
                break;
            case PowerNodeType.Battery:
                Batteries.Add(node);
                break;
            case PowerNodeType.Consumer:
                Consumers.Add(node);
                break;
            case PowerNodeType.Conduit:
                Conduits.Add(node);
                break;
        }
    }

    public int TotalGenerationPerTick()
    {
        int total = 0;
        foreach (var gen in Generators)
            total += gen.GenerationPerTick;
        return total;
    }

    public int TotalDemandPerTick()
    {
        int total = 0;
        foreach (var consumer in Consumers)
            total += consumer.DemandPerTick;
        return total;
    }

    public int TotalBatteryCapacity()
    {
        int total = 0;
        foreach (var bat in Batteries)
            total += bat.Capacity;
        return total;
    }
}
