using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Iface.Utils.Avalonia;

namespace AppNs.Interfaces;

//----------------------------
public interface IWindowService
{
  Task<Window> PrepareWindowAsync(object rootModel, IDictionary<string, object>? settings = null);
  Task<Window> ShowWindowAsync(object rootModel, bool setMainWindowAsOwner = true, IDictionary<string, object>? settings = null);
  Task<bool?> ShowUnmodalAndGetResult(object rootModel, bool setMainWindowAsOwner = true, IDictionary<string, object>? settings = null);
  Task<bool?> ShowModal(object rootModel, bool setMainWindowAsOwner = true, IDictionary<string, object>? settings = null);
}

