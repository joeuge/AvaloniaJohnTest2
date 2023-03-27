using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Caliburn.Micro;
using AppNs.CoreNs;
using AppNs.Interfaces;
using Iface.Utils;

namespace AppNs.Windows;

/* Для того чтобы DoubleClick на шапке не максимизировал окно есть две опции:
 * 1. window.ResizeMode = ResizeMode.NoResize
 * 2. Реализовать шапку (XAML) с использованием telerik:WindowHost.HitTestable="True" // справка: на элементах типа Grid это не работает
*/

[SingletonInstance]
[Contract(typeof(IGlobalModalService))]

public partial class WindowModalService : IGlobalModalService, IDialogServiceInternal
{
  private readonly IInfrastructure _infrastructure;
  ScreenOwnerType IDialogServiceInternal.ScreenOwnerType => ScreenOwnerType.Window;


  Task IDialogServiceInternal.OnCloseDialogAsync(IDialogProxyInternal dialogProxy)
  {
    return Task.CompletedTask;
  }


  public WindowModalService(IInfrastructure infrastructure)
  {
    _infrastructure = infrastructure;
  }


  public async Task<bool> ShowAsync(IDialog dialog,
    Action<IDialogProxy>? prepareDialog = null,
    Func<bool>? onOk = null)
  {
    var dialogProxy = new DialogProxy<IDialog>(this, dialog, out var task, onOk);
    await ShowCore(dialog, dialog.ViewContext, prepareDialog, dialogProxy);
    return await task;
  }

  // +viewContext

  public async Task<bool> ShowAsync(IDialog dialog, object viewContext,
    Action<IDialogProxy>? prepareDialog,
    Func<bool>? onOk = null)
  {
    var dialogProxy = new DialogProxy<IDialog>(this, dialog, out var task, onOk);
    await ShowCore(dialog, viewContext, prepareDialog, dialogProxy);
    return await task;
  }


  public async Task<bool> ShowAsync(IDialog dialog, string title)
  {
    var dialogProxy = new DialogProxy<IDialog>(this, dialog, out var task, null);
    await ShowCore(dialog, dialog.ViewContext, proxy => proxy.Caption = title, dialogProxy);
    return await task;
  }


  private async Task<bool> ShowCore(IDialog dialog, object viewContext, Action<IDialogProxy> prepareDialog, IDialogProxyInternal dialogProxy)
  {
    //dlg.MemContext = context;

    var disposeAfterClose = dialog.IsDisposeOnModalWindowClose;

    prepareDialog?.Invoke(dialogProxy);

    // autoModel будет создана далее
    //---------------------------
    var window = CreateWindowCore(out var windowOwner, dialog, viewContext, null, dialogProxy, restrictToMainWindow: false);
    //---------------------------
    var conductor = new WindowConductor((IDialogInternal)dialog, window, dialogProxy, disposeAfterClose);
    await conductor.InitializeAsync(); // -> rootModel.Activate();
    //---------------------------

    var dialogResult = false;

    //_infrastructure.BeginModalDialog();
    try
    {
      dialog.OnTestShowDialogBefore();
      //-----------------
      var dr = await window.ShowDialog<bool?>(windowOwner);
      //-----------------
      if ((dr ?? false) != dialog.DialogResult)
      {
      }
      //dialogResult = window.DialogResult ?? false;
      //dialogResult = dr ?? false;
      dialogResult = dialog.DialogResult;
      dialog.OnTestShowDialogAfter();
    }
    finally
    {
      //_infrastructure.EndModalDialog();
    }


    await dialogProxy.CloseAsync(dialogResult, windowIsClosed: true);
    return dialogResult;
  }

  private Window CreateWindowCore(out Window windowOwner, IDialog dialog, object viewContext
    , IDictionary<string, object>? settings
    , IDialogProxyInternal dialogProxy
    , bool restrictToMainWindow)
  {
    // window.Content = view, window.Style = style
    //--------------------------- 
    var window = EnsureWindow(out windowOwner, dialog, ViewLocator.LocateForModel(dialog, null, viewContext), restrictToMainWindow, dialogProxy.DialogOptions);
    //---------------------------

    ViewModelBinder.Bind(dialog, window, viewContext);
    // -> window.DataContext = rootModel // see Action.SetTarget(view, viewModel);
    // -> view2 = GetFirstNonGeneratedView(window) вернет Window.Content в качестве View
    // -> apply conventions for view2

    //---------------------------
    var autoModel = CreateAutoModel(dialogProxy);
    //---------------------------
    //window.Header = autoModel;
    //---------------------------
    dialogProxy.DialogAutoModel = autoModel; // страховка от GC

    if (false)
    {
      window.Bind(Layoutable.HeightProperty, new Binding("WindowHeight") { Source = autoModel, Mode = BindingMode.TwoWay });
      window.Bind(Layoutable.WidthProperty, new Binding("WindowWidth") { Source = autoModel, Mode = BindingMode.TwoWay });
    }
    else
    {
      window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
      window.Width = 400;
      window.Height = 300;
    }

    if (!ConventionManager.HasBinding(window, Window.TitleProperty))
    {
      var titleGot = false;
      if (dialogProxy?.Caption != null)
      {
        window.Title = dialogProxy.Caption;
        titleGot = true;
      }
      if (!titleGot)
      {
        var binding = new Binding("FullDisplayName") { Mode = BindingMode.OneWay };
        window.Bind(Window.TitleProperty, binding);
      }
    }

    window.SizeToContent = SizeToContent.WidthAndHeight; // WPF: has effect only while Width=Height=NaN // Avalonia: Width,Height игнорируются
    window.SizeToContent = SizeToContent.Manual;
    window.CanResize = (dialogProxy?.ResizeMode ?? CoreDefaults.DialogResizeMode) != ResizeMode.NoResize;
    //window.CaptionHeight = 8; // зона чувствительности (drag, dbl-click)

    // эти опции в текущей реализации ControlTemplate не используются
    //window.HideMinimizeButton = true; 
    //window.HideMaximizeButton = true;

    if (settings != null)
    {
      ApplySettings(window, settings);
    }

    return window;
  }

  private DialogAutoModel CreateAutoModel(IDialogProxyInternal dialogProxy)
  {
    var autoModel = new DialogAutoModel(DialogGenesis.GlobalModal, dialogProxy, onContentLoaded: null);
    return autoModel;
  }


  private Window EnsureWindow(out Window windowOwner, object rootModel, object view, bool restrictToMainWindow, DialogOptions dialogOptions)
  {
    if (!(view is Window window))
    {
      window = new DialogWindow // Window
      {
        Content = view,
      };

      //var styleKey = dialogOptions.IsToolsOverDocument ? "OverlayModalRadWindowStyle" : "ModalRadWindowStyle";
      //var style = (Style)Application.Current.FindResource(styleKey);

      /*
      var style = (Style)Application.Current.FindResource("ModalRadWindowStyle");
      if (style != null)
      {
        window.Style = style;
      }
      */

      window.SetValue(View.IsGeneratedProperty, true); // Used in Caliburn View.GetFirstNonGeneratedView()
      window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }
    windowOwner = InferOwnerOf(window, restrictToMainWindow);
    return window;
  }

  private Window InferOwnerOf(Window window, bool restrictToMainWindow)
  {
    var windowOwner = _infrastructure.MainWindow;

    if (!restrictToMainWindow)
    {
      var active = _infrastructure.Lifetime?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
      if (active != null)
        windowOwner = active;
    }

    if (windowOwner == null || ReferenceEquals(windowOwner, window))
      throw new Exception("ERROR");

    return windowOwner;
  }

  private void ApplySettings(object target, IEnumerable<KeyValuePair<string, object>> settings)
  {
    if (settings == null)
      return;
    var type = target.GetType();
    foreach (var setting in settings)
    {
      var property = type.GetProperty(setting.Key);
      if (property != null)
        property.SetValue(target, setting.Value, null);
    }
  }
    
  //====================================
  private class WindowConductor
  {
    private readonly Window _window;
    private readonly IDialogInternal _dialog;
    private readonly IGuardClose? _closeGuard;
    private readonly IDeactivate? _deactivate;
    private readonly IDialogSupport? _dialogSupport;
    private readonly IDialogProxy _dialogProxy;
    private bool _closingFromWindow;
    private bool _closingFromModel;
    private bool _actuallyClosing;
    private readonly bool _disposeAfterClose;

    public WindowConductor(IDialogInternal dialog, Window window, IDialogProxy dialogProxy, bool disposeAfterClose)
    {
      _dialog = dialog;
      _closeGuard = dialog as IGuardClose;
      _dialogSupport = dialog as IDialogSupport;
      _deactivate = dialog as IDeactivate;
      _window = window;
      _dialogProxy = dialogProxy;
      _disposeAfterClose = disposeAfterClose;
    }

    public async Task InitializeAsync()
    {
      if (_dialog is IActivate activate)
      {
        await activate.ActivateAsync();
      }

      if (_deactivate != null)
      {
        _deactivate.Deactivated += ModelDeactivated;
      }

      _window.Closing += WindowClosing; //window.PreviewClosed += WindowClosing;
      _window.Closed += WindowClosed;

      _window.KeyDown += Window_KeyDown;
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e) // todo: test async
    {
      //AppConsole.WriteLine(MessageAspects.Jo1, "***************  Window_KeyDown **********************");

      if (e.Key == Key.Escape)
      {
        if (_dialogProxy.DialogOptions.CloseWhenEscape)
        {
          CloseDialogAsync(false);
        }
        e.Handled = true; // WPF: не оставляем шансов Button.IsCancel, AccessKeyManager, InputManager.PostProcessInput Event
      }

      else if (e.Key == Key.Enter)
      {
        if (_dialogProxy.DialogOptions.CloseWhenEnter)
        {
          CloseDialogAsync(true);
        }
        e.Handled = true; // WPF: не оставляем шансов Button.IsDefault, AccessKeyManager, InputManager.PostProcessInput Event
      }

      else if (e.Key == Key.Tab)
      {
      }
    }

    private async Task CloseDialogAsync(bool dialogResult)
    {
      if (dialogResult)
      {
        await _dialog.DoOkAsync();
      }
      else
      {
        await _dialog.DoCancelAsync();
      }
    }

    private async void WindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
      if (e.Cancel)
        return;

      if (_actuallyClosing)
      {
        _actuallyClosing = false;
        return;
      }

      if (_dialog.DialogResult) // if (_window.DialogResult == true)
      {
        try
        {
          if (_dialogSupport != null)
          {
            _dialogSupport.OnOkBegin();
            if (_dialogSupport.HasErrors())
            {
              e.Cancel = true;
              return;
            }
          }

          if (_dialogProxy?.OnOk != null)
          {
            if (!_dialogProxy.OnOk())
            {
              e.Cancel = true;
              return;
            }
          }

          if (_dialogSupport != null)
          {
            if (_dialogSupport.IsNoExitByOk)
            {
              e.Cancel = true;
              return;
            }
          }

        }
        catch (Exception exception)
        {
          if (_dialogSupport == null || !_dialogSupport.ShowException(exception)) // todo async
          {
            //MessageBox.Show(exception.Message); // todo 123
          }
          e.Cancel = true;
          return;
        }
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

    /*
    private void WindowClosing(object sender, WindowPreviewClosedEventArgs e)
    {
      if (e.Cancel ?? false)
        return;

      if (_actuallyClosing)
      {
        _actuallyClosing = false;
        return;
      }

      if (_window.DialogResult == true)
      {
        try
        {
          if (_dialogSupport != null)
          {
            _dialogSupport.OnOkBegin();
            if (_dialogSupport.HasErrors())
            {
              e.Cancel = true;
              return;
            }
          }

          if (_dialogProxy?.OnOk != null)
          {
            if (!_dialogProxy.OnOk())
            {
              e.Cancel = true;
              return;
            }
          }

          if (_dialogSupport != null)
          {
            if (_dialogSupport.IsNoExitByOk)
            {
              e.Cancel = true;
              return;
            }
          }

        }
        catch (Exception exception)
        {
          if (_dialogSupport == null || !_dialogSupport.ShowException(exception))
          {
            //MessageBox.Show(exception.Message); // todo
          }
          e.Cancel = true;
          return;
        }

      }

      if (_closeGuard == null)
        return;

      var runningAsync = false;
      var shouldEnd = false;

      _closeGuard.CanClose(canClose => ((System.Action)(() =>
      {
        if (runningAsync && canClose)
        {
          _actuallyClosing = true;
          _window.Close(); // #tag#info# заключительный Close
        }
        else
          e.Cancel = !canClose;
        shouldEnd = true;
      })).OnUIThread());

      if (shouldEnd)
        return;

      runningAsync = true;
      e.Cancel = true; // #tag#info# ждем заключительный Close
    }
    */

    private async void WindowClosed(object? sender, EventArgs e)
    {
      _window.Closed -= WindowClosed;
      _window.Closing -= WindowClosing;
      _window.KeyDown -= Window_KeyDown;
      //_window.PreviewMouseDown -= Window_PreviewMouseDown;

      if (_deactivate != null)
        _deactivate.Deactivated -= ModelDeactivated;

      if (_closingFromModel)
        return;

      if (_deactivate == null)
        return;

      _closingFromWindow = true;
      await _deactivate.DeactivateAsync(close: _disposeAfterClose); // true: документ разрушается после вызова модального диалога // #john#info# повторное использование документа
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

      _closingFromModel = true;
      _actuallyClosing = true;

      _window.Close(_dialog.DialogResult);

      _actuallyClosing = false;
      _closingFromModel = false;

      return Task.FromResult(true); // why not Task.CompletedTask!
    }

  }
}