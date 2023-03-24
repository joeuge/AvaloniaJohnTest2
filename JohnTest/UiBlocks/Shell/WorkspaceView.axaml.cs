using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Caliburn.Micro;
using AppNs.Interfaces;
using AppNs.UiBlocks.ContextMenuNs;
using Iface.Utils;

namespace AppNs.UiBlocks.Shell
{
  public partial class WorkspaceView : UserControl, IWorkspaceView
  {
    private readonly IInfrastructure _infr;
    private IWorkspace _viewModel;

    public WorkspaceView()
    {
      InitializeComponent();
      _infr = IoC.Get<IInfrastructure>();
    }

    void IWorkspaceView.OnModelLoaded(IWorkspace viewModel)
    {
      _viewModel = DataContext as IWorkspace;
      Requires.Reference.NotNull(_viewModel, "IWorkspace");
    }


    /*
    private void MainContextMenu_OnContextMenuOpening(object? sender, CancelEventArgs e)
    {
      var context = new CollectorContext(_viewModel.TryGetWorkspaceHolder(), null, new Point());
      
      //-------------------------------
      _infr.CollectContextMenu(context);
      //-------------------------------

      var list = context.GetAllItems();
      if (list == null || list.Count == 0)
      {
        e.Cancel = true;
        return;
      }
      //-------------------------------
      MainContextMenu.Items = list;
      //-------------------------------
    }
    */
  }
}
