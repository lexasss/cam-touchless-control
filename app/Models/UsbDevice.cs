namespace CameraTouchlessControl;

/// <summary>
/// USB device descriptor
/// </summary>
public class UsbDevice
{
    /// <summary>
    /// The pattern is "USB\VID_{HEX}&PID_{HEX}&MI_{HEX}\{HEX}"
    /// </summary>
    public string ID { get; }
    public string Name { get; }
    public string? Description { get; }
    public string? Manufacturer { get; }
    /// <summary>
    /// GUID
    /// </summary>
    public string? KernelStreamingCategory { get; } = null;
    public string? ManufacturerID { get; } = null;

    public UsbDevice(string id, string name, string? description, string? manufacturer)
    {
        ID = id;
        Name = name;
        Description = description;
        Manufacturer = manufacturer;

        if (id.StartsWith(@"\\?\"))
        {
            var p = id.Split("#");
            if (p.Length >= 3)
            {
                ID = $@"{p[0][4..].ToUpper()}\{p[1].ToUpper()}\{p[2].ToUpper()}";
            }
            if (p.Length >= 4)
            {
                var cats = p[3].Split(@"\");
                KernelStreamingCategory = cats[0];
                if (cats.Length > 0)
                {
                    ManufacturerID = cats[1];
                }
            }
        }

        System.Diagnostics.Debug.WriteLine($"{Name} {ID} {Description} {Manufacturer} {KernelStreamingCategory} {ManufacturerID}");
    }

    public override string ToString() => Name;
}
