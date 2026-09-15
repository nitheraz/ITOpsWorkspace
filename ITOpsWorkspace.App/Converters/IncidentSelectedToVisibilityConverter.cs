using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.Converters;

// Compares the current row's Incident against the ViewModel's SelectedIncident
// and returns Visible only when they're the same ticket — drives the accordion expand/collapse.
public class IncidentSelectedToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2) return Visibility.Collapsed;
        if (values[0] is Incident current && values[1] is Incident selected)
            return current.ServiceNowSysId == selected.ServiceNowSysId
                ? Visibility.Visible
                : Visibility.Collapsed;

        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}