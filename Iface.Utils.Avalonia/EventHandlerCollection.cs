namespace Iface.Utils.Avalonia
{
  public class EventHandlerCollection
  {
    private readonly List<DelegateReference> _items = new List<DelegateReference>();

    public string Label { get; set; }

    public void AddHandlerImpl(Delegate handler)
    {
      lock (_items)
      {
        // todo: поиск существующего
        _items.Add(new DelegateReference(handler, isWeak: true));
      }
    }

    public void RemoveHandlerImpl(Delegate handler)
    {
      lock (_items)
      {
        _items.RemoveAll(it =>
        {
          var itHandler = it.Handler;
          return handler.Equals(itHandler) || itHandler == null;
        });
      }
    }


    public void AddHandler(Action handler)
    {
      AddHandlerImpl(handler);
    }

    public void AddHandler<T>(Action<T> handler)
    {
      AddHandlerImpl(handler);
    }

    public void AddHandler<T1, T2>(Action<T1, T2> handler)
    {
      AddHandlerImpl(handler);
    }

    public void RemoveHandler(Action handler)
    {
      RemoveHandlerImpl(handler);
    }

    public void RemoveHandler<T>(Action<T> handler)
    {
      RemoveHandlerImpl(handler);
    }

    public void RemoveHandler<T1, T2>(Action<T1, T2> handler)
    {
      RemoveHandlerImpl(handler);
    }


    public void RaiseImpl(Action<Action> marshal, params object[] args)
    {
      if (Label != null)
      {
      }

      if (marshal == null)
      {
        marshal = action => action();
      }

      // john изменил оригинальную реализацию от Caliburn EventAggregator, требуется проверка на практике

      DelegateReference[] items;
      lock (_items)
      {
        _items.RemoveAll(listener => listener.Handler == null);
        items = _items.ToArray();
      }

      if (items.Length == 0)
        return;

      marshal(() =>
      {
        // todo: bool Handled
        foreach (var handler in items.Select(it => it.Handler).Where(h => h != null))
          handler.DynamicInvoke(args);
      });
    }

    public void RaiseImplOldxxx(Action<Action> marshal, params object[] args)
    {
      if (marshal == null)
        marshal = action => action();

      marshal(() =>
      {
        DelegateReference[] items;
        lock (_items)
        {
          _items.RemoveAll(listener => listener.Handler == null);
          items = _items.ToArray();
        }

        // todo: bool Handled
        foreach (var handler in items.Select(it => it.Handler).Where(h => h != null))
          handler.DynamicInvoke(args);
      });
    }

    public void Raise()
    {
      RaiseImpl(null); // эквивалент RaiseOnCurrentThread()
    }

    #region Raise Helpers

    public void RaiseOnCurrentThread(params object[] args)
    {
      RaiseImpl(action => action(), args);
    }

    public void RaiseOnBackgroundThread(params object[] args)
    {
      RaiseImpl(action => Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default), args);
    }

    public void RaiseOnUIThread(params object[] args)
    {
      RaiseImpl(action => action.OnUIThread(), args);
    }

    public Task RaiseOnUIThreadAsync(params object[] args)
    {
      Task task = null;
      RaiseImpl(action => task = action.OnUIThreadAsync(), args);
      return task;
    }

    public void RaiseOnUIThreadBegin(params object[] args)
    {
      RaiseImpl(action => action.BeginOnUIThread(), args);
    }

    #endregion
  }

  //===========================================
  public class EventHandlerCollection<T> : EventHandlerCollection
  {
    public void AddHandler(Action<T> handler)
    {
      AddHandlerImpl(handler);
    }

    public void RemoveHandler(Action<T> handler)
    {
      RemoveHandlerImpl(handler);
    }

    public void Raise(T arg, Action<Action> marshal = null)
    {
      RaiseImpl(marshal, arg);
    }

    #region Raise Helpers

    public void RaiseOnCurrentThread(T arg)
    {
      base.RaiseOnCurrentThread(arg);
    }

    public void RaiseOnBackgroundThread(T arg)
    {
      base.RaiseOnBackgroundThread(arg);
    }

    public void RaiseOnUIThread(T arg)
    {
      base.RaiseOnUIThread(arg);
    }

    public Task RaiseOnUIThreadAsync(T arg)
    {
      return base.RaiseOnUIThreadAsync(arg);
    }

    public void RaiseOnUIThreadBegin(T arg)
    {
      base.RaiseOnUIThreadBegin(arg);
    }

    #endregion
  }

  //===========================================
  public class EventHandlerCollection<T1, T2> : EventHandlerCollection
  {
    public void AddHandler(Action<T1, T2> handler)
    {
      AddHandlerImpl(handler);
    }

    public void RemoveHandler(Action<T1, T2> handler)
    {
      RemoveHandlerImpl(handler);
    }

    // marshal = null: RaiseOnCurrentThread
    public void Raise(T1 arg1, T2 arg2, Action<Action> marshal = null)
    {
      RaiseImpl(marshal, arg1, arg2);
    }

    #region Raise Helpers

    public void RaiseOnCurrentThread(T1 arg1, T2 arg2)
    {
      base.RaiseOnCurrentThread(arg1, arg2);
    }

    public void RaiseOnBackgroundThread(T1 arg1, T2 arg2)
    {
      base.RaiseOnBackgroundThread(arg1, arg2);
    }

    public void RaiseOnUIThread(T1 arg1, T2 arg2)
    {
      base.RaiseOnUIThread(arg1, arg2);
    }

    public Task RaiseOnUIThreadAsync(T1 arg1, T2 arg2)
    {
      return base.RaiseOnUIThreadAsync(arg1, arg2);
    }

    public void RaiseOnUIThreadBegin(T1 arg1, T2 arg2)
    {
      base.RaiseOnUIThreadBegin(arg1, arg2);
    }

    #endregion
  }


}
