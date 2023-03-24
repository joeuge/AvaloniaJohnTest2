using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Caliburn.Micro;
using AppNs.CoreNs;
using AppNs.Interfaces;
using Iface.Utils;
using Iface.Utils.Avalonia;

namespace AppNs.UiBlocks.Shell;

public partial class ShellView : UserControl, IShellView
{
  private readonly IInfrastructureInternal _infr;
  private IShellInternal _viewModel;

  public ShellView()
  {
    InitializeComponent();
    if (Execute.InDesignMode) return;

    _infr = IoC.Get<IInfrastructureInternal>();
    AttachedToVisualTree += ShellView_AttachedToVisualTree;
    DetachedFromVisualTree += ShellView_DetachedFromVisualTree;
  }

  protected override void OnGotFocus(GotFocusEventArgs e)
  {
    base.OnGotFocus(e);
    var shell = _viewModel; // (IShell)DataContext;
    if (shell == null) return;
    var source = e.Source;
    //var originalSource = e.OriginalSource;
    shell.OnGotKeyboardFocus();
  }


  private void ShellView_AttachedToVisualTree(object? sender, global::Avalonia.VisualTreeAttachmentEventArgs e)
  {
  }

  private void ShellView_DetachedFromVisualTree(object? sender, global::Avalonia.VisualTreeAttachmentEventArgs e)
  {
  }

  void IShellView.OnModelLoaded(IShellInternal viewModel) //   этому моменту ресурсы приложени€ уже на месте!
  {
    _viewModel = viewModel;
  }

  private void OnKeyDown(object? sender, KeyEventArgs e)
  {
  }

  private void TabControlElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
  {
  }

}
