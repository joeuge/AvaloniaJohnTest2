using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using AppNs.CoreNs;
using Avalonia.Controls;
using Avalonia.Threading;

using Iface.Utils;

namespace AppNs
{
  internal class Program
  {
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
      if (DebugUtils.IsRu)
      {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      }

      //BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);

      //-------------
      var builder = BuildAvaloniaApp();

      var lifetime = new ClassicDesktopStyleApplicationLifetime
      {
        Args = args,
        ShutdownMode = ShutdownMode.OnExplicitShutdown
      };

      //-------------
      builder.SetupWithLifetime(lifetime);
      /* ->
        app.RegisterServices();
        app.Initialize();
        app.OnFrameworkInitializationCompleted();
      */
      
      var app = (App)builder.Instance;

      //-------------
      var infr = new Infrastructure(app, lifetime);
      infr.RunApplicationBeforeMainLoop();
      Dispatcher.UIThread.Post(() =>
      {
        infr.RunApplicationAfterMainLoop();
      });
      //-------------

      try
      {
        //-------------
        var ret = lifetime.Start(args);
        //-------------
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }

      if (true)
      {
        infr.Dispose();
      }
      //-------------
      Process.GetCurrentProcess().Kill(); // TODO это очень не красиво, но иначе остается живым поток Telerik.Reporting
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();


  }
}
