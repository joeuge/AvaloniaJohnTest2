using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Iface.Utils.Avalonia
{
  public static class MemberInfoExtensions 
  {
    public static FieldInfo GetField<TField>(this Type type, Expression<Func<TField>> expression)
    {
      return type.GetField(Caliburn.Micro.ExpressionExtensions.GetMemberInfo(expression).Name);
    }

    public static PropertyInfo GetProperty<TProperty>(this Type type, Expression<Func<TProperty>> expression)
    {
      return type.GetProperty(Caliburn.Micro.ExpressionExtensions.GetMemberInfo(expression).Name);
    }

    public static IEnumerable<T> GetAttributes<T>(this MemberInfo member, bool inherit)
    {
      return Attribute.GetCustomAttributes(member, inherit).OfType<T>();
    }

    public static string Description(this MemberInfo memberInfo)
    {
      var customAttribute = memberInfo.GetCustomAttribute<DescriptionAttribute>(false);
      return customAttribute?.Description;
    }

    public static string DisplayName(this MemberInfo memberInfo)
    {
      var customAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>(false);
      return customAttribute?.GetName();
    }

    public static string DisplayShortName(this MemberInfo memberInfo)
    {
      var customAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>(false);
      return customAttribute?.GetShortName();
    }

    public static string DisplayDescription(this MemberInfo memberInfo)
    {
      var customAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>(false);
      return customAttribute?.GetDescription();
    }

    public static int? DisplayOrder(this MemberInfo memberInfo)
    {
      var customAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>(false);
      return customAttribute?.GetOrder();
    }
  }
}
