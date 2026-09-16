using System.Windows;
using ITOpsWorkspace.App.ViewModels;

namespace ITOpsWorkspace.App.Views;

public partial class LockScreen : Window
{
    private readonly LockScreenViewModel _viewModel;

    public LockScreen(LockScreenViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void OnUnlockClicked(object sender, RoutedEventArgs e)
    {
        var success = await _viewModel.UnlockAsync(PasswordBox.Password);
        if (success)
        {
            DialogResult = true;
            Close();
        }
        else
        {
            PasswordBox.Clear();
        }
    }
}