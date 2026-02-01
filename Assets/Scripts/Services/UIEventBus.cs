using System;

public static class UIEventBus
{
    public static event Action<string> OnMapChanged;
    public static event Action<string> OnVehicleChanged;

    public static void RaiseMapChanged(string mapId) => OnMapChanged?.Invoke(mapId);
    public static void RaiseVehicleChanged(string vehicleId) => OnVehicleChanged?.Invoke(vehicleId);
}
