using System.Diagnostics;

namespace ABatStat;

public partial record BatteryInfo
{
    private static Process StartIoregProcess()
    {
        ProcessStartInfo psi = new() { FileName = "ioreg", UseShellExecute = false, Arguments = "-c AppleSmartBattery -w0", RedirectStandardOutput = true };
        Process? p = Process.Start(psi);
        if (p == null) throw new IOException("Failed to start ioreg process");
        return p;
    }

    public static BatteryInfo GetCurrentBatteryInfoFromIoreg()
    {
        using Process p = StartIoregProcess();
        using StreamReader sr = p.StandardOutput;
        MemoryStream ms = new();
        sr.BaseStream.CopyTo(ms);
        p.WaitForExit();
        ms.Position = 0;
        using StreamReader srr = new(ms);
        return GetCurrentBatteryInfoFromIoreg(srr);
    }

    public static async Task<BatteryInfo> GetCurrentBatteryInfoFromIoregAsync()
    {
        using Process p = StartIoregProcess();
        using StreamReader sr = p.StandardOutput;
        MemoryStream ms = new();
        await sr.BaseStream.CopyToAsync(ms);
        await p.WaitForExitAsync();
        ms.Position = 0;
        using StreamReader srr = new(ms);
        return await GetCurrentBatteryInfoFromIoregAsync(srr);
    }

    public static BatteryInfo GetCurrentBatteryInfoFromIoreg(TextReader ioregTextReader)
    {
        int? currentCapacity = null;
        int? maxCapacity = null;
        int? designCapacity = null;
        ProcessIoreg(ioregTextReader, (_, obj, property) =>
        {
            if (obj.Name.Span.SequenceEqual("AppleSmartBattery"))
            {
                ReadOnlySpan<char> p = property.Name.Span;
                if (p.SequenceEqual("CurrentCapacity"))
                {
                    if (!int.TryParse(property.Value.Span, out int currentCapacityN)) throw new InvalidDataException("Invalid CurrentCapacity");
                    currentCapacity = currentCapacityN;
                }
                if (p.SequenceEqual("MaxCapacity"))
                {
                    if (!int.TryParse(property.Value.Span, out int maxCapacityN)) throw new InvalidDataException("Invalid MaxCapacity");
                    maxCapacity = maxCapacityN;
                }
                if (p.SequenceEqual("DesignCapacity"))
                {
                    if (!int.TryParse(property.Value.Span, out int designCapacityN)) throw new InvalidDataException("Invalid DesignCapacity");
                    designCapacity = designCapacityN;
                }
            }
        });
        return new BatteryInfo(
            currentCapacity ?? throw new InvalidDataException("Missing CurrentCapacity"),
            maxCapacity ?? throw new InvalidDataException("Missing MaxCapacity"),
            designCapacity ?? throw new InvalidDataException("Missing DesignCapacity")
        );
    }

    public static async Task<BatteryInfo> GetCurrentBatteryInfoFromIoregAsync(TextReader ioregTextReader)
    {
        int? currentCapacity = null;
        int? maxCapacity = null;
        int? designCapacity = null;
        await ProcessIoregAsync(ioregTextReader, (_, obj, property) =>
        {
            if (obj.Name.Span.SequenceEqual("AppleSmartBattery"))
            {
                ReadOnlySpan<char> p = property.Name.Span;
                if (p.SequenceEqual("CurrentCapacity"))
                {
                    if (!int.TryParse(property.Value.Span, out int currentCapacityN)) throw new InvalidDataException("Invalid CurrentCapacity");
                    currentCapacity = currentCapacityN;
                }
                if (p.SequenceEqual("AppleRawMaxCapacity"))
                {
                    if (!int.TryParse(property.Value.Span, out int maxCapacityN)) throw new InvalidDataException("Invalid MaxCapacity");
                    maxCapacity = maxCapacityN;
                }
                if (p.SequenceEqual("DesignCapacity"))
                {
                    if (!int.TryParse(property.Value.Span, out int designCapacityN)) throw new InvalidDataException("Invalid DesignCapacity");
                    designCapacity = designCapacityN;
                }
            }
        });
        return new BatteryInfo(
            currentCapacity ?? throw new InvalidDataException("Missing CurrentCapacity"),
            maxCapacity ?? throw new InvalidDataException("Missing MaxCapacity"),
            designCapacity ?? throw new InvalidDataException("Missing DesignCapacity")
        );
    }

    private readonly record struct IoregObject(ReadOnlyMemory<char> Name, ReadOnlyMemory<char> Id);

    private readonly record struct IoregProperty(ReadOnlyMemory<char> Name, ReadOnlyMemory<char> Value);

    private delegate void PropertyProcessDelegate(IReadOnlyList<IoregObject> stack, IoregObject obj, IoregProperty property);

    private static void ProcessIoreg(TextReader ioregTextReader, PropertyProcessDelegate del)
    {
        List<IoregObject> objects = new();
        bool obj = false;
        while (ioregTextReader.ReadLine() is { } line) ProcessIoregLine(objects, ref obj, line, del);
    }

    private static async Task ProcessIoregAsync(TextReader ioregTextReader, PropertyProcessDelegate del)
    {
        List<IoregObject> objects = new();
        bool obj = false;
        while (await ioregTextReader.ReadLineAsync() is { } line) ProcessIoregLine(objects, ref obj, line, del);
    }

    private static void ProcessIoregLine(List<IoregObject> objects, ref bool obj, string line, PropertyProcessDelegate del)
    {
        if (!TryGetIoregDepth(line, out int depth)) return;
        if (obj)
        {
            if (line[depth] == '}')
            {
                obj = false;
                return;
            }
            int nameIdx = depth + 1;
            ReadOnlyMemory<char> name = line.AsMemory(nameIdx);
            int endNameIdx = name.Span.IndexOf('"');
            if (endNameIdx == -1) throw new InvalidDataException();
            if (objects.Count == 0) return;
            name = name[..endNameIdx];
            int valueIdx = nameIdx + endNameIdx + 4;
            ReadOnlyMemory<char> value = line.AsMemory(valueIdx);
            del(objects, objects[^1], new IoregProperty(name, value));
        }
        else
        {
            switch (line[depth])
            {
                case '{':
                    obj = true;
                    break;
                case '+':
                    int nDepth = depth / 2, ndd = objects.Count - nDepth;
                    if (ndd > 0) objects.RemoveRange(nDepth, ndd);
                    int nameIdx = depth + 4;
                    ReadOnlyMemory<char> name = line.AsMemory(nameIdx);
                    int endNameIdx = name.Span.IndexOf(' ');
                    if (endNameIdx == -1) throw new InvalidDataException();
                    name = name[..endNameIdx];
                    int idIdx = nameIdx + endNameIdx + 9;
                    ReadOnlyMemory<char> id = line.AsMemory(idIdx);
                    int endIdIdx = id.Span.IndexOf(' ');
                    if (endIdIdx == -1) throw new InvalidDataException();
                    id = id[..endIdIdx];
                    objects.Add(new IoregObject(name, id));
                    break;
                default:
                    throw new InvalidDataException();
            }
        }
    }


    private static bool TryGetIoregDepth(string line, out int depth)
    {
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c != ' ' && c != '|')
            {
                depth = i;
                return true;
            }
        }
        depth = line.Length;
        return false;
    }
}
