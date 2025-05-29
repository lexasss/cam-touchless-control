using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace CameraTouchlessControl;

public static class VideoInputEnumerator
{
    public static UsbDevice[] Get()
    {
        System.Diagnostics.Debug.WriteLine($"==== VideoInput ====");

        IMoniker[] moniker = new IMoniker[100];
        object? bagObj = null;
        List<UsbDevice> result = [];

        try
        {
            // Get the system device enumerator
            var srvType = Type.GetTypeFromCLSID(SystemDeviceEnum) ?? throw new Exception();

            // create device enumerator
            var comObj = Activator.CreateInstance(srvType) ?? throw new Exception();
            ICreateDevEnum enumDev = (ICreateDevEnum)comObj;

            // Create an enumerator to find filters of specified category
            enumDev.CreateClassEnumerator(VideoInputDevice, out IEnumMoniker enumMon, 0);
            Guid bagId = typeof(IPropertyBag).GUID;

            while (enumMon.Next(1, moniker, nint.Zero) == 0)
            {
                // get property bag of the moniker
#pragma warning disable CS8625
                moniker[0].BindToStorage(null, null, ref bagId, out bagObj);
#pragma warning restore CS8625

                var bag = (IPropertyBag)bagObj;

                object name = "";
                bag.Read("FriendlyName", ref name, nint.Zero);
                object description = "";
                bag.Read("Description", ref description, nint.Zero);
                object devicePath = "";
                bag.Read("DevicePath", ref devicePath, nint.Zero);
                object manufacturer = "";
                bag.Read("Manufacturer", ref manufacturer, nint.Zero);

                result.Add(new UsbDevice((string)devicePath,  (string)name, (string)description, (string)manufacturer));
            }
        }
        finally
        {
            if (bagObj != null)
            {
                Marshal.ReleaseComObject(bagObj);
            }
        }

        return result.ToArray();
    }

    // Internal

    internal static readonly Guid SystemDeviceEnum = new(0x62BE5D10, 0x60EB, 0x11D0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86);
    internal static readonly Guid VideoInputDevice = new(0x860BB310, 0x5D01, 0x11D0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86);

    [ComImport, Guid("55272A00-42CB-11CE-8135-00AA004BB851"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyBag
    {
        [PreserveSig]
        int Read(
            [In, MarshalAs(UnmanagedType.LPWStr)] string propertyName,
            [In, Out, MarshalAs(UnmanagedType.Struct)] ref object pVar,
            [In] nint pErrorLog);
        [PreserveSig]
        int Write(
            [In, MarshalAs(UnmanagedType.LPWStr)] string propertyName,
            [In, MarshalAs(UnmanagedType.Struct)] ref object pVar);
    }

    [ComImport, Guid("29840822-5B84-11D0-BD3B-00A0C911CE86"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ICreateDevEnum
    {
        [PreserveSig]
        int CreateClassEnumerator([In] ref Guid type, [Out] out IEnumMoniker enumMoniker, [In] int flags);
    }
}
