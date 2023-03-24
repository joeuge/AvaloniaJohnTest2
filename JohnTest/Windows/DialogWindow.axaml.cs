using Avalonia;
using Avalonia.Controls;

namespace AppNs.Windows
{
  public partial class DialogWindow : Window
  {
    public DialogWindow()
    {
      InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
      base.OnPropertyChanged(change);
    }
  }
}
