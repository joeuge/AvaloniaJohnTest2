using Caliburn.Micro;

namespace AppNs.Interfaces;

//==============================================
public interface IDialogPage : IPage
{
  //double Width { get; set; }
  //double Height { get; set; }

  string Content { get; set; } // todo: rename to Label // например, заголовок для поля ввода
  string OkButtonContent { get; set; }
  string CancelButtonContent { get; set; }
  T SetValidationHandler<T>(Func<T, bool> validationHandler) where T : class, IDialogPage;

  void DoOk();
  void DoCancel();
}


