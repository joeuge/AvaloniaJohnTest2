using System.Diagnostics;
using System.Windows.Input;

namespace Iface.Utils;

public class SimpleCommand : ICommand
{
  #region Static Service

  public static SimpleCommand Create<T>(Action<T> execute, Predicate<T> canExecute = null)
  {
    Predicate<object> nativeCanExecute = null;
    if (canExecute != null)
    {
      nativeCanExecute = p =>
      {
        if (p is T typedP)
        {
          return canExecute(typedP);
        }
        return false;
      };
    }

    var command = new SimpleCommand(p =>
    {
      if (p is T typedP)
      {
        execute(typedP);
      }
    }, nativeCanExecute);
    return command;
  }

  #endregion

  private readonly Action<object?> _execute;
  private readonly Predicate<object?>? _canExecute;


  public SimpleCommand(Action<object?> execute)
    : this(execute, null)
  {
  }

  public SimpleCommand(Action<object?> execute, Predicate<object?>? canExecute)
  {
    if (execute == null)
      throw new ArgumentNullException("execute");

    _execute = execute;
    _canExecute = canExecute;
  }


  /*
  public event EventHandler? CanExecuteChanged;

  private bool? _prevCanExecute;

  public bool CanExecute(object? parameter)
  {
    var newValue = _canExecute == null || _canExecute(parameter);
    if (CanExecuteChanged is not null && (!_prevCanExecute.HasValue || newValue != _prevCanExecute))
    {
      //CanExecuteChanged(this, EventArgs.Empty);
      _prevCanExecute = newValue;
    }

    return newValue;
  }
  */

  public event EventHandler? CanExecuteChanged
  {
    add { }
    remove { }
  }

  [DebuggerStepThrough]
  public bool CanExecute(object? parameter)
  {
    return _canExecute == null || _canExecute(parameter);
  }

  public void Execute(object? parameter)
  {
    _execute(parameter);
  }

}
