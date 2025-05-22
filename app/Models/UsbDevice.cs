namespace CameraTouchlessControl;

/// <summary>
/// Device descriptor
/// </summary>
public class UsbDevice
{
    public string ID { get; }
    public string Name { get; }
    public string? Description { get; }
    public string? Manufacturer { get; }

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
        }

        System.Diagnostics.Debug.WriteLine($"{Name} {ID} {Description} {Manufacturer}");
    }

    public override string ToString() => Name;
}
