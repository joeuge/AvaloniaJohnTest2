using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Iface.Utils;

[Serializable]
public class IfaceCancelException : ExternalException
{
  public IfaceCancelException()
  {
  }
  public IfaceCancelException(string message) : base(message)
  {
  }
  public IfaceCancelException(string message, Exception innerException) : base(message, innerException)
  {
  }
}
