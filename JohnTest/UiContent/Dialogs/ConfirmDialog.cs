using AppNs.CoreNs;
using AppNs.Interfaces;
using Iface.Utils;
using Iface.Utils.Avalonia;

namespace AppNs.UiContent.Dialogs;

[ViewType(typeof(ConfirmDialogView))]
[TransientInstance]
[Contract(typeof(IConfirmDialog))]

public class ConfirmDialog : Dialog, IConfirmDialog
{
  public override DialogOptions GetDialogOptions() => new DialogOptions(DialogHeaderType.NormalHeader)
  {
    CloseWhenEnter = true,
  };


  private string _extraToggleContent;
  private bool _extraToggleValue;
  private bool _useExtraToggle;


  public string ExtraToggleContent
  {
    get => _extraToggleContent;
    set => this.SetPropertyValue(ref _extraToggleContent, value);
  }


  public bool UseExtraToggle
  {
    get => _useExtraToggle;
    set => this.SetPropertyValue(ref _useExtraToggle, value);
  }


  public bool ExtraToggleValue
  {
    get => _extraToggleValue;
    set => this.SetPropertyValue(ref _extraToggleValue, value);
  }


  public IConfirmDialog SetOptions(ConfirmDialogOptions options)
  {
    Content = options.Content ?? "Confirm action";
    OkButtonContent = options.OkButtonContent ?? "OK";
    CancelButtonContent = options.CancelButtonContent ?? "Cancel";

    ExtraToggleContent = options.ExtraToggleContent;
    ExtraToggleValue = options.ExtraToggleDefaultValue;
    UseExtraToggle = !string.IsNullOrEmpty(ExtraToggleContent);

    return this;
  }
}
