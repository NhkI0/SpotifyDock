using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SpotifyDock;

public class NullToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType,
        object parameter, CultureInfo culture) =>
        value == null ? Visibility.Visible : Visibility.Collapsed;
    
    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType,
        object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;
     
    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class ProgressWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType,
        object parameter, CultureInfo culture)
    {
        if (values.Length == 2
            && values[0] is double percent
            && values[1] is double totalWidth)
        {
            return Math.Max(0, totalWidth * percent / 100);
        }

        return 0.0;
    }
    
    public object[] ConvertBack(object value, Type[] targetTypes,
        object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}