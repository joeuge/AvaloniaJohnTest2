using System.Text;

namespace Iface.Utils
{
  public enum MessagePrefix
  {
    None = 0,
    Default = 1,
    Num = 2,
    Time = 3,       // HH:mm:ss.fff
    ShortTime = 4,  // HH:mm:ss
  }

  // Список дублируется в AppMonitorVm // todo: может перевести в enum и собирать автоматически в AppMonitorVm
  public static class MessageAspects
  {
    public const int Unknown = 0;
    public const int Error = 1;
    public const int Warning = 2;
    public const int Important = 3;
    public const int Info = 4;
    public const int TestInfo = 7;
    public const int JoTrash = 100;
    public const int Jo1 = 101;
    public const int Jo2 = 102;
    public const int Jo3 = 103;
  }

  // todo: может перевести в enum и отображать заголовки в AppMonitorVm
  public static class MessageTopics
  {
    public const int SqlMessages = 1;
    public const int LoopMessages = 2;
    public const int Trace1 = 3;
    public const int MemoryLeak = 8;
    public const int John1 = 10;
    public const int John2 = 11;
  }

  //=============================
  public static class DebugUtils
  {
    public static bool IsRu { get; set; } = false;
    public static bool IsJohn { get; set; } = false; // это опция командной строки /john // либо AppBase.csproj: JOHN_DEBUG
#if DEBUG
    public static bool IsJohnDebug => IsJohn;
#else
    public static bool IsJohnDebug => false;
#endif
    public static bool IsJohnDebugPassword = false;

    public static bool IsAlex = false; // это опция командной строки /alex
#if DEBUG
    public static bool IsAlexDebug => IsAlex;
#else
    public static bool IsAlexDebug => false;
#endif

    public static bool IsFlag1 = false; // это опция командной строки /flag1
    public static bool IsSaveLoginSettingsSuccessOnly = false;
    public static bool UseAsyncCanClose = false;
    public static bool UseItemTemplateForLoadContentOnly = false;
    public static bool UseThemeManager = false;
    public static bool IsUiPlacesAltStrategy { get; set; }
    public static bool IsVeryBadApproach = true; // todo: 1)=false 2)выловить и устранить ошибки 3) удалить лишний код
    public static bool IsEventsNewVersion = true;
    //public static bool IsLoginWindowInsideMainWindow = false;
    public static bool UseMefCompositionService = false;

    public static bool IsTestingDeferredMef = true;

    private static int _messageNum;
    public static int GetNextMessageNum()
    {
      return ++_messageNum;
    }

    public static void Do(Action action)
    {
#if DEBUG
      action();
#endif
    }

    public static void WriteExceptionToConsole(Exception exception)
    {
      var items = ExceptionToStrings(exception);
      foreach (var item in items)
      {
        Console.WriteLine(item);
      }
    }

    public static IReadOnlyCollection<string> ExceptionToStrings(Exception exception)
    {
      var list = new List<string>();
      CollectStringsFromException(list, exception, true);
      return list;
    }

    internal static void CollectStringsFromException(List<string> list, Exception exception, bool withPrefix)
    {
      if (exception == null) return;

      list.Add(withPrefix ? "EXCEPTION: " + exception.Message : exception.Message);

      if (exception is AggregateException aggregateException)
      {
        aggregateException.InnerExceptions?.ForEach(e =>
        {
          CollectStringsFromException(list, e, withPrefix);
        });
        return;
      }

      if (exception.InnerException != null)
      {
        CollectStringsFromException(list, exception.InnerException, withPrefix);
      }
    }

  }

  //=============================
  public static class AppConsole
  {
    public static bool UseFilter = true;
    public static HashSet<int> Aspects;
    public static bool UseFile = false;
    public static string LogDirPath { get; set; }

    private static readonly Dictionary<int, int> Topics = new Dictionary<int, int>(); // <topic, verbosity>
    private static string _lastMessage;

    public static bool ContainsAspect(int aspect)
    {
      return Aspects != null && Aspects.Contains(aspect);
    }

    public static void AddAspect(int aspect)
    {
#if DEBUG || DEBUGCONSOLE
      if (Aspects == null)
      {
        Aspects = new HashSet<int>();
      }
      if (Aspects.Contains(aspect)) return;
      Aspects.Add(aspect);
#endif
    }

    public static void RemoveAspect(int aspect)
    {
#if DEBUG || DEBUGCONSOLE
      Aspects?.Remove(aspect);
#endif
    }

    public static void UpdateAspect(int aspect, bool isChecked)
    {
#if DEBUG || DEBUGCONSOLE
      if (isChecked)
        AddAspect(aspect);
      else
        RemoveAspect(aspect);
#endif
    }

    public static void EnsureTopic(int topic, int verbosity)
    {
#if DEBUG || DEBUGCONSOLE
      if (topic <= 0) return;
      Topics[topic] = verbosity;
#endif
    }

    public static void RemoveTopic(int topic)
    {
#if DEBUG || DEBUGCONSOLE
      Topics.Remove(topic);
#endif
    }

    public static void ClearTopics()
    {
#if DEBUG || DEBUGCONSOLE
      Topics.Clear();
#endif
    }

    public static void UpdateTopic(int topic, int verbosity)
    {
#if DEBUG || DEBUGCONSOLE
      if (verbosity <= 0)
        Topics.Remove(topic);
      else
        Topics[topic] = verbosity;
#endif
    }

    /*
    public static void ToggleTopic(int topic, int verbosity)
    {
#if DEBUG || DEBUGCONSOLE
      if (!Topics.TryGetValue(topic, out var currentVerbosity))
      {
        currentVerbosity = 0;
      }
      if (currentVerbosity == 0)
      {
        EnsureTopic(topic, verbosity);
      }
      else
      {
        RemoveTopic(topic);
      }
#endif
    }
    */

    public static int GetTopicVerbosity(int topic)
    {
#if DEBUG || DEBUGCONSOLE
      if (!Topics.TryGetValue(topic, out var topicVerbosity))
      {
        topicVerbosity = 0;
      }
      return topicVerbosity;
#else
      return 0;
#endif
    }

    // true = выводить сообщение
    public static bool TestTopic(int topic, int messageVerbosity = 1)
    {
#if DEBUG || DEBUGCONSOLE

      if (topic <= 0 || messageVerbosity <= 0)
        return false;

      if (!Topics.TryGetValue(topic, out var topicVerbosity))
      {
        topicVerbosity = 0;
      }

      return messageVerbosity <= topicVerbosity;
#else
      return false;
#endif
    }

    public static IReadOnlyDictionary<int, int> GetTopics()
    {
      return Topics;
    }


    // Если используется topic, то aspect игнорируется
    //-----------------------------------
    private static void WriteLineCore(int aspect, string message, int topic, int messageVerbosity, MessagePrefix prefixType = MessagePrefix.Default, bool showThread = false, bool rememberMessage = false)
    {
#if DEBUG || DEBUGCONSOLE
      if (UseFilter)
      {
        if (topic > 0)
        {
          if (messageVerbosity <= 0)
          {
            return;
          }
          if (!Topics.TryGetValue(topic, out var topicVerbosity))
          {
            return;
          }
          if (messageVerbosity > topicVerbosity)
          {
            return;
          }
        }
        else if (Aspects != null)
        {
          if (!Aspects.Contains(aspect))
          {
            return;
          }
        }
      }

      if (prefixType == MessagePrefix.Default)
        prefixType = MessagePrefix.Time;

      var prefix = string.Empty;

      switch (prefixType)
      {
        case MessagePrefix.Num:
          prefix = DebugUtils.GetNextMessageNum().ToString();
          break;

        case MessagePrefix.Time:
          prefix = DateTime.Now.ToString("HH:mm:ss.fff");
          break;

        case MessagePrefix.ShortTime:
          prefix = DateTime.Now.ToString("HH:mm:ss");
          break;
      }

      string output;

      if (topic > 0)
      {
        output = showThread
          ? $"{prefix}|{topic}-{messageVerbosity}|{message} Thread={Thread.CurrentThread.Name}, {Thread.CurrentThread.ManagedThreadId}"
          : $"{prefix}|{topic}-{messageVerbosity}|{message}";
      }
      else
      {
        output = showThread
          ? $"{prefix}|{aspect}|{message} Thread={Thread.CurrentThread.Name}, {Thread.CurrentThread.ManagedThreadId}"
          : $"{prefix}|{aspect}|{message}";
      }

      Console.WriteLine(output);

      if (_streamWriter != null)
      {
        WriteLineToFile(output);
      }

      if (rememberMessage)
      {
        _lastMessage = message;
      }
#endif
    }

    public static void WriteLine()
    {
#if DEBUG || DEBUGCONSOLE
      Console.WriteLine();
#endif
    }


    public static void WriteTopic(int topic, int verbosity, string message)
    {
#if DEBUG || DEBUGCONSOLE
      WriteLineCore(0, message, topic, verbosity);
#endif
    }


    public static void WriteLine(int aspect, string message, MessagePrefix prefixType = MessagePrefix.Default, bool showThread = false)
    {
#if DEBUG || DEBUGCONSOLE
      WriteLineCore(aspect, message, 0, 0, prefixType, showThread);
#endif
    }

    public static void WriteLine(string message, MessagePrefix prefixType = MessagePrefix.Default, bool showThread = false)
    {
#if DEBUG || DEBUGCONSOLE
      WriteLineCore(MessageAspects.Unknown, message, 0, 0, prefixType, showThread);
#endif
    }

    public static void WriteTime(int aspect, string message, bool showThread = false)
    {
#if DEBUG || DEBUGCONSOLE
      WriteLineCore(aspect, message, 0, 0, MessagePrefix.Time, showThread);
#endif
    }

    public static void WriteIfDifferent(int aspect, string message, MessagePrefix prefixType = MessagePrefix.Default, bool showThread = false)
    {
#if DEBUG || DEBUGCONSOLE
      if (string.Equals(_lastMessage, message)) return;
      WriteLineCore(aspect, message, 0, 0, prefixType, showThread, true);
#endif
    }



    public static bool ShowExceptionStackTrace { get; set; } = false;
    //----------------------
    public static void WriteException(string context, Exception exception, bool? showExceptionStackTrace = null)
    {
#if DEBUG || DEBUGCONSOLE
      WriteExceptionCore(MessageAspects.Error, 0, 0, context, exception, showExceptionStackTrace ?? ShowExceptionStackTrace);
#endif
    }

    //----------------------
    public static void WriteException(int topic, int messageVerbosity, string context, Exception exception, bool? showExceptionStackTrace = null)
    {
#if DEBUG || DEBUGCONSOLE
      WriteExceptionCore(MessageAspects.Error, topic, messageVerbosity, context, exception, showExceptionStackTrace ?? ShowExceptionStackTrace);
#endif
    }
    //----------------------
    private static void WriteExceptionCore(int aspect, int topic, int messageVerbosity, string context, Exception exception, bool showExceptionStackTrace)
    {
#if DEBUG || DEBUGCONSOLE

      var list = new List<string>();
      DebugUtils.CollectStringsFromException(list, exception, false);

      foreach (var item in list)
      {
        WriteLineCore(aspect, $"{context}: {item}", topic, messageVerbosity);
      }

      if (!showExceptionStackTrace) return;

      var innerException = exception.InnerException;
      while (innerException?.InnerException != null)
      {
        innerException = innerException.InnerException;
      }

      if (innerException != null)
      {
        WriteLineCore(aspect, innerException.StackTrace, topic, messageVerbosity);
      }
      WriteLineCore(aspect, exception.StackTrace, topic, messageVerbosity);
#endif
    }




    #region Output To File

    public static bool IsOutputToFileOpened => _streamWriter != null;

    private static readonly object Synchronizer = new object();
    private static StreamWriter _streamWriter;

    public static void OutputToFileStart()
    {
      OutputToFileStop();

      if (!UseFile) return;
      if (string.IsNullOrEmpty(LogDirPath)) return;

      var filePath = Path.Combine(LogDirPath, $"log_{DateTime.Now:dd_HH_mm_ss}.txt");
      var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None);
      _streamWriter = new StreamWriter(fileStream, Encoding.Unicode);
    }

    public static void OutputToFileStop()
    {
      if (_streamWriter == null)
      {
        return;
      }
      _streamWriter.Flush();
      _streamWriter.Close();
      _streamWriter.Dispose(); 
      _streamWriter = null;
    }

    private static void WriteLineToFile(string output)
    {
      lock (Synchronizer)
      {
        _streamWriter.WriteLine(output);
      }
    }

    #endregion

  }

}
