using System.Management;

namespace HandTracker;

/// <summary>
/// Manages the ist of available USB cameras, and fires events when this list changes.
/// </summary>
public class UsbService : IDisposable
{
    /// <summary>
    /// Port descriptor
    /// </summary>
    public class UsbCamera(string id, string name, string? description, string? manufacturer)
    {
        public string ID { get; } = id;
        public string Name { get; } = name;
        public string? Description { get; } = description;
        public string? Manufacturer { get; } = manufacturer;
    }

    /// <summary>
    /// Fires when a USB camera becomes available
    /// </summary>
    public event EventHandler<UsbCamera>? Inserted
    {
        add
        {
            _inserted += value;
            foreach (var camera in Cameras)
            {
                _inserted?.Invoke(this, camera);
            }
        }
        remove
        {
            _inserted -= value;
        }
    }

    /// <summary>
    /// Fires when a USB camera is removed
    /// </summary>
    public event EventHandler<UsbCamera>? Removed;

    /// <summary>
    /// List of all USB cameras connected to the system
    /// </summary>
    public UsbCamera[] Cameras => GetAvailableCameras();

    public UsbService()
    {
        Listen("__InstanceCreationEvent", "Win32_USBControllerDevice", ActionType.Inserted);
        Listen("__InstanceDeletionEvent", "Win32_USBControllerDevice", ActionType.Removed);
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

    event EventHandler<UsbCamera> _inserted = delegate { };

    readonly List<UsbCamera> _cachedCameras = [];

    readonly List<ManagementEventWatcher> _watchers = [];

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
                        var camera = CreateCamera(target.Properties);
                        if (camera != null)
                        {
                            _cachedCameras.Add(camera);
                            _inserted?.Invoke(this, camera);
                        }
                        break;
                    case ActionType.Removed:
                        var deviceID = GetDeviceID(target.Properties);
                        if (deviceID != null)
                        {
                            var cachedCamera = _cachedCameras.FirstOrDefault(camera => camera.ID == deviceID);
                            if (cachedCamera != null)
                            {
                                _cachedCameras.Remove(cachedCamera);
                                Removed?.Invoke(this, cachedCamera);
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

    private UsbCamera[] GetAvailableCameras()
    {
        List<UsbCamera> cameras = [];

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%Camera%'");

            foreach (var service in searcher.Get())
            {
                var id = service["DeviceID"]?.ToString() ?? service.ToString();
                var name = service["Name"]?.ToString() ?? service["Caption"]?.ToString() ?? "Generic USB camera";
                var description = service["Description"]?.ToString();
                var manufacturer = service["Manufacturer"]?.ToString();
                cameras.Add(new UsbCamera(id, name, description, manufacturer));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"USB ERROR: {ex.Message}");
        }

        _cachedCameras.Clear();
        
        foreach (var camera in cameras)
        {
            _cachedCameras.Add(camera);
        }

        return cameras.ToArray();
    }

    private static UsbCamera? CreateCamera(PropertyDataCollection props, string? deviceName = null)
    {
        string? deviceID = null;
        string? descrition = null;
        string? manufacturer = null;

        foreach (PropertyData property in props)
        {
            if (property.Name == "DeviceID")        // next 3 properties handle Win32_SerialPort
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
            else if (property.Name == "Dependent")  // this handles Win32_USBControllerDevice, as Win32_SerialPort stopped working
            {
                var usbControllerID = (string)property.Value;
                usbControllerID = usbControllerID.Replace("\"", "");
                var devID = usbControllerID.Split('=')[1];
                using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%{devID}%'");
                ManagementBaseObject[] records = searcher.Get().Cast<ManagementBaseObject>().ToArray();
                foreach (var rec in records)
                {
                    var name = (string?)rec.Properties["Name"]?.Value;
                    if (name?.ToLower().Contains("camera") ?? false)
                    {
                        return CreateCamera(rec.Properties, name);
                    }
                }
            }
        }

        return deviceID == null ? null : new UsbCamera(deviceID, deviceName ?? "USB camera", descrition, manufacturer);
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
            else if (property.Name == "Dependent")  // this handles Win32_USBControllerDevice, as Win32_SerialPort stopped working
            {
                var usbControllerID = (string)property.Value;
                usbControllerID = usbControllerID.Replace("\"", "").Replace(@"\\", @"\");
                deviceID = usbControllerID.Split('=')[1];
            }
        }

        return deviceID;
    }

    // Debugging
    /*
    static HashSet<string> PropsToPrint = new() { "Caption", "Description", "Manufacturer", "Name", "Service" };
    static HashSet<string> ManufacturersToPrint = new() { "microsoft" };
    static HashSet<string> ManufacturersNotToPrint = new() { "microsoft", "standard", "(standard", "intel", "acer", "rivet", "nvidia", "realtek", "generic" };

    static void PrintProperties(PropertyDataCollection props)
    {
        var indent = "    ";
        var man = props["Manufacturer"];
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
                ScreenLogger.Print($"{indent}{p.Name}: ({p.Type})");
                if (p.Value != null)
                {
                    if (p.Value is string[] strings)
                        ScreenLogger.Print($"{indent}{indent}{string.Join($"\n{indent}{indent}", strings)}");
                    else if (p.Value is ushort[] words)
                        ScreenLogger.Print($"{indent}{indent}{string.Join($"\n{indent}{indent}", words)}");
                    else if (p.Value is uint[] dwords)
                        ScreenLogger.Print($"{indent}{indent}{string.Join($"\n{indent}{indent}", dwords)}");
                    else if (p.Value is ulong[] qwords)
                        ScreenLogger.Print($"{indent}{indent}{string.Join($"\n{indent}{indent}", qwords)}");
                    else
                        ScreenLogger.Print($"{indent}{indent}{string.Join($"\n{indent}{indent}", (IEnumerable)p.Value)}");
                }
                else
                {
                    ScreenLogger.Print($"{indent}{indent}none");
                }
            }
            else
            {
                ScreenLogger.Print($"{indent}{p.Name} = {p.Value?.ToString()} ({p.Type})");
            }
        }
        ScreenLogger.Print();
    }*/

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