using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Avalonia.Layout;
using Caliburn.Micro;
using AppNs.Interfaces;
using AppNs.UiBlocks.ContextMenuNs;
using Iface.Utils;
using Iface.Utils.Avalonia;
using Action = System.Action;

namespace AppNs.CoreNs;

// based on Screen class !!!
public abstract class MasterScreen : Screen, IMasterScreenInternal, IViewLocatorAssistant
{
  public IInfrastructure Infrastructure { get; }
  public bool InDisposing { get; protected set; }

  private VarKey _dataId;
  private string _displayBaseName;
  private bool _isViewLoaded;
  private double _width = double.NaN; // = Auto
  private double _height = double.NaN; // = Auto
  protected virtual bool IsKeepView => true;



  #region Ctor+

  protected MasterScreen()
  {
    if (Execute.InDesignMode) return;
    Infrastructure = IoC.Get<IInfrastructure>();
    OnBaseCreated();
  }

  protected virtual void OnBaseCreated()
  {
    SetDefaultSize(false);
  }

  public virtual void SetDefaultSize(bool notify)
  {
    _width = double.NaN; // = Auto
    _height = double.NaN; // = Auto
    if (!notify) return;
    NotifyOfPropertyChange(() => Width);
    NotifyOfPropertyChange(() => Height);
    NotifyOfPropertyChange(() => Size);
  }

  public void SetAutoSize(bool notify)
  {
    _width = double.NaN; // = Auto
    _height = double.NaN; // = Auto
    if (!notify) return;
    NotifyOfPropertyChange(() => Width);
    NotifyOfPropertyChange(() => Height);
    NotifyOfPropertyChange(() => Size);
  }

  private InputElement? _view; // Visual, IVisual ?
  protected InputElement? View => _view;

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

  protected override void OnViewAttached(object view, object context) // Screen override
  {
    //base.OnViewAttached(view, context); // пустой метод
    if (!IsKeepView) return;

    if (_view != null && !ReferenceEquals(view, _view))
      throw new SystemException("ERROR");

    _view = view as InputElement;
    Requires.Reference.NotNull(_view, "view as FrameworkElement");
  }

  public override object GetView(object context = null)
  {
    if (IsKeepView)
    {
      return _view;
    }

    return base.GetView(context);
  }


  // Вызывается только один раз (на экземпляр(!) view) !!! by IPlatformProvider.ExecuteOnFirstLoad()
  protected override void OnViewLoaded(object view) // Screen override
  {
    //base.OnViewLoaded(view); // пустой метод

    if (_isViewLoaded)
    {
      // Создан новый экземпляр view! // MainMenuView?
      if (IsKeepView)
      {
        throw new SystemException("ERROR");
      }
    }

    _isViewLoaded = true;
  }



  protected override async Task OnActivateAsync(CancellationToken cancellationToken) // Screen override
  {
    await base.OnActivateAsync(cancellationToken);
  }

  protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken) // Screen override
  {
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

  #endregion


  public override async Task TryCloseAsync(bool? dialogResult = null)
  {
    if (InDisposing)
    {
      return;
    }

    if (Parent is IDialogProxyInternal modalDialog) // todo: remove?
    {
      if (await modalDialog.OverrideDialogTryClose(dialogResult ?? false))
        return;
    }

    // see XamlPlatformProvider.GetViewCloseAction() // ищет method "Close", property "DialogResult", property "IsOpen"
    await base.TryCloseAsync(dialogResult); // PlatformProvider.Current.GetViewCloseAction(this, Views.Values, dialogResult).OnUIThread();
  }


  #region Identity & So (Content-Load-Facility) 

  public int RuntimeId { get; } = GlobalUtils.NextRuntimeId();
  private Uri _iconUri;
  public Uri IconUri { get => _iconUri; set => this.SetPropertyValue(ref _iconUri, value); }
  public VarKey FactoryId { get; set; } // for serialization/deserialization

  public VarKey DataId
  {
    get => _dataId;
    set
    {
      if (VarKey.Equals(value, _dataId)) return;
      var oldId = _dataId;
      _dataId = value;
      OnDataIdChanged(oldId, _dataId);
      NotifyOfPropertyChange();
    }
  }

  protected virtual void OnDataIdChanged(VarKey oldId, VarKey newId)
  {
  }

  public string Group { get; set; }

  public virtual string GetToolCollectionKey()
  {
    return Group;
  }

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

  protected virtual string GetShortDisplayName() { return _displayBaseName ?? FactoryId?.ToString(); }
  public virtual string GetFullDisplayName() { return GetShortDisplayName(); }

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

  #endregion

  #region Parent info

  public object Owner { get; set; } // задается и используется только в некоторых сценариях
  public ScreenOwnerType OwnerType { get; set; }

  public IWorkspaceHolder TryGetWorkspaceHolder()
  {
    return ModelUtility.FindAncestor<IWorkspaceHolder>(Parent);
  }

  public IWorkspace TryGetWorkspace()
  {
    return ModelUtility.FindAncestor<IWorkspace>(Parent);
  }

  public T? TryGetWorkspace<T>() where T : class, IWorkspace
  {
    var workspace = TryGetWorkspace();
    if (workspace is T typedWorkspace)
      return typedWorkspace;
    return null;
  }

  public IGlobalModalService GetGlobalModalService()
  {
    return Infrastructure.WindowModalService;
  }
  
  #endregion


  #region Внешние настройки, Загрузка данных

  public async Task InitialConfigure(VarKey? dataId, object? dataContext)
  {
    await InitialConfigureImpl(dataId, dataContext);
  }

  protected virtual Task InitialConfigureImpl(VarKey? dataId, object? dataContext)
  {
    return Task.CompletedTask;
  }
  // Task<success>
  public virtual Task<bool> TryChangeDataIdAsync(VarKey? newId, object? dataContext = null)
  {
    DataId = newId;
    return Task.FromResult(true);
  }

  #endregion

  public virtual void RefreshView()
  {
    Refresh(); // Refresh View Bindings
  }


  public virtual void CollectContextMenu(CollectorContext context)
  {
  }


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

  ~MasterScreen()
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
