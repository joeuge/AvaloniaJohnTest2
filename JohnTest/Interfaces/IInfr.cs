using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Xml.Linq;
using AutoMapper;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Caliburn.Micro;
using AppNs.CoreNs;
using AppNs.UiBlocks.ContextMenuNs;
using Iface.Utils;
using Iface.Utils.Avalonia;
using Workspace = AppNs.UiBlocks.Shell.Workspace;

namespace AppNs.Interfaces;

internal interface IInfrastructureInternal : IInfrastructure
{
  void OnShellLoadedBegin();
  void OnShellLoadedEnd();
  Task<Workspace> CreateWorkspace();
}

public interface IInfrastructure : INotifyPropertyChangedEx
{
  App Application { get; }
  IClassicDesktopStyleApplicationLifetime Lifetime { get; }
  Window MainWindow { get; }
  IMainWindowViewModel MainWindowViewModel { get; }
  IShell Shell { get; }
  IWindowService WindowService { get; }
  IGlobalModalService WindowModalService { get; }
  IScreenFactoryService<IPage> PageFactoryService { get; }

  Task<IWorkspaceHolder> CreateWorkspaceHolder(IWorkspace workspace = null, WorkspaceHolderLockSeverity lockSeverity = WorkspaceHolderLockSeverity.None);

  #region Создание документов // Async

  #region Make Any Page (Regular or Widget)
  Task<T> MakeScreenAsync<T>(IScreenFactoryService factoryService, VarKey factoryId, VarKey dataId, object dataContext) where T : class, IMasterScreen;
  #endregion

  #region Make Regular Page 
  Task<T> MakePageAsync<T>(VarKey factoryId, Type iocType, VarKey dataId, object dataContext) where T : class, IPage; // 1) Factory 2) IoC=iocType
  Task<T> MakePageAsync<T>(VarKey factoryId, VarKey dataId = null, object dataContext = null) where T : class, IPage; // 1) Factory 2) IoC=<T>
  Task<T> MakePageByIoCAsync<T>(VarKey dataId = null, object dataContext = null) where T : class, IPage; // IoC=<T> // Without Factory
  Task<IPage> MakePageAsync(VarKey factoryId, Type iocType = null, VarKey dataId = null, object dataContext = null); // 1) Factory 2) IoC=iocType
  #endregion

  #endregion

  bool InShutdown { get; }
  void CollectContextMenu(CollectorContext context);
}



