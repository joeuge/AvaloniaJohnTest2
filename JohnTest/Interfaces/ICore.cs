using System.Text;
using System.Windows.Input;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Caliburn.Micro;
using AppNs.CoreNs;
using AppNs.UiBlocks.ContextMenuNs;
using AppNs.UiBlocks.ExtraWindows;
using Iface.Utils.Avalonia;

namespace AppNs.Interfaces;


public enum ResizeMode
{
  /// <summary>A window cannot be resized. The Minimize and Maximize buttons are not displayed in the title bar.</summary>
  NoResize,
  /// <summary>A window can only be minimized and restored. The Minimize and Maximize buttons are both shown, but only the Minimize button is enabled.</summary>
  CanMinimize,
  /// <summary>A window can be resized. The Minimize and Maximize buttons are both shown and enabled.</summary>
  CanResize,
  /// <summary>A window can be resized. The Minimize and Maximize buttons are both shown and enabled. A resize grip appears in the bottom-right corner of the window.</summary>
  CanResizeWithGrip,
}

public interface ICustomTools
{
}


//----------------------------
public interface IViewLocatorAssistant
{
  bool Enabled { get; }
  Type ViewType { get; } // наивысший приоритет
  object ViewContext { get; }
}



internal interface IModelTags
{
  bool InGentleRemovingFromParent { get; set; }
}

internal interface IParentPatch
{
  void EnsureChildIsRemoved(object child);
}

internal interface IConductorOfWindows
{
  IExtraWindowController FindWindow(IWorkspaceHolderInternal workspace);
}

//------------------------------
public interface IMasterScreen : IScreen, IHaveStates, IAutoDiscover
{
  IInfrastructure Infrastructure { get; }

  #region Identity & So
  int RuntimeId { get; } // todo: remove?
  VarKey FactoryId { get; set; }
  Uri IconUri { get; set; }
  VarKey DataId { get; set; } // persist
  string ViewContext { get; set; }
  bool UseViewLocatorAssistant { get; set; }
  #endregion

  #region Parent info
  object Owner { get; set; } // задается и используется только в некоторых сценариях
  ScreenOwnerType OwnerType { get; }
  object Parent { get; }
  IWorkspaceHolder TryGetWorkspaceHolder();
  IWorkspace TryGetWorkspace();
  T? TryGetWorkspace<T>() where T : class, IWorkspace;
  
  IGlobalModalService GetGlobalModalService();
  #endregion

  // эти методы вызываются до TryChangeDocIdAsync(docId)!
  Task InitialConfigure(VarKey? dataId, object? dataContext);

  // асинхронный паттерн загрузки данных
  Task<bool> TryChangeDataIdAsync(VarKey? newId, object? dataContext = null); // Task<success>

  void RefreshView(); // Refresh View Bindings

  void FinalDispose();
}

//------------------------------
internal interface IMasterScreenInternal : IMasterScreen
{
  #region Parent info
  new ScreenOwnerType OwnerType { get; set; }
  new object Parent { get; set; }
  #endregion

  //void Proceed
}



//------------------------------
public interface IPage : IMasterScreen
{
  #region Identity & So // +inherited: DisplayName
  string ShortDisplayName { get; }
  string FullDisplayName { get; }
  #endregion

  double Height { get; set; }
  double Width { get; set; }

  void   CollectContextMenu(CollectorContext context);
}

//------------------------------
internal interface IPageInternal : IPage, IMasterScreenInternal 
{
}




//==============================================
internal interface IWorkspaceHolderInternal : IWorkspaceHolder, IModelTags
{
  Task InternalActivateAsync(CancellationToken cancellationToken = default);

  #region Parent info
  new WorkspaceOwnerType OwnerType { get; set; }
  #endregion

  WindowStartupLocation WindowStartupLocation { get; set; }
}

//==============================================
public interface IWorkspaceHolder : IScreen
{
  #region Parent info
  int TempIndex { get; set; }
  object Parent { get; }
  WorkspaceOwnerType OwnerType { get; }
  WorkspaceOwnerType CoerceOwnerType();
  IExtraWindowController TryGetWorkspaceWindow(); // for workspaces from Shell.WinWorkspaces
  #endregion

  bool IsSelected { get; set; } // -> RadTabItem.IsSelected


  IWorkspace Workspace { get; set; } // не гарантирует смену Workspace! Причина: асинхронный паттерн CanClose()
  Task ActivateWorkspaceAsync(IWorkspace workspace); // эквивалент Workspace = 
  EventHandlerCollection<IWorkspace, object> WorkspaceChangedEvent { get; }
  EventHandlerCollection<WorkspaceOwnerType> OwnerChangedEvent { get; }

  // move to IWorkspaceInternal
  void CheckForActivateIn(WorkspaceOwnerType newOwnerType);

  Task ForceActivateContentAsync();
}


//==============================================
internal interface IWorkspaceInternal : IWorkspace
{
}

//==============================================
public interface IWorkspace : IScreen
{
  #region Parent info
  object Parent { get; }
  WorkspaceOwnerType OwnerType { get; }
  IWorkspaceHolder TryGetWorkspaceHolder();
  #endregion

  ICommand UiCloseCommand { get; }
  bool IsSelected { get; set; }

  IPage GetCurrentPage();

  // активация существующего (в workspace) документа
  Task ActivateExistedPageAsync(ScreenWithLocation location);

  bool IsGlobalDocument { get; }
  Guid GlobalDocId { get; set; }

  IPage Bed { get; set; } // не гарантирует смену Bed! Причина: асинхронный паттерн CanClose()

  Task ActivateBedAsync(IPage bed); // эквивалент Bed = 
}


//==============================================
public class ScreenWithLocation
{
  public IMasterScreen Screen { get; set; }
  //public IDocumentDecorator Decorator { get; set; }

  public void Clear()
  {
    Screen = null;
    //Decorator = null;
  }
}

public class ScreenIterationContext
{
  public bool WasCancelled { get; set; }
  public ScreenWithLocation ScreenWithLocation { get; } = new ScreenWithLocation();
}

public enum SingletonServiceType
{
  Unknown, Side, Popup, Tab
}

public interface ISingletonService// implementations: WorkspaceSingletonManager, PopupManager, ShellSidePanelManager
{
  SingletonServiceType SingletonServiceType { get; }
  Task Show(string itemId, WorkspacePreferences workspacePreferences = null);
  Task<IPage> GetPage(string itemId);
}

public interface IWorkspaceSingleton
{
  Task Show(WorkspacePreferences workspacePreferences = null);
  Task<IPage> GetPage();
  Task CloseAsync();
}

public interface IExtraWindowController
{
  event EventHandler WindowActivated;

  IWorkspaceHolder Owner { get; }
  IExtraWindow Window { get; }

  void        Show(bool       asIs);
  void        Close(bool      useCloseGuard);
  Task<bool>  CloseAsync(bool useCloseGuard); // true = success
  string      GetWindowTitle();
  //IBitmap? GetWindowIcon();

  Task ToMainWindowAsync();
}

