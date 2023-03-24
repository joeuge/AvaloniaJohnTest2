using System.Reflection;

namespace Iface.Utils
{
  public interface IDelegateReference
  {
    Delegate Handler { get; }
  }

  public class DelegateReference : IDelegateReference
  {
    private readonly Delegate _handler;
    private readonly WeakReference _targetReference;
    private readonly MethodInfo _method;
    private readonly Type _delegateType;

    /// <summary>
    /// Initializes a new instance of <see cref="DelegateReference"/>.
    /// </summary>
    /// <param name="handler">The original <see cref="Delegate"/> to create a reference for.</param>
    /// <param name="isWeak">If <see langword="true" /> the class will create a weak reference to the delegate, allowing it to be garbage collected. Otherwise it will keep a strong reference to the target.</param>
    /// <exception cref="ArgumentNullException">If the passed <paramref name="handler"/> is not assignable to <see cref="Delegate"/>.</exception>
    public DelegateReference(Delegate handler, bool isWeak)
    {
      if (handler == null)
        throw new ArgumentNullException(nameof(handler));

      if (!isWeak)
      {
        _handler = handler;
      }
      else
      {
        _targetReference = new WeakReference(handler.Target);
        _method = handler.Method;
        _delegateType = handler.GetType();
      }
    }

    /// <summary>
    /// Gets the <see cref="Delegate" /> (the target) referenced by the current <see cref="DelegateReference"/> object.
    /// </summary>
    /// <value><see langword="null"/> if the object referenced by the current <see cref="DelegateReference"/> object has been garbage collected; otherwise, a reference to the <see cref="Delegate"/> referenced by the current <see cref="DelegateReference"/> object.</value>
    public Delegate Handler => _handler ?? CreateDelegate();

    private Delegate CreateDelegate()
    {
      if (_method.IsStatic)
      {
        return Delegate.CreateDelegate(_delegateType, null, _method);
      }
      
      var target = _targetReference.Target;
      return target == null ? null : Delegate.CreateDelegate(_delegateType, target, _method);
    }
  }
}
