using Caliburn.Micro;
using AppNs.Interfaces;

namespace AppNs.CoreNs;


// Внимание: класс управляет свойствами передаваемого документа IDocument: OwnerType, Parent, DialogResult
public class DialogProxy<TDialog> : IDialogProxyInternal, IChild where TDialog : class, IDialog
{
  private readonly IDialogServiceInternal _dialogService;
  private readonly IDialogInternal _dialog;
  private readonly Action<bool, TDialog> _exitCallback;
  private readonly TaskCompletionSource<bool> _taskCompletionSource;

  object IChild.Parent
  {
    get => _dialogService;
    set { }
  }

  public TDialog Dialog => (TDialog) _dialog;
  IDialog IDialogProxy.Dialog => _dialog;
  public Func<bool> OnOk { get; } // false = прервать выход по Ok

  public string Caption { get; set; }
  public ResizeMode ResizeMode { get; set; }
  public object MemContext { get; set; }
  public object DialogAutoModel { get; set; }
  public DialogOptions DialogOptions { get; }
  

  public DialogProxy(IDialogServiceInternal dialogService, TDialog dialog, Action<bool, TDialog> exitCallback, Func<bool> onOk)
  {
    _dialogService = dialogService;
    _exitCallback = exitCallback;
    _dialog = (IDialogInternal) dialog;
    OnOk = onOk;
    ResizeMode = dialog?.ResizeMode ?? CoreDefaults.DialogResizeMode;
    DialogOptions = dialog?.GetDialogOptions() ?? DialogOptions.Default;

    _dialog.OwnerType = dialogService.ScreenOwnerType;

    if (_dialog is IChild dlg)
    {
      dlg.Parent = this; // по цепочке IChild документ может найти : ModelUtility.TryFindService<IDialogService>(doc);
    }
  }


  public DialogProxy(IDialogServiceInternal dialogService, TDialog dialog, out Task<bool> task, Func<bool> onOk) : this(
    dialogService, dialog, null, onOk)
  {
    _taskCompletionSource = new TaskCompletionSource<bool>();
    task = _taskCompletionSource.Task;
  }


  // Вызывается из Page.TryClose()
  // return IsHandled:
  // true  = завершить исполнение Page.TryClose()
  // false = продолжить исполнение Page.TryClose()
  public async Task<bool> OverrideDialogTryClose(bool dialogResult)
  {
    if (_dialogService.ScreenOwnerType == ScreenOwnerType.Window)
    {
      // модальные окна идут сюда
      return false; // Page.TryClose() должна продолжить свое исполнение
    }

    // ModalScreen, Side Screen Dialogs go here // реализация IModalService в CanvasWs шла сюда
    await CloseAsync(dialogResult);
    return true; // Page.TryClose() должна завершить свое исполнение
  }


  public async Task CloseAsync(bool dialogResult, bool windowIsClosed = false) // windowIsClosed added 18 apr 2022
  {
    if (dialogResult && OnOk != null && !windowIsClosed)
    {
      try
      {
        if (!OnOk())
        {
          return;
        }
      }
      catch (Exception exception)
      {
        //MessageBox.Show(exception.Message); // todo
        return;
      }
    }

    if (_dialogService.ScreenOwnerType == ScreenOwnerType.Window && !windowIsClosed)
    {
      // added 18 apr 2022
      // это должно привести к закрытию окна, выхода из модального диалога и вызову этой же функции с windowIsClosed = true
      // see WindowModalService: dialogProxy.CloseAsync(dialogResult, windowIsClosed: true);
      await Dialog.DeactivateAsync(close: true); 
      return;
    }

    try
    {
      _dialog.DialogResult = dialogResult;
      _exitCallback?.Invoke(dialogResult, Dialog);
      _taskCompletionSource?.SetResult(dialogResult);
    }
    finally
    {
      _dialog.OwnerType = ScreenOwnerType.None;
      await _dialogService.OnCloseDialogAsync(this);
    }
  }
}