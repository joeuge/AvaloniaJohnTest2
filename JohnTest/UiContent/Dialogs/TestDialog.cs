using System.Reactive;
using Caliburn.Micro;
using AppNs.CoreNs;
using AppNs.Interfaces;
using Iface.Utils;
using ReactiveUI;

namespace AppNs.UiContent.Dialogs;

[ViewType(typeof(TestDialogView))]
[TransientInstance]

public class TestDialog : Dialog
{
  public ReactiveCommand<Unit, Unit> TestCommand { get; }

  public TestDialog()
  {
    Width = 300;
    Height = 200;

    TestCommand = ReactiveCommand.CreateFromTask(TestCommandImpl, null, RxApp.MainThreadScheduler);
  }

  private async Task TestCommandImpl()
  {
    var modalService = IoC.Get<IGlobalModalService>();

    string text = null;
    await modalService.TryPromptAsync(result => text = result, content: "Some Text", okButtonContent: null, defaultValue: "John");

    if (text != null)
    {
      await modalService.AlertAsync($"Hello {text}!");
    }

    var dialog = IoC.Get<TestDialog>();

    var dr = await modalService.ShowAsync(dialog, "From TestDialog");

  }

}