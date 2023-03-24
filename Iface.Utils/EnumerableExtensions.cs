namespace Iface.Utils
{
  public static class EnumerableExtensions
  {
    /*
    public static void Apply<T>(this IEnumerable<T> enumerable, Action<T> action) // todo: оставить либо Apply<T>, либо ForEach<T>
    {
      foreach (var item in enumerable)
      {
        action(item);
      }
    }
    */

    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
      if (source == null) throw new ArgumentNullException(); 
      if (action == null) throw new ArgumentNullException();

      foreach (var item in source)
      {
        action(item);
      }

      return source; // зачем? сам и отвечаю: Flow style rule it
    }

    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
      if (source == null) throw new ArgumentNullException(); 
      if (action == null) throw new ArgumentNullException();

      int index = 0;
      foreach (var item in source)
      {
        action(item, index++);
      }

      return source; // зачем?
    }


    public static bool  IsNullOrEmpty<T>(this IEnumerable<T> source)
    {
      return source == null || !source.Any();
    }
    
    
    public static bool ContainsAll<T>(this IEnumerable<T> a, IEnumerable<T> b)
    {
      if (a == null) throw new ArgumentNullException(); 
      if (b == null) throw new ArgumentNullException();
      
      return !b.Except(a).Any();
    }
    
    
    public static bool ContainsAllIgnoreCase(this IEnumerable<string> a, IEnumerable<string> b)
    {
      if (a == null) throw new ArgumentNullException(); 
      if (b == null) throw new ArgumentNullException();
      
      return !b.Except(a, StringComparer.OrdinalIgnoreCase).Any();
    }


    public static bool ContainsIgnoreCase(this IEnumerable<string> source, string value)
    {
      return source.Contains(value, StringComparer.OrdinalIgnoreCase);
    }
  }
}