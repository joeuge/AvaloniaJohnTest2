using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Threading.Tasks;
using AppNs.Interfaces;
using Avalonia;
using Avalonia.Markup.Xaml;
using AppNs;
using AppNs.Interfaces;
using Iface.Utils;
using Iface.Utils.Avalonia;
using ReactiveUI;

namespace AppNs
{
  public partial class App : Application
  {
    public override void Initialize()
    {
      // UI Exceptions
      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException; // Avalonia????

      // Exceptions from another thread
      TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

      // Exceptions from Reactive UI
      RxApp.DefaultExceptionHandler = Observer.Create<Exception>(
        onNext: ex =>
        {
          if (Debugger.IsAttached)
          {
            Debugger.Break();
          }

          RxApp.MainThreadScheduler.Schedule(() =>
          {
#pragma warning disable CA1065 // Avoid exceptions in constructors -- In scheduler.
            throw new UnhandledErrorException(
              "An object implementing IHandleObservableErrors (often a ReactiveCommand or ObservableAsPropertyHelper) has errored, thereby breaking its observable pipeline. To prevent this, ensure the pipeline does not error, or Subscribe to the ThrownExceptions property of the object in question to handle the erroneous case.",
              ex);
#pragma warning restore CA1065
          });
        },

        onError: ex =>
        {
          if (Debugger.IsAttached)
          {
            Debugger.Break();
          }

          RxApp.MainThreadScheduler.Schedule(() =>
          {
            throw ex;
          });
        },

        onCompleted: () =>
        {
        }

      );


      //----------------------
      AvaloniaXamlLoader.Load(this);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
    }

    private static void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
    }


    public override void OnFrameworkInitializationCompleted()
    {
      base.OnFrameworkInitializationCompleted(); // выбросить?

      // for work of [assembly: XmlnsDefinition()] // see https://github.com/AvaloniaUI/Avalonia/issues/7200 // работает ли ?
      //GC.KeepAlive(typeof(IInfrastructure));
      //GC.KeepAlive(typeof(GlobalUtils));
      //GC.KeepAlive(typeof(AvaloniaTreeUtility));
    }

  }
}
