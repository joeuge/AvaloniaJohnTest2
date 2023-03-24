using System.ComponentModel;
using System.Windows.Input;
using AppNs.Interfaces;
using Iface.Utils;
using Iface.Utils.Avalonia;

namespace AppNs.CoreNs;

public class DialogAutoModel : LightModel
{
  private readonly Action<PropertyChangedEventArgs> _overrideOnPropertyChanged;
  public DialogGenesis DialogGenesis { get; }
  public readonly IDialogProxyInternal Proxy;
  public IDialog Dialog => Proxy.Dialog;
  public DialogOptions DialogOptions => Proxy.DialogOptions;
  public ICustomTools DialogTools { get; }
  private readonly string _caption1;
  private string _caption2;
  public string Caption => DialogOptions.HeaderType != DialogHeaderType.NormalHeader ? null : _caption1 ?? _caption2;
  public bool IsCaption => !string.IsNullOrEmpty(Caption);
  public bool IsMaximized { get; set; }
  public bool UsesHeader => DialogOptions.UsesHeader;
  public bool UsesOverlay => DialogOptions.IsToolsOver;
  public bool UsesCloseButton => DialogOptions.UsesCloseButton;
  public bool UsesCloseButtonInOverlay => UsesCloseButton && !UsesHeader;
  public bool IsSizeToggleVisible => IsMaximized || DialogOptions.CanFullSize;
  public int OverlayRightMargin => DialogOptions.IsOverlayRightMargin ? 16 : 0;

  public ICustomTools HeaderDialogTools => UsesOverlay ? null : DialogTools;
  public ICustomTools OverlayDialogTools => UsesOverlay ? DialogTools : null;

  private Action<DialogAutoModel> OnContentLoaded { get; }

  private double _windowHeight;
  public double WindowHeight { get => _windowHeight; set => SetPropertyValue(ref _windowHeight, value); }

  private double _windowWidth;
  public double WindowWidth { get => _windowWidth; set => SetPropertyValue(ref _windowWidth, value); }

  //public ResourceDescriptor CloseButtonStyle { get; }
  //public VerticalAlignment ButtonsVerticalAlignment { get; } = VerticalAlignment.Center;


  public ICommand SizeToggleCommand { get; }
  public ICommand CloseWithFalseCommand { get; }



  public DialogAutoModel(DialogGenesis dialogGenesis, IDialogProxyInternal proxy
    , Action<DialogAutoModel> onContentLoaded
    , Action<PropertyChangedEventArgs> overrideOnPropertyChanged = null)
  {
    IsNotifying = false;

    DialogGenesis = dialogGenesis;
    _overrideOnPropertyChanged = overrideOnPropertyChanged;

    var dialog = proxy.Dialog;
    var options = proxy.DialogOptions;
    Proxy = proxy;
    DialogTools = dialog.TryGetToolsForModalDialog();
    OnContentLoaded = onContentLoaded;
    _caption1 = proxy.Caption;
    _caption2 = dialog.GetFullDisplayName();
    Dialog.ApplyNormalDialogSize();
    WindowHeight = Dialog.Height + 40f;
    WindowWidth = Dialog.Width;

    dialog.ContentLoadedEvent.AddHandler(() =>
    {
      _caption2 = Dialog.GetFullDisplayName();
      Dialog.ApplyNormalDialogSize();
      WindowHeight = Dialog.Height + 40f;
      WindowWidth = Dialog.Width;
      Refresh();
      OnContentLoaded?.Invoke(this);
      Dialog.AfterApplyNormalDialogSize();
    });


    CloseWithFalseCommand = new SimpleCommand(async p =>
    {
      await Proxy.CloseAsync(dialogResult: false); // далее смотри OnCloseDialog
    }, null);


    SizeToggleCommand = new SimpleCommand(p =>
    {
      if (!IsMaximized)
      {
        if (!DialogOptions.CanFullSize)
          return;
      }
      ToggleSize();
    }, null);

    IsNotifying = true;
  }


  protected override void OnPropertyChanged(PropertyChangedEventArgs e)
  {
    if (_overrideOnPropertyChanged != null)
    {
      _overrideOnPropertyChanged(e);
      return;
    }
    base.OnPropertyChanged(e);
  }

  public void ToggleSize()
  {
    IsMaximized = !IsMaximized;
    Refresh();
  }


}