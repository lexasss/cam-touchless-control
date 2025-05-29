using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CameraTouchlessControl.Converters;

internal class BooleanToVisible : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isTrue = (bool)value;
        if (parameter != null)
            isTrue = !isTrue;
        return isTrue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visibility = (Visibility)value;
        return visibility == Visibility.Visible;
    }
}
