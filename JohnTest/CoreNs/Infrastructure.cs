using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Caliburn.Micro;
using AppNs.Interfaces;
using AppNs.UiBlocks.ContextMenuNs;
using AppNs.UiBlocks.Shell;
using AppNs.Windows;
using Avalonia.ReactiveUI;
using Iface.Utils;
using Iface.Utils.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using IServiceCollection = Microsoft.Extensions.DependencyInjection.IServiceCollection;
using ViewLocator = Caliburn.Micro.ViewLocator;

namespace AppNs.CoreNs;

//------------------------------------------------------
public class Infrastructure : PropertyChangedBase, IInfrastructureInternal
{
  //=============================
  private interface IRunHelper : IDisposable
  {
    Task Run();
  }
  //=============================

  // implementation of AssemblySource.FindTypeByNames() // instead using of Caliburn AssemblySourceCache
  private static IDictionary<string, Type> _typeNameCache;

  public App Application { get; }
  public OperatingSystemId OperatingSystemId { get; private set; }

  public IClassicDesktopStyleApplicationLifetime Lifetime { get; }
  public ServiceProvider ServiceProvider { get; private set; }
  public Window MainWindow { get; private set; }
  public MainWindowVm MainWindowViewModel { get; private set; }
  IMainWindowViewModel IInfrastructure.MainWindowViewModel => MainWindowViewModel;
  public Shell Shell { get; private set; }
  IShell IInfrastructure.Shell => Shell;

  // используется для поиска views
  public Assembly[] Assemblies1 { get; private set; }
  public Assembly[] Assemblies2 { get; private set; }
  public Assembly[] AllAssemblies { get; private set; }


  public IWindowService WindowService { get; private set; }
  public IGlobalModalService WindowModalService { get; private set; }
  public IScreenFactoryService<IPage> PageFactoryService { get; private set; }


  #region Ctor, Run, Configure Caliburn, IoC

  public Infrastructure(App application, IClassicDesktopStyleApplicationLifetime lifetime)
  {
    Application = application;
    Lifetime = lifetime;
    OperatingSystemId = OperatingSystem.IsLinux() ? OperatingSystemId.Linux : OperatingSystemId.Windows;
  }

  // call from Program.Main()
  public void RunApplicationBeforeMainLoop()
  {
    UiUtil.Initialize();
    OnRunBeforeMainLoop();
  }

  private void OnRunBeforeMainLoop()
  {
  }


  // call from Program.Main()
  public void RunApplicationAfterMainLoop()
  {
    RunApplicationAfterMainLoopAsync();
  }

  public async void RunApplicationAfterMainLoopAsync()
  {
    ConfigureTypeSources();
    ConfigureCaliburn();
    OnRunBeforeHelper();

    // run helper
    //--------------------
    using (var runHelper = CreateRunHelper())
    {
      await runHelper.Run();
    }
    //--------------------

    await RunApplicationFinish();
  }

  private void ConfigureTypeSources()
  {
    Assemblies1 = SelectAssemblies1().ToArray();
    Assemblies2 = SelectAssemblies2().ToArray();
    AllAssemblies = Assemblies1.Union(Assemblies2).ToArray();

    AssemblySource.FindTypeByNames = names =>
    {
      if (names == null || _typeNameCache == null)
      {
        return null;
      }
      var type = names.Select(n => _typeNameCache.GetValueOrDefault(n)).FirstOrDefault(t => t != null);
      return type;
    };
  }

  private void ConfigureCaliburn()
  {

    {
      var old = ViewLocator.LocateForModel;
      ViewLocator.LocateForModel = (model, displayLocation, context) =>
      {
        // если в context пришел Type, то это будет окончательный выбор ViewType и здесь ничего не трогать!

        // насильственное переопределение context
        if (!(context is Type))
        {
          if (model is IViewLocatorAssistant assistant && assistant.Enabled)
            context = assistant.ViewType ?? assistant.ViewContext;
        }

        return old(model, displayLocation, context);
      };
    }

    {
      // Настройка: 
      //   1) context is Type => ViewType // например, ViewModel может реализовать IViewLocatorAssistant
      //   2) +use ViewTypeAttribute
      //   3) ViewModel и View можно размещать в одной папке
      var old = ViewLocator.LocateTypeForModelType;
      ViewLocator.LocateTypeForModelType = (modelType, displayLocation, context) =>
      {
        // 1) context is Type = Got

        if (context is Type type)
          return type;

        // ищем ViewTypeAttribute

        // 2) ViewTypeAttribute 

        var array = modelType.GetAttributes<ViewTypeAttribute>(true).Where(it => it.ViewType != null).ToArray();
        if (array.Length > 0)
        {
          var attribute = array.FirstOrDefault(it => it.IdentityTest(context)) ?? array[0];
          return attribute.ViewType;
        }

        // 3) ищем View в одном namespace с ViewModel // только если context = null
        if (context == null)
        {
          var viewTypeName = modelType.FullName;
          if (viewTypeName != null && viewTypeName.EndsWith("Model"))
          {
            viewTypeName = viewTypeName.Remove(viewTypeName.Length - 5);
            var viewType = modelType.Assembly.GetType(viewTypeName); //var viewType = Type.GetType(viewTypeName);
            if (viewType != null)
              return viewType;
          }
        }

        // Это плохо, далее затратный способ поиска типа
        //AppConsole.WriteTime(MessageAspects.Warning, $"Затратный способ определения ViewType для {modelType}");

        EnsureTypeNameCache();

        return old(modelType, displayLocation, context);
      };
    }

  }

  private void OnRunBeforeHelper()
  {
  }

  private async Task RunApplicationFinish()
  {
    Lifetime.Exit += OnExit;

    await Shell.ShowPage(VarKeys.DummyPage);
  }


  private IEnumerable<Assembly> SelectAssemblies1()
  {
    yield return Assembly.GetEntryAssembly()!;
  }

  private IEnumerable<Assembly> SelectAssemblies2()
  {
    yield return typeof(IInfrastructure).Assembly;
    //yield return typeof(Iface.ClientFramework.Avalonia.Interfaces.ICoreInfrastructure).Assembly;
  }

  private void EnsureTypeNameCache()
  {
    if (_typeNameCache != null)
    {
      return;
    }
    _typeNameCache = new Dictionary<string, Type>();
    AllAssemblies.SelectMany(ExtractTypesForCache).Apply(t => _typeNameCache.Add(t.FullName, t));
  }

  private IEnumerable<Type> ExtractTypesForCache(Assembly assembly)
  {
    return ExtractViewTypes(assembly); // return Enumerable.Empty<Type>();
  }

  private static IEnumerable<Type> ExtractViewTypes(Assembly assembly)
  {
    return assembly.GetExportedTypes().Where(t => typeof(Control).IsAssignableFrom(t));
  }

  private void ApplyServiceProvider(ServiceProvider serviceProvider)
  {
    ServiceProvider = serviceProvider;

    IoC.GetInstance = GetInstance;
    IoC.GetAllInstances = GetAllInstances;
    IoC.BuildUp = BuildUp;
  }


  private object GetInstance(Type service, string key)
  {
    //return _container.GetService(service)!; // убого без строковых контрактов
    var result = Locator.Current.GetService(service, key);

    if (result == null)
    {
      //throw new Exception("Could not locate any instances of the contract.");
    }

    return result;
  }

  private IEnumerable<object> GetAllInstances(Type service)
  {
    //return _container.GetServices(service)!;
    //yield return xx;

    return Locator.Current.GetServices(service);
  }

  private void BuildUp(object instance) // todo
  {
  }


  public void OnFirstWindowBefore()
  {
  }

  private ResourceDictionary _topResources;
  private ResourceDictionary _appResources;
  
  private void OnShellCreated()
  {
  }

  void IInfrastructureInternal.OnShellLoadedBegin() { OnShellLoadedBegin(); }
  private void OnShellLoadedBegin()
  {
    MainWindowViewModel.DoStep1();
    PrepareShellButtons();
  }

  // UI thread
  void IInfrastructureInternal.OnShellLoadedEnd() { OnShellLoadedEnd(); }
  private async void OnShellLoadedEnd()
  {
    MainWindowViewModel.DoStep1();
    MainWindowViewModel.DoStep2();
    try
    {
      await DoLoginAfterStep2(isApplicationStart: true);
    }
    catch (Exception e)
    {
      // todo: Log
      try
      {
        Console.WriteLine(e);
      }
      catch
      {
      }

      //ShutdownApplication();
    }
    finally
    {
      OnAppLoaded();
    }
  }

  //----------------------------------------------------

  private bool _isLoginAfterDone;
  public bool IsLoginAfterDone => _isLoginAfterDone;

  // UI thread
  private async Task DoLoginAfterStep2(bool isApplicationStart)
  {
    _isLoginAfterDone = false;

    // (start 1)

    var cnt = 0;
    while (true)
    {
      try
      {
      }
      catch (Exception e)
      {
        if (cnt++ == 10)
        {
          throw;
        }

        Console.WriteLine(e);
        continue;
      }
      break;
    }

    _isLoginAfterDone = true;
  }

  private void OnAppLoaded() // UI thread
  {
    //GlobalUtil.ConsoleThread(nameof(OnAppLoaded));
  }

  private void PrepareShellButtons()
  {
  }

  #endregion

  #region Db-based-configuration Service // Singleton-DocFactories and so on

  private void ProceedDbBasedConfiguration()
  {
  }

  // both LiveEvents & LiveAlarms
  public virtual void ShowPageForLiveEventMonitor(string monitorId = null, bool useSingleton = true, IWorkspaceHolder preferredHolder = null)
  {
  }

  #endregion


  #region Run Helper

  //=============================
  private IRunHelper CreateRunHelper()
    => new RunHelper(this);

  //=============================
  private class RunHelper : IRunHelper
  {
    private Infrastructure AppInfrastructure { get; }
    private App Application => AppInfrastructure.Application;
    private IClassicDesktopStyleApplicationLifetime Lifetime => AppInfrastructure.Lifetime;
    private string[] CommandLineArgs => GetCommandLineArgs();

    public Stopwatch? Watch { get; private set; }

    // позиционные параметры
    public string CommandLineStr1 { get; private set; }
    public string CommandLineStr2 { get; private set; }

    //public string P1 { get; private set; }
    //public string P2 { get; private set; }
    public int UserId { get; private set; }


    public RunHelper(Infrastructure appInfrastructure)
    {
      AppInfrastructure = appInfrastructure;
    }

    private string[] GetCommandLineArgs() => Lifetime.Args;


    public async Task Run()
    {
      if (!await RunCore())
      {
        Shutdown();
      }
    }

    private async Task<bool> RunCore()
    {
      if (!await Prepare())
      {
        return false;
      }

      await BeforeLoginBlock1();
      await BeforeLoginBlock2();

      if (!await Finish())
      {
        return false;
      }

      return true;
    }

    // false = break Run
    private Task<bool> Prepare()
    {
      PlatformProvider.Current = new CoreXamlPlatformProvider();
      return Task.FromResult(true);
    }

    private Task BeforeLoginBlock1()
    {
      var services = new ServiceCollection();
      RegisterServices0(services);
      RegisterServices1(services);
      RegisterServicesAuto(services); // Собираем по аттрибутам [CreationPolicy] [Service]

      services.UseMicrosoftDependencyResolver(); // экземпляр services далее не использовать
      Locator.CurrentMutable.InitializeSplat();
      Locator.CurrentMutable.InitializeReactiveUI();

      var serviceProvider = services.BuildServiceProvider();

      // Since MS DI container is a different type, we need to re-register the built container with Splat again
      serviceProvider.UseMicrosoftDependencyResolver(); // запечатывает MicrosoftDependencyResolver._isImmutable=true в Locator.Current


      RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;

      AppInfrastructure.ApplyServiceProvider(serviceProvider);

      AppInfrastructure.EnsureTypeNameCache();

      return Task.CompletedTask;
    }

    private void RegisterServices0(IServiceCollection services)
    {
      // for such signature (with implementationFactory) AddTransient and AddSingleton have the same result
      //   AddSingleton - вызывает фабрику один раз и запоминает ссылку
      //   AddTransient - каждый раз вызывает фабрику
      services.AddTransient(typeof(IInfrastructureInternal), _ => AppInfrastructure);
      services.AddTransient(typeof(IInfrastructure), _ => AppInfrastructure);
    }


    private void RegisterServices1(IServiceCollection services)
    {
    }

    // Собираем по аттрибутам [TransientInstance] [SingletonInstance] [Contract]
    private void RegisterServicesAuto(IServiceCollection services) 
    {
      var dict = new Dictionary<SingletonInstanceAttribute, Type>(); // attr -> implType

      foreach (var implType in AppInfrastructure.AllAssemblies.SelectMany(ExtractServiceTypes))
      {
        // [SingletonInstance]
        var attrs = implType.GetCustomAttributes(inherit: true)
          .OfType<SingletonInstanceAttribute>()
          .Where(a=> a.IsConform(AppInfrastructure.OperatingSystemId))
          .ToArray();

        if (attrs.Length > 0)
        {
          var attr = attrs[0];
          if (attr.IsDisabled)
          {
            continue; // all others must be IsDisabled also
          }

          if (!dict.TryGetValue(attr, out var oldType))
          {
            dict.Add(attr, implType);
          }
          else
          {
            if (oldType.IsAssignableFrom(implType))
            {
              dict.Remove(attr);
              dict.Add(attr, implType);
            }
          }

          for (var i = 1; i < attrs.Length; i++)
          {
            attrs[i].IsDisabled = true;
          }

          continue;
        }

        // [TransientInstance] 
        var transientInstanceAttribute = implType
          .GetCustomAttributes(inherit: false)
          .OfType<TransientInstanceAttribute>()
          .FirstOrDefault(a => a.IsConform(AppInfrastructure.OperatingSystemId));

        if (transientInstanceAttribute != null)
        {
          // Collect Contracts from [Contract] attributes // у IServiceCollection нет возможности использовать (string contract), как это есть у IDependencyResolver
          RegisterService(services, implType, implType.GetCustomAttributes(inherit: false).OfType<ContractAttribute>()
              .Where(a => a.ContractType != null).Select(a => a.ContractType!).ToArray()
            , isSingleton: false);

        }
      }

      foreach (var implType in dict.Values)
      {
        // Collect Contracts from [Contract] attributes // у IServiceCollection нет возможности использовать (string contract), как это есть у IDependencyResolver
        RegisterService(services, implType, implType.GetCustomAttributes(inherit: true).OfType<ContractAttribute>()
            .Where(a => a.ContractType != null).Select(a => a.ContractType!).ToArray()
          , isSingleton: true);
      }
    }

    private void RegisterService(IServiceCollection services, Type implementationType, Type[] contractTypes, bool isSingleton)
    {
      if (isSingleton)
      {
        services.AddSingleton(implementationType, implementationType);

        foreach (var contractType in contractTypes)
        {
          if (contractType == implementationType)
            continue;
          services.AddSingleton(contractType, provider => provider.GetService(implementationType)); // or better AddTransient() ?
        }
      }
      else
      {
        if (contractTypes.Length == 0)
        {
          services.AddTransient(implementationType, implementationType);
          return;
        }
        foreach (var contractType in contractTypes)
        {
          services.AddTransient(contractType, implementationType); // todo: check contractType.IsAssignableFrom(implementationType)
        }
      }
    }

    static IEnumerable<Type> ExtractServiceTypes(Assembly assembly)
    {
      return assembly.GetExportedTypes()
        .Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface);
    }

    private Task BeforeLoginBlock2()
    {
      AppInfrastructure.WindowService = IoC.Get<IWindowService>();
      AppInfrastructure.WindowModalService = IoC.Get<IGlobalModalService>();

      AppInfrastructure.PageFactoryService = new ScreenFactoryService<IPage>(AppInfrastructure).SetScreenRoles(ScreenRoles.Page);
      AutoRegisterScreenFactories(); // Собираем фабрики документов по аттрибутам
      ModifyPageFactories(AppInfrastructure.PageFactoryService);

      // todo ? может в этой точке добалять ресурсы в Application.Current.Resources ?
      AppInfrastructure.OnFirstWindowBefore();
      return Task.CompletedTask;
    }


    //----------------------------------------------------
    private async Task<bool> Finish()
    {
      //------------------------
      var shell = IoC.Get<Shell>();
      await shell.OnImportsSatisfiedAsync();
      await shell.InternalActivateAsync();
      AppInfrastructure.Shell = shell;
      AppInfrastructure.OnShellCreated();
      //------------------------
      var mainWindowViewModel = IoC.Get<MainWindowVm>();
      await mainWindowViewModel.ActivateShell();
      var mainWindow = await AppInfrastructure.WindowService.PrepareWindowAsync(mainWindowViewModel);
      AppInfrastructure.MainWindowViewModel = mainWindowViewModel;
      AppInfrastructure.MainWindow = mainWindow;
      //------------------------

      Lifetime.MainWindow = mainWindow;
      Lifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;

      mainWindow.Show();

      if (Watch != null)
      {
        Watch.Stop();
        AppConsole.WriteTime(MessageAspects.Important, $"From Start = {Watch.ElapsedMilliseconds} ms, Run Helper Finish");
        Watch = null;
      }

      return true;
    }



    private void Shutdown()
    {
      if (Lifetime == null) return;

      Lifetime.Shutdown();
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
      if (disposing)
      {
        ReleaseManagedResources();
      }
    }

    private void ReleaseManagedResources()
    {
    }


    private void AutoRegisterScreenFactories() // Собираем фабрики документов по аттрибутам
    {
      var infr = AppInfrastructure;

      var types = infr.AllAssemblies.SelectMany(ExtractTypes);

      foreach (var type in types)
      {
        var screenAttr = type.GetCustomAttributes(inherit: false).OfType<MasterScreenAttribute>().FirstOrDefault();
        if (screenAttr == null)
        {
          continue;
        }
        if (!screenAttr.TryGetFactoryId(out var factoryId))
        {
          continue;
        }

        // 1. Resolve docType // required

        var screenType = screenAttr.ContractType;
        ITypeResolver screenTypeResolver = null;
        IDataIdResolver dataIdResolver = null;

        if (screenType == null)
        {
          var exportAttr = type.GetCustomAttributes(inherit: false).OfType<ContractAttribute>().FirstOrDefault();
          if (exportAttr != null)
          {
            screenType = exportAttr.ContractType;
          }
        }

        if (screenType == null)
        {
          if (typeof(ITypeResolver).IsAssignableFrom(type))
          {
            screenTypeResolver = (ITypeResolver)Activator.CreateInstance(type);
          }
          else
          {
            continue;
          }
        }

        // 2. Resolve docId // optional

        if (!screenAttr.TryGetDataId(out var dataId))
        {
          dataId = null;
          if (screenTypeResolver != null)
          {
            dataIdResolver = screenTypeResolver as IDataIdResolver;
          }
          else if (typeof(IDataIdResolver).IsAssignableFrom(type))
          {
            dataIdResolver = (IDataIdResolver)(Activator.CreateInstance(type));
          }
        }

        switch (screenAttr)
        {
          case PageAttribute attr:
            {
              if (infr.PageFactoryService.FindFactory(factoryId) != null)
                continue;

              var factory = infr.PageFactoryService.RegisterFactory(factoryId, screenAttr.FactoryName)
                .SetIsEmptyClass(attr.IsEmptyClass)
                .SetConf(conf => conf.SetCreatorByIoC(screenType, screenTypeResolver).SetDataId(dataId).SetDataIdResolver(dataIdResolver))
                .AsFactory();

              break;
            }
        }
      }
    }

    static IEnumerable<Type> ExtractTypes(Assembly assembly)
    {
      return assembly.GetExportedTypes()
        .Where(t => typeof(IAutoDiscover).IsAssignableFrom(t) //  INotifyPropertyChanged
                    && t.IsClass && !t.IsAbstract && !t.IsInterface);
    }

    private void ModifyPageFactories(IScreenFactoryService<IPage> factoryService)
    { }


  }
  //=============================

  #endregion






  #region Creation Service

  public virtual async Task<IWorkspaceHolder> CreateWorkspaceHolder(IWorkspace workspace = null, WorkspaceHolderLockSeverity lockSeverity = WorkspaceHolderLockSeverity.None)
  {
    var holder = new WorkspaceHolder(this);
    if (workspace != null)
    {
      await holder.ActivateWorkspaceAsync(workspace);
    }
    await holder.InternalActivateAsync();
    return holder;
  }

  Task<Workspace> IInfrastructureInternal.CreateWorkspace() => CreateWorkspace();
  private async Task<Workspace> CreateWorkspace()
  {
    var workspace = new Workspace(this);
    await workspace.InternalActivateAsync();
    return workspace;
  }

  #endregion


  #region Global Modal Mode Support

  private int _modalLevel;

  public bool IsModalMode => _modalLevel > 0;


  public void EndModalDialog()
  {
    _modalLevel--;
    if (_modalLevel > 0)
    {
      return;
    }
    if (_modalLevel < 0)
    {
      _modalLevel = 0;
    }
  }

  #endregion


  #region Collect Context Menu

  public virtual void CollectContextMenu(CollectorContext context)
  {
    if (context.WorkspaceHolder != null)
    {
      CollectContextMenuForWorkspace(context, context.WorkspaceHolder, context.WorkspaceHolder.Workspace);
      return;
    }
  }

  private void CollectContextMenuForWorkspace(CollectorContext context,
                                              IWorkspaceHolder workspaceHolder, IWorkspace workspace)
  {
    if (workspaceHolder.OwnerType == WorkspaceOwnerType.ShellTabs)
    {
      context.ContainerItems.Add(new CommandItem
      {
        DisplayName = "Move to new Window",
        Command = new SimpleCommand(p => { Shell.ToggleWorkspaceHolder(workspaceHolder); }),
      });
    }

    else if (workspaceHolder.OwnerType == WorkspaceOwnerType.ShellWindows)
    {
      context.ContainerItems.Add(new CommandItem
      {
        DisplayName = "Move to TabItem",
        Command = new SimpleCommand(p => { Shell.ToggleWorkspaceHolder(workspaceHolder); }),
      });
    }


    if (workspace != null)
    {
      var bed = workspace.Bed;
      bed?.CollectContextMenu(context); // Документ-подложка, можешь поучаствовать
    }
  }


  #endregion


  #region Commands

  public virtual async Task<VarKey> InputDataIdAsync(KeyInputMethod inputMethod, VarKey initialValue)
  {
    // todo trash
    switch (inputMethod)
    {
      case KeyInputMethod.None:
        return VarKey.Empty;
    }


    return VarKey.Empty;
    /*
    //------
    var text = await WindowModalService.InputStringAsync("Ввод идентификатора", "ИД", initialValue?.SerializeToString());
    //------
    return VarKey.Deserialize(text);
    */
  }

  #endregion


  #region Создание документов // Sync // todo: remove?

  #region Core

  // Реализация создания документа любого типа
  public static T MakeScreenCore<T>(IScreenFactoryService factoryService, VarKey factoryId, Type iocType, VarKey dataId, object dataContext, object owner
    , bool waitContent = false // added 29 march 2019 // установка в true при вызове из UI-Thread чревато блокировкой потока
    , bool inLoading = false // added 4 april 2019
    , Action<T> customSetup = null // added 4 march 2022
    ) where T : class, IMasterScreen
  {
    T? obj = null;

    var factory = factoryService?.FindFactory(factoryId);

    // Make by Factory
    if (factory != null)
    {
      obj = factory.MakeScreen<T>(dataId, dataContext, waitContent, inLoading, customSetup);
    }

    // Make by IoC
    if (obj == null && iocType != null)
    {
      var objx = IoC.GetInstance(iocType, null);
      obj = objx as T;

      if (obj == null)
      {
        throw new SystemException("ERROR");
      }

      if (!VarKey.IsNull(dataId) || dataContext != null)
      {
        var task = obj.TryChangeDataIdAsync(dataId, dataContext);
        if (waitContent)
        {
          task.Wait();
        }
      }
    }

    if (obj == null)
    {
      throw new SystemException($"Invalid factory {factoryId}");
    }

    obj.Owner = owner;
    return obj;
  }


  public IMasterScreen MakeScreenForRole(ScreenRole role, VarKey factoryId, Type iocType, VarKey dataId, object dataContext
    , bool waitContent // added 29 march 2019
    , bool inLoading // added 4 april 2019
  )
  {
    IScreenFactoryService factoryService = PageFactoryService;
    return MakeScreenCore<IMasterScreen>(factoryService, factoryId, iocType, dataId, dataContext, null, waitContent, inLoading);
  }


  #endregion

  #region Make Any Page (Regular or Widget)

  public T MakeScreen<T>(IScreenFactoryService factoryService, VarKey factoryId, VarKey dataId, object dataContext
    , Action<T> customSetup = null) where T : class, IMasterScreen
  {
    return MakeScreenCore<T>(factoryService ?? PageFactoryService, factoryId, null, dataId, dataContext, null, false, false, customSetup);
  }

  #endregion


  #region Make Regular Page

  // 1) Factory 2) IoC=iocType
  public T MakePage<T>(VarKey factoryId, Type iocType, VarKey dataId, object dataContext, Action<T> customSetup = null) where T : class, IPage
  {
    return MakeScreenCore<T>(PageFactoryService, factoryId, iocType, dataId, dataContext, null, false, false, customSetup);
  }

  // 1) Factory 2) IoC=<T>
  public T MakePage<T>(VarKey factoryId, VarKey dataId = null, object dataContext = null, Action<T> customSetup = null) where T : class, IPage
  {
    return MakeScreenCore<T>(PageFactoryService, factoryId ?? VarKey.Empty, typeof(T), dataId, dataContext, null, false, false, customSetup);
  }

  // IoC=<T> // Without Factory
  public T MakePageByIoC<T>(VarKey dataId = null, object dataContext = null) where T : class, IPage
  {
    return MakeScreenCore<T>(null, null, typeof(T), dataId, dataContext, null);
  }

  // 1) Factory 2) IoC=iocType
  public IPage MakePage(VarKey factoryId, Type iocType = null, VarKey dataId = null, object dataContext = null, Action<IPage> customSetup = null)
  {
    return MakeScreenCore<IPage>(PageFactoryService, factoryId, iocType, dataId, dataContext, null, false, false, customSetup);
  }
  #endregion

  
  #endregion


  #region Создание документов // Async

  #region Core

  // Реализация создания документа любого типа
  public static async Task<T> MakeScreenCoreAsync<T>(IScreenFactoryService factoryService, VarKey factoryId, Type iocType, VarKey dataId, object dataContext
    , object owner
    , bool inLoading = false
  ) where T : class, IMasterScreen
  {
    T obj = null;

    var factory = factoryService?.FindFactory(factoryId);

    // Make by Factory
    if (factory != null)
    {
      obj = await factory.MakeScreenAsync<T>(dataId, dataContext, inLoading);
    }

    // Make by IoC
    if (obj == null && iocType != null)
    {
      var objx = IoC.GetInstance(iocType, null);
      obj = objx as T;

      if (obj == null)
      {
        throw new SystemException("ERROR");
      }

      if (!VarKey.IsNull(dataId) || dataContext != null)
      {
        await obj.TryChangeDataIdAsync(dataId, dataContext);
      }
    }

    if (obj == null)
    {
      throw new SystemException($"Invalid factory {factoryId}");
    }

    obj.Owner = owner;
    return obj;
  }


  public async Task<IMasterScreen> MakeScreenForRoleAsync(ScreenRole role, VarKey factoryId, Type iocType, VarKey dataId, object dataContext
    , bool inLoading
  )
  {
    IScreenFactoryService factoryService = PageFactoryService;
    return await MakeScreenCoreAsync<IMasterScreen>(factoryService, factoryId, iocType, dataId, dataContext, null, inLoading);
  }


  #endregion

  #region Make Any Page (Regular or Widget)

  public async Task<T> MakeScreenAsync<T>(IScreenFactoryService factoryService, VarKey factoryId, VarKey dataId, object dataContext) 
    where T : class, IMasterScreen
  {
    return await MakeScreenCoreAsync<T>(factoryService ?? PageFactoryService, factoryId, null, dataId, dataContext, null, false);
  }

  #endregion


  #region Make Regular Page

  // 1) Factory 2) IoC=iocType
  public async Task<T> MakePageAsync<T>(VarKey factoryId, Type iocType, VarKey dataId, object dataContext) where T : class, IPage
  {
    return await MakeScreenCoreAsync<T>(PageFactoryService, factoryId, iocType, dataId, dataContext, null, false);
  }

  // 1) Factory 2) IoC=<T>
  public async Task<T> MakePageAsync<T>(VarKey factoryId, VarKey dataId = null, object dataContext = null) where T : class, IPage
  {
    return await MakeScreenCoreAsync<T>(PageFactoryService, factoryId ?? VarKey.Empty, typeof(T), dataId, dataContext, null, false);
  }

  // IoC=<T> // Without Factory
  public async Task<T> MakePageByIoCAsync<T>(VarKey dataId = null, object dataContext = null) where T : class, IPage
  {
    return await MakeScreenCoreAsync<T>(null, null, typeof(T), dataId, dataContext, null);
  }

  // 1) Factory 2) IoC=iocType
  public async Task<IPage> MakePageAsync(VarKey factoryId, Type iocType = null, VarKey dataId = null, object dataContext = null)
  {
    return await MakeScreenCoreAsync<IPage>(PageFactoryService, factoryId, iocType, dataId, dataContext, null, false);
  }
  #endregion


  #endregion



  #region Close, Dispose

  public bool InShutdown { get; private set; }
  public bool InReLogin { get; private set; }
  public UserAction UserAction { get; set; }

  public void ShutdownApplication(bool explicitShutdown) // без сохранения среды
  {
    InShutdown = true;
    if (explicitShutdown)
    {
      //Lifetime.TryShutdown();
      Lifetime.Shutdown();
      return;
    }
    CloseApplication();
  }

  public void CloseApplication()
  {
    MainWindow?.Close();
  }

  private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
  {
  }


  public void Dispose() // called by AppBootstrapperBase.OnExit()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  ~Infrastructure()
  {
    Dispose(false);
  }

  private void Dispose(bool disposing)
  {
    ReleaseUnmanagedResources();
    if (disposing)
    {
      ReleaseManagedResources();
    }
  }

  private void ReleaseUnmanagedResources()
  {
  }

  private void ReleaseManagedResources()
  {
  }

  #endregion

}
