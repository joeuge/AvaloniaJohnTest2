using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Caliburn.Micro;
using AppNs.CoreNs;
using AppNs.Interfaces;
using AppNs.Windows;
using Avalonia.Media.Imaging;
using Iface.Utils;

namespace AppNs.UiBlocks.ExtraWindows;

public partial class ExtraWindow : Window, IExtraWindow
{
  Control IExtraWindow.Control => this;

  object IExtraWindow.Header
  {
    get => Title;
    set => Title = value?.ToString();
  }

  /*
  IBitmap IExtraWindow.Icon
  {
    get => Icon;
    set => Icon = value as IBitmap;
  }
  */

  public double Top
  {
    get => Bounds.Top;
    set => Bounds = new Rect(Bounds.X, value, Bounds.Width, Bounds.Height);
  }

  public double Left
  {
    get => Bounds.Left;
    set => Bounds = new Rect(value, Bounds.Y, Bounds.Width, Bounds.Height);
  }

  public IWorkspaceHolder WorkspaceHolder { get; }


  //------------------
  public ExtraWindow() // XAML Designer?
  {
    if (!Execute.InDesignMode)
      throw new InvalidOperationException();
    InitializeComponent();
  }

  //------------------
  public ExtraWindow(IWorkspaceHolder workspaceHolder)
  {
    WorkspaceHolder = workspaceHolder;
    InitializeComponent();
  }


  void IExtraWindow.BringToFront()
  {
    var mem = Topmost;
    Topmost = false;
    Topmost = true;
    Topmost = mem;
  }

  void IExtraWindow.SetShowInTaskbar(bool value)
  {
    ShowInTaskbar = value;
  }


}