using Caliburn.Micro;
using AppNs.Interfaces;
using Iface.Utils;

namespace AppNs.CoreNs;

#region Interfaces // todo: Move to Interfaces-folder
//=================================================
// фабрика
public interface IScreenFactory
{
  VarKey FactoryId { get; }
  string FactoryName { get; } // Display

  ScreenRoles ScreenRoles { get; }
  bool HasRole(ScreenRole role);
  bool IsShowInLists { get; }

  uint GroupId { get; set; }
  int OrderIx { get; set; }
  Uri IconUri { get; set; }
  char Glyph { get; set; }
  bool IsEmptyClass { get; set; }
  IShowPageConf ShowPageConf { get; }
  void ConfigureShow(ISingletonService singletonService, string singletonId);
  void ConfigureShowForSplit(ISingletonService singletonService, string singletonId, Sides side, double bandWidth);
  IScreenFactoryConf GetConfUnsafe();

  void ClearCache();

  // Sync // todo: remove?

  IMasterScreen MakeScreen(VarKey? dataId = null, object? dataContext = null, bool waitContent = false
    , Action<IMasterScreen>? customSetup = null);

  TResult MakeScreen<TResult>(VarKey? dataId = null, object? dataContext = null, bool waitContent = false, bool inLoading = false
    , Action<TResult>? customSetup = null) where TResult : class, IMasterScreen;

  // Async

  Task<IMasterScreen> MakeScreenAsync(VarKey? dataId = null, object? dataContext = null);
  Task<TResult> MakeScreenAsync<TResult>(VarKey? dataId = null, object? dataContext = null, bool inLoading = false) where TResult : class, IMasterScreen;
}

//----------------------------
public interface IScreenFactory<out T> : IScreenFactory where T : class, IMasterScreen
{
  // Sync
  T MakeScreen(VarKey? dataId = null, object? dataContext = null, bool waitContent = false, Action<T>? customSetup = null);

  // Async
  //Task<T> MakeDocumentAsync(VarKey? dataId = null, object? documentContext = null); // compile error
}

//----------------------------
public interface IShowPageConf
{
  ISingletonService SingletonService { get; }
  string SingletonId { get; }

  // for show in shell split layout
  bool IsSplit { get; }
  Sides Side { get; }
  double BandWidth { get; }
}

//=================================================
// определение фабрики
public interface IScreenFactoryDef<T> where T : class, IMasterScreen
{
  // Page Roles
  IScreenFactoryDef<T> SetRoles(ScreenRoles roles); // None = Установить все возможные роли
  IScreenFactoryDef<T> ClearRoles();

  //-------------------------
  IScreenFactoryDef<T> SetConf(Action<IScreenFactoryConf<T>> confFillAction); // conf => Здесь нужно настроить конфигуратор
                                                                        //-------------------------
  IScreenFactoryDef<T> ShowInLists(bool value);
  IScreenFactoryDef<T> SetGroupId(uint value);
  IScreenFactoryDef<T> SetOrderIx(int value);
  IScreenFactoryDef<T> SetIconUri(Uri value);
  IScreenFactoryDef<T> SetGlyph(char value);
  IScreenFactoryDef<T> SetIsEmptyClass(bool value);
  IScreenFactoryDef<T> ConfigureShow(ISingletonService singletonService, string singletonId);
  IScreenFactoryDef<T> ConfigureShowForSplit(ISingletonService singletonService, string singletonId, Sides side, double bandWidth);

  IScreenFactory AsFactory();
}

//=================================================
// конфигуратор фабрики (задается внутри определения фабрики)
public interface IScreenFactoryConf<T> where T : class, IMasterScreen
{
  IScreenFactoryConf<T> SetDataId(VarKey dataId);
  IScreenFactoryConf<T> SetGroup(string screenGroup);
  IScreenFactoryConf<T> SetViewContext(string context);
  IScreenFactoryConf<T> SetCreator(Func<T> func);
  IScreenFactoryConf<T> SetCreatorByIoC(); // iocType = T
  IScreenFactoryConf<T> SetCreatorByIoC<TIocType>() where TIocType : T;
  IScreenFactoryConf<T> SetCreatorByIoC(Type iocType, string key = null);
  IScreenFactoryConf<T> SetCreatorByIoC(Type iocType, ITypeResolver iocTypeResolver, string key = null);
  IScreenFactoryConf<T> SetDataIdResolver(IDataIdResolver dataIdResolver);
  IScreenFactoryConf<T> SetProcessor(Action<T> action); // (doc)
}

public interface IScreenFactoryConf
{
  IScreenFactoryConf SetDataId(VarKey dataId);
}

//=================================================
// сервис фабрик
public interface IScreenFactoryService
{
  IScreenFactory FindFactory(VarKey factoryId);
}

public interface IScreenFactoryService<TBase> : IScreenFactoryService where TBase : class, IMasterScreen
{
  ScreenRoles ScreenRoles { get; }

  IScreenFactoryDef<TBase> RegisterFactory(VarKey factoryId, string factoryName); // use: +SetConf(c=>c.SetCreator(...))
  IScreenFactoryDef<T> RegisterFactory<T>(VarKey factoryId, string factoryName) where T : class, TBase;
  void RemoveFactory(VarKey factoryId);
  void Sort();

  IEnumerable<IScreenFactory<TBase>> GetFactories();
  new IScreenFactory<TBase> FindFactory(VarKey factoryId);
  void ClearCache();

  /* service for creation of standalone factories ONLY!
   * usage:
   *    var def = Infr.RegularDocFactoryService.CreateFactoryDef().SetDataId(...).SetCreatorByIoC(...);
   *    var factory = def.AsFactory();    */
  IScreenFactoryDef<TBase> CreateFactoryDef();
  IScreenFactoryDef<T> CreateFactoryDef<T>() where T : class, TBase;
}
#endregion

//====================================================================================
public class ScreenFactoryService<TBase> : IScreenFactoryService<TBase> where TBase : class, IMasterScreen
{
  readonly IInfrastructure _infr;
  readonly Dictionary<VarKey, IScreenFactory<TBase>> _factories;
  readonly List<IScreenFactory<TBase>> _sortedFactories;

  public ScreenRoles ScreenRoles { get; private set; }

  public ScreenFactoryService(IInfrastructure infr)
  {
    _infr = infr;
    _factories = new Dictionary<VarKey, IScreenFactory<TBase>>();
    _sortedFactories = new List<IScreenFactory<TBase>>();
  }

  #region Registration Stage

  public ScreenFactoryService<TBase> SetScreenRoles(ScreenRoles roles)
  {
    ScreenRoles = roles;
    return this;
  }

  public IScreenFactoryDef<TBase> RegisterFactory(VarKey factoryId, string factoryName)
  {
    return RegisterFactory<TBase>(factoryId, factoryName);
  }

  public IScreenFactoryDef<T> RegisterFactory<T>(VarKey factoryId, string factoryName) where T : class, TBase
  {
    var factory = new Factory<T>(this) { FactoryId = factoryId, FactoryName = factoryName };
    _factories.Add(factoryId, factory);
    _sortedFactories.Add(factory);
    return factory;
  }

  public IScreenFactoryDef<TBase> CreateFactoryDef()
  {
    return CreateFactoryDef<TBase>();
  }

  public IScreenFactoryDef<T> CreateFactoryDef<T>() where T : class, TBase
  {
    var factory = new Factory<T>(this);
    return factory;
  }

  public void RemoveFactory(VarKey factoryId)
  {
    var factory = FindFactory(factoryId);
    if (factory == null) return;
    _factories.Remove(factoryId);
    _sortedFactories.Remove(factory);
  }

  public void Sort()
  {
    _sortedFactories.Sort((it1, it2) => it1.OrderIx.CompareTo(it2.OrderIx));
  }
  #endregion

  #region Usage Stage

  public IEnumerable<IScreenFactory<TBase>> GetFactories() => _sortedFactories;

  IScreenFactory IScreenFactoryService.FindFactory(VarKey factoryId)
  {
    return FindFactory(factoryId);
  }

  public IScreenFactory<TBase> FindFactory(VarKey factoryId)
  {
    if (VarKey.IsNull(factoryId)) return null;
    return _factories.TryGetValue(factoryId, out var factory) ? factory : null;
  }

  public void ClearCache()
  {
    _factories.Values.ForEach(it => it.ClearCache());
  }

  #endregion

  //====================================================================================
  private class Factory<T> : IScreenFactory<T>, IScreenFactoryDef<T> where T : class, TBase
  {
    private readonly ScreenFactoryService<TBase> _owner;
    private Action<IScreenFactoryConf<T>> _confFillAction;
    private Conf<T> _conf;
    private ShowPageConf _showPageConf;

    public VarKey FactoryId { get; set; }
    public string FactoryName { get; set; } // FactoryDisplayName

    public ScreenRoles ScreenRoles { get; private set; }
    public bool IsShowInLists { get; private set; }
    public uint GroupId { get; set; }
    public int OrderIx { get; set; }
    public Uri IconUri { get; set; }
    public char Glyph { get; set; }
    public bool IsEmptyClass { get; set; }
    IShowPageConf IScreenFactory.ShowPageConf => _showPageConf;

    public Factory(ScreenFactoryService<TBase> owner)
    {
      _owner = owner;
      IsShowInLists = true;
      ScreenRoles = owner.ScreenRoles;
    }

    IScreenFactory IScreenFactoryDef<T>.AsFactory()
    {
      return this;
    }

    public void ClearCache()
    {
      _conf = null;
    }

    #region Page Roles

    // None = Установить все возможные роли
    public IScreenFactoryDef<T> SetRoles(ScreenRoles roles)
    {
      if (roles == ScreenRoles.None)
      {
        ScreenRoles = _owner.ScreenRoles; // Установили все возможные роли
      }
      else
      {
        ScreenRoles = roles & _owner.ScreenRoles;
      }
      return this;
    }

    public IScreenFactoryDef<T> ClearRoles()
    {
      ScreenRoles = 0;
      return this;
    }

    public bool HasRole(ScreenRole role)
    {
      var roles = ScreenRoles.Roles().ToArray();
      var ff = roles.Contains(role);
      return ff;
    }

    #endregion

    public IScreenFactoryDef<T> SetConf(Action<IScreenFactoryConf<T>> confFillAction)
    {
      _confFillAction = confFillAction;
      return this;
    }

    private Conf<T> GetConf()
    {
      if (_conf == null)
      {
        _conf = new Conf<T>();
        //_conf.SetCreatorByIoC();
        _confFillAction?.Invoke(_conf);
      }
      return _conf;
    }

    IScreenFactoryConf IScreenFactory.GetConfUnsafe()
    {
      return GetConf();
    }

    public IScreenFactoryDef<T> ShowInLists(bool value)
    {
      IsShowInLists = value;
      return this;
    }

    public IScreenFactoryDef<T> SetGroupId(uint value)
    {
      GroupId = value;
      return this;
    }

    public IScreenFactoryDef<T> SetOrderIx(int value)
    {
      OrderIx = value;
      return this;
    }

    public IScreenFactoryDef<T> SetIconUri(Uri value)
    {
      IconUri = value;
      return this;
    }

    public IScreenFactoryDef<T> SetGlyph(char value)
    {
      Glyph = value;
      return this;
    }

    public IScreenFactoryDef<T> SetIsEmptyClass(bool value)
    {
      IsEmptyClass = value;
      return this;
    }

    public IScreenFactoryDef<T> ConfigureShow(ISingletonService singletonService, string singletonId)
    {
      return ConfigureShowCore(singletonService, singletonId, false, Sides.None, double.NaN);
    }

    public IScreenFactoryDef<T> ConfigureShowForSplit(ISingletonService singletonService, string singletonId, Sides side, double bandWidth)
    {
      return ConfigureShowCore(singletonService, singletonId, true, side, bandWidth);
    }

    void IScreenFactory.ConfigureShow(ISingletonService singletonService, string singletonId)
    {
      ConfigureShowCore(singletonService, singletonId, false, Sides.None, double.NaN);
    }

    void IScreenFactory.ConfigureShowForSplit(ISingletonService singletonService, string singletonId, Sides side, double bandWidth)
    {
      ConfigureShowCore(singletonService, singletonId, true, side, bandWidth);
    }

    private IScreenFactoryDef<T> ConfigureShowCore(ISingletonService singletonService, string singletonId, bool isSplit, Sides side, double bandWidth)
    {
      if (_showPageConf == null)
      {
        _showPageConf = new ShowPageConf
        {
          SingletonService = singletonService,
          SingletonId = singletonId,
          IsSplit = isSplit,
          Side = side,
          BandWidth = bandWidth,
        };
      }
      return this;
    }


    //----------------------------- Sync // WPF legacy // todo: remove?
    T IScreenFactory<T>.MakeScreen(VarKey? dataId, object? dataContext, bool waitContent, Action<T>? customSetup)
    {
      return MakeScreenImplSync(dataId, dataContext, waitContent, false, customSetup);
    }

    IMasterScreen IScreenFactory.MakeScreen(VarKey? dataId, object? dataContext, bool waitContent, Action<IMasterScreen>? customSetup)
    {
      return MakeScreenImplSync(dataId, dataContext, waitContent, false, customSetup);
    }

    TResult IScreenFactory.MakeScreen<TResult>(VarKey? dataId, object? dataContext, bool waitContent, bool inLoading, Action<TResult>? customSetup)
    {
      Action<T> customSetup2 = null;
      if (customSetup != null)
      {
        customSetup2 = screen =>
        {
          if (!(screen is TResult screenx)) return;
          customSetup(screenx);
        };
      }

      object objx = MakeScreenImplSync(dataId, dataContext, waitContent, inLoading, customSetup2);

      if (!(objx is TResult obj))
      {
        throw new SystemException("ERROR");
      }

      return obj;
    }

    //----------------------------- Async
    async Task<IMasterScreen> IScreenFactory.MakeScreenAsync(VarKey? dataId, object? dataContext)
    {
      return await MakeScreenImplAsync(dataId, dataContext, waitContent: false, inLoading: false, customSetup: null);
    }

    async Task<TResult> IScreenFactory.MakeScreenAsync<TResult>(VarKey? dataId, object? dataContext, bool inLoading)
    {
      var objx = await MakeScreenImplAsync(dataId, dataContext, waitContent: false, inLoading, customSetup: null);

      if (!(objx is TResult obj))
      {
        throw new SystemException("ERROR");
      }

      return obj;
    }


    //------------------------------------ // WPF legacy // todo: remove?
    private T MakeScreenImplSync(VarKey? dataId, object? dataContext
      , bool waitContent // added 29 march 2019 // установка в true при вызове из UI-Thread чревато блокировкой потока
      , bool inLoading // added 4 april 2019
      , Action<T>? customSetup // added 4 march 2022
    )
    {
      // Create Doc

      var screen = CreateScreen(customSetup);

      var conf = GetConf();
      var resultDataId = dataId ?? conf.DataId;

      var task = ProceedScreenAsync(screen, resultDataId, dataContext, inLoading, inBlockingUI: waitContent);

      if (waitContent)
      {
        task.Wait();
      }

      return screen;
    }

    //------------------------------------
    private async Task<T> MakeScreenImplAsync(VarKey? dataId, object? dataContext
      , bool waitContent // todo: избавиться от синхронных реализаций MasterScreen.CustomizeImplSync() и выбросить это
      , bool inLoading
      , Action<T>? customSetup
    )
    {
      // Create Doc

      var screen = CreateScreen(customSetup);

      var conf = GetConf();
      var resultDataId = dataId ?? conf.DataId;

      await ProceedScreenAsync(screen, resultDataId, dataContext, inLoading, inBlockingUI: waitContent);

      return screen;
    }

    //------------------------------------
    private T CreateScreen(Action<T>? customSetup)
    {
      // Create Doc

      var conf = GetConf();

      var screen = conf.CreateScreen?.Invoke();

      if (screen == null)
      {
        var iocType = conf.IocType;
        if (iocType == null && conf.IocTypeResolver != null)
        {
          iocType = conf.IocTypeResolver.ResolveType();
        }
        if (iocType != null)
        {
          var scr = IoC.GetInstance(iocType, conf.IocKey);
          screen = scr as T;
        }
      }

      if (screen == null)
      {
        var scr = IoC.GetInstance(typeof(T), null);
        screen = scr as T;
      }

      if (screen == null)
      {
        throw new SystemException("ERROR");
      }

      CheckInstance(screen);

      /*
      if (doc is IDocFactoryCustomizable obj2)
        obj2.CustomizeByFactory(this);
      */

      screen.FactoryId = FactoryId;
      screen.IconUri = IconUri;

      //var doc2 = doc as IDocument;

      if (!string.IsNullOrEmpty(conf.ViewContext))
        screen.ViewContext = conf.ViewContext;

      // Process Object
      conf.ProceedAction?.Invoke(screen);
      customSetup?.Invoke(screen);

      return screen;
    }
    

    //------------------------------------
    private async Task ProceedScreenAsync(T screen, VarKey? dataId, object? dataContext
      , bool inLoading // added 4 april 2019
      , bool inBlockingUI
      )
    {
      try
      {

        var point = ScreenCustomizationPoint.Factory;
        if (inBlockingUI) point |= ScreenCustomizationPoint.IsUiBlocked;

        await screen.InitialConfigure(dataId, dataContext);

        if (!VarKey.IsNull(dataId) || dataContext != null)
        {
          await screen.TryChangeDataIdAsync(dataId, dataContext);
        }
      }
      catch (Exception e) // added 23 april 2021
      {
        AppConsole.WriteLine(MessageAspects.Warning, $"Page[{screen.DisplayName}] ОШИБКА ProceedDocumentAsync: {e.Message}");
        throw;
      }
    }


    //------------------------------------
    private void CheckInstance(IMasterScreen screen)
    {
      //_owner.CheckInstance(doc);

      Type requiredInterfaceType = null;
      var roles = ScreenRoles.Roles().ToArray();
      switch (roles.FirstOrDefault())
      {
        case ScreenRole.Page:
          requiredInterfaceType = typeof(IPage);
          break;
      }

      if (requiredInterfaceType != null)
      {
        if (!requiredInterfaceType.IsInstanceOfType(screen))
          throw new SystemException("ERROR");
      }
    }

  }

  //====================================================================================
  private class Conf<T> : IScreenFactoryConf<T>, IScreenFactoryConf where T : class, TBase
  {
    public Func<T> CreateScreen { get; private set; }
    public Action<T> ProceedAction { get; private set; } // (doc)

    private VarKey? _dataId;
    public VarKey? DataId
    {
      get
      {
        if (_dataId == null && DataIdResolver != null)
        {
          _dataId = DataIdResolver.ResolveDataId(); //_docId = DocIdResolver.ResolveDocId(isAppStarting: false);
        }
        return _dataId;
      }
      private set => _dataId = value;
    }

    public string Group { get; private set; }
    public string ViewContext { get; private set; }
    public Type IocType { get; private set; }
    public ITypeResolver IocTypeResolver { get; private set; }
    private IDataIdResolver DataIdResolver { get; set; }
    public string IocKey { get; private set; }

    public Conf()
    {
    }

    public IScreenFactoryConf<T> SetDataId(VarKey dataId)
    {
      DataId = dataId;
      return this;
    }

    IScreenFactoryConf IScreenFactoryConf.SetDataId(VarKey dataId)
    {
      DataId = dataId;
      return this;
    }


    public IScreenFactoryConf<T> SetGroup(string screenGroup)
    {
      Group = screenGroup;
      return this;
    }

    public IScreenFactoryConf<T> SetViewContext(string context)
    {
      ViewContext = context;
      return this;
    }

    public IScreenFactoryConf<T> SetCreator(Func<T> func)
    {
      CreateScreen = func;
      return this;
    }

    public IScreenFactoryConf<T> SetCreatorByIoC()
    {
      IocType = typeof(T);
      IocTypeResolver = null;
      IocKey = null;
      return this;
    }

    public IScreenFactoryConf<T> SetCreatorByIoC<TIocType>() where TIocType : T
    {
      IocType = typeof(TIocType);
      IocTypeResolver = null;
      IocKey = null;
      return this;
    }

    public IScreenFactoryConf<T> SetCreatorByIoC(Type iocType, string key = null)
    {
      IocType = iocType;
      IocTypeResolver = null;
      IocKey = key;
      return this;
    }

    public IScreenFactoryConf<T> SetCreatorByIoC(Type iocType, ITypeResolver iocTypeResolver, string key = null)
    {
      IocType = iocType;
      IocTypeResolver = iocType == null ? iocTypeResolver : null;
      IocKey = key;
      return this;
    }

    public IScreenFactoryConf<T> SetDataIdResolver(IDataIdResolver dataIdResolver)
    {
      DataIdResolver = dataIdResolver;
      return this;
    }

    public IScreenFactoryConf<T> SetProcessor(Action<T> action) // (doc)
    {
      ProceedAction = action;
      return this;
    }
  }

  //====================================================================================
  private class ShowPageConf : IShowPageConf
  {
    public ISingletonService SingletonService { get; set; } // { PopupManager | SideManager | WorkspaceSingletonManager }
    public string SingletonId { get; set; }

    // for show in shell split layout
    public bool IsSplit { get; set; }
    public Sides Side { get; set; } = Sides.None;
    public double BandWidth { get; set; } = double.NaN;
  }

}
