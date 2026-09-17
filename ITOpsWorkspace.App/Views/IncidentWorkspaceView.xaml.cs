using System.Windows.Controls;
using System.Windows.Input;

namespace ITOpsWorkspace.App.Views;

public partial class IncidentWorkspaceView : UserControl
{
    public IncidentWorkspaceView()
    {
        InitializeComponent();
    }

    private void ChatInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is not ViewModels.IncidentWorkspaceViewModel vm) return;

        e.Handled = true;
        if (vm.SendChatMessageCommand.CanExecute(null))
            vm.SendChatMessageCommand.Execute(null);
    }
}