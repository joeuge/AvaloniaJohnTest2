using Avalonia.Controls;
using Caliburn.Micro;
using Iface.Utils.Avalonia;

namespace AppNs.Interfaces;

//----------------------------
public interface IAutoDiscover
{
}

//----------------------------
public interface IHaveStates
{
  bool InDisposing { get; }
}

//----------------------------
public interface IHaveDialogResult
{
  public object? DialogResult { get; set; }
}

//----------------------------
public interface IDialogSupport
{
  bool IsNoExitByOk { get; } // true = (Ok -> Apply and Stays in dialog)
  void OnOkBegin(); // например: Update Sources Of Bindings: BindingOperations.GetBindingExpression(TextBox, TextBox.TextProperty)?.UpdateSource()
  bool HasErrors(); // true = прервать выход по Ok // например: Validation.GetHasError(TextBox)
  bool ShowException(Exception exception); // false = возможность отображения не реализована
}


//----------------------------
public interface IDialogChildScreen // Dialogs support
{
  void OnShowBefore();
  void OnShowAfter(bool dialogResult);
  void OnOkBegin();
  bool HasErrors();
  //void Activate();
  Task ActivateAsync(CancellationToken cancellationToken = default);
  //void Deactivate(bool close);
  Task DeactivateAsync(bool close, CancellationToken cancellationToken = default);
  bool TryInitializeFocus();
}

//----------------------------
public interface IFocusSupport
{
  bool TryInitializeFocus();
}

//----------------------------
public interface IDialogProxy
{
  IDialog Dialog { get; }
  string Caption { get; set; }
  ResizeMode ResizeMode { get; set; }
  Func<bool> OnOk { get; } // true = продолжить выход по Ok, false = прервать выход по Ok
  DialogOptions DialogOptions { get; }
}


//----------------------------
public interface IDialogProxyInternal : IDialogProxy
{
  object MemContext { get; set; }
  object DialogAutoModel { get; set; } // страховка от GC
  Task<bool> OverrideDialogTryClose(bool dialogResult);

  // такой вызов проходит мимо IGuardClose in WindowClosing() // выход из WindowClosing() сразу по if(_actuallyClosing)
  Task CloseAsync(bool dialogResult, bool windowIsClosed = false);
}



//----------------------
// Dialog Services
//----------------------

//----------------------------
// See IDialogService extensions in DialogMixins
//----------------------------
public interface IDialogService
{
  Task<bool> ShowAsync(IDialog dialog,
    Action<IDialogProxy>? prepareDialog = null,
    Func<bool>? onOk = null); // false = прервать выход по Ok

  Task<bool> ShowAsync(IDialog dialog,
    object viewContext,
    Action<IDialogProxy>? prepareDialog,
    Func<bool>? onOk = null); // false = прервать выход по Ok

  Task<bool> ShowAsync(IDialog dialog,
    string title);
}


//----------------------------
public interface IDialogServiceInternal : IDialogService
{
  ScreenOwnerType ScreenOwnerType { get; }
  Task OnCloseDialogAsync(IDialogProxyInternal dialogProxy);
}


//----------------------------
public interface IModalService : IDialogService
{
}

//----------------------------
public interface IGlobalModalService : IModalService
{
}


//----------------------------
public interface IModalScreen
{
  bool Visible { get; }
  EventHandlerCollection<bool, object> VisibleChangedEvent { get; }
  void ValidateFocus(bool isDelayed);
  bool TryInitializeFocus();
  void FixLocation(double left, double top);
  void UnFixLocation();
  void ToggleSize();
}


//----------------------------
public interface IWorkspaceModalService : IModalService // todo? rename to ILocalModalService
{
  int DialogCount { get; }
  Task CloseDialogsAsync();
}

//----------------------------
public interface ILocalUnmodalService : IDialogService
{
  int DialogCount { get; }

  void CloseDialogs();

  Task<bool> ShowAsync(bool clearDialogStack, IPage page, object viewContext
    , Action<IDialogProxy> prepareDialog
    , Func<bool> onOk = null); // false = прервать выход по Ok

  // синхронный вызов с делегатом завершения : exitCallback // нужен ли?

  void Show<TPage>(TPage document
    , Action<bool, TPage> exitCallback
    , Action<IDialogProxy> prepareDialog = null
    , Func<bool> onOk = null // false = прервать выход по Ok
  ) where TPage : class, IPage;

  void Show<TPage>(bool clearDialogStack, TPage document
    , Action<bool, TPage> exitCallback
    , Action<IDialogProxy> prepareDialog = null
    , Func<bool> onOk = null // false = прервать выход по Ok
  ) where TPage : class, IPage;
}



//----------------------
// Dialogs
//----------------------

public interface IDialog : IScreen, IHaveStates, IAutoDiscover
{
  IInfrastructure Infrastructure { get; }

  object Owner { get; set; } // задается и используется только в некоторых сценариях
  ScreenOwnerType OwnerType { get; }
  object Parent { get; }

  string ViewContext { get; set; }
  bool UseViewLocatorAssistant { get; set; }

  string DisplayBaseName { get; set; }
  string ShortDisplayName { get; }
  string FullDisplayName { get; }
  string GetFullDisplayName();

  bool DialogResult { get; }
  ResizeMode ResizeMode { get; }
  double Height { get; set; }
  double Width { get; set; }
  void ApplyNormalDialogSize();
  void AfterApplyNormalDialogSize();
  DialogOptions GetDialogOptions();
  ICustomTools TryGetToolsForModalDialog();
  // raise on UI Thread
  EventHandlerCollection ContentLoadedEvent { get; }

  bool IsDisposeOnModalWindowClose { get; set; }
  void OnTestShowDialogBefore();
  void OnTestShowDialogAfter();

  bool TryInitializeFocus();

  void RefreshView(); // Refresh View Bindings


  string Content { get; set; } // todo: rename to Label // например, заголовок для поля ввода
  string OkButtonContent { get; set; }
  string CancelButtonContent { get; set; }
  T SetValidationHandler<T>(Func<T, bool> validationHandler) where T : class, IDialog;

  Task DoOkAsync();
  Task DoCancelAsync();
}

//----------------------------
public interface IDialogInternal : IDialog
{
  new ScreenOwnerType OwnerType { get; set; }
  new object Parent { get; set; }

  new bool DialogResult { get; set; } // установка уже постфактум
}



//----------------------
// Specific Dialogs
//----------------------


public interface IAlertDialog : IDialog
{
  IAlertDialog SetOptions(AlertDialogOptions options);
}

public interface IConfirmDialog : IDialog
{
  bool ExtraToggleValue { get; }
  IConfirmDialog SetOptions(ConfirmDialogOptions options);
}


public interface IConfirmSaveDialog : IDialog
{
  string SaveButtonContent { get; set; }
  string DiscardButtonContent { get; set; }

  bool IsSaveSelected { get; }

  IConfirmSaveDialog SetOptions(ConfirmSaveDialogOptions options);
}


public interface IPromptDialog : IDialog
{
}


public interface IPromptStringDialog : IPromptDialog
{
  string Value { get; }

  IPromptStringDialog SetOptions(PromptStringDialogOptions options);
}


public interface ISelectDialog : IDialogPage
{
  int Value { get; set; }
  Dictionary<int, string> Values { get; set; }

  ISelectDialog SetOptions(SelectDialogOptions options);
}


//----------------------
// Dialog Options // Inputs & Outputs
//----------------------

public class AlertDialogOptions
{
  public string Title { get; set; }
  public string Content { get; set; }
  public string OkButtonContent { get; set; }
}


public class ConfirmDialogOptions
{
  public string Title { get; set; }
  public string Content { get; set; }
  public string OkButtonContent { get; set; }
  public string CancelButtonContent { get; set; }
  public string ExtraToggleContent { get; set; }
  public bool ExtraToggleDefaultValue { get; set; }
}


public class ConfirmSaveDialogOptions
{
  public string Title { get; set; }
  public string Content { get; set; }
  public string SaveButtonContent { get; set; }
  public string DiscardButtonContent { get; set; }
  public string CancelButtonContent { get; set; }
}


public enum ConfirmSaveDialogResult
{
  SaveChanges,
  DiscardChanges,
  Cancel,
}


public abstract class PromptDialogOptions
{
  public string Title { get; set; }
  public string Content { get; set; }
  public string OkButtonContent { get; set; }
  public string CancelButtonContent { get; set; }
  public string ExtraToggleContent { get; set; }
  public bool ExtraToggleDefaultValue { get; set; }
}


public class PromptStringDialogOptions : PromptDialogOptions
{
  public string DefaultValue { get; set; }
}


public class SelectDialogOptions
{
  public string Title { get; set; }
  public string Content { get; set; }
  public string OkButtonContent { get; set; }
  public string CancelButtonContent { get; set; }
  public Dictionary<int, string> Values { get; set; }
  public int? DefaultValue { get; set; }
  public bool IsFilterVisible { get; set; } = false;
}


