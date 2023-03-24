using AppNs.CoreNs;
using AppNs.Interfaces;
using Iface.Utils;

namespace AppNs.UiContent.Dialogs;

[ViewType(typeof(AlertDialogView))]
[TransientInstance]
[Contract(typeof(IAlertDialog))]

public class AlertDialog : Dialog, IAlertDialog
{
  public override DialogOptions GetDialogOptions() => new DialogOptions(DialogHeaderType.NormalHeader)
  {
    CloseWhenEnter = true,
  };

  public IAlertDialog SetOptions(AlertDialogOptions options)
  {
    Content         = options.Content         ?? "";
    OkButtonContent = options.OkButtonContent ?? "Close";

    return this;
  }

  public void DoTest2()
  {
  }

}