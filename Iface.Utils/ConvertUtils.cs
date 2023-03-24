using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace Iface.Utils
{
  public static class ConvertUtils
  {
    public static bool IsEmpty(Guid? guid)
    {
      return !guid.HasValue || guid.Value == Guid.Empty; 
    }

    public static Guid? ParseNullableGuid(string text)
    {
      var guid = ParseGuid(text);
      if (guid != Guid.Empty)
        return guid;
      return null;
    }

    public static Guid ParseGuid(string text)
    {
      Guid.TryParse(text, out var guid);
      return guid;
    }

    public static bool TryParseGuid(string text, out Guid guid)
    {
      return Guid.TryParse(text, out guid) && guid != Guid.Empty;
    }

    #region Parse strings // String to something

    #region (String to Type) - main conversion function

    public static bool TryParse(string text, Type conversionType, out object outValue)
    {
      outValue = null;
      if (String.IsNullOrEmpty(text))
        return false;

      if (conversionType.IsEnum)
      {
        try
        {
          outValue = Enum.Parse(conversionType, text);
          // return true; // march 2015!!!
          //return Enum.IsDefined(conversionType, outValue);

          if (Enum.IsDefined(conversionType, outValue))
            return true;

          // added 2019 april
          var typedValue = (Enum)outValue;
          var fff = typedValue.HasFlag(typedValue);

          return fff; 
        }
        catch (Exception)
        {
          return false;
        }
      }

      //???
      //----------------------------
      switch (Type.GetTypeCode(conversionType))
      {
        case TypeCode.String: outValue = text; return true;
        case TypeCode.Boolean: outValue = ToBool(text, false); return true;

        case TypeCode.Int16:
        case TypeCode.UInt16:
        case TypeCode.Int32:
        case TypeCode.UInt32:
        case TypeCode.Int64:
        case TypeCode.UInt64:
        //case TypeCode.Decimal: // removed from here april 2015
          outValue = ToInt(text, 0);
          outValue = Convert.ChangeType(outValue, conversionType); // added 18 march 2015 !!!!!!!!!!!
          return true;

        #region added april 2015
        case TypeCode.Decimal: outValue = ParseDecimalEx(text); return true;
        case TypeCode.Single: outValue = ParseSingleEx(text); return true;
        case TypeCode.Double: outValue = ParseDoubleEx(text); return true;
        #endregion
      }

      //----------------------------
      // 20 aug 2010
      var converter = TypeDescriptor.GetConverter(conversionType);
      if (converter != null)
      {
        try
        {
          outValue = converter.ConvertFromInvariantString(text);
        }
        catch
        {
          outValue = converter.ConvertFromString(text);
        }
        return true;
      }
      //----------------------------
      outValue = Convert.ChangeType(text, conversionType);
      return true;
      //----------------------------
    }
    #endregion

    public static bool TryParse<TValue>(string text, out TValue outValue)
    {
      if (TryParse(text, typeof(TValue), out var value))
      {
        outValue = (TValue)value;
        return true;
      }
      outValue = default(TValue);
      return false;
    }

    public static TValue Parse<TValue>(string text, TValue defValue)
    {
      return TryParse(text, out TValue value) ? value : defValue;
    }

    public static bool ParseBool(string text, bool defaultValue)
    {
      if (string.IsNullOrEmpty(text))
        return defaultValue;
      var s = text.Trim();
      if (s.Length == 0)
        return defaultValue;
      return s.Length > 0 && (s[0] == 't' || s[0] == 'T' || s[0] == '1');
    }

    public static bool ParseBoolDigit(string text, bool defaultValue)
    {
      if (string.IsNullOrEmpty(text))
        return defaultValue;
      switch (text[0])
      {
        case '0': return false;
        case '1': return true;
      }
      return defaultValue;
    }

    public static int ParseInt(string text, int defaultValue)
    {
      return int.TryParse(text, out var v) ? v : defaultValue;
    }

    public static int? ParseInt(string text)
    {
      if (int.TryParse(text, out var v))
        return v;
      return null;
    }

    public static long ParseLong(string text, long defaultValue)
    {
      return long.TryParse(text, out var v) ? v : defaultValue;
    }

    public static Decimal ParseDecimalEx(string text) 
    {
      if (String.IsNullOrEmpty(text)) return 0M;
      try
      { return Decimal.Parse(text, NumberStyles.Number, NumberFormatInfo.InvariantInfo); }
      catch (Exception)
      { return Decimal.Parse(text, NumberStyles.Number, NumberFormatInfo.CurrentInfo); }
    }

    public static Single ParseSingle(string text)
    {
      if (String.IsNullOrEmpty(text)) return 0;
      return Single.Parse(text, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
    }

    public static Single ParseSingleEx(string text)
    {
      if (String.IsNullOrEmpty(text)) return 0;
      try
      { return Single.Parse(text, NumberStyles.Float, NumberFormatInfo.InvariantInfo); }
      catch (Exception)
      { return Single.Parse(text, NumberStyles.Float, NumberFormatInfo.CurrentInfo); }
    }

    public static Double ParseDouble(string text)
    {
      if (String.IsNullOrEmpty(text)) return 0;
      return Double.Parse(text, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
    }

    public static Double ParseDoubleEx(string text)
    {
      if (String.IsNullOrEmpty(text)) return 0;
      try
      { return Double.Parse(text, NumberStyles.Float, NumberFormatInfo.InvariantInfo); }
      catch (Exception)
      { return Double.Parse(text, NumberStyles.Float, NumberFormatInfo.CurrentInfo); }
    }

    public static double ParseDoubleEx(string text, double defaultValue)
    {
      if (string.IsNullOrEmpty(text)) return defaultValue;
      try
      { return double.Parse(text, NumberStyles.Float, NumberFormatInfo.InvariantInfo); }
      catch (Exception)
      { return double.Parse(text, NumberStyles.Float, NumberFormatInfo.CurrentInfo); }
    }

    public static Single? ParseNullableSingle(string text)
    {
      try
      {
        if (text == null) return null;
        return ParseSingle(text);
      }
      catch (Exception)
      {
        return null;
      }
    }

    public static Single? ParseNullableSingleEx(string text)
    {
      try
      {
        if (text == null) return null;
        return ParseSingleEx(text);
      }
      catch (Exception)
      {
        return null;
      }
    }

    public static DateTime? ParseDateTime(string text)
    {
      if (string.IsNullOrEmpty(text)) return null;
      try
      {
        return DateTime.Parse(text, CultureInfo.InvariantCulture);
      }
      catch (Exception)
      {
        try
        {
          return DateTime.Parse(text, CultureInfo.CurrentCulture);
        }
        catch (Exception)
        {
          return null;
        }
      }
    }

    public static DateTime ParseDateTime(string text, DateTime defaultValue)
    {
      if (string.IsNullOrEmpty(text)) return defaultValue;
      try
      {
        return DateTime.Parse(text, CultureInfo.InvariantCulture);
      }
      catch (Exception)
      {
        try
        {
          return DateTime.Parse(text, CultureInfo.CurrentCulture);
        }
        catch (Exception)
        {
          return defaultValue;
        }
      }
    }

    #endregion

    #region  Object to something

    #region (Object to Type) - main conversion function

    public static bool TryChangeType(object inValue, Type conversionType, out object outValue)
    {
      outValue = null;
      if (inValue == null)
        return false;

      if (conversionType == typeof(string))
      {
        outValue = inValue.ToString();
        return true;
      }

      if (inValue is string s)
        return TryParse(s, conversionType, out outValue);

      outValue = Convert.ChangeType(inValue, conversionType);
      return true;
    }
    #endregion

    public static bool TryChangeType<TValue>(object inValue, out TValue outValue)
    {
      if (TryChangeType(inValue, typeof(TValue), out var value))
      {
        outValue = (TValue)value;
        return true;
      }
      outValue = default(TValue);
      return false;
    }

    public static object ChangeType(object inValue, Type conversionType, object defValue)
    {
      return TryChangeType(inValue, conversionType, out var value) ? value : defValue;
    }

    public static TValue ChangeType<TValue>(object inValue, TValue defValue)
    {
      return TryChangeType(inValue, typeof(TValue), out var value) ? (TValue)value : defValue;
    }

    public static int ToInt(object? pValue) { return ToInt(pValue, 0); }
    public static int ToInt(object? pValue, int pDefaultValue)
    {
      try
      {
        if (pValue == null || pValue == DBNull.Value)
          return pDefaultValue;
        return Convert.ToInt32(pValue);
      }
      catch (Exception)
      {
        return pDefaultValue;
      }
    }

    public static bool ToBool(object pValue) { return ToBool(pValue, false); }
    public static bool ToBool(object pValue, bool pDefaultValue)
    {
      try
      {
        if (pValue == null || pValue == DBNull.Value)
          return pDefaultValue;

        if (pValue is string s1)
          return ParseBool(s1, pDefaultValue);

        return Convert.ToBoolean(pValue);
      }
      catch (Exception)
      {
        return pDefaultValue;
      }
    }

    #endregion

    #region Format strings // Something to string

    public static string FormatBoolAsDigit(bool value)
    {
      return value ? "1" : "0";
    }

    public static string FormatAsDigit(this bool value)
    {
      return value ? "1" : "0";
    }

    public static string FormatDateTillSecond(DateTime value)
    {
      //var ss1 = value.ToShortTimeString();
      var ss2 = value.ToString("G", CultureInfo.InvariantCulture);
      return ss2;
    }

    #endregion

    public static object ToNull(string value)
    {
      return string.IsNullOrEmpty(value) ? null : value;
    }

    public static object ToNull(double value)
    {
      if (double.IsNaN(value))
        return null;
      return value;
    }

    public static object ToDbNull(object value)
    {
      return value ?? DBNull.Value;
    }

    public static object ToDbNull(string value)
    {
      return ToNull(value) ?? DBNull.Value;
    }

    public static object ToDbNull(double value)
    {
      return ToNull(value) ?? DBNull.Value;
    }

    public static object ToDbNull<T>(T? value) where T : struct
    {
      return value ?? (object)DBNull.Value;
    }


    public static string ToBase16String(byte[] arr, bool withSpaces = false)
    {
      //return Convert.ToBase64String(arr);
      //return arr == null ? null : BitConverter.ToString(arr);
      if (arr == null || arr.Length == 0) return null;
      var sb = new StringBuilder(arr.Length);
      /*
      foreach (var b in arr)
        sb.Append(b.ToString("X2"));
      */
      for (var i = 0; i < arr.Length; i++)
      {
        if (i != 0 && withSpaces)
          sb.Append(' ');
        sb.Append(arr[i].ToString("X2"));
      }
      return sb.ToString();
    }

  }
}
