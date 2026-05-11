namespace Meiameiameia.PowerGrid.Integrations;

internal enum PowerConsumerPowerState
{
    NotConnected,
    GridOffline,
    Standby,
    Powered,
    LowPower,
    ProcessingUnpowered
}

internal static class PowerConsumerPowerStatus
{
    public static PowerConsumerPowerState Classify(PowerConsumerSnapshot? consumer, PowerNetworkSnapshot? network)
    {
        if (consumer == null)
            return PowerConsumerPowerState.NotConnected;

        if (!HasGridInfrastructure(network))
            return PowerConsumerPowerState.NotConnected;

        if (consumer.IsProcessing)
        {
            if (consumer.EUAllocated > 0 || consumer.SpeedupFraction > 0f)
            {
                return consumer.DemandPerTick <= 0 || consumer.EUAllocated >= consumer.DemandPerTick
                    ? PowerConsumerPowerState.Powered
                    : PowerConsumerPowerState.LowPower;
            }

            return PowerConsumerPowerState.ProcessingUnpowered;
        }

        return HasUsablePower(network)
            ? PowerConsumerPowerState.Standby
            : PowerConsumerPowerState.GridOffline;
    }

    public static bool HasUsablePower(PowerNetworkSnapshot? network)
    {
        return network != null
            && HasGridInfrastructure(network)
            && (network.LastTickGenerated > 0
                || network.LastTickFromBatteries > 0
                || network.TotalStoredEU > 0);
    }

    private static bool HasGridInfrastructure(PowerNetworkSnapshot? network)
    {
        return network != null
            && (network.CableCount > 0
                || network.GeneratorCount > 0
                || network.BatteryCount > 0
                || network.ConduitCount > 0);
    }

    public static bool IsPositive(PowerConsumerPowerState state)
    {
        return state is PowerConsumerPowerState.Standby or PowerConsumerPowerState.Powered;
    }

    public static bool IsWarning(PowerConsumerPowerState state)
    {
        return state is PowerConsumerPowerState.LowPower
            or PowerConsumerPowerState.ProcessingUnpowered
            or PowerConsumerPowerState.GridOffline;
    }
}
