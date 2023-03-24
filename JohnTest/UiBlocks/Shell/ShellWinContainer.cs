using System.Collections.Specialized;
using Avalonia.Controls;
using Caliburn.Micro;
using AppNs.Interfaces;
using AppNs.UiBlocks.ExtraWindows;
using Iface.Utils;

namespace AppNs.UiBlocks.Shell;

internal sealed class ShellWinContainer : Conductor<IWorkspaceHolder>.Collection.AllActive, IParentPatch, IConductorOfWindows
{
  private readonly Shell _owner;
  private bool _closing;
  private readonly Dictionary<IWorkspaceHolderInternal, IExtraWindowController> _mapToWindows = new Dictionary<IWorkspaceHolderInternal, IExtraWindowController>();

  public IReadOnlyDictionary<IWorkspaceHolderInternal, IExtraWindowController> MapToWindows => _mapToWindows;

  #region Ctor+

  public ShellWinContainer(Shell owner)
  {
    _owner = owner;
    Parent = _owner;
    Items.CollectionChanged += (s, e) =>
    {
      switch (e.Action)
      {
        case NotifyCollectionChangedAction.Add:
          e.NewItems.OfType<IWorkspaceHolderInternal>().Apply(OnItemAdded);
          break;

        case NotifyCollectionChangedAction.Remove:
          e.OldItems.OfType<IWorkspaceHolderInternal>().Apply(OnItemRemoved);
          break;

        case NotifyCollectionChangedAction.Replace:
          e.OldItems.OfType<IWorkspaceHolderInternal>().Apply(OnItemRemoved);
          e.NewItems.OfType<IWorkspaceHolderInternal>().Apply(OnItemAdded);
          break;
          /*
          case NotifyCollectionChangedAction.Reset:
            Items.OfType<IWorkspaceHolderInternal>().Apply(OnItemRemoved);
            break;
          */
      }
    };
  }

  void OnItemAdded(IWorkspaceHolderInternal it)
  {
    var windowController = new ExtraWindowController(_owner.Infr, it); // альтернатива = new RadExtraWindowController(it);
    windowController.CreateWindow();

    _mapToWindows.Add(it, windowController);

    windowController.WindowActivated += Window_Activated;

    _owner.OnWorkspaceHolderAdded(it, WorkspaceOwnerType.ShellWindows);

    //it.InternalActivateAsync(); // added here in Avalonia
  }

  private void Window_Activated(object? sender, System.EventArgs e)
  {
    /*
    if (!(sender is ExtraWindow window)) return;
    foreach (var mapToWindow in _mapToWindows)
      if (ReferenceEquals(mapToWindow.Value.Window, window))
        _owner.CurrentWinWorkspaceHolder = mapToWindow.Key;
    */
  }

  private bool _guard222;

  void OnItemRemoved(IWorkspaceHolderInternal item)
  {
    if (_guard222) return;
    if (_mapToWindows.TryGetValue(item, out IExtraWindowController? windowController))
    {
      windowController.WindowActivated -= Window_Activated;
    }
    _mapToWindows.Remove(item);

    if (!_closing && Items.Contains(item))
    {
      // То есть это не реакция на удаление элемента из коллекции, а результат вызова IParentPatch.EnsureChildIsRemoved()
      _guard222 = true;
      try
      {
        Items.Remove(item);
      }
      finally
      {
        _guard222 = false;
      }
    }

    _owner.OnWorkspaceHolderRemoved(item, WorkspaceOwnerType.ShellWindows);
  }

  protected override async Task OnActivateAsync(CancellationToken cancellationToken) // Screen override
  {
    await base.OnActivateAsync(cancellationToken);
  }

  protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken) // Screen override
  {
    _closing = close;
    try
    {
      if (close)
      {
        Items.OfType<IWorkspaceHolderInternal>().Apply(OnItemRemoved); // костыль
      }

      await base.OnDeactivateAsync(close, cancellationToken);
      //WPF: 1) if (close) Items.OfType<IDeactivate>().Apply(x => x.Deactivate(close));
      //     2) if (close) Items.Clear(); // увы, без вызова NotifyCollectionChanged
    }
    finally
    {
      _closing = false;
    }
  }

  protected override void OnViewAttached(object view, object context) // Screen override
  {
  }

  protected override void OnViewLoaded(object view) // Screen override
  {
  }

  #endregion

  protected override IWorkspaceHolder EnsureItem(IWorkspaceHolder newItem)
  {
    if (newItem is IWorkspaceHolderInternal item)
    {
      item.OwnerType = WorkspaceOwnerType.ShellWindows;
    }
    return base.EnsureItem(newItem); // здесь (если отсутствует) добавляется в коллекцию, newItem.Parent = this
  }

  private WorkspacePreferences _tmpWorkspacePreferences;
  public async Task ActivateItemAsync(IWorkspaceHolder item, WorkspacePreferences workspacePreferences)
  {
    if (_closing || _activateItemGuard)
      return;

    _tmpWorkspacePreferences = workspacePreferences;
    try
    {
      await ActivateItemAsync(item);
    }
    finally
    {
      _tmpWorkspacePreferences = null;
    }
  }

  private bool _activateItemGuard = false;
  public override async Task ActivateItemAsync(IWorkspaceHolder item, CancellationToken cancellationToken = default)
  {
    if (_closing || _activateItemGuard)
      return;

    //item?.CheckForActivateIn(WorkspaceOwnerType.ShellWindows);
    if (item?.Parent != null && !ReferenceEquals(item.Parent, this))
      throw new SystemException("item has another parent");

    var xitem = (IWorkspaceHolderInternal)item;

    _activateItemGuard = true;
    var isNew = true;
    try
    {
      if (xitem != null)
      {
        if (_mapToWindows.TryGetValue(xitem, out var windowController))
        {
          isNew = false;
        }
        else
        {
          xitem.WindowStartupLocation = _tmpWorkspacePreferences?.WindowStartupLocation ?? WindowStartupLocation.CenterOwner;
        }
      }
      //---------------------
      await base.ActivateItemAsync(item); // если элемента нет в коллекции Items, то здесь он добавится
                               //---------------------
      if (xitem != null)
      {
        if (_mapToWindows.TryGetValue(xitem, out var windowController))
        {
          var window = windowController.Window;
          if (isNew) // новое окно
          {
            if (_tmpWorkspacePreferences != null)
            {
              //window.WindowStartupLocation = _tmpWorkspacePreferences.WindowStartupLocation;
              window.WindowState = _tmpWorkspacePreferences.WindowState;
              window.Top = _tmpWorkspacePreferences.Top;
              window.Left = _tmpWorkspacePreferences.Left;
              window.Width = _tmpWorkspacePreferences.Width;
              window.Height = _tmpWorkspacePreferences.Height;
            }
            else
            {
              //window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
              window.WindowState = WindowState.Normal;
            }
            windowController.Show(asIs: true);
          }
          else // существующее окно
          {
            if (_tmpWorkspacePreferences != null)
            {
              //window.WindowStartupLocation = _tmpWorkspacePreferences.WindowStartupLocation;
              window.WindowState = _tmpWorkspacePreferences.WindowState;
              window.Top = _tmpWorkspacePreferences.Top;
              window.Left = _tmpWorkspacePreferences.Left;
              window.Width = _tmpWorkspacePreferences.Width;
              window.Height = _tmpWorkspacePreferences.Height;
              windowController.Show(asIs: true);
            }
            else
            {
              windowController.Show(asIs: false); // Minimized -> Normal, Bring To Front
            }
          }
        }
      }
    }
    finally
    {
      _activateItemGuard = false;
    }
  }

  private static void ApplyWindowSettings(IExtraWindow window, WorkspacePreferences workspacePreferences)
  {
    if (window == null || workspacePreferences == null) return;
    window.Top = workspacePreferences.Top;
    window.Left = workspacePreferences.Left;
    window.Width = workspacePreferences.Width;
    window.Height = workspacePreferences.Height;
  }

  private bool _deactivateItemGuard = false;
  public override async Task DeactivateItemAsync(IWorkspaceHolder item, bool close, CancellationToken cancellationToken = default)
  {
    _deactivateItemGuard = true;
    try
    {
      /*
      var count1 = 0;
      if (close)
      {
        count1 = Items.Count;
      }
      */

      // здесь будет вызван (item as IDeactivate).Deactivate(close);
      await base.DeactivateItemAsync(item, close, cancellationToken); // не гарантирует закрытие! 1) item может не дать разрешение на закрытие (метод CanClose) 2) асинхронный паттерн

      /*
      if (close)
      {
        var count2 = Items.Count;
        if (count2 == count1)
        {
          AppConsole.WriteLine(MessageAspects.Jo1, $"Wins: ХОЛОСТОЙ ВЫЗОВ DeactivateItem(), Count = {count2}");
        }
      }
      */
    }
    finally
    {
      _deactivateItemGuard = false;
    }
  }

  protected override void OnActivationProcessed(IWorkspaceHolder item, bool success)
  {
    /*
    if (!ReferenceEquals(_owner.CurrentWinWorkspaceHolder, item))
      _owner.CurrentWinWorkspaceHolder = item;
    */

    base.OnActivationProcessed(item, success);
  }

  #region Удаление элементов 
  /*
  Для удаления отдельного элемента с проверкой разрешения нужно вызвать DeactivateItem(item, true).
  Для удаления отдельного элемента без проверки вызвать ForceCloseItem(item).
  Справка: WindowController подписан на событие item.Deactivated и закрывает окно
  */

  public async Task ForceCloseItemAsync(IWorkspaceHolder item)
  {
    await item.DeactivateAsync(true); // здесь разрешение на закрытие (метод CanClose) не проверятся
    Items.Remove(item);
  }


  public async Task ClearItemsAsync()
  {
    //AppConsole.WriteLine(MessageAspects.Jo1, $"Win Count (beg) = {Items.Count}");
    var items = Items.ToArray();
    foreach (var item in items)
    {
      await TryCloseItemAsync(item);
    }
    //AppConsole.WriteLine(MessageAspects.Jo1, $"Win Count (end) = {Items.Count}");
  }


  private async Task TryCloseItemAsync(IWorkspaceHolder item)
  {
    var canClose = await CanCloseAsync();
    if (!canClose)
      return;
    await item.DeactivateAsync(true);
    Items.Remove(item);
  }

  void IParentPatch.EnsureChildIsRemoved(object child) // вызывается из Holder 
  {
    // без этой заплатки закрытие окна не удалит элемент из коллекции
    if (_closing || _deactivateItemGuard) return;
    if (!(child is IWorkspaceHolderInternal item))
      return;
    OnItemRemoved(item);
  }

  public async Task<bool> GentleRemoveItemFromParent(IWorkspaceHolder item)
  {
    // Справка: item.Deactivate(false) здесь не происходит

    var xitem = (IWorkspaceHolderInternal)item;
    if (!Items.Contains(xitem))
      return false;
    xitem.InGentleRemovingFromParent = true;
    try
    {
      if (_mapToWindows.TryGetValue(xitem, out var windowController))
      {
        await windowController.CloseAsync(useCloseGuard: false);
      }
      Items.Remove(item);
    }
    finally
    {
      xitem.InGentleRemovingFromParent = false;
    }
    return true;
  }

  #endregion

  IExtraWindowController IConductorOfWindows.FindWindow(IWorkspaceHolderInternal item)
  {
    return _mapToWindows.TryGetValue(item, out IExtraWindowController window) ? window : null;
  }

  public IEnumerable<IExtraWindowController> GetWindowsUnOrdered()
  {
    return _mapToWindows.Values;
  }

  public IEnumerable<IExtraWindowController> GetWindows()
  {
    var items = Items.ToArray();
    foreach (var item in items)
    {
      var xitem = (IWorkspaceHolderInternal)item;
      if (_mapToWindows.TryGetValue(xitem, out var windowController))
      {
        yield return windowController;
      }
    }
  }

}
