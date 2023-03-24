using System.Reactive;
using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Caliburn.Micro;
using AppNs.CoreNs;
using AppNs.Interfaces;
using AppNs.UiContent.Dialogs;
using Iface.Utils;
using Iface.Utils.Avalonia;
using ReactiveUI;

namespace AppNs.UiBlocks.Shell;

[ViewType(typeof(ShellView))]
[SingletonInstance]
[Contract(typeof(IShell))]

public class Shell : Screen, IShellInternal
{
  public EventHandlerCollection<IWorkspaceHolder, object> CurrentTabChangedEvent { get; } = new EventHandlerCollection<IWorkspaceHolder, object>();
  public EventHandlerCollection<IWorkspace, object> CurrentTabWorkspaceChangedEvent { get; } = new EventHandlerCollection<IWorkspace, object>();

  private readonly ShellTabContainer _tabContainer;
  private readonly ShellWinContainer _winContainer;
  private IShellView _shellView;
  private IWorkspaceHolder _currentTabWorkspaceHolder;
  private IWorkspace _currentTabWorkspace;


  private IInfrastructureInternal _infr;
  public IInfrastructure Infr => _infr;
  internal IInfrastructureInternal InfrInternal => _infr;

  public IObservableCollection<IWorkspaceHolder> TabItems => _tabContainer.Items;
  public IObservableCollection<IWorkspaceHolder> WinItems => _winContainer.Items;

  public ReactiveCommand<Unit, Unit> NewPageCommand { get; }
  public ReactiveCommand<Unit, Unit> ShowDialogCommand { get; }
  public ReactiveCommand<Unit, Unit> InputStringCommand { get; }


  #region Ctor+

  public Shell() // XAML Designer: либо CompileBindings, либо пустой конструктор
  {
    if (!Execute.InDesignMode)
      throw new InvalidOperationException();
  }

  public Shell(IInfrastructure infr)
  {
    _infr = (IInfrastructureInternal)infr;
    NewPageCommand = ReactiveCommand.CreateFromTask(NewPageCommandImpl, null, RxApp.MainThreadScheduler);
    ShowDialogCommand = ReactiveCommand.CreateFromTask(ShowDialogCommandImpl, null, RxApp.MainThreadScheduler);
    InputStringCommand = ReactiveCommand.CreateFromTask(InputStringCommandImpl, null, RxApp.MainThreadScheduler);

    _tabContainer = new ShellTabContainer(this);
    _winContainer = new ShellWinContainer(this);
  }

  public async Task OnImportsSatisfiedAsync()
  {
    try
    {
      await CreateInitialBars();
      OnCreated();
    }
    catch (Exception e)
    {
    }
  }

  private Task CreateInitialBars()
  {
    return Task.CompletedTask;
  }

  public async Task InternalActivateAsync(CancellationToken cancellationToken = default)
  {
    await ((IActivate)this).ActivateAsync(cancellationToken);
  }


  private async Task NewPageCommandImpl()
  {
    await OpenPageInNewTab(new VarKey(FactoryIds.DummyPage));
  }

  private async Task ShowDialogCommandImpl()
  {
    var modalService = IoC.Get<IGlobalModalService>();
    var dialog = IoC.Get<TestDialog>();
    var dr = await modalService.ShowAsync(dialog, "From Shell");
  }
  
  private async Task InputStringCommandImpl()
  {
    var modalService = IoC.Get<IGlobalModalService>();

    string text = null;
    await modalService.TryPromptAsync(result => text = result, content: "Some text", okButtonContent: null, defaultValue: "John");

    if (text != null)
    {
      await modalService.AlertAsync($"Hello {text}!");
    }
  }


  protected virtual void OnCreated()
  {
  }

  protected override void OnViewAttached(object view, object context) // Screen override
  {
    if (view is AvaloniaObject d)
    {
      View.SetApplyConventions(d, false);
    }
  }

  protected override void OnViewLoaded(object view) // Screen override // Вызывается только один раз!!!
  {
    //base.OnViewLoaded(view); // пустой метод

    _shellView = view as IShellView;
    Requires.Reference.NotNull(_shellView, "IShellView");

    _infr.OnShellLoadedBegin();

    _shellView.OnModelLoaded(this);

    _infr.OnShellLoadedEnd(); // Async pattern
  }


  protected override async Task OnInitializeAsync(CancellationToken cancellationToken) // Screen override
  {
    await base.OnInitializeAsync(cancellationToken);
  }

  protected override async Task OnActivateAsync(CancellationToken cancellationToken) // Screen override
  {
    await ((IActivate)_tabContainer).ActivateAsync(cancellationToken); // во как!
    await ((IActivate)_winContainer).ActivateAsync(cancellationToken); // во как!
    await base.OnActivateAsync(cancellationToken);
  }

  protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken) // Screen override
  {
    await ((IDeactivate)_tabContainer).DeactivateAsync(close, cancellationToken);
    await ((IDeactivate)_winContainer).DeactivateAsync(close, cancellationToken);
    //------------------
    await base.OnDeactivateAsync(close, cancellationToken);
  }

  #endregion




  public void OnGotKeyboardFocus()
  {
  }


  private IWorkspaceHolder FocusedWorkspaceHolder
  {
    set => SetFocusedWorkspaceHolder(value);
  }

  private async Task SetFocusedWorkspaceHolder(IWorkspaceHolder value)
  {
    if (value == null)
      return;
    switch (value.OwnerType)
    {
      default:
        await SetCurrentTabWorkspaceHolder(value);
        break;

      case WorkspaceOwnerType.ShellWindows:
        break;

      case WorkspaceOwnerType.ShellSplit:
        break;
    }
  }

  public IWorkspaceHolder CurrentTabWorkspaceHolder
  {
    get => _currentTabWorkspaceHolder;
    set => SetCurrentTabWorkspaceHolder(value);
  }

  private static bool _guardSetCurrentTab;

  private async Task SetCurrentTabWorkspaceHolder(IWorkspaceHolder value)
  {
    if (_guardSetCurrentTab)
      return;
    _guardSetCurrentTab = true;
    try
    {
      var f2 = !ReferenceEquals(_currentTabWorkspaceHolder, value);

      if (f2)
      {
        UnWireCurrentTabWorkspaceHolder();
        //-----
        _currentTabWorkspaceHolder = value;
        //-----
        WireCurrentTabWorkspaceHolder();

        await ActivateTabContainerItem(value, false, -1);
      }

      if (f2)
      {
        NotifyOfPropertyChange(() => CurrentTabWorkspaceHolder);
        CurrentTabChangedEvent.Raise(_currentTabWorkspaceHolder, null);

        CurrentTabWorkspace = _currentTabWorkspaceHolder?.Workspace;
      }
    }
    finally
    {
      _guardSetCurrentTab = false;
    }
  }


  internal void OnWorkspaceHolderAdded(IWorkspaceHolderInternal holder, WorkspaceOwnerType ownerType)
  {
  }

  internal void OnWorkspaceHolderRemoved(IWorkspaceHolderInternal holder, WorkspaceOwnerType ownerType)
  {
    switch (ownerType)
    {
      case WorkspaceOwnerType.ShellWindows:
        //#tag#info# Игра в прятки
        if (!ExistsExtraWindows())
          BringToFrontMainWindow();
        break;

      case WorkspaceOwnerType.ShellTabs:
        break;
    }

    holder.OwnerType = WorkspaceOwnerType.None;
  }





  #region Current Tab Workspace Holder -> Workspace

  private void WireCurrentTabWorkspaceHolder()
  {
    _currentTabWorkspaceHolder?.WorkspaceChangedEvent.AddHandler(OnCurrentTabWorkspaceChanged);
  }

  private void UnWireCurrentTabWorkspaceHolder()
  {
    _currentTabWorkspaceHolder?.WorkspaceChangedEvent.RemoveHandler(OnCurrentTabWorkspaceChanged);
  }

  private void OnCurrentTabWorkspaceChanged(IWorkspace workspace, object dummy)
  {
    CurrentTabWorkspace = workspace;
  }

  public IWorkspace CurrentTabWorkspace
  {
    get => _currentTabWorkspace;
    private set
    {
      if (ReferenceEquals(_currentTabWorkspace, value)) return;
      _currentTabWorkspace = value;
      NotifyOfPropertyChange();
      CurrentTabWorkspaceChangedEvent.Raise(_currentTabWorkspace, null);
    }
  }
  #endregion



  #region MyRegion

  
  public async Task ToggleWorkspaceHolder(IWorkspaceHolder holder)
  {
    if (holder == null) return;
    if (holder.OwnerType == WorkspaceOwnerType.ShellSplit)
      return;
    var newLocation = holder.OwnerType == WorkspaceOwnerType.ShellWindows ? WorkspaceOwnerType.ShellTabs : WorkspaceOwnerType.ShellWindows;
    await MoveWorkspaceHolder(holder, newLocation);
  }

  public async Task MoveWorkspaceHolder(IWorkspaceHolder holder, WorkspaceOwnerType newLocation)
  {
    if (!CanMoveWorkspaceHolder(holder, newLocation))
      return;

    //GlobalUtil.ConsoleThread("MoveWorkspaceHolder===================BEG");

    await GentleRemoveWorkspaceHolderFromParent(holder);

    switch (newLocation)
    {
      default:
        await ActivateTabWorkspaceHolder(holder);
        break;
      case WorkspaceOwnerType.ShellWindows:
        await ActivateWinWorkspaceHolder(holder);
        break;
      case WorkspaceOwnerType.ShellSplit:
        await ActivateSplitWorkspaceHolder(holder);
        break;
    }
    //GlobalUtil.ConsoleThread("MoveWorkspaceHolder==================END");
  }

  public bool CanMoveWorkspaceHolder(IWorkspaceHolder holder, WorkspaceOwnerType newLocation)
  {
    if (holder == null) return false;
    return true;
  }

  public async Task ActivateWorkspaceHolder(IWorkspaceHolder holder, WorkspacePreferences? workspacePreferences = null)
  {
    if (workspacePreferences == null)
    {
      workspacePreferences = WorkspacePreferences.TailTab;
    }

    var isNew = false;
    var isWindow = false;
    var isSplit = false;

    switch (holder.OwnerType)
    {
      case WorkspaceOwnerType.None: // новый элемент
        isNew = true;
        isSplit = workspacePreferences.IsSplit;
        isWindow = workspacePreferences.IsWindow;
        break;

      case WorkspaceOwnerType.ShellTabs: // существующий элемент
        break;

      case WorkspaceOwnerType.ShellWindows: // существующий элемент
        isWindow = true;
        break;

      case WorkspaceOwnerType.ShellSplit: // существующий элемент
        isSplit = true;
        break;

      default:
        return;
    }

    if (isWindow)
    {
      await ActivateWinWorkspaceHolder(holder, workspacePreferences);
      return;
    }

    if (isSplit)
    {
      await ActivateSplitWorkspaceHolder(holder, workspacePreferences);
      return;
    }

    var isAddNextToActive = false;
    var addWithIndex = -1;
    if (isNew && !workspacePreferences.IsWindow)
    {
      isAddNextToActive = workspacePreferences.IsAddNextToActive;
      addWithIndex = workspacePreferences.AddWithIndex;
    }

    await ActivateTabWorkspaceHolder(holder, isAddNextToActive, addWithIndex);
  }

  public async Task ActivateTabWorkspaceHolder(IWorkspaceHolder holder, bool isAddNextToActive = false, int addWithIndex = -1)
  {
    if (holder.OwnerType == WorkspaceOwnerType.ShellWindows)
      return;
    if (holder.OwnerType == WorkspaceOwnerType.ShellSplit)
      return;
    await ActivateTabContainerItem(holder, isAddNextToActive, addWithIndex);
  }

  public async Task ActivateWinWorkspaceHolder(IWorkspaceHolder holder, WorkspacePreferences? workspacePreferences = null)
  {
    if (holder.OwnerType == WorkspaceOwnerType.ShellTabs)
      return;
    if (holder.OwnerType == WorkspaceOwnerType.ShellSplit)
      return;
    if (workspacePreferences != null)
    {
      if (workspacePreferences.IsWindow)
      {
      }
      else
      {
        workspacePreferences = null;
      }
    }
    await ActivateWinContainerItem(holder, workspacePreferences);
  }

  public async Task ActivateSplitWorkspaceHolder(IWorkspaceHolder holder, WorkspacePreferences? workspacePreferences = null)
  {
    await Task.CompletedTask;
    return;

    if (holder.OwnerType == WorkspaceOwnerType.ShellTabs)
      return;
    if (holder.OwnerType == WorkspaceOwnerType.ShellWindows)
      return;

    //SplitManager.ShowCore((IWorkspaceHolderInternal)holder, workspacePreferences);
  }


  private async Task ActivateTabContainerItem(IWorkspaceHolder item, bool isAddNextToActive, int addWithIndex)
  {
    _tabContainer.IsAddNextToActive = isAddNextToActive;
    _tabContainer.AddWithIndex = addWithIndex;
    await _tabContainer.ActivateItemAsync(item);
    //if (item == null) return;
    //_shellView.EnsureTabWorkspaceVisible(item);
  }

  private async Task ActivateWinContainerItem(IWorkspaceHolder item, WorkspacePreferences workspacePreferences)
  {
    await _winContainer.ActivateItemAsync(item, workspacePreferences);
    //if (item == null) return;
    // todo: Ensure Window Visible
  }

  public int GetTabIndex(IWorkspaceHolder item)
  {
    return _tabContainer.Items.IndexOf(item);
  }

  public async Task SetTabIndex(int index)
  {
    if (index < 0 || index >= _tabContainer.Items.Count)
      return;
    var item = _tabContainer.Items[index];
    await ActivateTabContainerItem(item, false, -1);
  }

  public async Task CloseCurrentTab()
  {
    await _currentTabWorkspaceHolder?.TryCloseAsync(null);
  }


  public async Task CloseCurrentExtraWindow()
  {
    await _currentTabWorkspaceHolder?.TryCloseAsync(null);
  }


  public async Task ActivateNextTab()
  {
    if (_currentTabWorkspaceHolder == null)
    {
      return;
    }
    var tabIndex = GetTabIndex(_currentTabWorkspaceHolder) + 1;
    if (tabIndex >= _tabContainer.Items.Count)
    {
      tabIndex = 0;
    }
    await SetTabIndex(tabIndex);
  }


  public async Task ActivatePreviousTab()
  {
    if (_currentTabWorkspaceHolder == null)
    {
      return;
    }
    var tabIndex = GetTabIndex(_currentTabWorkspaceHolder) - 1;
    if (tabIndex < 0)
    {
      tabIndex = _tabContainer.Items.Count - 1;
    }
    await SetTabIndex(tabIndex);
  }

  public async Task CloseWorkspaceHolder(IWorkspaceHolder holder)
  {
    if (_tabContainer.Items.Contains(holder))
    {
      await _tabContainer.DeactivateItemAsync(holder, true);
      return;
    }

    if (_winContainer.Items.Contains(holder))
    {
      await _winContainer.DeactivateItemAsync(holder, true);
    }
  }

  public async Task GentleRemoveWorkspaceHolderFromParent(IWorkspaceHolder holder)
  {
    var done = await _tabContainer.GentleRemoveItemFromParent(holder);
    if (done) return;
    done = await _winContainer.GentleRemoveItemFromParent(holder);
    if (done) return;
    //await SplitManager.GentleRemoveItemFromParent(holder);
  }

  #endregion



  public async Task ActivateExistedPage(ScreenWithLocation location)
  {
    var holder = location.Screen?.TryGetWorkspaceHolder();
    var workspace = holder?.Workspace;
    if (workspace == null)
      return;
    await SetFocusedWorkspaceHolder(holder);
    await workspace.ActivateExistedPageAsync(location);
  }


  #region Show Documents


  #region LEVEL 0: Show document as IWorkspace.Bed in target IWorkspaceHolder (null = new IWorkspaceHolder)

  // todo: +flag: (переходить на новую вкладку или нет)
  // Если holder задан и нет препятствий его использовать, то он и будет использован
  // see #john#info#bed#
  public async Task<T> ShowPageHCore<T>(IWorkspaceHolder? holder, VarKey? dataId, object? dataContext
    , Func<VarKey?, object?, Task<T>> getPage // (docId, documentContext)
    , Action<T>? proceedPage
    , bool isAddNextToActive
    , Guid? globalDocId = null
    )
    where T : class, IPage
  {
    if (holder == null || holder.Workspace != null)
    {
      holder = await _infr.CreateWorkspaceHolder();

      var workspace = await _infr.CreateWorkspace();
      if (globalDocId.HasValue)
      {
        workspace.GlobalDocId = globalDocId.Value;
      }
      await holder.ActivateWorkspaceAsync(workspace);

      var page = await getPage(dataId, dataContext);
      proceedPage?.Invoke(page);

      await workspace.DecoratePageAndSetBed(page, ChangeOverlays.Custom);
    }

    await ActivateWorkspaceHolder(holder, isAddNextToActive ? WorkspacePreferences.NextToActiveTab : WorkspacePreferences.TailTab);

    return (T)holder.Workspace.Bed;
  }

  public async Task ShowPageH(IWorkspaceHolder? holder, IPage page)
  {
    await ShowPageHCore(holder, VarKey.Empty, null, (x1, x2) => Task.FromResult(page), null, true);
  }

  // 1) Factory 2) IoC=iocType
  public async Task<IPage> ShowPageH(IWorkspaceHolder? holder, VarKey? factoryId, Type? iocType = null, VarKey? dataId = null, object? dataContext = null)
  {
    return await ShowPageHCore(holder, dataId, dataContext
      , (x1, x2) => _infr.MakePageAsync(factoryId, iocType, dataId, dataContext)
      , null, isAddNextToActive: true);
  }

  #endregion


  #region LEVEL 1: Show document на релевантном месте (согласно настройкам фабрик) 

  public async Task<T> ShowPageCore<T>(IScreenFactory factory, IWorkspaceHolder? preferredHolder, VarKey? dataId, object? dataContext
    , bool isAddNextToActive
    , bool isSuppressSingleton
    )
    where T : class, IPage
  {
    Requires.Arg.NotNull(factory, "factory");

    if (factory.ShowPageConf != null && !isSuppressSingleton)
    {
      var pagex = await factory.ShowPageConf.SingletonService.GetPage(factory.ShowPageConf.SingletonId);
      var page = pagex as T;
      if (!VarKey.IsNull(dataId))
      {
        var success = await page?.TryChangeDataIdAsync(dataId, dataContext);
      }

      var preferences = WorkspacePreferences.TailTab;

      if (factory.ShowPageConf.IsSplit)
      {
        preferences = new CustomWorkspacePreferences(factory.ShowPageConf.Side, factory.ShowPageConf.BandWidth, allowReplaceSplit: true);
      }
      else if (factory.ShowPageConf.SingletonService.SingletonServiceType == SingletonServiceType.Tab)
      {
        var tabIndex = -1;

        if (preferredHolder != null)
        {
          tabIndex = GetTabIndex(preferredHolder);
          if (tabIndex != -1)
          {
            // todo: если уже страница есть, то не закрывать вкладку
            await preferredHolder.TryCloseAsync(null); // закроем и затем откроем новую вкладку на этом месте
          }
        }

        if (tabIndex != -1)
        {
          preferences = CustomWorkspacePreferences.ForTab(isAddNextToActive:false, tabIndex);
        }
        else
        {
          preferences = isAddNextToActive ? WorkspacePreferences.NextToActiveTab : WorkspacePreferences.TailTab;
        }
      }

      await factory.ShowPageConf.SingletonService.Show(factory.ShowPageConf.SingletonId, preferences);

      if (page == null) 
        throw new SystemException("ERROR");
      return page;
    }

    return await ShowPageHCore(preferredHolder, dataId, dataContext
      , (x1, x2) => factory.MakeScreenAsync<T>(dataId, dataContext)
      , null, isAddNextToActive);
  }

  public async Task<T> ShowPage<T>(IScreenFactory factory, IWorkspaceHolder? preferredHolder = null, VarKey? dataId = null, object? dataContext = null,
    bool isSuppressSingleton = false)
    where T : class, IPage
  {
    return await ShowPageCore<T>(factory, preferredHolder, dataId, dataContext, true, isSuppressSingleton);
  }

  public async Task<IPage> ShowPage(IScreenFactory factory, IWorkspaceHolder? preferredHolder = null, VarKey? dataId = null, object? dataContext = null,
    bool isSuppressSingleton = false)
  {
    return await ShowPageCore<IPage>(factory, preferredHolder, dataId, dataContext, true, isSuppressSingleton);
  }

  public async Task<T> ShowPage<T>(VarKey factoryId, IWorkspaceHolder? preferredHolder = null, VarKey? dataId = null, object? dataContext = null,
    bool isSuppressSingleton = false)
    where T : class, IPage
  {
    var factory = Infr.PageFactoryService.FindFactory(factoryId);
    return await ShowPageCore<T>(factory, preferredHolder, dataId, dataContext, true, isSuppressSingleton);
  }

  public async Task<IPage> ShowPage(VarKey factoryId, IWorkspaceHolder? preferredHolder = null, VarKey? dataId = null, object? dataContext = null,
    bool isSuppressSingleton = false)
  {
    return await ShowPage<IPage>(factoryId, preferredHolder, dataId, dataContext, isSuppressSingleton);
  }

  #endregion
  

  #region LEVEL 2 // IF (VarKeys.LiveEvents) THEN _infr.ShowDocumentForLiveEventMonitor() ELSE -> LEVEL 1

  // holder = null: new Tab
  public async Task OpenPageInTab(IWorkspaceHolder? holder, VarKey factoryId,
    VarKey? dataId = null,
    object? dataContext = null,
    bool activateAfterOpening = true,
    bool isSuppressSingleton = false)
  {
    await ShowPage(factoryId, holder, dataId, dataContext, isSuppressSingleton);
  }

  public async Task OpenPageInThisTab(IWorkspaceHolder holder, VarKey factoryId,
    VarKey? dataId = null,
    object? dataContext = null,
    bool isSuppressSingleton = false)
  {
    await OpenPageInTab(holder, factoryId, dataId, dataContext, true, isSuppressSingleton);
  }


  public async Task OpenPageInNewTab(VarKey factoryId,
    VarKey? dataId = null,
    object? dataContext = null,
    bool activateAfterOpening = true,
    bool isSuppressSingleton = false)
  {
    await OpenPageInTab(null, factoryId, dataId, dataContext, activateAfterOpening, isSuppressSingleton);
  }

  #endregion


  #endregion


  public IEnumerator<IWorkspaceHolder> GetWorkspaceHolderEnumerator(bool withTabs, bool withWins, bool withSplit)
  {
    if (withTabs)
    {
      foreach (var item in _tabContainer.Items)
      {
        yield return item;
      }
    }
    if (withWins)
    {
      foreach (var item in _winContainer.Items)
      {
        yield return item;
      }
    }
  }

  // return true = action прервал итерацию
  // action return true -> terminate iteration
  public bool IterateWorkspaceHolders(bool withTabs, bool withWins, bool withSplit, Func<IWorkspaceHolder, bool> action)
  {
    var enumerator = GetWorkspaceHolderEnumerator(withTabs, withWins, withSplit);
    if (enumerator == null) return false;
    while (enumerator.MoveNext())
    {
      if (action(enumerator.Current))
      {
        return true;
      }
    }
    return false;
  }


  // return true = action прервал итерацию
  // action return true -> terminate iteration
  public bool IterateWorkspaces<T>(bool withTabs, bool withWins, bool withSplit, Func<IWorkspaceHolder, T, bool> action) where T : class, IWorkspace
  {
    var enumerator = GetWorkspaceHolderEnumerator(withTabs, withWins, withSplit);
    if (enumerator == null) return false;
    while (enumerator.MoveNext())
    {
      var holder = enumerator.Current;
      if (!(holder?.Workspace is T workspace)) continue;
      if (action(holder, workspace))
      {
        return true;
      }
    }
    return false;
  }

  public bool ContainsWorkspace(IWorkspace workspace, bool toActivate)
  {
    return IterateWorkspaces<IWorkspace>(true, true, true, (holder, workspaceX) =>
    {
      if (ReferenceEquals(workspaceX, workspace))
      {
        if (toActivate)
        {
          FocusedWorkspaceHolder = holder;
        }
        return true;
      }
      return false;
    });
  }

  #region Поиск обычных документов (no Tiles, no Overlays)

  public void IteratePages<T>(Action<IWorkspaceHolder, IWorkspace, ScreenIterationContext, T> callback) where T : class, IPage
  {
    IteratePages(new ScreenIterationContext(), callback);
  }

  public void IteratePages<T>(ScreenIterationContext context, Action<IWorkspaceHolder, IWorkspace, ScreenIterationContext, T> callback) where T : class, IPage
  {
    IterateWorkspaces<IWorkspace>(true, true, true, (holder, workspace) =>
    {
      if (workspace.Bed is T page)
      {
        var location = context.ScreenWithLocation;
        location.Clear();
        location.Screen = page;
        callback(holder, workspace, context, page);
      }
      return context.WasCancelled;
    });
  }

  #endregion


  #region Clear, Misc

  public async Task ClearTabItems()
  {
    await _tabContainer.ClearItemsAsync();
  }

  public async Task ClearTabItemsButItem(IWorkspaceHolder butItem)
  {
    await _tabContainer.ClearItemsButItemAsync(butItem);
  }

  public async Task ClearTabItemsOnRight(IWorkspaceHolder borderItem)
  {
    await _tabContainer.ClearItemsOnRightAsync(borderItem);
  }

  public Task ClearSplitItems()
  {
    return Task.CompletedTask;
    //SplitManager.Close();
  }

  public async Task ClearWinItems()
  {
    await _winContainer.ClearItemsAsync();
  }

  public async Task CloseExtraWindows()
  {
    await _winContainer.ClearItemsAsync();

    //#tag#info# Игра в прятки
    BringToFrontMainWindow();
  }

  public async Task<bool> CloseExtraWindowAsync(IExtraWindowController windowController) // true = success
  {
    if (windowController == null) return false;
    var success = await windowController.CloseAsync(useCloseGuard: true);

    //#tag#info# Игра в прятки
    if (!ExistsExtraWindows())
      BringToFrontMainWindow();

    return success;
  }

  public IEnumerable<IExtraWindowController> GetExtraWindows()
  {
    return _winContainer.GetWindows();
  }

  public bool ExistsExtraWindows()
  {
    return _winContainer.Items.Count > 0;
  }

  public void BringToFrontMainWindow()
  {
    _infr.MainWindow.Activate();
    //_infr.MainWindow.Focus();
  }


  #endregion

}