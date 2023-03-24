using System.Linq.Expressions;
using Caliburn.Micro;

namespace Iface.Utils.Avalonia
{
  public static class ExpressionMixins
  {
    public static string GetPropertyName<TProperty>(Expression<Func<TProperty>> property)
    {
      return property.GetMemberInfo().Name;
    }

    public static string GetPropertyName<TTarget, TProperty>(Expression<Func<TTarget, TProperty>> property)
    {
      return property.GetMemberInfo().Name;
    }
  }

}
