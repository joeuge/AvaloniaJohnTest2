using Caliburn.Micro;
using AppNs.Interfaces;
using Iface.Utils;

namespace AppNs.CoreNs;

public abstract class Page : MasterScreen, IPage, IPageInternal, IViewLocatorAssistant
{
  protected Page()
  {
  }
}
