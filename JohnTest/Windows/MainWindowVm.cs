using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Caliburn.Micro;
using AppNs.Interfaces;
using AppNs.UiBlocks.ContextMenuNs;
using Iface.Utils;

namespace AppNs.Windows;

[ViewType(typeof(MainWindow))]
[SingletonInstance]
[Contract(typeof(IMainWindowViewModel))]

public class MainWindowVm : Conductor<IShell>, IMainWindowViewModel
{
  private readonly IInfrastructureInternal _infr;
  public IShell Shell { get; }

  private IMainWindowView _view;
  private WindowState _windowState = WindowState.Maximized;

  public WindowState WindowState
  {
    get => _windowState;
    set
    {
      if (value == WindowState.Normal)
      {
      }
      _windowState = value;
      NotifyOfPropertyChange(() => WindowState);
    }
  }

  private ResizeMode _resizeMode = ResizeMode.CanResize;

  public ResizeMode ResizeMode
  {
    get => _resizeMode;
    set
    {
      _resizeMode = value;
      NotifyOfPropertyChange(() => ResizeMode);
    }
  }

  private double _width = 1000.0;

  public double Width
  {
    get => _width;
    set
    {
      _width = value;
      NotifyOfPropertyChange(() => Width);
    }
  }

  private double _height = 800.0;

  public double Height
  {
    get => _height;
    set
    {
      _height = value;
      NotifyOfPropertyChange(() => Height);
    }
  }

  private double _left;

  public double Left
  {
    get => _left;
    set
    {
      _left = value;
      NotifyOfPropertyChange(() => Left);
    }
  }

  private double _top;

  public double Top
  {
    get => _top;
    set
    {
      _top = value;
      NotifyOfPropertyChange(() => Top);
    }
  }

  private string _title = "Клиент10 «ОИК Диспетчер НТ»";

  public string Title
  {
    get => _title;
    set
    {
      _title = value;
      NotifyOfPropertyChange(() => Title);
    }
  }

  public MainWindowVm(IInfrastructure infr, IShell shell)
  {
    _infr = (IInfrastructureInternal)infr;
    Shell = shell;

    DisplayName = "Main Window";
  }


  public async Task ActivateShell()
  {
    _width = 400d;
    _height = 300d;
    _windowState = WindowState.Normal;

    RegisterAppsContextMenu();

    await ActivateItemAsync(Shell);
  }


  protected override void OnViewLoaded(object view)
  {
    //base.OnViewLoaded(view);

    _view = view as IMainWindowView;
    _view?.OnModelLoaded(this);
  }

  public void DoStep1()
  {
    _view.DoStep1();
  }

  public void DoStep2()
  {
    _view.DoStep2();
  }

  #region AppsContextMenu

  public IObservableCollection<ItemBase> AppsContextMenu { get; } = new BindableCollection<ItemBase>();


  private void RegisterAppsContextMenu()
  {
  }

  private void AddAppsContextMenuItem(string name,
                                      ICommand command)
  {
    AppsContextMenu.Add(new CommandItem
    {
      DisplayName = name,
      Command = command,
    });
  }

  #endregion

}