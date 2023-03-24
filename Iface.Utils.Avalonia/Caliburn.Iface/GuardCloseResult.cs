using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Iface.Utils;

namespace Caliburn.Micro;

public class GuardCloseResult : IResult
{
  public IGuardClose Guard { get; }
  public event EventHandler<ResultCompletionEventArgs> Completed;

  public GuardCloseResult(IGuardClose guard)
  {
    Guard = guard;
  }

  public async void Execute(CoroutineExecutionContext context)
  {
    try
    {
      var canClose = await Guard.CanCloseAsync();

      Completed?.Invoke(this, new ResultCompletionEventArgs { WasCancelled = !canClose });
    }
    catch (Exception exception)
    {
      if (exception is IfaceCancelException || exception?.InnerException is IfaceCancelException)
      {
        Completed?.Invoke(this, new ResultCompletionEventArgs { WasCancelled = true });
        return;
      }

      Completed?.Invoke(this, new ResultCompletionEventArgs { Error = exception });
    }
  }
}
