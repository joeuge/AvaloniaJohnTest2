using System.Collections.Specialized;
using Caliburn.Micro;
using AppNs.Interfaces;
using Iface.Utils;
using static System.Net.Mime.MediaTypeNames;

namespace AppNs.UiBlocks.Shell;

internal sealed class ShellTabContainer : Conductor<IWorkspaceHolder>.Collection.OneActive, IParentPatch
{
  private readonly Shell _owner;
  private bool _closing;

  #region Ctor+

  public ShellTabContainer(Shell owner)
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

  private bool _guard222;

  void OnItemRemoved(IWorkspaceHolderInternal item)
  {
    if (_guard222) return;
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

    _owner.OnWorkspaceHolderRemoved(item, WorkspaceOwnerType.ShellTabs);
  }

  void OnItemAdded(IWorkspaceHolderInternal item)
  {
    _owner.OnWorkspaceHolderAdded(item, WorkspaceOwnerType.ShellTabs);
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
      //------------------
      await base.OnDeactivateAsync(close, cancellationToken);
      //------------------
      // WPF:
      //    1) if (close) Items.OfType<IDeactivate>().Apply(x => x.Deactivate(close));
      //    2) if (close) Items.Clear(); // увы, без вызова NotifyCollectionChanged
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

  internal bool IsAddNextToActive { get; set; }
  internal int AddWithIndex { get; set; } = -1;

  protected override IWorkspaceHolder EnsureItem(IWorkspaceHolder newItem)
  {
    if (newItem is IWorkspaceHolderInternal xitem)
    {
      xitem.OwnerType = WorkspaceOwnerType.ShellTabs;
    }

    // patch: открываем новую вкладку не в конце, а после ActiveItem или в позиции NextIndex

    var newIndex = -2;

    if (AddWithIndex != -1)
    {
      newIndex = AddWithIndex;
      AddWithIndex = -1;
    }

    if (IsAddNextToActive)
    {
      IsAddNextToActive = false;
      newIndex = -1;
      if (ActiveItem != null)
      {
        var curIndex = Items.IndexOf(ActiveItem);
        if (curIndex != -1 && curIndex < Items.Count - 1)
          newIndex = curIndex + 1;
      }
    }

    if (newIndex != -2 && newItem != null && Items.IndexOf(newItem) == -1)
    {
      if (newIndex >= 0 && newIndex < Items.Count)
        Items.Insert(newIndex, newItem);
      else
        Items.Add(newItem);
    }

    return base.EnsureItem(newItem); // здесь (если отсутствует) добавляется в коллекцию (в конец), newItem.Parent = this
  }

  private bool _activateItemGuard = false;
  public override async Task ActivateItemAsync(IWorkspaceHolder item, CancellationToken cancellationToken = default)
  {
    if (_closing || _activateItemGuard)
      return;

    //item?.CheckForActivateIn(WorkspaceOwnerType.ShellTabs);
    if (item?.Parent != null && !ReferenceEquals(item.Parent, this))
      throw new SystemException("item has another parent");

    _activateItemGuard = true;

    try
    {
      var isNew = !ReferenceEquals(item, ActiveItem);

      await base.ActivateItemAsync(item, cancellationToken);
    }
    finally
    {
      _activateItemGuard = false;
    }
  }

  private bool _deactivateItemGuard = false;
  public override async Task DeactivateItemAsync(IWorkspaceHolder item, bool close, CancellationToken cancellationToken = default)
  {
    if (close && item != null)
    {
      item.TempIndex = Items.IndexOf(item);
    }

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
          AppConsole.WriteLine(MessageAspects.Jo1, $"Tabs: ХОЛОСТОЙ ВЫЗОВ DeactivateItem(), Count = {count2}");
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
    if (!ReferenceEquals(_owner.CurrentTabWorkspaceHolder, item)) // john: todo: + if (success) ?
      _owner.CurrentTabWorkspaceHolder = item;

    base.OnActivationProcessed(item, success);
  }

  #region Удаление элементов 
  /*
  Для удаления отдельного элемента с проверкой разрешения нужно вызвать DeactivateItem(item, true).
  Для удаления отдельного элемента без проверки вызвать ForceCloseItem(item).
  */

  public async Task ForceCloseItemAsync(IWorkspaceHolder item)
  {
    await item.DeactivateAsync(true); // здесь разрешение на закрытие (метод CanClose) не проверятся
    Items.Remove(item);
  }

  public async Task ClearItemsAsync()
  {
    //AppConsole.WriteLine(MessageAspects.Jo1, $"Tab Count (beg) = {Items.Count}");
    var items = Items.ToArray();
    foreach (var item in items)
    {
      await TryCloseItemAsync(item);
    }
    //AppConsole.WriteLine(MessageAspects.Jo1, $"Tab Count (end) = {Items.Count}");
  }

  public async Task ClearItemsButItemAsync(IWorkspaceHolder butItem)
  {
    var items = Items.Where(it => !ReferenceEquals(it, butItem)).ToArray();
    foreach (var item in items)
    {
      await TryCloseItemAsync(item);
    }
  }

  public async Task ClearItemsOnRightAsync(IWorkspaceHolder borderItem)
  {
    if (borderItem == null) return;
    var index = Items.IndexOf(borderItem);
    if (index == -1) return;

    var items = Items.ToArray();
    for (var i = index + 1; i < items.Length; i++)
    {
      await TryCloseItemAsync(items[i]);
    }
  }

  private async Task TryCloseItemAsync(IWorkspaceHolder item)
  {
    var canClose = await CanCloseAsync();
    if (!canClose)
      return;
    await item.DeactivateAsync(true);
    Items.Remove(item);
  }

  /*
  private void TryCloseItemV1(IWorkspaceHolder item)
  {
    CloseStrategy.Execute(new[] { item }, (canClose, closable) =>
    {
      if (!canClose)
        return;
      item.Deactivate(true);
      Items.Remove(item);
    });
  }
  */

  /*
  private void TryCloseItem(IWorkspaceHolder item)
  {
    item.CanClose(canClose =>
    {
      if (!canClose)
        return;
      item.Deactivate(true);
      Items.Remove(item);
    });
  }
  */

  void IParentPatch.EnsureChildIsRemoved(object child) // вызывается из Holder 
  {
    return; // пока все работает и без нижележащего кода
    if (_closing || _deactivateItemGuard) return;
    if (!(child is IWorkspaceHolderInternal item))
      return;
    OnItemRemoved(item);
  }

  public Task<bool> GentleRemoveItemFromParent(IWorkspaceHolder item)
  {
    // Справка: item.Deactivate(false) здесь не происходит

    var xitem = (IWorkspaceHolderInternal)item;
    if (!Items.Contains(xitem))
      return Task.FromResult(false);

    xitem.InGentleRemovingFromParent = true;
    try
    {
      Items.Remove(item);
    }
    finally
    {
      xitem.InGentleRemovingFromParent = false;
    }
    return Task.FromResult(true);
  }

  #endregion

}
