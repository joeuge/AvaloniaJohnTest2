using Avalonia;
using Avalonia.Controls;
using AppNs.Interfaces;
using Avalonia.Media.Imaging;

namespace AppNs.UiBlocks.ExtraWindows
{
  public interface IExtraWindow
  {
    Control Control { get; } // itself
    IWorkspaceHolder WorkspaceHolder { get; }

    WindowState WindowState { get; set; }
    double Top { get; set; }
    double Left { get; set; }

    double Width { get; set; }
    double MinWidth { get; set; }
    double MaxWidth { get; set; }

    double Height { get; set; }
    double MinHeight { get; set; }
    double MaxHeight { get; set; }

    object Header { get; set; }

    //void Show();
    void Close();
    void BringToFront();
    void SetShowInTaskbar(bool value);
  }
}
