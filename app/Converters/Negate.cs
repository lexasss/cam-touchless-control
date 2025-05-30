﻿using System.Globalization;
using System.Windows.Data;

namespace CameraTouchlessControl.Converters;

internal class Negate :  IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(bool)value;
    }
}
