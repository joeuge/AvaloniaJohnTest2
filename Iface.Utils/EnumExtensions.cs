using System.ComponentModel;
using System.Reflection;

namespace Iface.Utils
{
  public static class EnumExtensions
  {
    public static MemberInfo GetField(this Enum value)
    {
      return value.GetType().GetMember(value.ToString()).FirstOrDefault();
    }

    public static string GetDescription(this Enum value)
    {
      return value.GetType()
                  .GetMember(value.ToString())
                  .FirstOrDefault()
                  ?.GetCustomAttribute<DescriptionAttribute>()
                  ?.Description
             ?? value.ToString();
    }

    public static string MyGetName(this Enum value)
    {
      return Enum.GetName(value.GetType(), value);
    }

    public static TEnum[] GetValues<TEnum>() where TEnum : struct, Enum
      => (TEnum[])Enum.GetValues(typeof(TEnum));

  }
}