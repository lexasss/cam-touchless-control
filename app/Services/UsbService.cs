using Leap;
using System.Collections;
using System.Management;

namespace CameraTouchlessControl;

/// <summary>
/// Manages a ist of available USB devices of a certain PNP class
/// and fires events when this list changes.
/// </summary>
public class UsbService : IDisposable
{
    /// <summary>
    /// Device descriptor
    /// </summary>
    public class UsbDevice(string id, string name, string? description, string? manufacturer)
    {
        public string ID { get; } = id;
        public string Name { get; } = name;
        public string? Description { get; } = description;
        public string? Manufacturer { get; } = manufacturer;
    }

    /// <summary>
    /// Fires when a USB device becomes available
    /// </summary>
    public event EventHandler<UsbDevice>? Inserted;

    /// <summary>
    /// Fires when a USB device is removed
    /// </summary>
    public event EventHandler<UsbDevice>? Removed;

    /// <summary>
    /// List of all USB devices connected to the system
    /// </summary>
    public UsbDevice[] Devices => GetAvailableDevices();

    public UsbService(string[] pnpClasses)
    {
        _pnpClasses = pnpClasses;

        Listen("__InstanceCreationEvent", "Win32_USBControllerDevice", ActionType.Inserted);
        Listen("__InstanceDeletionEvent", "Win32_USBControllerDevice", ActionType.Removed);

        GetAvailableDevices();
    }

    public void Dispose()
    {
        foreach (var w in _watchers)
        {
            w.Dispose();
        }

        _watchers.Clear();

        GC.SuppressFinalize(this);
    }

    // Internal

    private enum ActionType
    {
        Inserted,
        Removed
    }

    readonly List<UsbDevice> _cachedDevices = [];
    readonly List<ManagementEventWatcher> _watchers = [];
    readonly string[] _pnpClasses;

    private void Listen(string source, string target, ActionType actionType)
    {
        var query = new WqlEventQuery($"SELECT * FROM {source} WITHIN 2 WHERE TargetInstance ISA '{target}'");
        var watcher = new ManagementEventWatcher(query);

        watcher.EventArrived += (s, e) =>
        {
            try
            {
                using var target = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                switch (actionType)
                {
                    case ActionType.Inserted:
                        var device = CreateDevice(target.Properties);
                        if (device != null)
                        {
                            _cachedDevices.Add(device);
                            Inserted?.Invoke(this, device);
                        }
                        break;
                    case ActionType.Removed:
                        var deviceID = GetDeviceID(target.Properties);
                        if (deviceID != null)
                        {
                            var cachedDevice = _cachedDevices.FirstOrDefault(device => device.ID == deviceID);
                            if (cachedDevice != null)
                            {
                                _cachedDevices.Remove(cachedDevice);
                                Removed?.Invoke(this, cachedDevice);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"USB ERROR: {ex.Message}");
            }
        };

        _watchers.Add(watcher);

        try
        {
            watcher.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    private UsbDevice[] GetAvailableDevices()
    {
        List<UsbDevice> devices = [];

        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_PnPEntity"); //  WHERE Caption LIKE '%{_searchKeyword}%'

            foreach (var service in searcher.Get())
            {
                var pnpClass = service["PNPClass"]?.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(pnpClass))
                    continue;
                if (!_pnpClasses.Any(c => string.Compare(c, pnpClass, true) == 0))
                    continue;

                var id = service["DeviceID"]?.ToString() ?? service.ToString();
                var name = service["Name"]?.ToString() ?? "Unknown USB device";
                var description = service["Description"]?.ToString();
                var manufacturer = service["Manufacturer"]?.ToString();
                devices.Add(new UsbDevice(id, name, description, manufacturer));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"USB ERROR: {ex.Message}");
        }

        _cachedDevices.Clear();
        
        foreach (var device in devices)
        {
            _cachedDevices.Add(device);
        }

        return devices.ToArray();
    }

    private UsbDevice? CreateDevice(PropertyDataCollection props, string? deviceName = null)
    {
        string? deviceID = null;
        string? descrition = null;
        string? manufacturer = null;

        foreach (PropertyData property in props)
        {
            if (property.Name == "DeviceID")
            {
                deviceID = (string?)property.Value;
            }
            else if (property.Name == "Description")
            {
                descrition = (string?)property.Value;
            }
            else if (property.Name == "Manufacturer")
            {
                manufacturer = (string?)property.Value;
            }
            else if (property.Name == "Dependent")  // this handles Win32_USBControllerDevice
            {
                var usbControllerID = (string)property.Value;
                usbControllerID = usbControllerID.Replace("\"", "");
                var devID = usbControllerID.Split('=')[1];
                using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%{devID}%'");
                ManagementBaseObject[] records = searcher.Get().Cast<ManagementBaseObject>().ToArray();

                foreach (var rec in records)
                {
                    var pnpClass = (string?)rec.Properties["PNPClass"]?.Value;

                    if (string.IsNullOrEmpty(pnpClass))
                        continue;
                    if (!_pnpClasses.Any(c => string.Compare(c, pnpClass, true) == 0))
                        continue;

                    var name = (string?)rec.Properties["Name"]?.Value;
                    return CreateDevice(rec.Properties, name);
                }
            }
        }

        return deviceID == null ? null : new UsbDevice(deviceID, deviceName ?? "Unknown USB device", descrition, manufacturer);
    }

    private static string? GetDeviceID(PropertyDataCollection props)
    {
        string? deviceID = null;

        foreach (PropertyData property in props)
        {
            if (property.Name == "DeviceID")
            {
                deviceID = (string?)property.Value;
            }
            else if (property.Name == "Dependent")
            {
                var usbControllerID = (string)property.Value;
                usbControllerID = usbControllerID.Replace("\"", "").Replace(@"\\", @"\");
                deviceID = usbControllerID.Split('=')[1];
            }
        }

        return deviceID;
    }

    /*
    // Debugging
    static HashSet<string> PropsToPrint = ["Caption", "Description", "Manufacturer", "Name", "Service"];
    static HashSet<string> ManufacturersToPrint = ["microsoft"];
    static HashSet<string> ManufacturersNotToPrint = ["microsoft", "standard", "(standard", "intel", "acer", "rivet", "nvidia", "realtek", "generic"];

    static void PrintProperties(PropertyDataCollection props)
    {
        var indent = "    ";
        //var man = props["Manufacturer"];
        //if (ManufacturersNotToPrint.Any(m => man?.Value?.ToString()?.ToLower().StartsWith(m) ?? false))
        //    return;
        //if (!ManufacturersToPrint.Any(m => man?.Value?.ToString()?.ToLower().StartsWith(m) ?? false))
        //    return;

        foreach (PropertyData p in props)
        {
            //if (!PropsToPrint.Contains(p.Name))
            //    continue;
            if (p.IsArray)
            {
                System.Diagnostics.Debug.WriteLine($"{indent}{p.Name}: ({p.Type})");
                if (p.Value != null)
                {
                    if (p.Value is string[] strings)
                        System.Diagnostics.Debug.WriteLine($"{indent}{indent}{string.Join($"\n{indent}{indent}", strings)}");
                    else if (p.Value is ushort[] words)
                        System.Diagnostics.Debug.WriteLine($"{indent}{indent}{string.Join($"\n{indent}{indent}", words)}");
                    else if (p.Value is uint[] dwords)
                        System.Diagnostics.Debug.WriteLine($"{indent}{indent}{string.Join($"\n{indent}{indent}", dwords)}");
                    else if (p.Value is ulong[] qwords)
                        System.Diagnostics.Debug.WriteLine($"{indent}{indent}{string.Join($"\n{indent}{indent}", qwords)}");
                    else
                        System.Diagnostics.Debug.WriteLine($"{indent}{indent}{string.Join($"\n{indent}{indent}", (IEnumerable)p.Value)}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"{indent}{indent}none");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"{indent}{p.Name} = {p.Value?.ToString()} ({p.Type})");
            }
        }
        System.Diagnostics.Debug.WriteLine("");
    }

    /*
    static COMUtils()
    {
        ScreenLogger.Print("==== PnP devices ===");
        using var pnp = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%' OR Caption LIKE '%Smellodi%'");
        var records = pnp.Get().Cast<ManagementBaseObject>().ToArray();
        foreach (var rec in records)
            PrintProperties(rec.Properties);
        ScreenLogger.Print("====================");
    }*/
}