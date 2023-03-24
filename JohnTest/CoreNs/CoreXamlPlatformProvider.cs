using Avalonia.Controls;
using Caliburn.Micro;
using AppNs.Interfaces;

namespace AppNs.CoreNs;

public class CoreXamlPlatformProvider : XamlPlatformProvider
{
  public override Func<CancellationToken, Task> GetViewCloseAction(object viewModel, ICollection<object> views, bool? dialogResult)
  {
    foreach (var contextualView in views)
    {
      if (contextualView is Window window) // john added
      {
        return ct =>
        {
          if (viewModel is IHaveDialogResult haveDialogResult)
          {
            haveDialogResult.DialogResult = dialogResult;
          }
          window.Close(dialogResult);
          return Task.FromResult(true); // why not Task.CompletedTask!
        };
      }

      var viewType = contextualView.GetType();
      var closeMethod = viewType.GetMethod("Close", new Type[0]);
      if (closeMethod != null)
        return ct => {
          var isClosed = false;
          if (dialogResult != null)
          {
            var resultProperty = contextualView.GetType().GetProperty("DialogResult");
            if (resultProperty != null)
            {
              resultProperty.SetValue(contextualView, dialogResult, null);
              isClosed = true;
            }
          }

          if (!isClosed)
          {
            closeMethod.Invoke(contextualView, null);
          }
          return Task.FromResult(true); // why not Task.CompletedTask!
        };

      var isOpenProperty = viewType.GetProperty("IsOpen");
      if (isOpenProperty != null)
      {
        return ct =>
        {
          isOpenProperty.SetValue(contextualView, false, null);

          return Task.FromResult(true); // why not Task.CompletedTask!
        };
      }
    }

    return ct =>
    {
      LogManager.GetLog(typeof(Screen)).Info("TryClose requires a parent IConductor or a view with a Close method or IsOpen property.");
      return Task.FromResult(true);
    };
  }
}
