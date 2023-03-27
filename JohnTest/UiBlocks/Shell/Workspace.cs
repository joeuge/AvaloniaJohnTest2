using System.Collections;
using System.ComponentModel;
using System.Reactive;
using System.Windows.Input;
using System.Xml.Linq;
using Caliburn.Micro;
using AppNs.Interfaces;
using AppNs.UiBlocks.ContextMenuNs;
using Iface.Utils;
using Iface.Utils.Avalonia;
using ReactiveUI;

namespace AppNs.UiBlocks.Shell;

// Derived from Conductor<IDocument>, where IDocument is Bed (подложка)

[ViewType(typeof(WorkspaceView))]

internal class Workspace : Conductor<IPage>, IWorkspaceInternal
{
  public List<CommandItem> TmpMenuItems { get; set; } // todo 123


  public EventHandlerCollection<IPage, object> BedChangedEvent { get; } = new EventHandlerCollection<IPage, object>();

  protected IInfrastructure Infr { get; private set; }
  private string _workspaceName;
  private string _description;
  private bool _isViewLoaded;
  private bool _isSelected;
  //private readonly OverlayContainer _overlayContainer;
  private IWorkspaceView _view;
  private IPage _lastWiredBed;

  #region Identity & So 

  public WorkspaceType WorkspaceType => WorkspaceType.Canvas;
  public Guid WorkspaceId { get; set; }
  public int RuntimeId { get; } = GlobalUtils.NextRuntimeId();
  public int RuntimeNpp { get; } = GlobalUtils.NextRuntimeId2();

  public static string ToolCollectionKey => "canvas";
  public virtual string GetToolCollectionKey() { return ToolCollectionKey; }

  public override string DisplayName
  {
    get => GetShortDisplayName();
    set => SetWorkspaceName(value, true);
  }

  public string WorkspaceName
  {
    get => _workspaceName;
    set => SetWorkspaceName(value, true);
  }

  public DisplayNameType ShortDisplayNameType { get; set; }

  protected virtual void SetWorkspaceName(string value, bool notify)
  {
    if (value == _workspaceName) return;
    _workspaceName = value;
    if (!notify) return;
    NotifyOfPropertyChange(() => WorkspaceName);
    NotifyOfPropertyChange(() => DisplayName);
    NotifyOfPropertyChange(() => ShortDisplayName);
    NotifyOfPropertyChange(() => FullDisplayName);
  }

  public string ShortDisplayName => GetShortDisplayName();
  public string FullDisplayName => GetFullDisplayName();
  //public virtual Uri IconSource => null;
  public Uri IconUri => GetIconUri();
  public bool IsIcon => IconUri != null;

  protected virtual string GetShortDisplayName()
  {
    return GetDisplayName(ShortDisplayNameType, isShort: true);
  }

  protected virtual string GetFullDisplayName()
  {
    return GetDisplayName(ShortDisplayNameType, isShort: false);
  }

  private string GetDisplayName(DisplayNameType type, bool isShort)
  {
    switch (type)
    {
      default:
        return GetDisplayNameForIdentity();

      case DisplayNameType.IdentityAndContent:
        return $"{GetDisplayNameForIdentity()}:{GetDisplayNameForContent(isShort)}";

      case DisplayNameType.Content:
        return GetDisplayNameForContent(isShort);
    }
  }


  private string GetDisplayNameForIdentity()
  {
    return _workspaceName ?? RuntimeNpp.ToString();
  }

  private string GetDisplayNameForContent(bool isShort)
  {
    if (Bed == null)
      return null;
    return isShort ? Bed.ShortDisplayName : Bed.FullDisplayName;
  }

  protected virtual Uri GetIconUri()
  {
    return Bed?.IconUri;
  }


  public virtual void NotifyOfDisplayPropertiesChange()
  {
    NotifyOfPropertyChange(() => DisplayName);
    NotifyOfPropertyChange(() => ShortDisplayName);
    NotifyOfPropertyChange(() => FullDisplayName);
    NotifyOfPropertyChange(() => IconUri);
  }

  public string Description
  {
    get => _description;
    set
    {
      if (value == _description) return;
      _description = value;
      NotifyOfPropertyChange(() => Description);
    }
  }

  public bool HasIdentity()
  {
    return WorkspaceId.Equals(Guid.Empty);
  }

  public bool IsTrue(Guid? id)
  {
    if (id.HasValue && !id.Value.Equals(WorkspaceId))
      return false;
    return true;
  }
  #endregion

  #region Ctor+

  public Workspace(IInfrastructure infr)
  {
    Infr = infr;
    ShortDisplayNameType = DisplayNameType.Content;
  }

  public async Task InternalActivateAsync(CancellationToken cancellationToken = default) // todo: call after ctor
  {
    await ((IActivate)this).ActivateAsync(cancellationToken);
  }

  protected override void OnViewAttached(object view, object context) // Screen override
  {
    if (_view != null && !ReferenceEquals(view, _view))
      throw new SystemException("ERROR");

    _view = view as IWorkspaceView;
    Requires.Reference.NotNull(_view, "IWorkspaceView");
  }

  protected override void OnViewLoaded(object view) // Screen override // Вызывается только один раз!!!
  {
    if (_isViewLoaded)
      throw new SystemException("ERROR");

    _isViewLoaded = true;

    _view.OnModelLoaded(this);
  }

  public override object GetView(object context = null)
  {
    return _view;
  }

  #endregion

  #region Parent info

  public bool IsLoose()
  {
    return Parent == null;
  }

  public WorkspaceOwnerType OwnerType
  {
    get
    {
      var holder = TryGetWorkspaceHolder();
      return holder?.OwnerType ?? WorkspaceOwnerType.None;
    }
  }

  public IWorkspaceHolder TryGetWorkspaceHolder()
  {
    if (Parent is IWorkspaceHolder parent)
      return parent;
    return null;
  }

  #endregion

  #region Attributes & So

  public bool IsViewLoaded => _isViewLoaded;

  public bool IsSelected
  {
    get => _isSelected;
    set
    {
      if (value == _isSelected) return;
      _isSelected = value;
      NotifyOfPropertyChange(() => IsSelected);
    }
  }

  #endregion



  #region IWorkspace : Documents // Currents, Activate

  public IPage GetCurrentPage()
  {
    var page = ActiveItem;
    return page;
  }

  public async Task ActivateExistedPageAsync(ScreenWithLocation location)
  {
    if (!(location.Screen is IPage bed)) return;
    //if (ReferenceEquals(Bed, bed)) return;
    await ActivateBedAsync(bed);
  }

  #endregion

  #region Bed. Conductor of Bed.

  private Guid _globalDocId = Guid.Empty;

  public Guid GlobalDocId
  {
    get => _globalDocId;
    set
    {
      _globalDocId = value;

      if (IsGlobalDocument)
      {
        //TurnOffHistory();
      }
    }
  }
  public bool IsGlobalDocument => !GlobalDocId.Equals(Guid.Empty);

  private bool _isBedLocked;
  public bool IsBedLocked // Внимание! Holder.IsBedLocked и CanvasWs.IsBedLocked дополняют друг друга
  {
    get => _isBedLocked || IsGlobalDocument;
    set
    {
      _isBedLocked = value;

      if (_isBedLocked)
      {
        //TurnOffHistory();
      }
    }
  }

  // doc = null : clear bed
  public async Task DecoratePageAndSetBed(IMasterScreen page, ChangeOverlays changeOverlays)
  {
    ShortDisplayNameType = DisplayNameType.Content; // added 1 march 2021

    var pagex = page as IPage;

    await ActivateBedAsync(pagex);

  }

  public IPage Bed
  {
    get => ActiveItem;
    set => ActivateBedAsync(value);
  }

  public async Task ActivateBedAsync(IPage bed)
  {
    var oldValue = ActiveItem;
    if (ReferenceEquals(oldValue, bed))
      return;

    if (IsBedLocked && oldValue != null)
    {
      throw new SystemException("Страница не разрешает загружать другое содержимое"); //Throws.Error("Нельзя загружать другой тип страницы");
    }

    await ActivateItemAsync(bed); // не гарантирует смену ActiveItem! // Справка: События поднимаются в ChangeActiveItem()

    // tmp
    var holder = TryGetWorkspaceHolder();
    if (holder == null) return;

    var menuItems = new List<CommandItem>();
    if (holder.OwnerType == WorkspaceOwnerType.ShellTabs)
    {
      menuItems.Add(new CommandItem
      {
        DisplayName = "Move to new Window",
        Command = ReactiveCommand.CreateFromTask<IWorkspaceHolder>(ToggleWorkspaceHolder, null, RxApp.MainThreadScheduler),
        CommandParameter = holder
      });
    }
    else
    {
      menuItems.Add(new CommandItem
      {
        DisplayName = "Move to TabItem",
        Command = ReactiveCommand.CreateFromTask<IWorkspaceHolder>(ToggleWorkspaceHolder, null, RxApp.MainThreadScheduler),
        CommandParameter = holder
      });
    }

    TmpMenuItems = menuItems;
    NotifyOfPropertyChange(()=>TmpMenuItems);
  }

  private async Task ToggleWorkspaceHolder(IWorkspaceHolder holder)
  {
    await Infr.Shell.ToggleWorkspaceHolder(holder);
  }

  // Вызывается только при смене ActiveItem
  protected override async Task ChangeActiveItemAsync(IPage newItem, bool closePrevious, CancellationToken cancellationToken)
  {
    if (!ReferenceEquals(newItem, _lastWiredBed) && _lastWiredBed != null)
    {
      //AddToHistory();
    }

    //-------------------------
    await base.ChangeActiveItemAsync(newItem, closePrevious, cancellationToken); // здесь произойдет реальная смена ActiveItem
    //-------------------------

    if (!ReferenceEquals(newItem, _lastWiredBed) && _lastWiredBed != null)
    {
      UnWireBed();
    }

    WireBed();

    NotifyOfPropertyChange(() => Bed); // Notify Of Change "Bed"
    if (ShortDisplayNameType != DisplayNameType.Identity)
    {
      NotifyOfPropertyChange(() => DisplayName);
      NotifyOfPropertyChange(() => ShortDisplayName);
    }
    NotifyOfPropertyChange(() => FullDisplayName);
    NotifyOfPropertyChange(() => IconUri);

    BedChangedEvent.Raise(Bed, null);
  }


  private void WireBed()
  {
    var bed = Bed;
    if (bed == null) return;
    if (ReferenceEquals(bed, _lastWiredBed)) return;
    bed.PropertyChanged += BedOnPropertyChanged;
    _lastWiredBed = bed;
  }

  private void UnWireBed()
  {
    if (_lastWiredBed == null) return;
    _lastWiredBed.PropertyChanged -= BedOnPropertyChanged;
    _lastWiredBed = null;
  }

  private void BedOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
  {
    if (propertyChangedEventArgs.PropertyName == "FullDisplayName")
    {
      NotifyOfPropertyChange(() => FullDisplayName);
      return;
    }
    if (propertyChangedEventArgs.PropertyName == "IconUri")
    {
      NotifyOfPropertyChange(() => IconUri);
      return;
    }
    if (ShortDisplayNameType == DisplayNameType.Identity)
      return;
    if (propertyChangedEventArgs.PropertyName == "ShortDisplayName")
    {
      NotifyOfPropertyChange(() => ShortDisplayName);
      NotifyOfPropertyChange(() => DisplayName);
      return;
    }
  }

  #endregion

  #region Commands

  private ICommand _uiCloseCommand;
  public ICommand UiCloseCommand => _uiCloseCommand ?? (_uiCloseCommand = CreateUiCloseCommand());
  protected virtual ICommand CreateUiCloseCommand()
  {
    return new SimpleCommand(async p => await TryCloseAsync(null), p => true);
  }

  #endregion

}
