using System.ComponentModel;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Caliburn.Micro;
using AppNs.Interfaces;
using Iface.Utils;

namespace AppNs.Windows;

[SingletonInstance]
[Contract(typeof(IWindowService))]

public class WindowService : IWindowService
{
  // используется для вызова главного окна
  public async Task<Window> PrepareWindowAsync(object rootModel, IDictionary<string, object>? settings = null)
  {
    var controller = await CreateControllerAsync(rootModel, false, false, settings);
    return controller.Window;
  }

  // 
  public async Task<Window> ShowWindowAsync(object rootModel, bool setMainWindowAsOwner = true, IDictionary<string, object>? settings = null)
  {
    var controller = await CreateControllerAsync(rootModel, false, setMainWindowAsOwner, settings);
    controller.ShowWindow();
    return controller.Window;
  }

  // используется для начального вызова Login Form
  public async Task<bool?> ShowUnmodalAndGetResult(object rootModel, bool setMainWindowAsOwner = true, IDictionary<string, object>? settings = null)
  {
    var controller = await CreateControllerAsync(rootModel, true, setMainWindowAsOwner, settings);
    return await controller.ShowUnmodalAndGetResult();
  }

  // используется для повторного вызова Login Form
  public async Task<bool?> ShowModal(object rootModel, bool setMainWindowAsOwner = true, IDictionary<string, object>? settings = null)
  {
    var controller = await CreateControllerAsync(rootModel, true, setMainWindowAsOwner, settings);
    return await controller.ShowModal();
  }


  private static async Task<WindowController> CreateControllerAsync(object rootModel, bool isDialog, bool setMainWindowAsOwner
    , IDictionary<string, object>? settings)
  {
    var view = ViewLocator.LocateForModel(rootModel, null, null);

    WindowController controller;
    switch (view)
    {
      case Window window:
        var f1 = window.IsVisible;
        var f2 = window.IsActive;
        var f3 = window.IsInitialized;
        controller = new WindowController(rootModel, window, isDialog, setMainWindowAsOwner);
        break;
      default:
        throw new Exception();
    }
    await controller.InitializeAsync(settings);
    return controller;
  }

  private static void ApplySettings(object target, IEnumerable<KeyValuePair<string, object>> settings)
  {
    //if (settings == null) return;
    var type = target.GetType();
    foreach (var setting in settings)
    {
      var property = type.GetProperty(setting.Key);
      if (property != null)
        property.SetValue(target, setting.Value, null);
    }
  }


  //====================================
  private class WindowController 
  {
    private readonly Window _window;
    private readonly object _rootModel;
    private readonly IGuardClose? _closeGuard;
    private readonly IDeactivate? _deactivate;
    private bool _closingFromWindow;
    private bool _closingFromModel;
    private bool _actuallyClosing;
    private readonly bool _isDialog;
    private readonly bool _setMainWindowAsOwner;

    public Window Window => _window;

    public WindowController(object rootModel, Window window, bool isDialog, bool setMainWindowAsOwner)
    {
      _rootModel = rootModel;
      _closeGuard = rootModel as IGuardClose;
      _deactivate = rootModel as IDeactivate;
      _window = window;
      _isDialog = isDialog;
      _setMainWindowAsOwner = setMainWindowAsOwner;
    }

    public async Task InitializeAsync(IDictionary<string, object>? settings)
    {
      //_window.ShowInTaskbar = true;

      if (_rootModel is IActivate activate)
      {
        await activate.ActivateAsync();
      }

      _window.Closed += WindowClosed;
        
      if (_deactivate != null)
      {
        _deactivate.Deactivated += ModelDeactivated;
      }

      if (_closeGuard != null)
      {
        _window.Closing += WindowClosing;
      }

      ViewModelBinder.Bind(_rootModel, _window, null); // -> GetFirstNonGeneratedView(window) вернет Window.Content в качестве View

      if (_rootModel is IHaveDisplayName && !ConventionManager.HasBinding(_window, Window.TitleProperty))
      {
        var binding = new Binding("DisplayName") { Mode = BindingMode.TwoWay };
        _window.Bind(Window.TitleProperty, binding);
      }

      if (settings != null)
      {
        ApplySettings(_rootModel, settings);
        ApplySettings(_window, settings);
      }
    }
      
    private async void WindowClosing(object? sender, CancelEventArgs e)
    {
      if (e.Cancel)
        return;

      if (_actuallyClosing)
      {
        _actuallyClosing = false;
        return;
      }

      if (_closeGuard == null)
        return;

      // исходный вызов сбрасываем
      e.Cancel = true; 

      await Task.Yield();

      var canClose = await _closeGuard.CanCloseAsync(CancellationToken.None);

      if (!canClose)
        return;

      // повторный вызов
      _actuallyClosing = true;
      _window.Close(); // _window._dialogResult не трогается // остается прежним от исходного вызова
    }

    private async void WindowClosed(object? sender, EventArgs e)
    {
      _window.Closed -= WindowClosed;
      _window.Closing -= WindowClosing;

      if (_closingFromModel)
        return;

      if (_deactivate == null)
        return;

      _closingFromWindow = true;
      await _deactivate.DeactivateAsync(true);
      _closingFromWindow = false;
    }

    private Task ModelDeactivated(object sender, DeactivationEventArgs e)
    {
      if (!e.WasClosed)
      {
        return Task.FromResult(false); // why not Task.CompletedTask!
      }

      _deactivate.Deactivated -= ModelDeactivated;

      if (_closingFromWindow)
      {
        return Task.FromResult(true); // why not Task.CompletedTask!
      }

      // Вряд ли в этом сервисе нам нужна эта ветка

      _closingFromModel = true;
      _actuallyClosing = true;

      _window.Close(); // todo: _window.Close(_rootModel.DialogResult)

      _actuallyClosing = false;
      _closingFromModel = false;

      return Task.FromResult(true); // why not Task.CompletedTask!
    }



    public void ShowWindow()
    {
      var mainWindow = GetMainWindow();

      if (mainWindow == null)
      {
        //(Application.Current.ApplicationLifetime as ClassicDesktopStyleApplicationLifetime).MainWindow = _window;
      }

      var ownerWindow = _setMainWindowAsOwner && !ReferenceEquals(mainWindow, _window) ? mainWindow : null;

      if (ownerWindow != null)
      {
        _window.Show(ownerWindow);
      }
      else
      {
        _window.Show();
      }
    }

    public async Task<bool?> ShowModal()
    {
      var ownerWindow = GetMainWindow();
      return await _window.ShowDialog<bool?>(ownerWindow);
    }


    public Task<bool?> ShowUnmodalAndGetResultXxx()
    {
      var taskCompletionSource = new TaskCompletionSource<bool?>();

      Observable.FromEventPattern<EventHandler, EventArgs>(
          x => _window.Closed += x,
          x => _window.Closed -= x)
        .Take(1)
        .Subscribe(_ =>
        {
          if (_rootModel is IHaveDialogResult haveDialogResult)
          {
            if (haveDialogResult.DialogResult is bool dialogResult)
            {
              taskCompletionSource.SetResult(dialogResult);
            }
            else
            {
              taskCompletionSource.SetResult(null);
            }
          }
        });

      ShowWindow();
      return taskCompletionSource.Task;
    }


    private TaskCompletionSource<bool?> _taskCompletionSource;
      
    public Task<bool?> ShowUnmodalAndGetResult()
    {
      _taskCompletionSource = new TaskCompletionSource<bool?>();
      _window.Closed += WindowClosed2;

      ShowWindow();
        
      return _taskCompletionSource.Task;
    }

    private void WindowClosed2(object? sender, EventArgs e)
    {
      _window.Closed -= WindowClosed2;
      if (_rootModel is IHaveDialogResult haveDialogResult)
      {
        if (haveDialogResult.DialogResult is bool dialogResult)
        {
          _taskCompletionSource.SetResult(dialogResult);
          return;
        }
      }
      _taskCompletionSource.SetResult(null);
    }


    private Window? GetMainWindow()
    {
      return (Application.Current?.ApplicationLifetime as ClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }
  }

}