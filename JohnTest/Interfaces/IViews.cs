using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AppNs.Interfaces;

internal interface IMainWindowView
{
  void OnModelLoaded(IMainWindowViewModel viewModel);
  void DoStep1();
  void DoStep2();
}

internal interface IShellView
{
  void OnModelLoaded(IShellInternal viewModel);
}


/*
public interface IWidgetListView
{
  void OnModelLoaded(IWidgetListDocument viewModel);
}
*/

internal interface IWorkspaceHolderView
{
  //bool IsLoaded { get; }
  void OnModelLoaded(IWorkspaceHolder viewModel);
}

internal interface IWorkspaceView
{
  void OnModelLoaded(IWorkspace viewModel);
}