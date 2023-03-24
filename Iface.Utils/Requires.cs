using System.Globalization;

namespace Iface.Utils
{
  public static class Requires
  {
    public static class Arg
    {
      public static void NotNull<T>(T value, string parameterName) where T : class
      {
        if (value == null)
          throw new ArgumentNullException(parameterName);
      }

      public static void NotNull<T>(T value) where T : class
      {
        if (value == null)
          throw new ArgumentNullException();
      }

      public static void NotNullOrEmpty(string value, string parameterName)
      {
        NotNull(value, parameterName);
        if (value.Length == 0)
          throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, @"'{0}' cannot be an empty string." //Strings.ArgumentException_EmptyString
              , new[] { (object)parameterName })
            , parameterName);
      }

      public static void NotNullOrEmpty(string value)
      {
        if (string.IsNullOrEmpty(value))
          throw new ArgumentNullException();
      }
    }

    public static class Reference
    {
      public static void NotNull<T>(T value, string parameterName) where T : class
      {
        if (value == null)
          throw new NullReferenceException(parameterName); 
      }

      public static void NotNull<T>(T value) where T : class
      {
        if (value == null)
          throw new NullReferenceException();
      }

      public static void Null<T>(T value, string parameterName) where T : class
      {
        if (value != null)
          throw new SystemException(string.Format(CultureInfo.CurrentCulture, @"'{0}' should be a null.", new[] { (object)parameterName }));
      }
    }
  }
}
