using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Caliburn.Micro;
using AppNs.CoreNs;
using Iface.Utils.Avalonia;

namespace AppNs.Interfaces;

internal interface IShellInternal : IShell
{
}

public interface IShell : IGuardClose, IDeactivate  
{
  Task ClearTabItems();
  Task ClearTabItemsButItem(IWorkspaceHolder butItem);
  Task ClearTabItemsOnRight(IWorkspaceHolder borderItem);
  Task ClearSplitItems();
  Task ClearWinItems();

  #region for XAML bindings 
  IObservableCollection<IWorkspaceHolder> TabItems { get; } // Tab - вкладки
  IObservableCollection<IWorkspaceHolder> WinItems { get; } // окна
  #endregion

  IWorkspaceHolder CurrentTabWorkspaceHolder { get; set; }

  IWorkspace CurrentTabWorkspace { get; } // = CurrentTabWorkspaceHolder.Workspace

  Task CloseWorkspaceHolder(IWorkspaceHolder holder); // нормальный цикл (проверка, деактивация, удаление из родительской коллекции)
                                                      //void GentleRemoveWorkspaceFromParent(IWorkspace workspace); // только удаление из родительской коллекции
  Task ToggleWorkspaceHolder(IWorkspaceHolder holder); // Window <-> Tab
  Task MoveWorkspaceHolder(IWorkspaceHolder holder, WorkspaceOwnerType newLocation);
  bool CanMoveWorkspaceHolder(IWorkspaceHolder holder, WorkspaceOwnerType newLocation);

  // если экземпляр существует - он будет активирован, иначе - добавляется
  Task ActivateWorkspaceHolder(IWorkspaceHolder holder, WorkspacePreferences? workspacePreferences = null);
  Task ActivateTabWorkspaceHolder(IWorkspaceHolder holder, bool isAddNextToActive = false, int addWithIndex = -1); // только для новых экземпляров!
  Task ActivateWinWorkspaceHolder(IWorkspaceHolder holder, WorkspacePreferences? workspacePreferences = null); // только для новых экземпляров!

  int GetTabIndex(IWorkspaceHolder item);
  Task SetTabIndex(int index);
  Task CloseCurrentTab();
  Task CloseCurrentExtraWindow();
  Task ActivateNextTab();
  Task ActivatePreviousTab();

  Task ActivateExistedPage(ScreenWithLocation location);


  #region Поиск Workspaces

  // return true = action прервал итерацию
  // action return true -> terminate iteration
  bool IterateWorkspaceHolders(bool withTabs, bool withWins, bool withSplit, Func<IWorkspaceHolder, bool> action);

  // return true = action прервал итерацию
  // action return true -> terminate iteration
  bool IterateWorkspaces<T>(bool withTabs, bool withWins, bool withSplit, Func<IWorkspaceHolder, T, bool> action) where T : class, IWorkspace;

  bool ContainsWorkspace(IWorkspace workspace, bool toActivate);
  #endregion

  #region Поиск обычных документов (no Overlays, no Tiles)

  void IteratePages<T>(Action<IWorkspaceHolder, IWorkspace, ScreenIterationContext, T> callback) where T : class, IPage;
  #endregion

  #region Show Documents

  #region LEVEL 0: Show document as ICanvasWorkspace.Bed in target IWorkspaceHolder (null = new IWorkspaceHolder) 

  Task ShowPageH(IWorkspaceHolder? holder, IPage page);

  // 1) Factory 2) IoC=iocType
  Task<IPage> ShowPageH(IWorkspaceHolder? holder, VarKey? factoryId, Type? iocType = null, VarKey? dataId = null, object? dataContext = null);

  #endregion

  #region LEVEL 1: Show document на релевантном месте (согласно настройкам фабрик) 

  Task<T> ShowPage<T>(IScreenFactory factory, IWorkspaceHolder? preferredHolder = null, VarKey? dataId = null,
    object? dataContext = null,
    bool isSuppressSingleton = false)
    where T : class, IPage;

  Task<IPage> ShowPage(IScreenFactory factory, IWorkspaceHolder? preferredHolder = null, VarKey? dataId = null,
    object? dataContext = null,
    bool isSuppressSingleton = false);

  Task<T> ShowPage<T>(VarKey factoryId, IWorkspaceHolder? preferredHolder = null, VarKey? dataId = null,
    object? dataContext = null,
    bool isSuppressSingleton = false)
    where T : class, IPage;

  Task<IPage> ShowPage(VarKey factoryId, IWorkspaceHolder? preferredHolder = null, VarKey? dataId = null,
    object? dataContext = null,
    bool isSuppressSingleton = false);

  #endregion

  #region LEVEL 2 // IF (VarKeys.LiveEvents) THEN _infr.ShowDocumentForLiveEventMonitor() ELSE -> LEVEL 1

  // holder = null: new holder
  Task OpenPageInTab(IWorkspaceHolder? holder,
    VarKey factoryId,
    VarKey? dataId = null,
    object? dataContext = null,
    bool activateAfterOpening = true,
    bool isSuppressSingleton = false);

  Task OpenPageInThisTab(IWorkspaceHolder holder,
    VarKey factoryId,
    VarKey? dataId = null,
    object? dataContext = null,
    bool isSuppressSingleton = false);


  Task OpenPageInNewTab(VarKey factoryId,
    VarKey? dataId = null,
    object? dataContext = null,
    bool activateAfterOpening = true,
    bool isSuppressSingleton = false);


  #endregion

  #endregion

  void OnGotKeyboardFocus();

}


