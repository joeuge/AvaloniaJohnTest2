using Avalonia.Controls;
using AppNs.Interfaces;

namespace AppNs.UiBlocks.Shell
{
  public partial class WorkspaceHolderView : UserControl, IWorkspaceHolderView
  {
    public WorkspaceHolderView()
    {
      InitializeComponent();
    }

    void IWorkspaceHolderView.OnModelLoaded(IWorkspaceHolder viewModel)
    {
    }

  }
}
