using System.ComponentModel;
using System.Runtime.CompilerServices;
using Caliburn.Micro;

namespace Iface.Utils.Avalonia
{

  /* example:
  private int? _point;
  public int? Point { get => _point; set => SetPropertyValue(ref _point, value); }
  */

  public static class CaliburnExtensions
  {
    public static void SetPropertyValue<TValue>(this PropertyChangedBase model, ref TValue field, TValue value, [CallerMemberName] string? propertyName = null)
    {
      if (propertyName == null)
      {
        Console.WriteLine("My CaliburnExtensions Problem");
      }

      /*
      if (propertyName == "InitialCenter")
      {
        Console.WriteLine("zzzzzzzzzzzzzzzzzzzzzzz");
      }
      */

      if (Equals(field, value))
      {
        return;
      }

      field = value;
      model.NotifyOfPropertyChange(propertyName);
    }
  }

  public abstract class ModelBase : PropertyChangedBase
  {
    [Browsable(false)]
    public override bool IsNotifying
    {
      get => base.IsNotifying;
      set => base.IsNotifying = value;
    }

    protected void SetPropertyValue<TValue>(ref TValue field, TValue value, [CallerMemberName] string? propertyName = null)
    {
      if (ReferenceEquals(field, value))
        return;
      if (Equals(field, value))
        return;
      field = value;
      NotifyOfPropertyChange(propertyName);
    }

    protected void SetPropertyValue<TValue>(ref TValue field, TValue value, Action<TValue, TValue> onChanged, [CallerMemberName] string? propertyName = null)
    {
      if (ReferenceEquals(field, value))
        return;
      if (Equals(field, value))
        return;
      field = value;
      onChanged?.Invoke(field, value);
      NotifyOfPropertyChange(propertyName);
    }

    protected void SetPropertyReference<TValue>(ref TValue field, TValue value, [CallerMemberName] string propertyName = null)
    {
      if (ReferenceEquals(field, value))
        return;
      field = value;
      NotifyOfPropertyChange(propertyName);
    }

    protected void SetPropertyReference<TValue>(ref TValue field, TValue value, Action<TValue, TValue> onChanged, [CallerMemberName] string propertyName = null)
    {
      if (ReferenceEquals(field, value))
        return;
      field = value;
      onChanged?.Invoke(field, value);
      NotifyOfPropertyChange(propertyName);
    }

  }
}
