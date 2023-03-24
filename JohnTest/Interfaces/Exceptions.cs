using System.Runtime.Serialization;
using Iface.Utils;
using Npgsql;

namespace AppNs.Interfaces;

public enum OikErrorCode
{
  Unknown,
  NoConnection = 1,
  SqlError = 2,
  NoRbPort,
  NotLoginDone
}

//====================================
[Serializable]
public class OikException : Exception
{
  #region Static Service
  public static void SafeReThrow(Exception exception, string message = null)
  {
    if (exception is PostgresException) // There are troubles with PostgresException in Npgsql 3.2.6 // Проверил 5.0.7 - no troubles
    {
      throw new OikException(OikErrorCode.SqlError, exception);
    }
    throw exception;
  }
  #endregion

  public OikErrorCode ErrorCode { get; }

  public override string Message => ErrorCode.GetDescription();

  public OikException()
  {
  }

  public OikException(OikErrorCode errorCode)
  {
    ErrorCode = errorCode;
  }

  public OikException(OikErrorCode errorCode, Exception innerException) : base(null, innerException)
  {
    ErrorCode = errorCode;
  }

  protected OikException(SerializationInfo info, StreamingContext context)
    : base(info, context)
  {
    ErrorCode = (OikErrorCode)info.GetValue(nameof(ErrorCode), typeof(OikErrorCode));
  }


  public override void GetObjectData(SerializationInfo info, StreamingContext context)
  {
    base.GetObjectData(info, context);
    info.AddValue("ErrorCode", ErrorCode);
  }
}

//====================================