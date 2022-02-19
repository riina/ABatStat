using System;
using ABatStat;

BatteryInfo bi = BatteryInfo.GetCurrentBatteryInfo();
Console.WriteLine($"Battery charge: {(int)((float)bi.CurrentCapacity / bi.MaxCapacity * 100)}%");
Console.WriteLine($"Battery health: {(int)((float)bi.MaxCapacity / bi.DesignCapacity * 100)}%");
