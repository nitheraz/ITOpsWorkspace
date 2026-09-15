using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ITOpsWorkspace.App.Converters;

// Compares a filter tab's own label against the currently selected filter,
// and returns the appropriate Brush for either Background or Foreground.
public class FilterTabStyleConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2 || values[0] is not string label || values[1] is not string current)
            return DependencyProperty.UnsetValue;

        bool isSelected = label == current;
        string mode = parameter as string ?? "Background";

        if (mode == "Background")
        {
            return isSelected
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B5CF6"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A24"));
        }
        else // Foreground
        {
            return isSelected
                ? Brushes.White
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}