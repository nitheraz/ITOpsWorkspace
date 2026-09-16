using System.Windows;
using ITOpsWorkspace.App.ViewModels;

namespace ITOpsWorkspace.App.Views;

public partial class OrgSetupWindow : Window
{
    private readonly OrgSetupViewModel _viewModel;

    public OrgSetupWindow(OrgSetupViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        var success = await _viewModel.SaveAsync();
        if (success)
        {
            DialogResult = true;
            Close();
        }
    }
}