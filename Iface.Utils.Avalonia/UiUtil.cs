using Avalonia.Threading;

namespace Iface.Utils.Avalonia
{
  /* Caliburn расширяет Action посредством методов static class Execute -> PlatformProvider
   *
   * Когда параметр Action (более удобно), то можно так
   *  - Class.OnUIThread(Method);
   *  - Class.OnUIThread(()=>{});
   * 
   * Когда параметр Delegate (более универсально и позволяет args), то нужно так
   *  - Class.OnUIThread(new System.Action(Method));
   *  - Class.OnUIThread(new System.Action(()=>{}));
   * 
   */


  public static class UiUtil
  {
    public static void Initialize()
    {
      UiDispatcher = Dispatcher.UIThread;
    }

    public static Dispatcher UiDispatcher { get; set; }

    private static void ValidateDispatcher()
    {
      if (UiDispatcher == null)
      {
        throw new InvalidOperationException("Not initialized with dispatcher.");
      }
    }

    private static bool CheckAccess()
    {
      ValidateDispatcher();
      return UiDispatcher.CheckAccess();
    }


    //-------------------------------
    public static void OnUIThread(this Delegate handler, params object[] args)
    {
      if (CheckAccess())
      {
        handler.DynamicInvoke(args);
        return;
      }

      void ResultHandler()
      {
        handler.DynamicInvoke(args);
      }

      ExecuteOnUIThread(ResultHandler); // -> UiDispatcher.InvokeAsync(action).GetAwaiter().GetResult(); // Ужасно!!!!
    }

    //-------------------------------
    public static void BeginOnUIThread(this Delegate handler, params object[] args)
    {
      void ResultHandler()
      {
        handler.DynamicInvoke(args);
      }

      UiDispatcher.Post(ResultHandler);
    }

    //-------------------------------
    public static Task OnUIThreadAsync(this Delegate handler, params object[] args)
    {
      void ResultHandler()
      {
        handler.DynamicInvoke(args);
      }
      return UiDispatcher.InvokeAsync(ResultHandler);
    }



    /* 
     * Далее сигнатуры с Action
    */


    //-------------------------------
    public static void OnUIThreadOrBeginOnUIThread(this Action action)
    {
      if (CheckAccess())
      {
        action();
        return;
      }

      UiDispatcher.Post(action);
    }

    //-------------------------------
    public static void Execute(this Action action, ActionExecuteType executeType)
    {
      switch (executeType)
      {
        case ActionExecuteType.CurrentThread:
          action();
          return;

        case ActionExecuteType.UIThread:
          ExecuteOnUIThread(action);
          return;

        case ActionExecuteType.UIThreadBegin:
          UiDispatcher.Post(action);
          return;

        default:
          throw new ArgumentOutOfRangeException(nameof(executeType), executeType, null);
      }
    }


    /* added 11 june 2020
     * 
     * Дублируем Caliburn.Micro(class Execute) // +prefix: Execute чтобы не пересекаться
    */

    public static void ExecuteOnUIThread(this Action action)
    {
      if (CheckAccess())
      {
        action();
        return;
      }

      UiDispatcher.InvokeAsync(action).GetAwaiter().GetResult(); // Ужасно!!!!
    }


  }
}
