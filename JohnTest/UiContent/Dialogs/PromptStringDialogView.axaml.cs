using AppNs.Interfaces;
using Avalonia.Controls;
using Avalonia.Media;

namespace AppNs.UiContent.Dialogs
{
  public partial class PromptStringDialogView : UserControl
  {
    public PromptStringDialogView()
    {
      InitializeComponent();
    }

    /*
    protected override void OnRender(DrawingContext drawingContext)
    {
      base.OnRender(drawingContext);

      Value.Focus();
      Value.CaretIndex = Value.Text.Length;
    }
    */

    public override void Render(DrawingContext context)
    {
      base.Render(context);

      if (CoreDefaults.ProblemWithFocus)
        return;

      Value.Focus();
      if (!string.IsNullOrEmpty(Value.Text))
      {
        Value.CaretIndex = Value.Text.Length;
      }
    }
  }
}
