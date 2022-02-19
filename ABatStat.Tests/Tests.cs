using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ABatStat.Tests;

public class Tests
{
    private const string SampleIoregTextFile = "ioreg_sample.txt";

    private readonly string? _sample;

    public Tests()
    {
        try
        {
            _sample = File.ReadAllText(SampleIoregTextFile);
        }
        catch
        {
            // ignored
        }
    }

    [Test]
    public async Task TestReadIoregSample()
    {
        if (_sample == null) throw new AssertionException($"Failed to find sample file {SampleIoregTextFile}");
        BatteryInfo batteryInfo = await BatteryInfo.GetCurrentBatteryInfoFromIoregAsync(new StringReader(_sample));
        Assert.That(batteryInfo.CurrentCapacity, Is.EqualTo(3005));
        Assert.That(batteryInfo.MaxCapacity, Is.EqualTo(3531));
        Assert.That(batteryInfo.DesignCapacity, Is.EqualTo(4790));
    }

    [Test]
    public async Task TestReadActual()
    {
        if (!OperatingSystem.IsMacOS()) throw new InconclusiveException("Not supported on platform other than osx");
        BatteryInfo batteryInfo = await BatteryInfo.GetCurrentBatteryInfoAsync();
        AssertValid(batteryInfo);
    }

    [Test]
    public async Task TestReadIoregActual()
    {
        if (!OperatingSystem.IsMacOS()) throw new InconclusiveException("Not supported on platform other than osx");
        BatteryInfo batteryInfo = await BatteryInfo.GetCurrentBatteryInfoFromIoregAsync();
        AssertValid(batteryInfo);
    }

    private void AssertValid(BatteryInfo batteryInfo)
    {
        Assert.That(batteryInfo.DesignCapacity, Is.GreaterThanOrEqualTo(0));
        Assert.That(batteryInfo.MaxCapacity, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(batteryInfo.DesignCapacity));
        Assert.That(batteryInfo.CurrentCapacity, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(batteryInfo.MaxCapacity));
    }
}
