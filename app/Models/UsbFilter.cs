using System.Management;

namespace CameraTouchlessControl;

public class UsbFilter(string property, string[] values)
{
    public string Property => property;
    public string[] Values => values;

    public bool IsMatching(ManagementBaseObject obj)
    {
        var propValue = obj[Property]?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(propValue))
            return false;

        return Values.Any(v => v.Equals(propValue, StringComparison.CurrentCultureIgnoreCase));
    }
}
