using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Caliburn.Micro;
using AppNs.CoreNs;
using AppNs.Interfaces;
using Iface.Utils;

namespace AppNs.Windows;

public partial class MainWindow : Window, IMainWindowView
{
  private IInfrastructure _infr;
  private IMainWindowViewModel _viewModel;

  public MainWindow()
  {
    var f1 = IsVisible;
    var f2 = IsActive;
    var f3 = IsInitialized;

    InitializeComponent();
    if (Execute.InDesignMode) return;

    var f1x = IsVisible;
    var f2x = IsActive;
    var f3x = IsInitialized;

    ShowInTaskbar = true;

    AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);

  }

  public void OnModelLoaded(IMainWindowViewModel viewModel)
  {
    _infr = IoC.Get<IInfrastructure>();
    _viewModel = viewModel;
  }

  public void DoStep1()
  {
    //IsVisible = false;
  }

  public void DoStep2()
  {
    //IsVisible = true;
  }

  private bool _hard = true;
  private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
  {
  }

  private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
  {
  }

}