using System.Windows;
using ITOpsWorkspace.App.ViewModels;

namespace ITOpsWorkspace.App.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void OnLoginClicked(object sender, RoutedEventArgs e)
    {
        var success = await _viewModel.LoginAsync(PasswordBox.Password);
        if (success)
        {
            DialogResult = true;
            Close();
        }
        // If not success but IsChoosingGroup is now true, the window stays open
        // and the XAML visibility bindings automatically switch to Step 2.
    }

    private async void OnConfirmGroupClicked(object sender, RoutedEventArgs e)
    {
        var success = await _viewModel.ConfirmGroupSelectionAsync();
        if (success)
        {
            DialogResult = true;
            Close();
        }
    }
}