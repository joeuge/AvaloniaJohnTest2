using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;

namespace Iface.Utils
{
  public static class GlobalUtils
  {
    private static int _runtimeId;
    public static int NextRuntimeId() // гарантированно > 0
    {
      var result = Interlocked.Increment(ref _runtimeId);
      if (result >= 0) return result;
      Interlocked.Exchange(ref _runtimeId, 0);
      result = Interlocked.Increment(ref _runtimeId);
      return result;
    }

    private static int _runtimeId2;
    public static int NextRuntimeId2() // гарантированно > 0
    {
      var result = Interlocked.Increment(ref _runtimeId2);
      if (result >= 0) return result;
      Interlocked.Exchange(ref _runtimeId2, 0);
      result = Interlocked.Increment(ref _runtimeId2);
      return result;
    }

  }

}