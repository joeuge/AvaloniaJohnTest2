using System.Collections;
using Caliburn.Micro;

namespace Iface.Utils.Avalonia
{
  public static class ModelUtility
  {
    public static T FindAncestor<T>(object start) where T : class
    {
      return TryFindAncestor(start, out T item) ? item : null;
    }

    public static bool TryFindAncestor<T>(object start, out T result) where T : class
    {
      var item = start as IChild;
      while (item != null)
      {
        if (item is T tmp)
        {
          result = tmp;
          return true;
        }
        item = item.Parent as IChild;
      }
      result = null;
      return false;
    }


    public static bool TryFindService<T>(object start, out T result) where T : class
    {
      var item = start as IChild;
      while (item != null)
      {
        if (GetService(item, out result))
          return true;
        item = item.Parent as IChild;
      }
      result = null;
      return false;
    }

    private static bool GetService<T>(object item, out T result) where T : class
    {
      if (item is T tmp)
      {
        result = tmp;
        return true;
      }
      result = null;
      return false;
    }

  }

}
