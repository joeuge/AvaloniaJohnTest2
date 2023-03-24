using AppNs.CoreNs;
using AppNs.Interfaces;
using Iface.Utils;
using Iface.Utils.Avalonia;

namespace AppNs.UiContent.Dialogs;

[ViewType(typeof(PromptStringDialogView))]
[TransientInstance]
[Contract(typeof(IPromptStringDialog))]


public class PromptStringDialog : Dialog, IPromptStringDialog
{
  private string _value;

  public string Value
  {
    get => _value;
    set => this.SetPropertyValue(ref _value, value);
  }

  public override DialogOptions GetDialogOptions() => new DialogOptions(DialogHeaderType.NormalHeader)
  {
    CloseWhenEnter = true,
  };

  public IPromptStringDialog SetOptions(PromptStringDialogOptions options)
  {
    Content = options.Content;
    OkButtonContent = options.OkButtonContent ?? "OK";
    CancelButtonContent = options.CancelButtonContent ?? "Cancel";
    Value = options.DefaultValue;

    return this;
  }
}
