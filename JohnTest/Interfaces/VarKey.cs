using Newtonsoft.Json;

namespace AppNs.Interfaces;

public enum VarKeyType // todo: + Int32
{
  Undefined, // added 19.07.2019
  Guid,
  UInt32,
  String,
  Tma
}

// every instance is immutable
[Serializable] // added 13 jan 2020 // это позволяет обмен через Clipboard
public class VarKey
{
  #region Static Service

  public static readonly VarKey Empty = new VarKey();
  public static VarKey NewGuid() { return new VarKey(Guid.NewGuid()); }

  public static bool IsNull(VarKey? v)
  {
    return v == null || v.IsEmpty;
  }

  public static bool Equals(VarKey? v1, VarKey? v2)
  {
    if (v1 == null)
      return v2 == null || v2.IsEmpty;
    return v1.Equals(v2);
  }

  public static bool TryParse(VarKeyType type, string text, out VarKey result) // без префиксов!
  {
    result = null;
    try
    {
      result = Parse(type, text);
      return !IsNull(result);
    }
    catch (Exception e)
    {
      return false;
    }
  }

  public static VarKey Parse(VarKeyType type, string text) // без префиксов!
  {
    switch (type)
    {
      case VarKeyType.Guid:
        return new VarKey(Guid.Parse(text));
      case VarKeyType.UInt32:
        return new VarKey(UInt32.Parse(text));
    }
    return new VarKey(text);
  }


  public static VarKey CreateJson(object obj)
  {
    return new VarKey(JsonConvert.SerializeObject(obj));
  }

  public static bool TryDeserialize(string text, out VarKey result, bool convertStrings = false)
  {
    result = null;
    try
    {
      result = Deserialize(text, convertStrings);
      return !IsNull(result);
    }
    catch (Exception e)
    {
      return false;
    }
  }

  public static VarKey Deserialize(string text, bool convertStrings = false)
  {
    if (string.IsNullOrEmpty(text))
    {
      return null;
    }

    var type = VarKeyType.String;
    var strValue = text;

    var arr = text.Split(new[] { ':' }, 2);

    if (arr.Length > 1)
    {
      switch (arr[0])
      {
        case "G":
          type = VarKeyType.Guid;
          strValue = arr[1];
          break;
        case "N":
        case "UInt32":
          type = VarKeyType.UInt32;
          strValue = arr[1];
          break;
        case "S":
          type = VarKeyType.String;
          strValue = arr[1];
          break;
        case "T":
          type = VarKeyType.Tma;
          strValue = arr[1];
          break;
      }
    }

    if (convertStrings && type == VarKeyType.String)
    {
      if (strValue.All(char.IsNumber))
      {
        type = VarKeyType.UInt32;
      }
    }

    return Parse(type, strValue);
  }

  public static bool TryCreate(object value, out VarKey result)
  {
    result = null;

    if (value == null)
      return false;

    switch (value)
    {
      case string vv:
        result = new VarKey(vv);
        break;

      case Guid vv:
        result = new VarKey(vv);
        break;

      case Int32 vv:
        result = new VarKey(vv);
        break;

      case UInt32 vv:
        result = new VarKey(vv);
        break;

      case Int16 vv:
        result = new VarKey(vv);
        break;

      default:
        result = new VarKey(value.ToString());
        break;
    }

    return !IsNull(result);
  }
    
  #endregion

  private readonly bool _isEmpty;
  private readonly VarKeyType _type;
  private readonly Guid _guidValue;
  private readonly UInt32 _uint32Value;
  private readonly string _stringValue;

  public bool IsEmpty => _isEmpty;
  public VarKeyType Type => _type;
  public Guid GuidValue => _guidValue;
  public UInt32 UInt32Value => _uint32Value;
  public string StringValue => _stringValue;

  public object Value // не может быть null!
  {
    get
    {
      if (_isEmpty)
      {
        return string.Empty;
      }
      switch (_type)
      {
        case VarKeyType.Guid:
          return _guidValue;
        case VarKeyType.UInt32:
          return _uint32Value;
        case VarKeyType.String:
          return _stringValue;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }
  }


  #region Ctor+

  public VarKey()
  {
    _isEmpty = true;
  }

  public VarKey(Guid value)
  {
    if (value.Equals(Guid.Empty))
      throw new SystemException("ERROR");
    _type = VarKeyType.Guid;
    _guidValue = value;
  }


  public VarKey(Int32 value)
    : this((UInt32) value)
  {
      
  }

  public VarKey(UInt32 value)
  {
    if (value == 0)
      throw new SystemException("ERROR");
    _type = VarKeyType.UInt32;
    _uint32Value = value;
  }

  public VarKey(string value)
  {
    if (string.IsNullOrEmpty(value))
      throw new SystemException("ERROR");
    _type = VarKeyType.String;
    _stringValue = value;
  }

  public override int GetHashCode()
  {
    return Value.GetHashCode();
  }

  public override bool Equals(object? obj)
  {
    if (obj is VarKey other)
      return Equals(other);
    //throw new ArgumentOutOfRangeException();
    return false;
  }

  public bool Equals(VarKey? other)
  {
    if (other == null || other.IsEmpty) return IsEmpty;
    if (IsEmpty) return false;
    if (_type != other.Type) return false;
    switch (_type)
    {
      case VarKeyType.Guid:
        return _guidValue.Equals(other.GuidValue);
      case VarKeyType.UInt32:
        return _uint32Value.Equals(other.UInt32Value);
      case VarKeyType.String:
        return _stringValue.Equals(other.StringValue, StringComparison.Ordinal);
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  #endregion

  public override string ToString()
  {
    return ToStringCore();
  }

  public string ToStringCore(bool usePrefix = true, char delimiter = ':')
  {
    if (_isEmpty) return String.Empty;
    switch (_type)
    {
      case VarKeyType.Guid:
        return (usePrefix ? "G" + delimiter : "") + _guidValue.ToString("D");
      case VarKeyType.UInt32:
        return (usePrefix ? "N" + delimiter : "") + _uint32Value;
      case VarKeyType.String:
        return (usePrefix ? "S" + delimiter : "") + _stringValue;
      default:
        return String.Empty;
    }
  }

  public string SerializeToString()
  {
    return ToStringCore(_type != VarKeyType.String, ':');
  }

  #region Value Conversions

  public string AsString()
  {
    return Value.ToString();
  }


  public bool TryAsInt(out int value)
  {
    switch (_type)
    {
      case VarKeyType.UInt32:
        value = (int) _uint32Value;
        return true;
        
      case VarKeyType.String:
        return int.TryParse(_stringValue, out value);
        
      default:
        value = int.MinValue;
        return false;
    }
  }

  #endregion
}