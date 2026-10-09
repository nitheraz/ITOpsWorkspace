
using System.Windows;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.Views;

public partial class ResolutionDialog : Window
{
    public List<IncidentResolutionCode> ResolutionCodes { get; }

    public string SelectedResolutionCode { get; private set; } =
        string.Empty;

    public string ResolutionNotes { get; private set; } =
        string.Empty;

    public ResolutionDialog(
        List<IncidentResolutionCode> resolutionCodes)
    {
        InitializeComponent();

        ResolutionCodes = resolutionCodes;
        DataContext = this;

        ResolutionCodeComboBox.SelectedIndex = -1;
    }

    private void ResolveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ResolutionCodeComboBox.SelectedValue is not string code ||
            string.IsNullOrWhiteSpace(code))
        {
            MessageBox.Show(
                this,
                "Please select a resolution code.",
                "Resolution Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var notes = ResolutionNotesTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(notes))
        {
            MessageBox.Show(
                this,
                "Please enter resolution notes.",
                "Resolution Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            ResolutionNotesTextBox.Focus();
            return;
        }

        SelectedResolutionCode = code;
        ResolutionNotes = notes;

        DialogResult = true;
    }
}