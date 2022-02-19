namespace ABatStat;

public partial record BatteryInfo
{
    public int CurrentCapacity { get; init; }
    public int MaxCapacity { get; init; }
    public int DesignCapacity { get; init; }

    public BatteryInfo(int currentCapacity, int maxCapacity, int designCapacity)
    {
        CurrentCapacity = currentCapacity;
        MaxCapacity = maxCapacity;
        DesignCapacity = designCapacity;
    }

    public static BatteryInfo GetCurrentBatteryInfo()
    {
        if (OperatingSystem.IsMacOS()) return GetCurrentBatteryInfoFromIoreg();
        throw new PlatformNotSupportedException();
    }

    public static Task<BatteryInfo> GetCurrentBatteryInfoAsync()
    {
        if (OperatingSystem.IsMacOS()) return GetCurrentBatteryInfoFromIoregAsync();
        throw new PlatformNotSupportedException();
    }
}
