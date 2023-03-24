using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Layout;
using Caliburn.Micro;
using AppNs.Interfaces;
using Iface.Utils;
using Iface.Utils.Avalonia;

namespace AppNs.CoreNs;

public abstract class Dialog : Screen, IDialogInternal, IViewLocatorAssistant, IDialogSupport, IDataErrorInfo
  , IHaveDialogResult

{
  public IInfrastructure Infrastructure { get; }
  public bool InDisposing { get; protected set; }
  public bool IsDisposeOnModalWindowClose { get; set; } = true; // added 16 dec 2019 // #john#info# повторное использование документа

  private ICommand _uiCloseCommand;
  public ICommand UiCloseCommand => _uiCloseCommand ?? (_uiCloseCommand = CreateUiCloseCommand());
  protected virtual ICommand CreateUiCloseCommand()
  {
    return new SimpleCommand(async p => await TryCloseAsync(null), p => true);
  }
    
  //private VarKey _docId;
  private string _displayBaseName;
  private string _description;
  private bool _isViewLoaded;
  private bool _visible = true;
  private double _width = Double.NaN; // = Auto
  private double _height = Double.NaN; // = Auto
  /* 
  Если HorizontalAlignment, VerticalAlignment = Stretch, то
    1) Workspace Overlay: все пространство
    2) Workspace Modal Mode: все пространство
  */
  private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Stretch;
  private VerticalAlignment _verticalAlignment = VerticalAlignment.Stretch;

  //public DocumentRoles PotentialDocumentRoles { get; set; }

  public virtual bool IsEmptyClass => false;




  #region Tools для внедрения в ModalScreen, WindowModalService

  public virtual ICustomTools TryGetToolsForModalDialog()
  {
    return null;
  }

  #endregion


  private bool _isModified;

  public bool IsModified
  {
    get => _isModified;
    set
    {
      if (value == _isModified)
        return;
      _isModified = value;
      OnIsModifiedChanged();
    }
  }

  protected void SetIsModifiedProtected(bool value)
  {
    _isModified = value;
  }

  protected virtual void OnIsModifiedChanged()
  {
    NotifyOfPropertyChange();
  }





  private string _content;
  private string _okButtonContent = "Ok";
  private string _cancelButtonContent = "Cancel";
  private List<IDialogChildScreen>? _childScreens;

  public string Content // например, заголовок для поля ввода
  {
    get => _content;
    set => this.SetPropertyValue(ref _content, value);
  }

  public string OkButtonContent
  {
    get => _okButtonContent;
    set => this.SetPropertyValue(ref _okButtonContent, value);
  }

  public string CancelButtonContent
  {
    get => _cancelButtonContent;
    set => this.SetPropertyValue(ref _cancelButtonContent, value);
  }

  private Func<object, bool> _validationHandler;

  public T SetValidationHandler<T>(Func<T, bool> validationHandler) where T : class, IDialog
  {
    if (validationHandler != null)
    {
      _validationHandler = doc => validationHandler(this as T);
    }
    return this as T;
  }






  #region Ctor+

  public bool IsContentLocked { get; private set; }

  public void LockContent()
  {
    IsContentLocked = true;
    OnContentLocked();
  }

  protected virtual void OnContentLocked()
  { }


  protected Dialog()
  {
    if (Execute.InDesignMode) return;

    Infrastructure = IoC.Get<IInfrastructure>();
    //PotentialDocumentRoles = DocumentRoles.Regular;

    HorizontalAlignment = HorizontalAlignment.Center;
    VerticalAlignment = VerticalAlignment.Center;

    OnBaseCreated();
  }

  protected virtual void OnBaseCreated()
  {
    SetDefaultSize(false);
  }

  public virtual void SetDefaultSize(bool notify)
  {
    _width = Double.NaN; // = Auto
    _height = Double.NaN; // = Auto
    if (!notify) return;
    NotifyOfPropertyChange(() => Width);
    NotifyOfPropertyChange(() => Height);
    NotifyOfPropertyChange(() => Size);
  }

  public void SetAutoSize(bool notify)
  {
    _width = Double.NaN; // = Auto
    _height = Double.NaN; // = Auto
    if (!notify) return;
    NotifyOfPropertyChange(() => Width);
    NotifyOfPropertyChange(() => Height);
    NotifyOfPropertyChange(() => Size);
  }


  protected object View { get; private set; }

  protected override void OnViewAttached(object view, object context) // Screen override
  {
    if (View != null && !ReferenceEquals(view, View))
      throw new SystemException("ERROR");

    View = view;
    Requires.Reference.NotNull(View, "View");
  }

  public override object GetView(object context = null)
  {
    return View;
  }

  protected override async void OnViewLoaded(object view) // Screen override // Вызывается только один раз!!! by IPlatformProvider.ExecuteOnFirstLoad()
  {
    _isViewLoaded = true;

    var h = ViewLoaded;
    h?.Invoke(this, EventArgs.Empty);

    await ViewLoadedHook();
  }

  protected virtual Task ViewLoadedHook()
  {
    return Task.CompletedTask;
  }

  protected override Task OnInitializeAsync(CancellationToken cancellationToken)
  {
    return Task.FromResult(true);
  }

  protected override async Task OnActivateAsync(CancellationToken cancellationToken)
  {
    if (_childScreens != null)
    {
      foreach (var screen in _childScreens)
      {
        await screen.ActivateAsync(cancellationToken);
      }
    }
  }

  protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
  {
    //_childScreens?.ForEach(screen => screen.Deactivate(close));

    if (_childScreens != null)
    {
      foreach (var screen in _childScreens)
      {
        await screen.DeactivateAsync(close, cancellationToken);
      }
    }

    if (close)
    {
      InDisposing = true;
    }

    await base.OnDeactivateAsync(close, cancellationToken);

    if (close)
    {
      FinalDispose();
    }
  }

  public override async Task TryCloseAsync(bool? dialogResult = null)
  {
    if (InDisposing)
    {
      return;
    }

    if (Parent is IDialogProxyInternal dialogProxy)
    {
      var isHandled = await dialogProxy.OverrideDialogTryClose(dialogResult ?? false);
      if (isHandled)
        return;
    }

    // see XamlPlatformProvider.GetViewCloseAction() // ищет method "Close", property "DialogResult", property "IsOpen"
    await base.TryCloseAsync(dialogResult); // PlatformProvider.Current.GetViewCloseAction(this, Views.Values, dialogResult).OnUIThread();
  }

  public override Task<bool> CanCloseAsync(CancellationToken cancellationToken = default)
  {
    return Task.FromResult(true);
  }



  #endregion


  // raise on UI Thread
  public EventHandlerCollection ContentLoadedEvent { get; } = new EventHandlerCollection(); // added 10.03.2022


  #region Life Cycle Virtual Methods

  // added 27 sep 2019
  protected virtual void OnShowBefore()
  {
    _childScreens?.Clear();
    RegisterChildScreens();
    _childScreens?.ForEach(screen => screen.OnShowBefore());
  }

  protected virtual void RegisterChildScreens()
  {
  }

  protected void RegisterChildScreen(IDialogChildScreen screen)
  {
    if (screen == null) return;

    if (_childScreens == null)
    {
      _childScreens = new List<IDialogChildScreen>();
    }

    _childScreens.Add(screen);
  }

  // added 27 sep 2019
  protected virtual void OnShowAfter(bool dialogResult)
  {
    _childScreens?.ForEach(screen => screen.OnShowAfter(dialogResult));
    _childScreens?.Clear();
  }

  #endregion



  #region События, ожидание состояния

  private event EventHandler ViewLoaded; // = delegate { };

  public bool IsViewLoaded => _isViewLoaded;


  public Task WaitViewLoadedAsync()
  {
    var taskSource = new TaskCompletionSource<object>();

    if (_isViewLoaded)
    {
      taskSource.SetResult(null);
    }
    else
    {
      void WrapperFunc(object s, EventArgs e)
      {
        ViewLoaded -= WrapperFunc;
        taskSource.SetResult(null);
      }
      ViewLoaded += WrapperFunc;
    }
    return taskSource.Task;
  }

  public Task WaitIsActiveAsync()
  {
    var taskSource = new TaskCompletionSource<object>();

    if (IsActive)
    {
      taskSource.SetResult(null);
    }
    else
    {
      void WrapperFunc(object s, ActivationEventArgs e)
      {
        Activated -= WrapperFunc;
        taskSource.SetResult(null);
      }
      Activated += WrapperFunc;
    }
    return taskSource.Task;
  }


  #endregion



  public virtual bool TryInitializeFocus()
  {
    if (View is IFocusSupport focusSupport)
    {
      if (focusSupport.TryInitializeFocus())
        return true;
    }

    if (_childScreens != null)
    {
      if (_childScreens.Any(screen => screen.TryInitializeFocus()))
      {
        return true;
      }
    }

    return false;
  }


  #region IDialogSupport

  public bool IsNoExitByOk { get; protected set; } // true = (Ok -> Apply and Stays in dialog)

  // Метод вызывается дважды 1) in DoOk() before TryClose(true) 2) inside TryClose(true), see ModalService: WindowConductor.WindowClosing()
  public virtual void OnOkBegin() // закрыть TextEdit, например
  {
    _childScreens?.ForEach(screen => screen.OnOkBegin());

    if (View is IDialogSupport dialogSupport)
    {
      dialogSupport.OnOkBegin();
    }
  }

  // Метод вызывается дважды 1) in DoOk() before TryClose(true) 2) inside TryClose(true), see ModalService: WindowConductor.WindowClosing()
  public virtual bool HasErrors() // true = прервать выход по Ok
  {
    if (_childScreens != null)
    {
      if (_childScreens.Any(screen => screen.HasErrors()))
      {
        return true;
      }
    }

    if (View is IDialogSupport dialogSupport)
    {
      return dialogSupport.HasErrors();
    }
    return false;
  }

  // метод, который вызывается еще до вызова TryClose(true)
  protected virtual Task<bool> ValidateOk() // false = прервать выход по Ok. Также можно поднять Exception.
  {
    return (_validationHandler == null || _validationHandler(this)) ? Task.FromResult(true) : Task.FromResult(false);
  }

  public virtual bool ShowException(Exception exception) // false = возможность отображения не реализована
  {
    if (View is IDialogSupport dialogSupport)
    {
      return dialogSupport.ShowException(exception);
    }
    return false; // возможность отображения не реализована
  }

  #endregion

  public async Task DoOkAsync()
  {
    //AppConsole.WriteLine(MessageAspects.Jo1, "@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ DoOk()");
    try
    {
      OnOkBegin();

      if (HasErrors())
        return;

      if (!await ValidateOk())
        return;
    }
    catch (Exception exception)
    {
      if (!ShowException(exception))
      {
        //MessageBox.Show(exception.Message); // todo
      }
      return;
    }

    await TryCloseAsync(true); // это не гарантирует закрытие // see CanClose

    OnTryCloseTrueDone();
  }

  // странный метод, который вызывается после вызова TryClose(true), так как сам по себе TryClose(true) ничего не гарантирует
  protected virtual void OnTryCloseTrueDone()
  {
  }

  public async Task DoCancelAsync()
  {
    await TryCloseAsync(false);
  }

  #region IDataErrorInfo
  // для нормальной валидации нужно:
  //  - override string GetError(string propertyName)
  //  - override bool HasErrors()
  //  - WPF: <TextBox Text="{Binding Value, ValidatesOnDataErrors=true}"

  string IDataErrorInfo.this[string columnName] => GetError(columnName);
  string IDataErrorInfo.Error => null; // WPF это не использует, WinForms - да

  protected virtual string GetError(string propertyName)
  {
    return null;
  }

  #endregion








  public bool DialogResult { get; private set; }

  bool IDialogInternal.DialogResult
  {
    get => DialogResult;
    set => DialogResult = value;
  }

  object? IHaveDialogResult.DialogResult
  {
    get => DialogResult ? BooleanBoxes.TrueBox : BooleanBoxes.FalseBox;
    set => DialogResult = value is true;
  }

  public virtual DialogOptions GetDialogOptions()
  {
    return DialogOptions.Default;
  }

  public virtual ResizeMode ResizeMode => CoreDefaults.DialogResizeMode;

  public virtual void ApplyNormalDialogSize()
  {
  }

  public virtual void AfterApplyNormalDialogSize()
  {
  }



  public virtual void OnTestShowDialogBefore() { }
  public virtual void OnTestShowDialogAfter() { }

  #region Identity & So (Content-Load-Facility) 


  public override string DisplayName
  {
    get => GetShortDisplayName();
    set => SetDisplayBaseName(value, true);
  }

  public string DisplayBaseName
  {
    get => _displayBaseName;
    set => SetDisplayBaseName(value, true);
  }

  public string ShortDisplayName => GetShortDisplayName();
  public string FullDisplayName => GetFullDisplayName();

  protected virtual void SetDisplayBaseName(string value, bool notify)
  {
    if (value == _displayBaseName) return;
    _displayBaseName = value;
    if (!notify) return;
    NotifyOfPropertyChange(() => DisplayName);
    NotifyOfPropertyChange(() => ShortDisplayName);
    NotifyOfPropertyChange(() => FullDisplayName);
  }

  protected virtual string GetShortDisplayName() { return _displayBaseName; }
  public virtual string GetFullDisplayName() { return GetShortDisplayName(); }

  public virtual void NotifyOfDisplayPropertiesChange()
  {
    NotifyOfPropertyChange(() => DisplayName);
    NotifyOfPropertyChange(() => ShortDisplayName);
    NotifyOfPropertyChange(() => FullDisplayName);
  }

  public virtual string ViewContext { get; set; }
  public Type ViewType { get; set; }

  public bool UseViewLocatorAssistant { get; set; }
  bool IViewLocatorAssistant.Enabled => UseViewLocatorAssistant;
  Type IViewLocatorAssistant.ViewType => ViewType;
  object IViewLocatorAssistant.ViewContext => ViewContext;

  protected void OverrideViewType(Type viewType)
  {
    ViewType = viewType;
    UseViewLocatorAssistant = true;
  }
  #endregion

  #region Attributes & So


  public Size Size
  {
    get => new Size(_width, _height);
    set
    {
      var ff = false;
      if (!_width.Equals(value.Width))
      {
        ff = true;
        _width = value.Width;
        NotifyOfPropertyChange(() => Width);
      }
      if (!_height.Equals(value.Height))
      {
        ff = true;
        _height = value.Height;
        NotifyOfPropertyChange(() => Height);
      }
      if (ff)
        NotifyOfPropertyChange(() => Size);
    }
  }

  public double Width
  {
    get => _width;
    set
    {
      if (_width.Equals(value)) return;
      _width = value;
      NotifyOfPropertyChange(() => Width);
      NotifyOfPropertyChange(() => Size);
    }
  }

  public double Height
  {
    get => _height;
    set
    {
      if (_height.Equals(value)) return;
      _height = value;
      NotifyOfPropertyChange(() => Height);
      NotifyOfPropertyChange(() => Size);
    }
  }

  public HorizontalAlignment HorizontalAlignment // IOverlay, Модальные диалоги
  {
    get => _horizontalAlignment;
    set
    {
      if (_horizontalAlignment.Equals(value)) return;
      _horizontalAlignment = value;
      NotifyOfPropertyChange(() => HorizontalAlignment);
    }
  }
  public VerticalAlignment VerticalAlignment // IOverlay, Модальные диалоги
  {
    get => _verticalAlignment;
    set
    {
      if (_verticalAlignment.Equals(value)) return;
      _verticalAlignment = value;
      NotifyOfPropertyChange(() => VerticalAlignment);
    }
  }
  #endregion

  #region Parent info


  public object Owner { get; set; } // задается и используется только в некоторых сценариях
  public ScreenOwnerType OwnerType { get; set; }


  #endregion


  public virtual void RefreshView()
  {
    Refresh(); // Refresh View Bindings
  }

  #region Some Command Implementation

  public void MainExecute(object parameter)
  {
    MainExecuteImpl(parameter);
  }

  [DebuggerStepThrough]
  public bool CanMainExecute(object parameter)
  {
    return CanMainExecuteImpl(parameter);
  }

  #region Альтернатива ICommand = Продублировано для возможности использования в Caliburn Message.Attach
  public void MainExecute2(object parameter)
  {
    MainExecuteImpl(parameter);
  }

  [DebuggerStepThrough]
  public bool CanMainExecute2(object parameter)
  {
    return CanMainExecuteImpl(parameter);
  }
  #endregion

  protected virtual int GetMainCommandIndex()
  {
    return 0;
  }

  protected virtual bool CanMainExecuteImpl(object parameter)
  {
    var commandIndex = GetMainCommandIndex();
    return CanMainExecuteCore(commandIndex, parameter); ;
  }

  protected virtual bool CanMainExecuteCore(int commandIndex, object parameter)
  {
    return true;
  }

  protected virtual void MainExecuteImpl(object parameter)
  {
    var commandIndex = GetMainCommandIndex();
    MainExecuteBefore(commandIndex, parameter);
    MainExecuteCore(commandIndex, parameter);
    MainExecuteAfter(commandIndex, parameter);
  }

  protected virtual void MainExecuteBefore(int commandIndex, object parameter)
  {
  }

  protected virtual void MainExecuteCore(int commandIndex, object parameter)
  {
    switch (commandIndex)
    {
      case 1: MainExecuteCoreAlt1(parameter); return;
      case 2: MainExecuteCoreAlt2(parameter); return;
      default: MainExecuteCore(parameter); return;
    }
  }

  protected virtual void MainExecuteCore(object parameter)
  {
  }

  protected virtual void MainExecuteCoreAlt1(object parameter)
  {
    MainExecuteCore(parameter);
  }

  protected virtual void MainExecuteCoreAlt2(object parameter)
  {
    MainExecuteCore(parameter);
  }

  protected virtual void MainExecuteAfter(int commandIndex, object parameter)
  {
  }

  #endregion



  #region Dispose

  protected virtual ActionExecuteType ThreadForDispose => ActionExecuteType.CurrentThread;

  public void FinalDispose()
  {
    UiUtil.Execute(FinalDisposeImpl, ThreadForDispose);
  }

  private void FinalDisposeImpl()
  {
    InDisposing = true;
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  ~Dialog()
  {
    Dispose(false);
  }

  protected virtual void Dispose(bool disposing)
  {
    ReleaseUnmanagedResources();
    if (disposing)
    {
      ReleaseManagedResources();
    }
  }

  // может прилететь не из UI-потока
  protected virtual void ReleaseManagedResources()
  {
  }

  protected virtual void ReleaseUnmanagedResources()
  {
  }

  #endregion

}