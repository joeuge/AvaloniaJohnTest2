using System.ComponentModel;
using System.Windows.Input;
using Avalonia.Controls;
using Caliburn.Micro;
using AppNs.Interfaces;
using Iface.Utils;
using Avalonia.Media.Imaging;

namespace AppNs.UiBlocks.ExtraWindows;

//====================================
/*
 * WPF: При закрытии главного окна порядок возникновения событий:
 *   MainWindow.Closing | PreviewClosed
 *   ThisWindow.Closing НЕ ВОЗНИКАЕТ !!!
 *   ThisWindow.Closed
 *   MainWindow.Closed
 * 
*/

//====================================
public class ExtraWindowController : IExtraWindowController //: ModelBase
{
  private ExtraWindow _window;
  private readonly IModelTags _modelTags;
  private readonly IGuardClose? _closeGuard;
  private bool _useCloseGuard = true;
  private bool _closingFromWindow;
  private bool _closingFromModel;
  private bool _actuallyClosing;
  private bool _inCanClose; // todo: remove

  private bool InGentleRemovingFromParent() { return _modelTags?.InGentleRemovingFromParent ?? false; }

  internal IInfrastructureInternal Infr { get; }
  public IWorkspaceHolder Owner { get; }
  public IExtraWindow Window => _window;

  public ICommand ToMainWindowCommand { get; }


  public event EventHandler WindowActivated
  {
    add => _window.Activated += value;
    remove => _window.Activated -= value;
  }

  public ExtraWindowController()
  {
    if (!Execute.InDesignMode)
      throw new InvalidOperationException();
  }

  public ExtraWindowController(IInfrastructure infr, IWorkspaceHolder owner)
  {
    Infr = (IInfrastructureInternal)infr;
    Owner = owner;
    _modelTags = owner as IModelTags;
    _closeGuard = owner as IGuardClose;

    ToMainWindowCommand = new SimpleCommand(async _ => await Infr.Shell.MoveWorkspaceHolder(Owner, WorkspaceOwnerType.ShellTabs));
  }

  public void CreateWindow()
  {
    //-------------------------
    CreateWindowCore();
    //-------------------------

    Window.SetShowInTaskbar(false); // RadWindowInteropHelper.SetShowInTaskbar(_window, false);

    //Caliburn.Micro.Action.SetTarget(_window, this); // -> _window.DataContext = this
    Window.Control.SetValue(Caliburn.Micro.Action.TargetProperty, this); // -> _window.DataContext = this

    // approach 1
    //_window.SetResourceReference(FrameworkElement.StyleProperty, "MyRadWindowStyle");

    // approach 2
    //var style = (Style)Application.Current.FindResource("MyRadWindowStyle");
    //_window.Style = style;

    //_window.SetValue(View.IsGeneratedProperty, true); // Used in Caliburn View.GetFirstNonGeneratedView()

    //ViewModelBinder.Bind(_owner, _window, null); // -> GetFirstNonGeneratedView(window) вернет Window.Content в качестве View

    //var binding = new Binding("Workspace.FullDisplayName") { Mode = BindingMode.OneWay };
    //_window.SetBinding(HeaderedContentControl.HeaderProperty, binding);

    SetWindowTitle();
    if (Owner.Workspace != null)
    {
      Owner.Workspace.PropertyChanged += Workspace_PropertyChanged; //Owner.Workspace.PropertyChanged += (_, e) => SetWindowTitleAndIcon();
    }

    //Owner.Activate(); // так было в WPF
    Owner.Deactivated += OwnerDeactivated;

    // Wire Window
    _window.Closing += WindowClosing;
    _window.Closed += WindowClosed;
  }

  private IExtraWindow CreateWindowCore()
  {
    _window = new ExtraWindow(Owner)
    {
      //ShowMinButton = false,
      //ShowMaxRestoreButton = true,
      //SnapsToDevicePixels = true,
      WindowStartupLocation = ((IWorkspaceHolderInternal)Owner).WindowStartupLocation, // WindowStartupLocation.CenterOwner
    };

    return _window;
  }


  private void Workspace_PropertyChanged(object? sender, PropertyChangedEventArgs e)
  {
    SetWindowTitle();
  }

  public void Show(bool asIs)
  {
    if (_window == null)
      return;

    if (_inCanClose)
      return; // вряд ли это возможно

    if (asIs)
    {
      _window.Show(Infr.MainWindow);
      return;
    }

    if (Window.WindowState == WindowState.Minimized)
      Window.WindowState = WindowState.Normal;

    _window.Show(Infr.MainWindow);

    // наличие следующего фрагмента в определенной ситуации приводит к "пряткам главного окна" при закрытии последнего ExtraWindow //todo
    //#tag#info# Игра в прятки

    Window.BringToFront();
  }

  public void Close(bool useCloseGuard)
  {
    _useCloseGuard = useCloseGuard;
    _window.Close();
  }

  private TaskCompletionSource<bool> _closeCompletionSource;
  public Task<bool> CloseAsync(bool useCloseGuard) // true = success
  {
    if (_window == null)
    {
      return Task.FromResult(true);
    }

    _useCloseGuard = useCloseGuard;
    if (_closeCompletionSource == null)
    {
      _closeCompletionSource = new TaskCompletionSource<bool>();
      _window.Close();
    }
    return _closeCompletionSource.Task;
  }


  private void SetWindowTitle()
  {
    Window.Header = Owner.Workspace?.DisplayName;
  }

  public string GetWindowTitle()
  {
    //return _owner.Workspace?.FullDisplayName;
    return Window?.Header?.ToString();
  }


  /*
  public IBitmap? GetWindowIcon()
  {
    return Window?.Icon;
  }
  */

  private bool _inGentleRemovingFromParent;
  //---------------------------------------
  private async void WindowClosing(object? sender, CancelEventArgs e)
  {
    //GlobalUtil.ConsoleThread("WindowController.WindowClosing");

    _inGentleRemovingFromParent = InGentleRemovingFromParent();
    if (_inGentleRemovingFromParent)
      return;

    if (e.Cancel)
    {
      _closeCompletionSource?.SetResult(false);
      return;
    }

    if (_actuallyClosing)
    {
      _actuallyClosing = false;
      return;
    }

    if (_closeGuard == null || !_useCloseGuard)
      return;

    // исходный вызов сбрасываем
    e.Cancel = true;

    await Task.Yield();

    _inCanClose = true; // todo: delete
    try
    {
      try
      {
        var canClose = await _closeGuard.CanCloseAsync(CancellationToken.None);

        if (!canClose)
        {
          _closeCompletionSource?.SetResult(false);
          return;
        }
      }
      catch (Exception exception)
      {
        if (exception is IfaceCancelException || exception?.InnerException is IfaceCancelException)
        {
          _closeCompletionSource?.SetResult(false);
          return;
        }
      }
    }
    finally
    {
      _inCanClose = false;
    }

    // повторный вызов
    _actuallyClosing = true;
    _window.Close(); // _window._dialogResult не трогается // остается прежним от исходного вызова
  }

  //---------------------------------------
  private async void WindowClosed(object? sender, EventArgs ea)
  {
    //GlobalUtil.ConsoleThread("WindowController.WindowClosed");
    var xxxInGentleRemovingFromParent = InGentleRemovingFromParent(); // remove

    // UnWire Window
    _window.Closed -= WindowClosed;
    _window.Closing -= WindowClosing;

    Owner.Deactivated -= OwnerDeactivated;

    if (Owner.Workspace != null)
    {
      Owner.Workspace.PropertyChanged -= Workspace_PropertyChanged;
    }

    try
    {
      if (_closingFromModel)
        return;

      _closingFromWindow = true;

      if (_inGentleRemovingFromParent)
      {
        await Owner.DeactivateAsync(close: false); // можно попробовать и без этого, но перемещение с предварительной деактивацией представляется более надежным
      }
      else
      {
        await Owner.DeactivateAsync(close: true);
      }

      _closingFromWindow = false;
    }
    finally
    {
      _closeCompletionSource?.SetResult(true);
    }


    /*
    if (Feature.John.ChopTheDead.IsEnabled)
    {
      _window = null;
    }
    */

    _window = null;
  }

  private Task OwnerDeactivated(object sender, DeactivationEventArgs e)
  {
    if (!e.WasClosed)
    {
      return Task.CompletedTask;
    }
    Owner.Deactivated -= OwnerDeactivated;

    if (_closingFromWindow)
    {
      return Task.CompletedTask;
    }

    _closingFromModel = true;
    _actuallyClosing = true;

    _useCloseGuard = false;

    _window.Close();

    _actuallyClosing = false;
    _closingFromModel = false;

    return Task.CompletedTask;
  }


  public async Task ToMainWindow() // Caliburn here
  {
    await Infr.Shell.MoveWorkspaceHolder(Owner, WorkspaceOwnerType.ShellTabs); // Infr.Shell.ToggleWorkspaceHolder(_owner);
  }

  public async Task ToMainWindowAsync()
  {
    await Infr.Shell.MoveWorkspaceHolder(Owner, WorkspaceOwnerType.ShellTabs);
  }


#if DEBUG
  ~ExtraWindowController()
  {
    AppConsole.WriteTopic(MessageTopics.MemoryLeak, 10, $@"The MetroExtraWindowController destructor is executing.");
  }
#endif

}

