namespace Iface.Utils
{
  //==================================
  public enum ActionExecuteType
  {
    CurrentThread,
    //UIThreadInvoke,
    UIThreadBegin,
    UIThread,
  }

  //==================================
  public static class BooleanBoxes
  {
    public static object TrueBox { get; } = true;
    public static object FalseBox { get; } = false;

    public static object Box(bool value)
    {
      return value ? TrueBox : FalseBox;
    }
  }

}
