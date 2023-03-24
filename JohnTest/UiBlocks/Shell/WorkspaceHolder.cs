using System.Windows.Input;
using Avalonia.Controls;
using Caliburn.Micro;
using AppNs.Interfaces;
using Iface.Utils;
using Iface.Utils.Avalonia;

namespace AppNs.UiBlocks.Shell;

[ViewType(typeof(WorkspaceHolderView))]
[TransientInstance]
[Contract(typeof(IWorkspaceHolder))]

public class WorkspaceHolder : Conductor<IWorkspace>, IWorkspaceHolderInternal
{
  public EventHandlerCollection<IWorkspace, object> WorkspaceChangedEvent { get; } = new EventHandlerCollection<IWorkspace, object>();
  public EventHandlerCollection<WorkspaceOwnerType> OwnerChangedEvent { get; } = new EventHandlerCollection<WorkspaceOwnerType>();

  protected IInfrastructure Infr;
  private bool _isViewLoaded;
  private bool _isSelected;
  private IWorkspaceHolderView _view;

  bool IModelTags.InGentleRemovingFromParent { get; set; }


  #region Ctor+

  public override object Parent
  {
    get => base.Parent;
    set
    {
      base.Parent = value; // для Breakpoint
    }
  }

  public WorkspaceHolder(IInfrastructure infr)
  {
    Infr = infr;
  }

  public async Task InternalActivateAsync(CancellationToken cancellationToken = default) // todo: call after ctor
  {
    await ((IActivate)this).ActivateAsync(cancellationToken);
  }


  protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken) // Screen override
  {
    AppConsole.WriteLine(MessageAspects.JoTrash, $"WorkspaceHolder: OnDeactivate({close})");

    var inGentleRemoving = ((IWorkspaceHolderInternal)this).InGentleRemovingFromParent;

    await base.OnDeactivateAsync(close, cancellationToken);
    // info: в этой точке 
    //    1) данный экземпляр пока еще в коллекции родительского Conductor

    if (close || inGentleRemoving)
    {
      //OwnerType = WorkspaceOwnerType.None;
      if (Parent is IParentPatch parent)
      {
        // may 2021 info: этот хук для ShellTabContainer не работает, так как к этому моменту Parent = null
        parent.EnsureChildIsRemoved(this);
      }
    }
  }

  public override async Task<bool> CanCloseAsync(CancellationToken cancellationToken = default)
  {
    var inGentleRemoving = ((IWorkspaceHolderInternal)this).InGentleRemovingFromParent;
    if (inGentleRemoving)
    {
      return true;
    }
    return await base.CanCloseAsync(cancellationToken);
  }

  protected override void OnViewAttached(object view, object context) // Screen override
  {
    if (_view != null && !ReferenceEquals(view, _view))
      throw new SystemException("ERROR");

    _view = view as IWorkspaceHolderView;
    Requires.Reference.NotNull(_view, "IWorkspaceHolderView");
  }

  protected override void OnViewLoaded(object view) // Screen override // Вызывается только один раз!!!
  {
    AppConsole.WriteLine(MessageAspects.JoTrash, $"WorkspaceHolder: OnViewLoaded() beg");

    if (_isViewLoaded)
      throw new SystemException("ERROR");

    //base.OnViewLoaded(view); // пустой метод

    _isViewLoaded = true;

    _view.OnModelLoaded(this);

    //EnsureContextResources();

    var h = ViewLoaded;
    h?.Invoke(this, new ViewAttachedEventArgs { View = _view }); //ViewLoaded(this, new ViewAttachedEventArgs { View = _view });
  }

  public override object GetView(object context = null)
  {
    return _view;
  }

  #endregion

  #region События загрузки Workspace

  private event EventHandler<ViewAttachedEventArgs> ViewLoaded; // = delegate { };

  public bool ExecuteAfterViewLoad(EventHandler<ViewAttachedEventArgs> handler)
  {
    if (handler == null)
      return false;

    if (_isViewLoaded)
    {
      handler(this, new ViewAttachedEventArgs { View = _view });
      return true;
    }

    void WrapperFunc(object s, ViewAttachedEventArgs e)
    {
      ViewLoaded -= WrapperFunc;
      handler(s, e);
    }
    ViewLoaded += WrapperFunc;

    return false;
  }

  public Task WaitViewLoadedAsync()
  {
    var taskSource = new TaskCompletionSource<object>();

    if (_isViewLoaded)
    {
      taskSource.SetResult(null);
    }
    else
    {
      void WrapperFunc(object s, ViewAttachedEventArgs e)
      {
        ViewLoaded -= WrapperFunc;
        taskSource.SetResult(null);
      }
      ViewLoaded += WrapperFunc;
    }
    return taskSource.Task;
  }

  #endregion

  #region Parent info

  public int TempIndex { get; set; }

  public bool IsLoose()
  {
    return Parent == null;
  }

  private WorkspaceOwnerType _ownerType;
  public WorkspaceOwnerType OwnerType
  {
    get => _ownerType;
    set
    {
      if (_ownerType == value) return;
      _ownerType = value;
      OwnerChangedEvent.Raise(_ownerType);
      //NotifyOfPropertyChange(() => OwnerType);
    }
  }

  public WorkspaceOwnerType CoerceOwnerType()
  {
    if (Parent == null && OwnerType != WorkspaceOwnerType.None)
      OwnerType = WorkspaceOwnerType.None;
    return OwnerType;
  }

  public IExtraWindowController TryGetWorkspaceWindow() // todo: rename to TryGetExtraWindowController
  {
    if (Parent is IConductorOfWindows parent)
      return parent.FindWindow(this);
    return null;
  }

  public void CheckForActivateIn(WorkspaceOwnerType newOwnerType)
  {
    if (Parent == null) return;
    if (OwnerType != newOwnerType)
      throw new SystemException("I have another parent");
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

  WindowStartupLocation IWorkspaceHolderInternal.WindowStartupLocation { get; set; }
  #endregion




  #region Workspace. Conductor of Workspace.

  public IWorkspace Workspace
  {
    get => ActiveItem;
    set => ActivateWorkspaceAsync(value);
  }

  public async Task ActivateWorkspaceAsync(IWorkspace workspace)
  {
    var oldValue = ActiveItem;
    if (ReferenceEquals(oldValue, workspace))
      return;

    await ActivateItemAsync(workspace); // не гарантирует смену ActiveItem! // Справка: События поднимаются в ChangeActiveItem()
  }

  protected override IWorkspace EnsureItem(IWorkspace newItem)
  {
    return base.EnsureItem(newItem); // здесь (если отсутствует) добавляется в коллекцию, newItem.Parent = this
  }

  // Вызывается только при смене ActiveItem
  protected override async Task ChangeActiveItemAsync(IWorkspace newItem, bool closePrevious, CancellationToken cancellationToken)
  {
    await base.ChangeActiveItemAsync(newItem, closePrevious, cancellationToken); // здесь произойдет реальная смена ActiveItem

    NotifyOfPropertyChange(() => Workspace); // Notify Of Change "Workspace"
    WorkspaceChangedEvent.Raise(Workspace, null);
  }

  // Вызывается всякий раз при вызове ActivateItem/DeactivateItem (даже при повторах)
  protected override void OnActivationProcessed(IWorkspace item, bool success) // ConductorBase<T> override
  {
    base.OnActivationProcessed(item, success); // raise event ActivationProcessed
  }

  #endregion

  public async Task ForceActivateContentAsync()
  {
    var ff = IsActive;

    await ((IActivate)this).ActivateAsync(); // по идее достаточно только этого, все остальное - страховка от некачественного кодирования

    // Workspace
    if (ActiveItem != null)
    {
      var oldIsActive = ActiveItem.IsActive;
      await ActivateItemAsync(ActiveItem); // -> ActiveItem.Activate();
      var newIsActive = ActiveItem.IsActive;
    }
  }

}
