using Avalonia.Controls;

namespace AppNs.Interfaces;

public interface IMainWindowViewModel
{
  WindowState WindowState { get; set; }
  double Width { get; set; }
  double Height { get; set; }

  string Title { get; set; }
  //ImageSource Icon { get; set; }

  IShell Shell { get; }

  void DoStep1();
  void DoStep2();
}



