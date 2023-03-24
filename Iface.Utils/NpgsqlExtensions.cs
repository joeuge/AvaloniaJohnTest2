using System.Data;
using System.Data.Common;
using Npgsql;
using NpgsqlTypes;

namespace Iface.Utils
{
  public static class NpgsqlExtensions
  {
    public static NpgsqlDataReader ExecuteReaderSeq(this NpgsqlCommand cmd)
    {
      return cmd.ExecuteReader(CommandBehavior.SequentialAccess);
    }


    public static async Task<DbDataReader> ExecuteReaderSeqAsync(this NpgsqlCommand cmd)
    {
      return await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess)
                      .ConfigureAwait(false);
    }


    public static string GetStringOrDefault(this DbDataReader reader,
                                            int               ordinal)
    {
      if (reader.IsDBNull(ordinal))
      {
        return null;
      }
      return reader.GetString(ordinal);
    }


    public static byte[] GetByteArrayOrDefault(this DbDataReader reader,
                                               int               ordinal)
    {
      if (reader.IsDBNull(ordinal))
      {
        return null;
      }
      return (byte[]) reader[ordinal];
    }


    public static int? GetInt32OrDefault(this DbDataReader reader,
                                        int               ordinal,
                                        int? defaultValue = null)
    {
      if (reader.IsDBNull(ordinal))
      {
        return defaultValue;
      }
      return reader.GetInt32(ordinal);
    }


    public static NpgsqlParameter AddWithNullableValue(this NpgsqlParameterCollection collection,
                                                       string                         parameterName,
                                                       NpgsqlDbType                   parameterType,
                                                       object                         value)
    {
      if (value == null)
      {
        return collection.AddWithValue(parameterName, DBNull.Value);
      }
      return collection.AddWithValue(parameterName, parameterType, value);
    }


    public static byte[] GetAllBytes(this NpgsqlDataReader reader, int ordinal) // todo al или GetByteArrayOrDefault
    {
      if (reader.IsDBNull(ordinal))
      {
        return null;
      }

      var test0 = reader[ordinal];
      var test1 = test0 as byte[]; // todo: ТАК МОЖНО?

      var stream = reader.GetStream(ordinal);
      using (var ms = new MemoryStream())
      {
        stream.CopyTo(ms);
        return ms.ToArray();
      }
    }
  }
}