using Caliburn.Micro;
using Iface.Utils;

namespace AppNs.Interfaces
{
  public static class DialogMixins
  {
    // DialogResult можно узнать у самого документа: 
    // var doc = await ShowAsync()
    // doc?.DialogResult
    //------------------------------------------

    public static async Task<TDialog?> ShowAsync<TDialog>(this IDialogService dialogService, Action<IDialogProxy, TDialog>? prepareDialog)
      where TDialog : class, IDialog
    {
      var dialog = IoC.Get<TDialog>();
      if (dialog == null) throw new NullReferenceException();

      Action<IDialogProxy>? action = null;
      if (prepareDialog != null)
      {
        action = dialogProxy => prepareDialog(dialogProxy, dialog);
      }

      var dialogResult = await dialogService.ShowAsync(dialog, action);
      return dialogResult ? dialog : null;
    }


    //------------------------------------------

    public static async Task AlertAsync(this IDialogService dialogService, AlertDialogOptions options)
    {
      var dialog = IoC.Get<IAlertDialog>()?.SetOptions(options);
      if (dialog == null) throw new NullReferenceException();

      await dialogService.ShowAsync(dialog, options.Title);
    }


    public static async Task AlertAsync(this IDialogService dialogService,
      string content = null,
      string okButtonContent = null)
    {
      await dialogService.AlertAsync(new AlertDialogOptions
      {
        Content = content,
        OkButtonContent = okButtonContent,
      });
    }


    //------------------------------------------

    public static async Task<bool> ConfirmAsync(this IDialogService dialogService, ConfirmDialogOptions options)
    {
      var dialog = IoC.Get<IConfirmDialog>()?.SetOptions(options);
      if (dialog == null) throw new NullReferenceException();

      return await dialogService.ShowAsync(dialog, options.Title);
    }

    public static async Task<bool> ConfirmAsync(this IDialogService dialogService,
      string content = null,
      string okButtonContent = null)
    {
      return await ConfirmAsync(dialogService, new ConfirmDialogOptions
      {
        Content = content,
        OkButtonContent = okButtonContent,
      });
    }

    //------------------------------------------

    public static async Task<bool> ConfirmWithExtraToggleAsync(this IDialogService dialogService,
      Action<bool> extraToggleResultCallback,
      ConfirmDialogOptions options)
    {
      var dialog = IoC.Get<IConfirmDialog>()?.SetOptions(options);
      if (dialog == null) throw new NullReferenceException();

      var isConfirmed = await dialogService.ShowAsync(dialog, options.Title);

      extraToggleResultCallback(dialog.ExtraToggleValue);
      return isConfirmed;
    }


    public static async Task<bool> ConfirmWithExtraToggleAsync(this IDialogService dialogService,
      Action<bool> extraToggleResultCallback,
      string content = null,
      string extraToggleContent = null,
      bool extraToggleDefaultValue = false,
      string okButtonContent = null)
    {
      return await ConfirmWithExtraToggleAsync(dialogService,
        extraToggleResultCallback,
        new ConfirmDialogOptions
        {
          Content = content,
          ExtraToggleContent = extraToggleContent,
          ExtraToggleDefaultValue = extraToggleDefaultValue,
          OkButtonContent = okButtonContent,
        });
    }

    //------------------------------------------

    public static async Task<ConfirmSaveDialogResult> ConfirmSaveAsync(this IDialogService dialogService, ConfirmSaveDialogOptions options)
    {
      var dialog = IoC.Get<IConfirmSaveDialog>()?.SetOptions(options);
      if (dialog == null) throw new NullReferenceException();

      if (!await dialogService.ShowAsync(dialog, options.Title))
      {
        return ConfirmSaveDialogResult.Cancel;
      }
      return dialog.IsSaveSelected ? ConfirmSaveDialogResult.SaveChanges : ConfirmSaveDialogResult.DiscardChanges;
    }


    //------------------------------------------

    public static async Task<bool> TryPromptAsync(this IDialogService dialogService,
      Action<string> resultCallback,
      PromptStringDialogOptions options,
      Func<IPromptStringDialog, bool> validationHandler = null)
    {
      var dialog = IoC.Get<IPromptStringDialog>()?.SetOptions(options).SetValidationHandler(validationHandler);
      if (dialog == null) throw new NullReferenceException();

      if (!await dialogService.ShowAsync(dialog, options.Title))
      {
        return false;
      }
      resultCallback(dialog.Value);
      return true;
    }


    public static async Task<bool> TryPromptAsync(this IDialogService dialogService,
      Action<string> resultCallback,
      string content = null,
      string okButtonContent = null,
      string defaultValue = null,
      Func<IPromptStringDialog, bool> validationHandler = null)
    {
      return await TryPromptAsync(dialogService, resultCallback, new PromptStringDialogOptions
      {
        Content = content,
        OkButtonContent = okButtonContent,
        DefaultValue = defaultValue,
      }, validationHandler);
    }


  }
}
